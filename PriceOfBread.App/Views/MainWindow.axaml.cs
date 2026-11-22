using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using PriceOfBread.App.Layout;
using PriceOfBread.App.ViewModels;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PriceOfBread.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        // UI自動撮影モード（#134・2026-07-25）：`--uishot <dir>` 起動でタイトル→自動開始→目標年まで進行→全タブを連番PNG保存→終了。
        // 開発ビルドの実地UI確認を無人化する恒久機能（外部の画面操作ツールに依存しない）。
        if (Program.UiShotDir is not null) Opened += (_, _) => RunUiShot(Program.UiShotDir);
        if (Program.UiShotDir is not null && !Program.UiShotVisible) HideForShots();
        if (Program.PerfOut is not null) Opened += (_, _) => RunPerf(Program.PerfOut);
        if (Program.BakeMaskOut is not null) Opened += (_, _) => { try { MapPane.BakeLandmask(Program.BakeMaskOut, Program.BakeMaskForRoads); } catch (Exception ex) { BootLog.Write("bakemask 失敗: " + ex.Message); } Environment.Exit(0); };
    }

    private void Capture(string dir, string name)
    {
        try
        {
            var ps = new PixelSize(Math.Max(1, (int)Bounds.Width), Math.Max(1, (int)Bounds.Height));
            using var bmp = new RenderTargetBitmap(ps, new Vector(96, 96));
            bmp.Render(this);
            bmp.Save(Path.Combine(dir, name));
        }
        catch (Exception ex) { BootLog.Write($"uishot capture失敗 {name}: {ex.Message}"); }
    }

    // ★A1 無人計測（2026-09-05・グラフィック/動作改善プログラム）：uishot と同じ経路で盤面まで進み、数字だけを書く。
    //   起動秒＝プロセス開始→盤面（GameViewModel）到達。四半期ms＝速度3で40四半期を進めた実時間÷40（UIの更新込み）。
    //   フレームms＝全景/中心移動の直後に RequestAnimationFrame を12回連ねた間隔の平均（描画の重さ）。
    private async void RunPerf(string outPath)
    {
        var lines = new System.Collections.Generic.List<string>();
        try
        {
            var procStart = System.Diagnostics.Process.GetCurrentProcess().StartTime;
            await Task.Delay(600);
            if (DataContext is not MainWindowViewModel mwvm) { Environment.Exit(2); return; }
            mwvm.ShowNationSelect();
            await Task.Delay(300);
            if (mwvm.CurrentPage is ViewModels.NationSelectViewModel nsvm)
                foreach (var c in nsvm.Recommended) { c.ChooseCommand.Execute(null); break; }
            // 盤面が出るまで待つ（助走キャッシュ2026-09-05で数秒に短縮＝AutoStart で世界を二重に組まない・Exit の前に必ずファイルを書く）
            for (int i = 0; i < 360 && mwvm.CurrentPage is not GameViewModel; i++) await Task.Delay(250);
            await Task.Delay(1500);
            if (mwvm.CurrentPage is not GameViewModel gvm) { lines.Add("error	盤面に到達しない"); try { System.IO.File.WriteAllLines(outPath, lines); } catch { } Environment.Exit(3); return; }
            gvm.Layout.Open(Layout.Slot.Map);   // 計測は地図が中央にある状態（地図は中央に出て初めて見える）
            await Task.Delay(800);
            lines.Add($"startup_s\t{(DateTime.Now - procStart).TotalSeconds:F1}");
            // 四半期ms（速度3・40四半期）
            static int MonthsOf(string d) { var p = d.Split('/'); return int.TryParse(p[0], out var y) && p.Length > 1 && int.TryParse(p[1], out var mo) ? y * 12 + mo : 0; }
            int m0 = MonthsOf(gvm.DateText);
            var sw = System.Diagnostics.Stopwatch.StartNew();
            gvm.SetSpeedCommand.Execute("3");
            if (!gvm.Playing) gvm.TogglePauseCommand.Execute(null);
            for (int i = 0; i < 2400 && MonthsOf(gvm.DateText) - m0 < Program.PerfQuarters * 3; i++) {
                await Task.Delay(100);
                // ポップアップ（自動一時停止）を無人計測では閉じて再開する（計測の穴・2026-09-05）：閉じずに Playing だけ戻すと次tickで再停止する
                gvm.TensionPopupOpen = false; gvm.WarPopupOpen = false; gvm.WarEndPopupOpen = false; gvm.FactionCrisisOpen = false; gvm.LawOpenPopupOpen = false;
                gvm.ElectionPopupOpen = false; gvm.CallToArmsOpen = false; gvm.ConfirmOpen = false;
                if (!gvm.Playing) gvm.TogglePauseCommand.Execute(null);
            }
            if (gvm.Playing) gvm.TogglePauseCommand.Execute(null);
            int dq = (MonthsOf(gvm.DateText) - m0) / 3;
            lines.Add($"quarter_ms\t{(dq > 0 ? sw.Elapsed.TotalMilliseconds / dq : double.NaN):F0}\tquarters\t{dq}");
            lines.Add($"clock	ticks	{gvm.PerfTicks}	elapsed_raw_s	{gvm.PerfElapsedRaw:F1}	elapsed_used_s	{gvm.PerfElapsedUsed:F1}	days	{gvm.PerfDays:F0}	speed	{gvm.PerfSpeedSeen}	wall_s	{sw.Elapsed.TotalSeconds:F1}");
            if (dq > 0) lines.Add($"per_quarter_split_ms	stepday	{gvm.PerfStepDayMs / dq:F0}	daily_refresh	{gvm.PerfDailyMs / dq:F0}	month_refresh	{gvm.PerfMonthRefreshMs / dq:F0}	quarter_refresh	{gvm.PerfQuarterRefreshMs / dq:F0}");
            // フレームms（全景・中心移動）
            var gv = this.GetVisualDescendants().OfType<MapPane>().FirstOrDefault();
            async Task<double> FrameMs(Action act)
            {
                act();
                var times = new System.Collections.Generic.List<double>();
                var tcs = new TaskCompletionSource<bool>();
                var swf = System.Diagnostics.Stopwatch.StartNew();
                int n = 0;
                void Tick(TimeSpan _)
                {
                    times.Add(swf.Elapsed.TotalMilliseconds); swf.Restart();
                    if (++n >= 12) tcs.TrySetResult(true); else RequestAnimationFrame(Tick);
                }
                RequestAnimationFrame(Tick);
                await Task.WhenAny(tcs.Task, Task.Delay(3000));
                if (times.Count <= 1) return double.NaN;
                times.RemoveAt(0);
                return times.Average();
            }
            if (gv is not null)
            {
                lines.Add($"frame_ms_world\t{await FrameMs(() => gv.ZoomToWorld()):F1}");
                lines.Add($"frame_ms_center\t{await FrameMs(() => gvm.CenterNationCommand.Execute("フランス")):F1}");
                // ★パン試験（2026-09-06・作者指示「マップスクロールがかなり重い」）：欧州の州名帯でドラッグ相当（毎フレーム −8px）を60フレーム
                async Task<double> PanMs(int frames)
                {
                    var times = new System.Collections.Generic.List<double>();
                    var tcs = new TaskCompletionSource<bool>();
                    var swp = System.Diagnostics.Stopwatch.StartNew();
                    int n = 0;
                    void Tick(TimeSpan _)
                    {
                        gv!.PanBy(-8, 0);
                        times.Add(swp.Elapsed.TotalMilliseconds); swp.Restart();
                        if (++n >= frames) tcs.TrySetResult(true); else RequestAnimationFrame(Tick);
                    }
                    RequestAnimationFrame(Tick);
                    await Task.WhenAny(tcs.Task, Task.Delay(15000));
                    if (times.Count <= 2) return double.NaN;
                    times.RemoveAt(0);
                    return times.Average();
                }
                gv.SetZoomFactor(4.0); gvm.CenterNationCommand.Execute("フランス"); await Task.Delay(400);
                lines.Add($"frame_ms_pan_zoom4\t{await PanMs(60):F1}");
                gv.ZoomToWorld(); await Task.Delay(400);
                lines.Add($"frame_ms_pan_world\t{await PanMs(60):F1}");
                lines.Add($"arms\tss={Program.LandSS}\tinterp={Program.LandInterp}");
                // 計測の床（2026-09-06）：地図層を全部隠してパン＝ハーネス/描画ループ自体の1フレーム時間（層の寄与を引き算する基準）
                if (gv.FindControl<Avalonia.Controls.Grid>("MapRoot") is { } mapRoot)
                {
                    mapRoot.IsVisible = false; await Task.Delay(200);
                    lines.Add($"frame_ms_pan_nomap	{await PanMs(60):F1}");
                    mapRoot.IsVisible = true; await Task.Delay(200);
                }
                // 層の寄与（パン・全景）：辺の層／政治塗りを1つずつ外して測る
                var keepEdges = gvm.MapEdges; gvm.MapEdges = new();
                lines.Add($"frame_ms_pan_world_no_edges	{await PanMs(60):F1}");
                gvm.MapEdges = keepEdges;
                var keepFill2 = gvm.PoliticalFill; gvm.PoliticalFill = null;
                lines.Add($"frame_ms_pan_world_no_fill	{await PanMs(60):F1}");
                gvm.PoliticalFill = keepFill2;
                // 層ごとの寄与（2026-09-05）：ラベルを空にして全景を測る→政治の塗りも外して測る＝どの層がフレーム時間を食うか（★F7：州○・障壁○は廃止済み）
                var keepLabels = gvm.MapLabels; var keepFill = gvm.PoliticalFill;
                gvm.MapLabels = new();
                lines.Add($"frame_ms_world_no_labels	{await FrameMs(() => gv.ZoomToWorld()):F1}");
                gvm.PoliticalFill = null;
                lines.Add($"frame_ms_world_no_fill	{await FrameMs(() => gv.ZoomToWorld()):F1}");
                gvm.MapLabels = keepLabels; gvm.PoliticalFill = keepFill;
            }
            foreach (var t in BootLog.Recent()) lines.Add("bootlog\t" + t);
        }
        catch (Exception ex) { lines.Add("error\t" + ex.Message); }
        try { System.IO.File.WriteAllLines(outPath, lines); } catch { }
        Environment.Exit(0);
    }

    /// <summary>外側が縦に流れていないかの判定（設計 §7-3）を BootLog に書く。各枠について：①ページの中身のうち、欄の中の流れ（ScrollViewer）の外にあるものが枠の外へはみ出していないか
    /// ②欄の中の流れ（ScrollViewer）の数と、そのうち実際に流れている（中身が枠より長い）数。縦にはみ出したものは名前を出して「流れている」とする。横に切れるものは別に名前を出す（旧のタブの固定幅の列＝段3 で直す物）。外側＝枠・左右の列・窓のどれにも外側の流れが無いこと。</summary>
    private void LogOuterFlow(GameView gv, string tag)
    {
        var parts = new System.Collections.Generic.List<string>();
        bool ok = true;
        foreach (var (pane, slot, page, frame) in UiShotWindows.MountedAll(gv))
        {
            if (page is MapPane) continue;   // 地図は箱の外へ描きが出ても箱で切られる絵＝流れの判定の対象外（中の絵が箱より大きいのは仕様）
            double hh = frame.Bounds.Height, ww = frame.Bounds.Width;
            var over = new System.Collections.Generic.List<string>(); var wide = new System.Collections.Generic.List<string>();
            foreach (var c in page.GetVisualDescendants().OfType<Control>())
            {
                if (c.Bounds.Width <= 0 || c.Bounds.Height <= 0 || !c.IsEffectivelyVisible) continue;   // 畳まれた欄の中身（親が隠れている）は対象外・畳んだ欄の中身は測り直されない＝前に配った大きさが残る
                if (c.GetVisualAncestors().TakeWhile(a => !ReferenceEquals(a, page)).Any(a => a is ScrollViewer or MapPane)) continue;   // 欄の中の流れの中身と、地図の中の絵（箱で切られる）は対象外
                var br = c.TranslatePoint(new Point(c.Bounds.Width, c.Bounds.Height), frame);
                if (br is { } qa && qa.Y > hh + 1 && frame is FloatingWindow) { }   // 窓は全体を 1 本で流す（作者の指示 2026-11-12）＝窓の下へ伸びた中身は窓の流れの中
                else if (br is { } qb && qb.Y > hh + 1) { over.Add($"縦に{c.GetType().Name}{(string.IsNullOrEmpty(c.Name) ? "" : "#" + c.Name)}(+{qb.Y - hh:F0})"); if (over.Count >= 3) break; }
                else if (br is { } q2 && q2.X > ww + 1 && wide.Count < 2) wide.Add($"{c.GetType().Name}{(string.IsNullOrEmpty(c.Name) ? "" : "#" + c.Name)}(+{q2.X - ww:F0})");
            }
            var scrolls = page.GetVisualDescendants().OfType<ScrollViewer>().ToList();
            int flowing = scrolls.Count(v => v.Extent.Height > v.Viewport.Height + 1);
            if (over.Count > 0) ok = false;
            parts.Add($"{SlotInfo.Name(slot)}{(pane == 0 ? "（背景）" : "（窓）")} {ww:F0}×{hh:F0} 欄の中で流れる{flowing}/{scrolls.Count}" + (over.Count > 0 ? " 縦にはみ出し " + string.Join(" ", over) : "") + (wide.Count > 0 ? " 横にはみ出し（切れる）" + string.Join(" ", wide) : ""));
        }
        // 窓全体：外側の ScrollViewer が無い（枠・列・本体の直下に ScrollViewer が居ない）＝中身の高さが窓を超えない
        double winH = Bounds.Height;
        double gvH = gv.DesiredSize.Height;
        bool outerFlowing = !(ok && gvH <= winH + 1);
        _gateOuter++; if (outerFlowing) _gateOuterFlowing++;
        TallyAlignment(gv, tag);
        TallyBroken(gv, tag);   // 同じ撮影で、載っている表の見出しと値の列の左端が揃っているかも数える
        BootLog.Write($"uishot: 外側の流れ[{tag}] {(!outerFlowing ? "流れていない" : "流れている")}　窓{Bounds.Width:F0}×{winH:F0}・本体{gvH:F0}　{string.Join(" / ", parts)}");
    }

    /// <summary>撮影の窓を画面の外に 1920×1080 で置く（手前に出さない・タスクバーに出さない・枠なし＝中身が 1920×1080）。</summary>
    private void HideForShots()
    {
        ShowActivated = false;
        ShowInTaskbar = false;
        WindowDecorations = Avalonia.Controls.WindowDecorations.None;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.Manual;
        Width = 1920; Height = 1080;
        Position = OffScreen;
        Opened += (_, _) => Position = OffScreen;   // 開く時に画面の内へ戻されるので、開いた後にもう一度外へ
    }

    private static readonly PixelPoint OffScreen = new(-20000, -20000);

    /// <summary>撮影の窓を 1920 幅へ（フルスクリーン→最大化の順に試す）。画面の状態で窓の状態が効かない回があったので、大きさを BootLog に残す。
    /// 画面に出さない撮影（既定）は <see cref="HideForShots"/> で既に 1920 幅＝窓の状態は変えない（フルスクリーンにすると画面に出る）。</summary>
    private async Task EnsureBigWindow()
    {
        for (int i = 0; i < 3 && Bounds.Width < 1800 && Program.UiShotVisible; i++)
        {
            WindowState = i == 1 ? WindowState.Maximized : WindowState.FullScreen;
            await Task.Delay(900);
        }
        BootLog.Write($"uishot: 窓 {Bounds.Width:F0}×{Bounds.Height:F0}（状態 {WindowState}）");
    }

    private async void RunUiShot(string rootDir)
    {
        try
        {
            string dir = rootDir;   // 複数の年を渡した時は時点ごとに <rootDir>/<年> へ付け替える（年が 1 つなら rootDir 直下＝従来どおり）
            Directory.CreateDirectory(dir);
            int crashBefore = CrashLogUiExceptions();   // crash.log の UI の例外の数を前後で数える（総括）
            await Task.Delay(1200);   // タイトル描画待ち
            Capture(dir, "01_タイトル.png");
            BootLog.Write("uishot: 01撮影済み→AutoStart");
            if (DataContext is not MainWindowViewModel mwvm) { BootLog.Write("uishot: DataContext不一致でExit(2)"); Environment.Exit(2); return; }
            // 国家選択画面（2026-08-11）＝AutoStart はここを飛ばすので、この画面だけ無人で確認できなかった。
            // おすすめのカードと、その下に常時並ぶ「その他の国」を1枚に撮る。
            mwvm.ShowNationSelect();
            await Task.Delay(800);
            Capture(dir, "01b_国家選択.png");
            if (mwvm.CurrentPage is ViewModels.NationSelectViewModel nsvm)
            {
                foreach (var c in nsvm.Recommended) BootLog.Write($"uishot: おすすめ {c.Title}{c.Difficulty} {c.Description}");   // 難易度は見出しから分離（2026-08-15）＝ログでは連結して従来の形を保つ
                BootLog.Write($"uishot: その他の国 {nsvm.Others.Count}カ国/先頭 {(nsvm.Others.Count > 0 ? nsvm.Others[0].Title : "―")}");
                // ★プレイヤーが実際に通る経路そのものを1回踏む＝カードを押す → 読み込み画面 → 助走 → 盤面。
                //   AutoStart はこの経路を飛ばすので、ここを踏まないと「価格を設定中...」が出るかを無人で確認できない。
                // ★2026-09-07 修正：ここは先頭のおすすめカードを押していたので `--uishot <dir> <国index>` の国指定が
                //   無視されていた（どの国を渡しても同じ盤面＝無人検証の穴）。index の一致するカードを押す。
                var pick = nsvm.Recommended.FirstOrDefault(c => c.Index == Program.UiShotNation)
                           ?? nsvm.Others.FirstOrDefault(c => c.Index == Program.UiShotNation)
                           ?? nsvm.Recommended.FirstOrDefault();
                nsvm.Spectate = Program.Autoplay;   // --autoplay ＝観戦で開始（国の選択の切り替えと同じ口）
                pick?.ChooseCommand.Execute(null);
                BootLog.Write($"uishot: 選んだ国 index={pick?.Index} {pick?.Name}");
                // ★読み込み画面の初回フレームを撮る＝レイアウトを明示的に走らせてから撮る。
                //   待つだけでは measure/arrange 前の空の木を撮ってしまう（真っ白なPNGになる）。
                //   助走は裏スレッド（2026-08-15）＝UIスレッドは空いており、実機ではこの絵が即座に出て点が300msごとに増える。
                UpdateLayout();
                Capture(dir, "01d_読み込み.png");
                BootLog.Write($"uishot: カード押下直後の画面={mwvm.CurrentPage?.GetType().Name}");
            }
            // ★カードから開始した場合は**盤面が出るまで待つ**だけにする（2026-08-11）。
            //   ここで AutoStart を呼ぶと、裏スレッドで走り出した助走（2026-08-15）と合わせて**世界を二重に組む**ことになり、
            //   実測で撮影が02の手前で終了した（捨てられる側のVMがタイマーを持ったまま残る・Background優先度時代の教訓）。
            //   2026-08-15：待ちは実時間のポーリングになった（旧＝同期構築がUIスレッドを塞ぎ、実質「構築完了まで」だった）
            //   ＝遅い環境で待ち切れず二重構築へ落ちないよう上限を60秒に延長（盤面が出れば即抜ける）。
            for (int i = 0; i < 240 && mwvm.CurrentPage is LoadingViewModel; i++) await Task.Delay(250);
            if (mwvm.CurrentPage is not GameViewModel) mwvm.AutoStart(Program.UiShotNation);   // 国選択を通らなかった場合の従来経路
            BootLog.Write("uishot: AutoStart完了");
            await Task.Delay(2500);   // ゲーム画面の構築＋初回Refresh
            if (mwvm.CurrentPage is not GameViewModel gvm) { BootLog.Write("uishot: 遷移失敗でExit(3)"); Capture(dir, "02_遷移失敗.png"); Environment.Exit(3); return; }
            await EnsureBigWindow();   // 窓が小さいまま（フルスクリーンが効かない）撮ると各ページが狭い枠の絵になる
            Capture(dir, "02_開始直後.png");
            BootLog.Write("uishot: 02撮影済み");
            gvm.SetSpeedCommand.Execute("3");
            gvm.Playing = false;   // 撮影は実時間で進めない（下の進行の輪が日を数えて回す）
            // ★多時点の撮影（観戦モードの撮影の土台）：年を複数渡すと、各年に着くたびに一時停止して全ページを <rootDir>/<年>/ へ撮り、続きを回す。
            //   映す国が途中で消えたら（再併合）付け直した国で撮り続け、<rootDir>/撮影の記録.txt に年と国の名を書く。年が 1 つなら従来どおり（rootDir 直下）。
            var years = Program.UiShotYears;
            bool spectate = gvm.Spectating;
            int k = 0;
            PointRecord(rootDir, $"撮影開始　{(spectate ? "観戦" : "通常")}　国={ShownNationName(gvm)}　年={string.Join(",", years)}　種={Program.Seed}");
            SeedPastWars(gvm);
            for (int yi = 0; yi < years.Length; yi++)
            {
            int targetYear = years[yi];
            dir = years.Length == 1 ? rootDir : Path.Combine(rootDir, targetYear.ToString());
            Directory.CreateDirectory(dir);
            // ★撮影の進行は実時間に合わせない（2026-11-12）：画面の速度 3 を 500 ミリ秒ごとに覗くと 1950 年まで 1 時間かかり、
            //   窓への答えが効く日も実時間で決まって走るたびに別の世界になった。遊ぶ時と同じ日ごとの道（GameViewModel.StepDaysForShot＝OnTick と共通の本体）を
            //   1 四半期ずつ回し、窓が開いた日で止めて台本の答えを返す。目標の年の 1 月 1 日でちょうど止まる。
            int targetDay = (targetYear - SimCore.Calendar.StartYear) * SimCore.Calendar.DaysPerYear;
            int stuck = 0;
            for (int guard = 0; guard < 200000; guard++)
            {
                FollowShownNation(rootDir, mwvm, ref gvm);   // 映す国が付け直された（再併合・添字のずれ）なら新しい画面の VM を握り直す
                gvm.Playing = false;                          // 時間は撮影が進める（タイマーの実時間では進めない）
                // 観戦ではモーダルを押さずに閉じるだけ（押すとシムへ書く＝AI の判断でなくなる）。開いたこと自体が「プレイヤー専用の経路が観戦で開いた」証拠なので記録する。
                if (spectate) { NotePastWars(rootDir, gvm); CloseModalsForSpectate(rootDir, gvm); }
                // ★2026-09-07：ポップアップは世界を止め、閉じても時間は再開しない（作者裁定2026-08-24）。台本の答えは中身を変えない選択肢。
                if (gvm.ElectionPopupOpen) gvm.ElectionAbstainCommand.Execute(null);
                if (gvm.TensionPopupOpen) gvm.TensionIgnoreCommand.Execute(null);
                if (gvm.WarPopupOpen) gvm.WarDeclareGoCommand.Execute(null);   // 最後通牒＝受けて立つ
                if (gvm.WarEndPopupOpen) gvm.WarEndAckCommand.Execute(null);
                if (gvm.CallToArmsOpen) gvm.CallToArmsRefuseCommand.Execute(null);
                if (gvm.FactionCrisisOpen) gvm.FactionCloseCommand.Execute(null);   // 要求は法の経路＝通知は閉じるだけ
                if (gvm.LawOpenPopupOpen) gvm.LawOpenCloseCommand.Execute(null);    // 新たな法の窓＝閉じるだけ
                if (gvm.SideChoiceOpen) gvm.SideChoiceStayCommand.Execute(null);
                if (gvm.SimDayNumber >= targetDay) break;
                if (gvm.AnyPopupOpen)
                {
                    if (++stuck % 20 == 1) BootLog.Write($"uishot: 進行が止まっている {gvm.DateText}（緊張{gvm.TensionPopupOpen} 開戦{gvm.WarPopupOpen} 終戦{gvm.WarEndPopupOpen} 危機{gvm.FactionCrisisOpen} 法{gvm.LawOpenPopupOpen} 総選挙{gvm.ElectionPopupOpen} 参戦{gvm.CallToArmsOpen} 陣営{gvm.SideChoiceOpen} 確認{gvm.ConfirmOpen}「{gvm.ConfirmTitle}」 メニュー{gvm.MenuOpen} 終局{gvm.EndingOpen}）");
                    if (stuck > 200) break;
                    if (gvm.EndingOpen) { PointRecord(rootDir, $"{gvm.DateText}　終局の窓が開いた＝閉じて続ける"); gvm.CloseEndingCommand.Execute(null); }
                    await Task.Delay(20);
                    continue;
                }
                stuck = 0;
                gvm.StepDaysForShot(Math.Min(SimCore.Calendar.DaysPerQuarter, targetDay - gvm.SimDayNumber), out _);
                await Task.Delay(1);   // 描画へ手番を返す（画面が固まらない）
            }
            gvm.Playing = false;
            // 目標の年が終局の年なら終局の窓（順位）が最後の四半期で開く＝画像を隠すので撮る前に閉じる。
            if (gvm.EndingOpen) { PointRecord(rootDir, $"{gvm.DateText}　終局の窓を閉じてから撮る"); gvm.CloseEndingCommand.Execute(null); }
            await Task.Delay(500);
            FollowShownNation(rootDir, mwvm, ref gvm);
            if (spectate) NotePastWars(rootDir, gvm);
            // 観戦か年が複数なら、表.txt・点検.txt を画像より前に（世界を進めない）書き、画面の側の表も足す。03b（再生中の地図）だけは世界が進むので、この時点の最後へ回す（下）。
            bool tableMode = spectate || years.Length > 1;
            PointBegin(rootDir, dir, gvm, targetYear, tableMode);   // 表.txt と点検.txt を書き、この時点の判定の数えを始める
            // ★(501) 段2：地図は分割に単独で置ける 1 つのページ＝地図の撮影は〈1 枚の枠に地図〉で撮る（旧の中央の地図に近い絵）。最初に載せた瞬間に自国を中央へ寄せる（HomeView）。
            var lay = gvm.Layout;
            lay.Reset(); lay.Open(Slot.Map);
            await Task.Delay(1500);
            var gameView = this.GetVisualDescendants().OfType<GameView>().First();
            BootLog.Write("uishot: " + gameView.MapView.BoxReport());
            Capture(dir, "03_地図_一時停止.png");
            // 州ホバー（左上パネル）＝マウスの無い無人撮影では**開けない**（緊張モーダルのツールチップと同じ制約）。
            // 中身をログへ落として実測する＝生産材/輸出材の上位3材と世界シェアが空でないこと・幻の値でないことを追える。
            // ★F7（2026-09-28）：州○の廃止＝州の当たりは塗り分けのセル（MapHitAt）。各州・各未領有標的の**自分の座標**を当てて、
            //   自分が返るかを数える＝座標→州の当たりの無人の確かめ（自分が返らない州＝同じ格子を先着の種に取られた／隣の種の方が近い）。
            var seenHover = new System.Collections.Generic.HashSet<string>();
            string playerName = gvm.PlayerName.Split('　')[0];   // 「プロイセン　列強5位」→国名だけ（ホバー題は「州名（国名）」）
            int hitOk = 0, hitAll = 0, colOk = 0, colAll = 0;
            var hitMiss = new System.Collections.Generic.List<string>();
            var provHits = new System.Collections.Generic.List<(bool Mine, ViewModels.MapHoverInfo Hv)>();
            foreach (var sim in gvm.Sim.Nations)
                foreach (var rg in sim.World.Regions)
                {
                    if (!MapData.CityCoords.TryGetValue(rg.Name, out var co)) continue;
                    var (hx, hy) = MapData.Project(co.Lat, co.Lon);
                    var hit = gvm.MapHitAt(hx, hy);
                    hitAll++;
                    if (ReferenceEquals(hit.Region, rg)) hitOk++;
                    else if (hitMiss.Count < 12) hitMiss.Add(rg.Name + "→" + (hit.Region?.Name ?? hit.Hover?.Title ?? "なし"));
                    if (hit.Region is not null && hit.Hover is { } hh) provHits.Add((hh.Title.Contains(playerName, StringComparison.Ordinal), hh));
                }
            foreach (var pw in gvm.Sim.Diplomacy.Powers)
            {
                if (pw.Nation is not null) continue;
                foreach (var col in pw.ColonyList)
                {
                    if (Math.Abs(col.Latitude) < 1e-9 && Math.Abs(col.Longitude) < 1e-9) continue;
                    var (hx, hy) = MapData.Project(col.Latitude, col.Longitude);
                    colAll++;
                    if (gvm.MapHitAt(hx, hy).Hover?.Title.StartsWith(col.Name + "　", StringComparison.Ordinal) == true) colOk++;
                }
            }
            BootLog.Write($"uishot: 州の当たり 自分の座標で自分 {hitOk}/{hitAll}・未領有の標的 {colOk}/{colAll}" + (hitMiss.Count > 0 ? "・外れ " + string.Join(" ", hitMiss) : ""));
            foreach (bool mine in new[] { true, false })         // 自国の州を先に＝作者が実際に見る画面から確かめられる
                foreach (var (m, hv) in provHits)
                {
                    if (m != mine || hv.Lines.Count == 0 || !seenHover.Add(hv.Title)) continue;
                    BootLog.Write($"uishot: 州ホバー {hv.Title}\n  " + string.Join("\n  ", hv.Lines.Select(l => l.Text)));
                    if (seenHover.Count >= 12) break;
                }
            if (!tableMode)   // 従来の撮影（年が 1 つ・通常）はここで撮る。観戦・多時点では世界が進むので時点の最後（PointEnd の後）で撮る
            {
                gvm.TogglePauseCommand.Execute(null);   // 再生（停止/再生の幅ブレ検証用に両状態を撮る）
                await Task.Delay(300);
                Capture(dir, "03b_地図_再生中.png");
                gvm.TogglePauseCommand.Execute(null);   // 停止へ戻す
                await Task.Delay(300);
            }
            // 欧州へカメラを振って1枚（2026-09-02）＝政治の塗り分けは**国が密集した所**が本番（開始国の周りだけでは見えない）
            gvm.CenterNationCommand.Execute("フランス");
            await Task.Delay(600);
            Capture(dir, "03c_地図_欧州.png");
            // Escメニュー（2026-09-06・セーブのスロット化の確認）
            gvm.MenuOpen = true; await Task.Delay(300); Capture(dir, "11b_Escメニュー.png"); gvm.MenuOpen = false; await Task.Delay(200);
            // 州名の帯（2026-09-05）＝フィット×4 でパリを中心に＝州名の出方を決定的に撮る（作者裁定＝地図のアイコン類は置かない）。
            if (this.GetVisualDescendants().OfType<MapPane>().FirstOrDefault() is { } gvz)
            {
                gvz.SetZoomFactor(4.0);
                gvm.CenterNationCommand.Execute("フランス");
                await Task.Delay(700);
                Capture(dir, "03e_地図_州名帯.png");
            }
            // ★州パネル（2026-09-07 作者指示「政治・支配度は半円で」）：州をピン留めして撮る＝支配度の半円（(399) で区分は常に動く）とピンの表示の無人確認
            //   ★F7：州○の廃止＝自国の首都の座標を塗り分けのセルで当てる（クリックと同じ MapHitAt）
            var pin = gvm.PlayerCapitalCanvas() is { } pc ? gvm.MapHitAt(pc.X, pc.Y) : default;
            if (pin.Region is not null && pin.Hover is not null)
            {
                gvm.TogglePin(pin.Hover, pin.Region); gvm.Layout.Open(Slot.Province); await Task.Delay(500); Capture(dir, "03f_州パネル.png");   // 州の窓を開いて撮る（W20＝窓が写っていなかった）
                gvm.Layout.Close(Slot.Province);
                gvm.HoverPinned = false; gvm.MapHover = null; await Task.Delay(200);
            }
            // 全景（2026-09-02）＝州名が消えて**国名（英語）**が出る帯。政治の塗り分けの全体像もここで見る。
            if (this.GetVisualDescendants().OfType<MapPane>().FirstOrDefault() is { } gv)
            {
                gv.ZoomToWorld();
                await Task.Delay(700);
                Capture(dir, "03d_地図_全景.png");
                // ★島の塗り（2026-10-23・作者指摘「四国やルソン島が塗りから漏れている」）：州の無い島へ海を渡る割当（MapFill の段2）を拡大して撮る。
                gv.SetZoomFactor(6.0);
                foreach (var (nm, la, lo) in new[] { ("日本", 35.0, 135.0), ("フィリピン", 11.5, 122.5), ("地中海", 38.5, 17.0), ("北欧", 57.0, 14.0), ("カリブ", 18.5, -72.0), ("東インド", -5.0, 120.0) })
                {
                    gvm.CenterOnLatLon(la, lo);
                    await Task.Delay(500);
                    Capture(dir, $"03g_島_{nm}.png");
                }
                gv.ZoomToWorld();
                await Task.Delay(300);
            }
            // ★(501) 段2：旧の「タブを順に開く」は「ページを順に開く」になった。中央に 1 ページずつ。
            //   ページの外側が縦に流れていない判定（設計 §7-3）を各ページで BootLog に書く。
            k = 4;
            foreach (var slot in SlotInfo.All.Where(x => x != Slot.Map && x != Slot.Province))
            {
                UiShotWindows.CloseAll(lay); UiShotWindows.OpenFull(gameView, lay, slot);
                await Task.Delay(900);
                Capture(dir, $"{k:D2}_ページ_{SlotInfo.Name(slot)}.png");
                LogOuterFlow(gameView, SlotInfo.Name(slot));
                // 縦に長いページは最下端まで送った状態も撮る（下端見切れの検証・2026-07-25）
                var sv = this.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault(v => v.Name == "PageScroll");
                if (sv is not null && sv.Extent.Height > sv.Viewport.Height + 1)
                {
                    sv.ScrollToEnd();
                    await Task.Delay(400);
                    Capture(dir, $"{k:D2}b_ページ_{SlotInfo.Name(slot)}_最下端.png");
                    sv.ScrollToHome();
                    await Task.Delay(200);
                }
                k++;
            }
            await Page3Shots(gameView, gvm, dir);   // (501) 段3：政治と法・外交と戦争の撮影と確かめ（Views/MainWindow.Page3Shots.cs）
            // 配置の見本：既定（地図）・左の列を畳んだ図
            lay.Reset();
            await Task.Delay(1200);
            Capture(dir, $"{k++:D2}_配置_既定.png");
            LogOuterFlow(gameView, "既定の配置");
            lay.SetLeftOpen(false);
            await Task.Delay(800);
            Capture(dir, $"{k++:D2}_配置_左の列を畳む.png");
            LogOuterFlow(gameView, "左の列を畳む");
            lay.SetLeftOpen(true);
            var back = LayoutModel.FromJson(lay.ToJson());
            BootLog.Write($"uishot: 配置の往復 {(back.ToJson() == lay.ToJson() ? "一致" : "不一致")}・左 {(back.LeftOpen ? "開" : "閉")}");
            lay.Reset();
            await Task.Delay(300);
            await ShootPagesFiscalPopulationDiscovery(dir, gvm, gameView, lay);   // 段3：財政と貨幣・人口・発見の 3 ページを撮る（Views/MainWindow.UiShot.P2.cs）
            // 地図が中央に出ていない間の動き（外れた地図に触れない）
            BootLog.Write($"uishot: 地図が中央に無い間 Loaded={gameView.MapView.IsLoaded}・既定の配置の見えているもの={string.Join(",", lay.Shown())}");
            // 緊張モーダル（2026-08-10）：ポップアップ類は無人撮影の穴だった（発火待ちでは撮れない）。
            // 検出経路と同じ組み立てを通して最も緊張の高い相手で開く＝理由の内訳とボタン脇3行を実画面で確認できる。
            if (gvm.OpenTensionPopupForCapture())
            {
                await Task.Delay(700);
                Capture(dir, $"{k++:D2}_緊張モーダル.png");
                // 2つのボタンのホバー（作者裁定2026-08-10で政治的帰結の3行はツールチップへ移った）。
                // ⚠**写真は撮れない**：ツールチップは別ウィンドウのポップアップで、この RenderTargetBitmap（＝主ウィンドウの視覚木）
                //   には一切写らない（実測2026-08-10＝開いた状態で撮ると緊張モーダルの絵とバイト単位で同一だった）。
                //   よって撮影はせず、**開いて中身をログへ落とす**＝ホバーが開くこと・文言が空でないことは無人で追える。
                foreach (var btn in new[] { "TensionProvokeButton", "TensionEaseButton" })
                {
                    var b = this.GetVisualDescendants().OfType<Button>().FirstOrDefault(v => v.Name == btn);
                    if (b is null) { BootLog.Write($"uishot: {btn} が見つからない"); continue; }
                    BootLog.Write($"uishot: {btn} のホバー内容={ToolTip.GetTip(b)}");
                    ToolTip.SetIsOpen(b, true);
                    await Task.Delay(400);
                    ToolTip.SetIsOpen(b, false);
                    await Task.Delay(200);
                }
                gvm.TensionIgnoreCommand.Execute(null);
                await Task.Delay(300);
            }
            // 新たな法の窓（検出経路と同じ組み立てで開く）＝欄が切れずに出ることを撮る
            if (gvm.OpenLawOpenPopupForCapture())
            {
                await Task.Delay(700);
                Capture(dir, $"{k++:D2}_新たな法の窓.png");
                gvm.LawOpenCloseCommand.Execute(null);
                await Task.Delay(300);
            }
            // 発見のページの行数（旧は発見パネル）
            lay.Open(Slot.Discovery);
            await Task.Delay(600);
            BootLog.Write($"uishot: 発見のページ 行数={gvm.DiscoveryTable.Rows.Count}");
            lay.Close(Slot.Discovery);   // 発見の窓を開いたまま次を撮らない（生産の供給の網の左 2/3 が隠れていた・撮影の台本の直し）
            await Task.Delay(300);
            lay.Open(Slot.Production);
            await Task.Delay(900);
            Capture(dir, $"{k++:D2}_生産タブ.png");
            BootLog.Write($"uishot: 生産タブ撮影済み CurrentPage={mwvm.CurrentPage?.GetType().Name}");
            // 財を選んだ状態（右の詳細パネル）も撮る（2026-08-02）。★ここは長らく無人検証の穴で、
            // 「商品をクリックするとフリーズする」型の不具合を uishot が一度も踏めていなかった。
            gvm.SelectProdGood(PriceOfBread.SimCore.Catalog.SteelId);   // 鋼（加工財＝工場側。素材だけでなく工場の詳細パネルを踏む）
            await Task.Delay(1200);
            BootLog.Write($"uishot: 財選択後 CurrentPage={mwvm.CurrentPage?.GetType().Name} ProductionShown={gvm.ProductionShown}");
            Capture(dir, $"{k++:D2}_生産タブ_財選択.png");
            // popが直接食べる財も撮る（2026-08-10 項目6）＝用途の先頭に出る「食料」の行は、pop需要のある財でしか撮れない
            // （鋼＝BaseNeed 0 では出ない＝無人撮影の穴だった）。パン(62)＝主食で全州に産地がある。
            gvm.SelectProdGood(PriceOfBread.SimCore.Catalog.BreadId);
            await Task.Delay(1200);
            Capture(dir, $"{k++:D2}_生産タブ_主食を選択.png");
            // ⚠ツールチップは別ウィンドウのポップアップ＝この RenderTargetBitmap には写らない（緊張モーダルと同じ）。
            //   「誰が買ったか」（階級別シェア）は**中身をログへ落として**無人で追う。
            if (gvm.ProdDetail is { } pd && pd.Uses.Count > 0)
                BootLog.Write($"uishot: 用途の先頭={pd.Uses[0].Name} {pd.Uses[0].Amount}/買い手=\n{pd.Uses[0].Tip}");
            // 盤の右下（2026-08-10 項目1）＝国有化と巨大建造物は**キャンバスの中**に移した＝初期位置（左上）では写らず、
            // 右下まで送ると現れる。この2枚が「スクロールに追従しない」ことの無人での証拠になる。
            var ps = this.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault(v => v.Name == "ProdScroll");
            if (ps is not null)
            {
                ps.Offset = new Vector(Math.Max(0, ps.Extent.Width - ps.Viewport.Width), Math.Max(0, ps.Extent.Height - ps.Viewport.Height));
                await Task.Delay(600);
                BootLog.Write($"uishot: 盤 {ps.Extent.Width}×{ps.Extent.Height} 視野 {ps.Viewport.Width}×{ps.Viewport.Height} 送り {ps.Offset}");
                Capture(dir, $"{k++:D2}_生産タブ_盤の右下.png");
            }
            // 生産・財の価格・貿易ページの撮影の段と検査（(501) 段3・Views/UiShotPageP1.cs）
            try { await UiShotPageP1.Run(this, gvm, dir, n => Capture(dir, n), tag => LogOuterFlow(gameView, tag), () => k++); }
            catch (Exception ex) { BootLog.Write($"uishot: 生産と市場の段 失敗 {ex}"); }
            // 問題の一覧の撮影と、飛び先の確かめ（(501) 段4・段5・Views/MainWindow.UiShotFinal.cs）
            try { await ShootFinal(dir, gvm, gameView, () => k++); }
            catch (Exception ex) { BootLog.Write($"uishot: 最後の段 失敗 {ex}"); }
            // 段 6（2026-10-23）：生産と地図の Tab 切り替え・年表の 3 つの絞り・外交の地図の欄の首都・流した表（Views/MainWindow.UiShotS6.cs）
            try { await ShootS6(dir, gvm, gameView, () => k++); }
            catch (Exception ex) { BootLog.Write($"uishot: 段6 失敗 {ex}"); }
            await ShootWindows(dir, gvm, gameView, () => k++);   // 浮かぶ窓（2026-11-11・Views/MainWindow.UiShotWindows.cs）
            PointEnd(rootDir, gvm, targetYear);   // この時点の判定（外側の流れ・表の左端・UI の例外・異常の行）を記録へ
            if (tableMode)   // 03b：再生中の地図は世界を進める＝表.txt と他の画像の暦をずらさないよう最後に撮る（この絵だけ数日後）
            {
                lay.Reset(); lay.Open(Slot.Map);
                await Task.Delay(800);
                gvm.StepDaysForShot(5, out _);   // 再生中の絵＝数日進めて撮る（実時間で回さない）
                await Task.Delay(300);
                Capture(dir, "03b_地図_再生中.png");
                PointRecord(rootDir, $"[{targetYear}年] 03b_地図_再生中.png は再生中の絵＝暦 {gvm.DateText}（表.txt の暦 {_pDumpDate} より後）");
                await Task.Delay(300);
            }
            if (yi < years.Length - 1) { lay.Reset(); await Task.Delay(300); gvm.SetSpeedCommand.Execute("3"); }   // 続きを回す
            }   // for 年
            // セーブの往復（(501) 段1）：年次系列を名前と値の組で書く形の確かめ。一時の置き場へセーブ→ロードして、系列の名前・点数・和と日付が一致するかをログへ。作者の本物のセーブには触れない。
            try
            {
                string saveDir = Path.Combine(dir, "save_roundtrip");
                GameViewModel.SaveDirOverride = saveDir;
                gvm.SaveSlotCommand.Execute("1");
                var loaded = GameViewModel.TryLoad(1, null, null);
                string a = gvm.SeriesSummary, b = loaded?.SeriesSummary ?? "（ロード失敗）";
                BootLog.Write($"uishot: セーブの往復 系列 {(a == b ? "一致" : "不一致")}・日付 {(loaded?.DateText == gvm.DateText ? "一致" : "不一致")}・観戦 {(loaded?.Spectating == gvm.Spectating ? "一致" : "不一致")}・系列{gvm.SeriesSummary.Split(',').Length}本\n  {a}\n  {b}");
                loaded?.Shutdown();
            }
            catch (Exception ex) { BootLog.Write($"uishot: セーブの往復 失敗 {ex}"); }
            finally { GameViewModel.SaveDirOverride = null; }
            // 共通の部品の見本（(501) 段1）：表・線グラフ・半円を固定の見本の値で 1 枚に並べる（部品の見た目を無人で確かめる）。窓の中身を差し替えて撮り、撮ったら戻す。
            try
            {
                var savedContent = Content;
                Content = UiShotParts.Build();
                await Task.Delay(900);
                Capture(dir, $"{k++:D2}_部品の見本.png");
                Content = UiShotParts.BuildPages();
                await Task.Delay(900);
                Capture(dir, $"{k++:D2}_部品の見本_ページの欄.png");
                Content = savedContent;
            }
            catch (Exception ex) { BootLog.Write($"uishot: 部品の見本 失敗 {ex}"); }
            LogUiShotSummary(crashBefore);
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            BootLog.Write($"uishot失敗: {ex}");
            Environment.Exit(4);
        }
    }
}
