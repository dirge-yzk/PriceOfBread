using PriceOfBread.App.Controls;
using PriceOfBread.App.Layout;
using PriceOfBread.App.ViewModels;
using PriceOfBread.SimCore.Display;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PriceOfBread.App.Views;

// Views/MainWindow.UiShotMulti.cs ＝ `--uishot` の多時点の撮影（年をカンマで複数）と観戦の撮影の補助。
// 時点ごとの表.txt・点検.txt（UiShotDump）と、<出力dir>/撮影の記録.txt（年・映す国の名・時点ごとの判定）を書く。観測だけで盤面には書かない。
public partial class MainWindow
{
    // 時点ごとの判定の基準（LogOuterFlow と TallyAlignment と BootLog が数える値の、時点の頭の値）
    private int _pOuter0, _pFlow0, _pTab0, _pTabBad0, _pUi0, _pTables, _pFindings;
    private readonly List<string> _pLines = new();
    private string _pDumpDate = "";                         // 表.txt を書いた時点の暦（画像の暦と突き合わせる＝PointEnd が記録へ書く）
    private IReadOnlyList<TableSnapshot>? _pPrevSnaps;      // 前回の時点の表の写し（点検の「前回より下がった行」の規則が読む）
    private readonly HashSet<string> _seenPastWars = new(); // 記録へ書いた終わった戦争の鍵（同じ日に何本終わっても全て書く）

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
            SeedPastWars(g);
            gvm.SetSpeedCommand.Execute("3");
        }
    }

    /// <summary>画面の VM が持つ終わった戦争の鍵を、書かずに覚える（付け直した国の古い戦争を新規として書かない）。</summary>
    private void SeedPastWars(GameViewModel g)
    {
        _seenPastWars.Clear();
        foreach (var p in g.PastWarRows) _seenPastWars.Add(p.Key);
    }

    /// <summary>終わった戦争の欄に増えた行を全て記録へ書く。終戦の窓は同じ四半期に 1 本しか開かない（Refresh の仕様）ので、窓の記録だけでは 2 本目以降が抜ける＝欄の増分を正とする。</summary>
    private void NotePastWars(string rootDir, GameViewModel g)
    {
        foreach (var p in g.PastWarRows.ToArray())
            if (_seenPastWars.Add(p.Key))
                PointRecord(rootDir, $"{g.DateText}　終わった戦争の欄に増えた：{p.Title}　{p.Detail.Replace('\n', ' ')}");
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
    private void PointBegin(string rootDir, string dir, GameViewModel g, int year, bool extras)
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
            // 観戦・多時点の撮影だけ、画面の側の表（財政・外交・POP）とヘッダーも書く。年が 1 つだけの従来の撮影は表の目録だけ（従来どおり）
            var r = UiShotDump.Write(dir, g.Sim, w, $"{year}年（暦 {g.DateText}）{ShownNationName(g)}{(g.Spectating ? "・観戦" : "")}",
                extras ? CollectExtra(g) : null, extras ? _pPrevSnaps : null);
            _pTables = r.TableCount; _pFindings = r.Findings; _pDumpDate = g.DateText;
            if (extras) _pPrevSnaps = r.Snapshots;
        }
        catch (Exception ex) { BootLog.Write($"uishot: 表の書き出し失敗 {ex}"); }
    }

    /// <summary>画面の側だけが持つ表と、ヘッダーの文字を集める。ページを開いて「見えた瞬間の更新」を走らせてから、画面が持つ写しをそのまま読む（新しい計算はしない）。
    /// 盤面には書かない。配置は呼び側が戻す。</summary>
    private static UiShotDump.UiShotExtra CollectExtra(GameViewModel g)
    {
        var lay = g.Layout;
        var header = new List<string> { $"国={g.PlayerTitle}", $"暦={g.DateText}", g.TreasuryText, g.DebtHeaderText, g.ExpansionText };
        header.AddRange(g.HeaderStats.Select(h => h.Text));
        header.RemoveAll(string.IsNullOrWhiteSpace);
        var t = new List<TableSnapshot>();
        void Show(Slot slot) { lay.Close(slot); lay.Open(slot); }

        Show(Slot.FiscalMoney);
        t.Add(g.SpendTable with { Name = g.SpendTitle });
        t.Add(g.IncomeTable with { Name = g.IncomeTitle });
        t.Add(g.LedgerTable with { Name = g.LedgerTitle });

        Show(Slot.Population);
        var age = g.AgePyramid;
        var ageRows = Enumerable.Range(0, age.Labels.Count).Select(i => (IReadOnlyList<string>)[age.Labels[i], Fmt.PopFmtK(age.Male[i]), Fmt.PopFmtK(age.Female[i])]).ToList();
        if (g.ChildLaborLine.Length > 0) ageRows.Add([g.ChildLaborLine, "", ""]);
        t.Add(UiShotDump.TextTable("pop.age", "年齢の層", Page.Population, ["層", "男性", "女性"], ageRows));
        t.Add(UiShotDump.TextTable("pop.births", "出生と死", Page.Population, ["線", "最新", "最小", "最大", "点の数"],
            [LineRow("粗出生率", g.BirthRateValues, "P2"), LineRow("粗死亡率", g.DeathRateValues, "P2"), LineRow("合計出生率", g.FertilityValues, "F2")]));

        Show(Slot.DiplomacyWar);
        g.RefreshDiplomacyPage();
        t.Add(g.NationTable);
        t.Add(UiShotDump.TextTable("diplomacy.wars", "戦争", Page.DiplomacyWar, ["相手", "戦勝点", "開戦", "戦力", "損失", "戦費", "厭戦"],
            g.WarRows.Count == 0 ? [["交戦中の戦争なし"]] : g.WarRows.Select(r => (IReadOnlyList<string>)[r.Enemy, r.Score, r.Info, r.Forces, r.Losses, r.Cost, r.Exhaustion]).ToList()));
        t.Add(UiShotDump.TextTable("diplomacy.pastWars", "終わった戦争", Page.DiplomacyWar, ["題", "詳細"],
            g.PastWarRows.Count == 0 ? [["なし"]] : g.PastWarRows.Select(r => (IReadOnlyList<string>)[r.Title, r.Detail]).ToList()));
        var armyRows = new List<IReadOnlyList<string>>(); var armyGroups = new List<string?>(); string? grp = null;
        foreach (var r in g.ArmyNeeds)
        {
            if (r.IsHeader) { grp = r.Header; continue; }
            armyRows.Add([r.Name, r.Need, r.Have, r.Fill]); armyGroups.Add(grp);
        }
        t.Add(UiShotDump.TextTable("diplomacy.army", "軍事力の必要量", Page.DiplomacyWar, ["名前", "必要量", "実際", "充足率"],
            armyRows.Count == 0 ? [["軍備なし"]] : armyRows, armyRows.Count == 0 ? null : armyGroups));
        t.Add(UiShotDump.TextTable("diplomacy.factions", "反乱勢力", Page.DiplomacyWar, ["名前", "反乱規模", "結束", g.FactionProximityHeader, "要求"],
            g.DomesticFactions.Count == 0 ? [["目立った反乱勢力なし"]] : g.DomesticFactions.Select(r => (IReadOnlyList<string>)[r.Name, r.Size, r.Cohesion, r.Strength, r.Demands]).ToList()));
        t.Add(UiShotDump.TextTable("diplomacy.mediation", "他国の戦争", Page.DiplomacyWar, ["戦争", "内容", "調停"],
            g.MediationRows.Count == 0 ? [["他国の戦争なし"]] : g.MediationRows.Select(r => (IReadOnlyList<string>)[r.Title, r.Info, r.CanMediate ? "提案できる" : "不可 " + r.Reason]).ToList()));

        Show(Slot.Discovery);   // 発見の表は目録が読む（Build が書く）。ここは見えた瞬間の更新を画面と同じに走らせるだけ
        return new UiShotDump.UiShotExtra(header, t);
    }

    /// <summary>線グラフの最新・最小・最大・点の数（有限の点だけ）。</summary>
    private static IReadOnlyList<string> LineRow(string name, IReadOnlyList<double> v, string fmt)
    {
        var f = v.Where(double.IsFinite).ToList();
        if (f.Count == 0) return [name, "—", "—", "—", "0"];
        return [name, f[^1].ToString(fmt), f.Min().ToString(fmt), f.Max().ToString(fmt), f.Count.ToString()];
    }

    /// <summary>時点の終わり：この時点の判定（外側の流れ・表の左端・UI の例外・異常の行）を記録へ。</summary>
    private void PointEnd(string rootDir, GameViewModel g, int year)
    {
        BootLog.Tap = null;
        PointRecord(rootDir, $"[{year}年] 暦={g.DateText}　国={ShownNationName(g)}　外側の流れ {_gateOuter - _pOuter0}回の判定中 流れている{_gateOuterFlowing - _pFlow0}回・表の左端 {_gateTables - _pTab0}表の判定中 ずれた{_gateTablesBad - _pTabBad0}表・UIの例外 {BootLog.UiExceptions - _pUi0}件・表.txt {_pTables}枚 点検の指摘 {_pFindings}件　表.txt の暦={_pDumpDate}（{(_pDumpDate == g.DateText ? "画像と一致" : "画像とずれた")}）");
        string[] lines; lock (_pLines) lines = _pLines.ToArray();
        foreach (var l in lines) PointRecord(rootDir, "　　" + l);
    }
}
