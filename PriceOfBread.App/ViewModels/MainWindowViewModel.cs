using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using PriceOfBread.SimCore;

namespace PriceOfBread.App.ViewModels;

// 画面遷移のホスト：タイトル → 国選択 → ゲーム。CurrentPage を差し替えるだけ（ViewLocator が View を解決）。
public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty] private ViewModelBase _currentPage;

    public MainWindowViewModel()
    {
        _currentPage = new TitleViewModel(GoToNationSelect, LoadQuick);
    }

    /// <summary>観戦モード（画面は通常と同じで、選んだ国の判断も全て AI が行う）。国の選択の切り替えと `--autoplay` が立てる。新規ゲームの組み立てだけが読む
    /// （ロードしたゲームの観戦かどうかは世界から導く＝<see cref="SimFactory.IsSpectating"/>・セーブには旗を足さない）。</summary>
    public bool Spectate { get; set; } = Program.Autoplay;

    private void GoToNationSelect() => CurrentPage = new NationSelectViewModel((i, spectate) => { Spectate = spectate; StartGame(i); }, Spectate);

    /// <summary>国を選んでからゲーム画面へ。間に「価格を設定中」を挟む（作者指示2026-08-11）。
    /// ★2026-08-15：開始前の助走（数秒）を**裏スレッド**へ移した＝UIスレッドが空くので読み込み画面の点が動く
    /// （旧＝Background優先度でUIスレッド上に同期構築＝描画が凍り、点を動かせなかった）。
    /// ⚠<see cref="GameViewModel"/> の静的フィールド初期化子は Avalonia のブラシを持つ＝裏スレッドであの型に触れると
    /// 型初期化がUIスレッド外で走り得る。シムの組み立てと助走は <see cref="BuildWarmedSim"/>（SimCore の型のみ）で行い、
    /// GameViewModel の生成は UIスレッドの <see cref="FinishStart"/> まで遅らせる。
    /// ★async void にしない＝裏スレッドの例外は必ず UIスレッドへ運んで再送出
    /// （Dispatcher.UnhandledException が crash.log へ記録する既存経路・Program.cs）＝投げっぱなしのTaskで握り潰さない。</summary>
    private void StartGame(int playerIndex)
    {
        bool spectate = Spectate;   // 裏スレッドへ渡す前に UI スレッドで確定
        var loading = new LoadingViewModel("価格を設定中");
        CurrentPage = loading;
        _ = Task.Run(() =>
        {
            try
            {
                var sim = BuildWarmedSim(playerIndex, spectate);
                Dispatcher.UIThread.Post(() => FinishStart(loading, sim, playerIndex));
            }
            catch (Exception ex)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    loading.Stop();
                    if (ReferenceEquals(CurrentPage, loading)) GoToTitle();   // 読み込み画面のまま固めない＝操作を返してから記録
                    ExceptionDispatchInfo.Capture(ex).Throw();
                });
            }
        });
    }

    /// <summary>助走済みのシムからゲーム画面へ（UIスレッド専用＝GameViewModel はタイマーを起動する）。
    /// ★読み込み画面が既に別画面へ差し替わっていたら捨てる（uishot のフォールバック等に追い越された
    /// 遅着の世界で現画面を潰さない）。</summary>
    private void FinishStart(LoadingViewModel loading, MultiNationSim sim, int playerIndex)
    {
        loading.Stop();
        if (!ReferenceEquals(CurrentPage, loading)) return;
        CurrentPage = Wire(new GameViewModel(sim, playerIndex, GoToTitle, LoadQuick));
    }

    /// <summary>新規ゲームの世界を組んで開始前の助走を回す（SimCore の純粋計算のみ＝どのスレッドでも安全）。
    /// ⚠決定論：手順と順序は旧 GameViewModel コンストラクタと厳密に同一に保つこと＝シード1789UL・
    /// エージェントの生成順（プレイヤー国だけ NullAgent）・専売0は助走の**前**・助走は StartWarmup の1本。
    /// <para>地図中心UI（2026-06-29 再構成）：世界全体を描くため world.json の全国家を投入（先頭7＝従来の選択可能国と同順＝playerIndex 互換）。</para>
    /// <para>開始前の助走（作者裁定2026-08-10）＝**自己対戦バッチと同じ1本**（<see cref="MultiNationSim.StartWarmup"/>）を通す。
    /// 長さは Coefficients.StartWarmupQuarters の1本で決まる。助走は暦を進めない（World.WarmupFrozen）＝エンジンの開始日も表示と同じ 1783/01/01。</para>
    /// </summary>
    // ★A2'（2026-09-05・動作改善）：助走済みの世界の**キャッシュ**。起動の約31秒は StartWarmupQuarters=20 の助走（シム定義＝変えない）。
    //   2回目以降は SaveGame（savetest でビット恒等）から読むだけ＝数秒。鍵＝SimCore の MVID（ビルドごとに変わる＝コード/埋め込みデータの変更で自動失効）
    //   ＋playerIndex（プレイヤー国だけ NullAgent＝国ごとに別）＋助走長。書けない/読めないときは従来どおり組む（致命的にしない）。
    private static string WarmCachePath(int playerIndex, bool spectate)
    {
        var mvid = typeof(MultiNationSim).Assembly.ManifestModule.ModuleVersionId.ToString("N");
        return System.IO.Path.Combine(GameViewModel.SaveDir, SimFactory.WarmCacheFileName(mvid, playerIndex, new Coefficients().StartWarmupQuarters, spectate, Program.Seed));   // 観戦は鍵が別（世界が別物）・種も鍵
    }
    private const string WarmMagic = "POBWARM2";

    private static MultiNationSim BuildWarmedSim(int playerIndex, bool spectate)
    {
        string cachePath = WarmCachePath(playerIndex, spectate);
        try
        {
            if (System.IO.File.Exists(cachePath))
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                using var fs = System.IO.File.OpenRead(cachePath);
                using var br = new System.IO.BinaryReader(fs, System.Text.Encoding.UTF8, leaveOpen: true);
                if (br.ReadString() == WarmMagic)
                {
                    var cached = SaveGame.Load(fs);
                    BootLog.Write($"⏱ 助走キャッシュから読み込み {sw.ElapsedMilliseconds}ms（{System.IO.Path.GetFileName(cachePath)}）");
                    return cached;
                }
            }
        }
        catch (Exception ex) { BootLog.Write($"助走キャッシュ読み込み失敗（従来どおり組む）: {ex.Message}"); }
        var built = BuildWarmedSimFresh(playerIndex, spectate);
        try
        {
            System.IO.Directory.CreateDirectory(GameViewModel.SaveDir);
            using var fs = System.IO.File.Create(cachePath);
            using var bw = new System.IO.BinaryWriter(fs, System.Text.Encoding.UTF8, leaveOpen: true);
            bw.Write(WarmMagic);
            bw.Flush();
            SaveGame.Save(built, fs);
            BootLog.Write($"助走キャッシュを書いた（{System.IO.Path.GetFileName(cachePath)}）");
        }
        catch (Exception ex) { BootLog.Write($"助走キャッシュ書き込み失敗: {ex.Message}"); }
        return built;
    }

    private static MultiNationSim BuildWarmedSimFresh(int playerIndex, bool spectate)
    {
        // プレイヤー国は NullAgent（介入なし）＝UIが政策を握る。他国は効用AI。観戦は全国が効用AIで IsPlayer なし（SimFactory.Make）。
        var sim = SimFactory.Make(WorldData.Nations(), playerIndex, spectate, Program.Seed);
        // ★専売は材を1つも選ばない状態で開始（作者裁定2026-09-20＝率という状態が無い）＝World.StateMonopolyMargins が全て0の既定そのもの。書き込みは不要。
        sim.StartWarmup();
        return sim;
    }

    // UI自動撮影モード（#134）：国選択画面を飛ばして直接開始。★ここは同期のまま（UIスレッドで組む）＝撮影側は
    // 「盤面が出ている」前提で次の操作へ進むため、裏スレッド化しない（uishot 専用の入口・実プレイヤーは通らない）。
    public void AutoStart(int playerIndex)
    {
        var sim = BuildWarmedSim(playerIndex, Spectate);
        CurrentPage = Wire(new GameViewModel(sim, playerIndex, GoToTitle, LoadQuick));
    }
    // 同モード：国選択画面そのものを撮るための入口（AutoStart はここを飛ばすので、この画面だけ無人で確認できなかった）。
    public void ShowNationSelect() => GoToNationSelect();
    private void GoToTitle() => CurrentPage = new TitleViewModel(GoToNationSelect, LoadQuick);   // ゲーム内メニュー「タイトルに戻る」

    // クイックロード（タイトル「続きから」・ゲーム内F9）：セーブから新しいゲームVMを作って差し替える。失敗時は現状維持。
    private void LoadQuick() => LoadSlot(GameViewModel.NewestSlot() >= 0 && CurrentPage is TitleViewModel ? GameViewModel.NewestSlot() : 0);
    // ★スロットのロード（2026-09-06）：タイトルの「続きから」は最新のスロット、ゲーム内 F9 はクイック、Escメニューは指定スロット。
    private void LoadSlot(int slot)
    {
        var vm = GameViewModel.TryLoad(slot, GoToTitle, LoadQuick);
        if (vm is not null) { (CurrentPage as GameViewModel)?.Shutdown(); CurrentPage = Wire(vm); }
    }
    private GameViewModel Wire(GameViewModel vm) { vm.OnLoadSlot = LoadSlot; vm.OnSwitchPlayer = SwitchPlayer; vm.OnFollowPlayer = FollowPlayer; return vm; }
    // ★作者裁定 f（2026-09-28）：エンジンが操作国を移した後（反乱側の国が再併合された）＝画面だけを付け直す。操作者（NullAgent）と外交の IsPlayer はエンジンが移し済み
    //   ＝SwitchPlayer と違い旧国の操作者に触らない（旧国はもう Nations に居ない／添字がずれている）。
    private void FollowPlayer(int newIndex, string notice)
    {
        if (CurrentPage is not GameViewModel old) return;
        var sim = old.Sim;
        if (newIndex < 0 || newIndex >= sim.Nations.Count) return;
        old.Shutdown();
        var vm = Wire(new GameViewModel(sim, newIndex, GoToTitle, LoadQuick));
        if (notice.Length > 0) vm.ShowNotice(notice);
        CurrentPage = vm;
    }
    // ★内戦の陣営選択（P8・2026-09-06）：同じ世界のまま操作国を反乱側の新国家へ。旧国は効用AIへ・新国は操作者（NullAgent）へ・外交の IsPlayer も移す。
    private void SwitchPlayer(int newIndex)
    {
        if (CurrentPage is not GameViewModel old) return;
        var sim = old.Sim;
        if (newIndex < 0 || newIndex >= sim.Nations.Count) return;
        if (old.Spectating) { old.Shutdown(); CurrentPage = Wire(new GameViewModel(sim, newIndex, GoToTitle, LoadQuick)); return; }   // 観戦は操作者も IsPlayer も触らず画面だけ付け替える
        int oldIndex = old.PlayerIndex;
        sim.Nations[oldIndex].SetAgent(new UtilityAgent());
        sim.Nations[newIndex].SetAgent(new NullAgent());
        foreach (var p in sim.Diplomacy.Powers) p.IsPlayer = false;
        sim.Nations[newIndex].World.SelfPower!.IsPlayer = true;
        old.Shutdown();
        CurrentPage = Wire(new GameViewModel(sim, newIndex, GoToTitle, LoadQuick));
    }
}
