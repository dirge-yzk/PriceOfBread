using Avalonia;
using System;

namespace PriceOfBread.App;

// 起動ライフサイクルの記録（crash.log 共用）：まれに起動直後へ数秒でコード0終了するフレークの診断用（2026-07-19）。
internal static class BootLog
{
    private static readonly string PathLog = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PriceOfBread", "crash.log");
    private static readonly System.Collections.Generic.List<string> _recent = new();
    /// <summary>この走りで握った UI スレッドの例外の数（撮影の総括が前後で数える。ファイル crash.log は他のプロセスも書くので、この走りだけの数もここで持つ）。</summary>
    public static int UiExceptions;
    /// <summary>書かれた行を横取りする口（撮影が時点ごとの異常の行を拾う。メモリ上の 200 行の窓では 1 時点ぶんが溢れうる）。</summary>
    public static System.Action<string>? Tap;
    public static System.Collections.Generic.IReadOnlyList<string> Recent() { lock (_recent) return _recent.ToArray(); }
    public static void Write(string msg)
    {
        try
        {
            lock (_recent) { _recent.Add($"{DateTime.Now:HH:mm:ss.fff} {msg}"); if (_recent.Count > 200) _recent.RemoveAt(0); }
            try { Tap?.Invoke(msg); } catch { }
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(PathLog)!);
            System.IO.File.AppendAllText(PathLog, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {msg}\n");
        }
        catch { }
    }
}

sealed class Program
{
    // UI自動撮影モード（#134・2026-07-25）：`--uishot <出力dir> [開始国index] [目標年]` で起動すると
    // タイトル→自動で国選択→高速で目標年まで進行→各タブを開いて連番PNG保存→終了コード0。
    // 開発ビルドの実地UI確認を無人化する恒久機能（computer-useのアプリ解決がStartメニュー未登録exeに効かないため自前で撮る）。
    public static string? UiShotDir;
    public static int UiShotNation = 2;   // 既定＝イギリス（world.json順：仏0・日1・英2・米3）
    public static int UiShotYear = 1793;  // 既定＝10年進めて市場・貿易が立った状態（複数の年を渡した時は最後の年）
    public static int[] UiShotYears = { 1793 };   // `--uishot <dir> <国> 1790,1800,1810`：各年に着いたら一時停止して全ページを <dir>/<年>/ に撮る。年が 1 つなら <dir> 直下（従来どおり）
    // ★観戦モード（`--autoplay`・単独でも `--uishot` と一緒でも）：選んだ国にも効用AIを差し、画面は通常と同じ。MainWindowViewModel.Spectate の初期値。
    public static bool Autoplay;
    // ★世界の種（`--seed <数>`・既定 1789＝従来どおり）。観戦の撮影で「別の世界でも同じ事が起きるか」を確かめる用。画面からは選べない。
    public static ulong Seed = SimFactory.DefaultSeed;
    // ★無人計測モード（A1・2026-09-05）：`--perf <出力txt>` で起動→自動で国選択→起動秒・四半期あたりms（速度3・40四半期）・全景/中心移動のフレームms を1行ずつ書いて終了。
    public static string? PerfOut;
    // ★地図スクロールの計測腕（2026-09-06・作者指示「マップスクロールがかなり重い」）：背景の焼き倍率・補間・四半期数を引数で切り替えて frame_ms を比べる。
    public static double LandSS = 5.0;             // --perf-ss N（既定5＝2026-08-31の設定）
    public static string LandInterp = "HighQuality"; // --perf-interp HighQuality|MediumQuality|LowQuality
    public static int PerfQuarters = 40;           // --perf-quarters N（0＝四半期試験を飛ばしフレーム試験だけ）
    // ★陸マスクの焼き直し（2026-09-05・作者指摘「陸上取引が内海を無視」の調査で landmask.bin にスーダン〜ヒジャーズの穴を発見）：
    //   `--bakemask <出力bin>` で App と同じ SVG 描画器（transform・<g class> 対応）から 0.1° 格子の陸マスクを焼いて終了。SimCore/landmask.bin と同じ形式（収縮1格子・LSB先）。
    public static string? BakeMaskOut;
    public static bool BakeMaskForRoads;   // `--bakemask <out> land`＝陸路用（any-coverage・収縮なし）

