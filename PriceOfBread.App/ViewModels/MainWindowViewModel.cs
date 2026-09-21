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

    private void GoToNationSelect() => CurrentPage = new NationSelectViewModel(StartGame);

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
        var loading = new LoadingViewModel("価格を設定中");
        CurrentPage = loading;
        _ = Task.Run(() =>
        {
            try
            {
                var (sim, prices) = BuildWarmedSim(playerIndex);
                Dispatcher.UIThread.Post(() => FinishStart(loading, sim, playerIndex, prices));
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
    private void FinishStart(LoadingViewModel loading, MultiNationSim sim, int playerIndex, List<double>[]? warmupPrices)
    {
        loading.Stop();
        if (!ReferenceEquals(CurrentPage, loading)) return;
        CurrentPage = Wire(new GameViewModel(sim, playerIndex, warmupPrices, GoToTitle, LoadQuick));
    }

    /// <summary>新規ゲームの世界を組んで開始前の助走を回す（SimCore の純粋計算のみ＝どのスレッドでも安全）。
    /// ⚠決定論：手順と順序は旧 GameViewModel コンストラクタと厳密に同一に保つこと＝シード1789UL・
    /// エージェントの生成順（プレイヤー国だけ NullAgent）・専売0は助走の**前**・助走は StartWarmup の1本。
    /// <para>地図中心UI（2026-06-29 再構成）：世界全体を描くため world.json の全国家を投入（先頭7＝従来の選択可能国と同順＝playerIndex 互換）。</para>
    /// <para>開始前の助走（作者裁定2026-08-10）＝**自己対戦バッチと同じ1本**（<see cref="MultiNationSim.StartWarmup"/>）を通す。
    /// 長さは Coefficients.StartWarmupQuarters の1本で決まる。助走は暦を進めない（World.WarmupFrozen）＝エンジンの開始日も表示と同じ 1783/01/01。</para>
    /// <para>★助走中の価格標本は素の List&lt;double&gt; に積む（Avalonia の型に触れない）＝GameViewModel が引き取って
    /// 価格グラフの先頭になる（作者フィードバック2026-08-10 項目1＝開幕の1四半期目から線が引ける）。刻みは本編と同じ四半期
    /// ＝x軸は一様。観測だけ＝力学は不変。式と40本上限は GameViewModel.Production.cs の SampleWorldPrices の1本（呼ぶだけ）。</para></summary>
    // ★A2'（2026-09-05・動作改善）：助走済みの世界の**キャッシュ**。起動の約31秒は StartWarmupQuarters=20 の助走（シム定義＝変えない）。
    //   2回目以降は SaveGame（savetest でビット恒等）から読むだけ＝数秒。鍵＝SimCore の MVID（ビルドごとに変わる＝コード/埋め込みデータの変更で自動失効）
    //   ＋playerIndex（プレイヤー国だけ NullAgent＝国ごとに別）＋助走長。書けない/読めないときは従来どおり組む（致命的にしない）。
    private static string WarmCachePath(int playerIndex)
    {
        var mvid = typeof(MultiNationSim).Assembly.ManifestModule.ModuleVersionId.ToString("N");
        return System.IO.Path.Combine(GameViewModel.SaveDir, $"warm_{mvid}_{playerIndex}_{new Coefficients().StartWarmupQuarters}.pob");
    }
    private const string WarmMagic = "POBWARM1";

    private static (MultiNationSim Sim, List<double>[]? PriceHist) BuildWarmedSim(int playerIndex)
    {
        string cachePath = WarmCachePath(playerIndex);
        try
        {
            if (System.IO.File.Exists(cachePath))
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                using var fs = System.IO.File.OpenRead(cachePath);
                using var br = new System.IO.BinaryReader(fs, System.Text.Encoding.UTF8, leaveOpen: true);
                if (br.ReadString() == WarmMagic)
                {
                    int nLists = br.ReadInt32();
                    List<double>[]? hist = nLists > 0 ? new List<double>[nLists] : null;
                    for (int i = 0; i < nLists; i++)
                    {
                        int n = br.ReadInt32();
                        var l = new List<double>(n);
                        for (int k = 0; k < n; k++) l.Add(br.ReadDouble());
                        hist![i] = l;
                    }
                    var cached = SaveGame.Load(fs);
                    BootLog.Write($"⏱ 助走キャッシュから読み込み {sw.ElapsedMilliseconds}ms（{System.IO.Path.GetFileName(cachePath)}）");
                    return (cached, hist);
                }
            }
        }
        catch (Exception ex) { BootLog.Write($"助走キャッシュ読み込み失敗（従来どおり組む）: {ex.Message}"); }
        var built = BuildWarmedSimFresh(playerIndex);
        try
        {
            System.IO.Directory.CreateDirectory(GameViewModel.SaveDir);
            using var fs = System.IO.File.Create(cachePath);
            using var bw = new System.IO.BinaryWriter(fs, System.Text.Encoding.UTF8, leaveOpen: true);
            bw.Write(WarmMagic);
            var hist = built.PriceHist;
            bw.Write(hist?.Length ?? 0);
            if (hist is not null) foreach (var l in hist) { bw.Write(l.Count); foreach (var v in l) bw.Write(v); }
            bw.Flush();
            SaveGame.Save(built.Sim, fs);
            BootLog.Write($"助走キャッシュを書いた（{System.IO.Path.GetFileName(cachePath)}）");
        }
        catch (Exception ex) { BootLog.Write($"助走キャッシュ書き込み失敗: {ex.Message}"); }
        return built;
    }

    private static (MultiNationSim Sim, List<double>[]? PriceHist) BuildWarmedSimFresh(int playerIndex)
    {
        NationConfig[] cfgs = WorldData.Nations();
        var playerCfg = cfgs[playerIndex];
        // プレイヤー国は NullAgent（介入なし）＝UIが政策を握る。他国は効用AI。
        var sim = new MultiNationSim(cfgs, 1789UL,
            cfg => ReferenceEquals(cfg, playerCfg) ? new NullAgent() : new UtilityAgent());
        sim.Diplomacy.Powers[playerIndex].IsPlayer = true;   // 外交AIも自国を介入しない
        var player = sim.Nations[playerIndex].World;
        // ★専売は材を1つも選ばない状態で開始（作者裁定2026-09-20＝率という状態が無い）＝World.StateMonopolyMargins が全て0の既定そのもの。書き込みは不要。
        List<double>[]? hist = null;
        sim.StartWarmup(() => hist = GameViewModel.SampleWorldPrices(sim, player, hist));   // 本編の四半期標本と同じ1本（2026-09-11 #19a）
        return (sim, hist);
    }

    // UI自動撮影モード（#134）：国選択画面を飛ばして直接開始。★ここは同期のまま（UIスレッドで組む）＝撮影側は
    // 「盤面が出ている」前提で次の操作へ進むため、裏スレッド化しない（uishot 専用の入口・実プレイヤーは通らない）。
    public void AutoStart(int playerIndex)
    {
        var (sim, prices) = BuildWarmedSim(playerIndex);
        CurrentPage = Wire(new GameViewModel(sim, playerIndex, prices, GoToTitle, LoadQuick));
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
    private GameViewModel Wire(GameViewModel vm) { vm.OnLoadSlot = LoadSlot; vm.OnSwitchPlayer = SwitchPlayer; return vm; }
    // ★内戦の陣営選択（P8・2026-09-06）：同じ世界のまま操作国を反乱側の新国家へ。旧国は効用AIへ・新国は操作者（NullAgent）へ・外交の IsPlayer も移す。
    private void SwitchPlayer(int newIndex)
    {
        if (CurrentPage is not GameViewModel old) return;
        var sim = old.Sim;
        if (newIndex < 0 || newIndex >= sim.Nations.Count) return;
        int oldIndex = old.PlayerIndex;
        sim.Nations[oldIndex].SetAgent(new UtilityAgent());
        sim.Nations[newIndex].SetAgent(new NullAgent());
        foreach (var p in sim.Diplomacy.Powers) p.IsPlayer = false;
        sim.Nations[newIndex].World.SelfPower!.IsPlayer = true;
        old.Shutdown();
        CurrentPage = Wire(new GameViewModel(sim, newIndex, null, GoToTitle, LoadQuick));
    }
}
