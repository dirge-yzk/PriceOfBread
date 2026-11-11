using PriceOfBread.App.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;

namespace PriceOfBread.App.Views;

// Views/MainWindow.UiShotMulti.cs ＝ `--uishot` の多時点の撮影（年をカンマで複数）と観戦の撮影の補助。
// 時点ごとの表.txt・点検.txt（UiShotDump）と、<出力dir>/撮影の記録.txt（年・映す国の名・時点ごとの判定）を書く。観測だけで盤面には書かない。
public partial class MainWindow
{
    // 時点ごとの判定の基準（LogOuterFlow と TallyAlignment と BootLog が数える値の、時点の頭の値）
    private int _pOuter0, _pFlow0, _pTab0, _pTabBad0, _pUi0, _pTables, _pFindings;
    private readonly List<string> _pLines = new();

    private static string RecordPath(string rootDir) => Path.Combine(rootDir, "撮影の記録.txt");

    /// <summary>撮影の記録へ 1 行足す。</summary>
    private static void PointRecord(string rootDir, string line)
    {
        try { Directory.CreateDirectory(rootDir); File.AppendAllText(RecordPath(rootDir), line + "\n", new System.Text.UTF8Encoding(false)); }
        catch (Exception ex) { BootLog.Write("uishot: 撮影の記録に書けない " + ex.Message); }
        BootLog.Write("uishot: 記録 " + line);
    }

    /// <summary>いま映している国の名前（消えた画面の VM でも落ちない）。</summary>
    private static string ShownNationName(GameViewModel g)
    {
        try
        {
            if (g.PlayerIndex >= 0 && g.PlayerIndex < g.Sim.Nations.Count) return g.Sim.Nations[g.PlayerIndex].World.SelfPower?.Name ?? g.PlayerName;
        }
        catch { }
        return g.PlayerName;
    }

    /// <summary>映す国が付け直されて画面の VM が差し替わっていたら握り直す（記録に年と国の名を書く）。</summary>
    private void FollowShownNation(string rootDir, MainWindowViewModel mwvm, ref GameViewModel gvm)
    {
        if (mwvm.CurrentPage is GameViewModel g && !ReferenceEquals(g, gvm))
        {
            PointRecord(rootDir, $"{g.DateText}　映す国を付け直した：{ShownNationName(gvm)} → {ShownNationName(g)}");
            gvm = g;
            gvm.SetSpeedCommand.Execute("3");
        }
    }

    /// <summary>観戦中に開いたモーダルを、押さずに閉じる。開いたこと自体が「プレイヤー専用の経路が観戦で開いた」証拠なので記録に書く。</summary>
    private void CloseModalsForSpectate(string rootDir, GameViewModel g)
    {
        void Note(string what) => PointRecord(rootDir, $"{g.DateText}　観戦でモーダルが開いた：{what}");
        if (g.TensionPopupOpen) { Note("緊張"); g.TensionPopupOpen = false; }
        if (g.WarPopupOpen) { Note("最後通牒"); g.WarPopupOpen = false; }
        if (g.WarEndPopupOpen) { Note("終戦・通知「" + (g.WarEndPopupBody ?? "").Replace('\n', ' ') + "」"); g.WarEndPopupOpen = false; }
        if (g.FactionCrisisOpen) { Note("国内勢力の危機"); g.FactionCrisisOpen = false; }
        if (g.ElectionPopupOpen) { Note("総選挙"); g.ElectionPopupOpen = false; }
        if (g.CallToArmsOpen) { Note("参戦の要請"); g.CallToArmsOpen = false; }
        if (g.SideChoiceOpen) { Note("陣営の選択"); g.SideChoiceOpen = false; }
        if (g.ConfirmOpen) { Note("確認「" + g.ConfirmTitle + "」"); g.ConfirmOpen = false; }
    }

    /// <summary>時点の頭：表.txt と点検.txt を書き、判定の数えの基準を取る。</summary>
    private void PointBegin(string rootDir, string dir, GameViewModel g, int year)
    {
        _pOuter0 = _gateOuter; _pFlow0 = _gateOuterFlowing; _pTab0 = _gateTables; _pTabBad0 = _gateTablesBad; _pUi0 = BootLog.UiExceptions;
        lock (_pLines) _pLines.Clear();
        BootLog.Tap = line =>
        {
            if (line.Contains("] 流れている", StringComparison.Ordinal) || line.Contains("左端がずれている", StringComparison.Ordinal) || line.Contains("失敗", StringComparison.Ordinal)
                || line.Contains("不一致", StringComparison.Ordinal) || line.Contains("UIThread例外", StringComparison.Ordinal) || line.Contains("UnhandledException", StringComparison.Ordinal)
                || line.Contains("進行が止まっている", StringComparison.Ordinal))
                lock (_pLines) _pLines.Add(line.Length > 300 ? line[..300] + "…" : line.Replace('\n', ' '));
        };
        _pTables = 0; _pFindings = 0;
        try
        {
            var w = g.Sim.Nations[g.PlayerIndex].World;
            var r = UiShotDump.Write(dir, g.Sim, w, $"{year}年（暦 {g.DateText}）{ShownNationName(g)}{(g.Spectating ? "・観戦" : "")}");
            _pTables = r.TableCount; _pFindings = r.Findings;
        }
        catch (Exception ex) { BootLog.Write($"uishot: 表の書き出し失敗 {ex}"); }
    }

    /// <summary>時点の終わり：この時点の判定（外側の流れ・表の左端・UI の例外・異常の行）を記録へ。</summary>
    private void PointEnd(string rootDir, GameViewModel g, int year)
    {
        BootLog.Tap = null;
        PointRecord(rootDir, $"[{year}年] 暦={g.DateText}　国={ShownNationName(g)}　外側の流れ {_gateOuter - _pOuter0}回の判定中 流れている{_gateOuterFlowing - _pFlow0}回・表の左端 {_gateTables - _pTab0}表の判定中 ずれた{_gateTablesBad - _pTabBad0}表・UIの例外 {BootLog.UiExceptions - _pUi0}件・表.txt {_pTables}枚 点検の指摘 {_pFindings}件");
        string[] lines; lock (_pLines) lines = _pLines.ToArray();
        foreach (var l in lines) PointRecord(rootDir, "　　" + l);
    }
}