    // ★起動ロード中のフルスクリーンずれ（作者報告2026-08-15）：助走（「価格を設定中...」）は同期で数秒UIスレッドを塞ぐ。
    //   その間にクリックすると、Windowsが「応答なし」ウィンドウの代わりに**ゴースト窓**（DWMの複製）を出し、これはドラッグで
    //   動かせてしまう。復帰後の実窓はズレた位置を引き継ぎ、枠なしフルスクリーンなので掴む場所がなく戻せない。
    //   ゴースト機構をプロセス単位で無効化＝応答待ちの間、窓はそもそも動かせない（待たせる理由はロード画面が伝えている）。
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern void DisableProcessWindowsGhosting();

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        if (OperatingSystem.IsWindows()) DisableProcessWindowsGhosting();   // ↑ゴースト窓の無効化（user32はWindows専用＝ガード）
        // ★3桁ごとのカンマの全廃（作者裁定2026-08-10「これはUI全体に適応」）。
        // ⚠書式指定子（"N0" 等）を1つずつ潰す方式は必ず漏れる——**共有の書式関数**（PopFmtK/Men）や、
        //   これから書かれる行が同じ罠を踏み直す。桁区切り文字そのものを空にすれば、どの経路から出た数値にも
        //   カンマが付かない＝全廃が構造的に保証される（k/M の単位表記は各書式関数が持つので影響しない）。
        // ⚠小数点は変えない（数値の意味は不変）。P0 は PercentGroupSeparator を、通貨書式は CurrencyGroupSeparator を
        //   別に読むので3つとも空にする。数値の**解析**は影響を受けない（データ読み込みは System.Text.Json＝不変文化）。
        var noGrouping = (System.Globalization.CultureInfo)System.Globalization.CultureInfo.CurrentCulture.Clone();
        noGrouping.NumberFormat.NumberGroupSeparator = "";
        noGrouping.NumberFormat.PercentGroupSeparator = "";
        noGrouping.NumberFormat.CurrencyGroupSeparator = "";
        System.Globalization.CultureInfo.DefaultThreadCurrentCulture = noGrouping;   // 以後に作られるスレッドも含めて既定
        System.Globalization.CultureInfo.CurrentCulture = noGrouping;                // 起動スレッド（＝UIスレッド）
        for (int i = 0; i < args.Length; i++)
            if (args[i] == "--perf" && i + 1 < args.Length) PerfOut = args[i + 1];
            else if (args[i] == "--perf-ss" && i + 1 < args.Length) LandSS = double.Parse(args[i + 1], System.Globalization.CultureInfo.InvariantCulture);
            else if (args[i] == "--perf-interp" && i + 1 < args.Length) LandInterp = args[i + 1];
            else if (args[i] == "--perf-quarters" && i + 1 < args.Length) PerfQuarters = int.Parse(args[i + 1]);
            else if (args[i] == "--bakemask" && i + 1 < args.Length) { BakeMaskOut = args[i + 1]; BakeMaskForRoads = i + 2 < args.Length && args[i + 2] == "land"; }
            else if (args[i] == "--uishot" && i + 1 < args.Length)
            {
                UiShotDir = args[i + 1];
                if (i + 2 < args.Length && int.TryParse(args[i + 2], out int n)) UiShotNation = n;
                if (i + 3 < args.Length && !args[i + 3].StartsWith("--", StringComparison.Ordinal))
                {
                    var ys = UiShotArgs.ParseYears(args[i + 3]);
                    if (ys is { Length: > 0 }) { UiShotYears = ys; UiShotYear = ys[^1]; }
                    else BootLog.Write($"uishot: 年の引数が読めない「{args[i + 3]}」→既定の{UiShotYear}年");
                }
            }
            else if (args[i] == "--autoplay") Autoplay = true;
            else if (args[i] == "--seed" && i + 1 < args.Length)
            {
                if (ulong.TryParse(args[i + 1], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out ulong sd)) Seed = sd;
                else BootLog.Write($"seed: 種の引数が読めない「{args[i + 1]}」→既定の{Seed}");
            }
        // ★(501) 段2：撮影・計測・陸マスクの焼きは、作者の本物の配置（%LOCALAPPDATA%/PriceOfBread/配置.json）を読まず・書かない＝別の場所の配置を使う（毎回、初めの配置から始まる）。
        if (UiShotDir is not null || PerfOut is not null || BakeMaskOut is not null)
            Layout.LayoutStore.PathOverride = System.IO.Path.Combine(UiShotDir ?? System.IO.Path.GetTempPath(), "配置_検査用.json");
        // クラッシュログ（GUIサブシステム＝コンソール無しで例外が見えない問題の恒久対策）：
        // 未処理例外を %LOCALAPPDATA%\PriceOfBread\crash.log へ記録してから落ちる（書き手は BootLog.Write の1本）。
        void Log(string msg) => BootLog.Write(msg);
        AppDomain.CurrentDomain.UnhandledException += (_, e) => Log($"UnhandledException: {e.ExceptionObject}");
        // ★2026-08-02：**UIスレッドの例外**を捕まえる（作者報告「生産のない財をクリックすると落ちる」）。
        // ⚠ボタン押下・バインディング更新の中で投げられた例外は AppDomain.UnhandledException を**通らずに**
        //   プロセスを落とすことがあり、crash.log に1行も残らなかった（実際に残っていなかった）。
        // ★Handled=true で落とさずに記録する＝安全網。⚠これは修正ではなく**診断できる形にする**ためのもので、
        //   ログに出た例外は必ず根本を直すこと（握り潰しを常態にしない）。
        Avalonia.Threading.Dispatcher.UIThread.UnhandledException += (_, e) =>
        {
            BootLog.UiExceptions++;
            Log($"UIThread例外(握って継続): {e.Exception}");
            e.Handled = true;
        };
        System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (_, e) => Log($"UnobservedTaskException: {e.Exception}");
        try
        {
            BootLog.Write("起動");
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            Log("正常終了");
        }
        catch (Exception ex)
        {
            Log($"Main例外: {ex}");
            throw;
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
