using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using PriceOfBread.SimCore;
using PriceOfBread.SimCore.Display;

namespace PriceOfBread.Runner;

/// <summary>★(607) Runner play の報告に足す〈数と状態だけ〉の節（テストプレイ 日本1 の指摘 J・K）。世界は読むだけ（書かない）＝成績表に効かない別のコマンド（play）の中だけにある。
/// 見出しは名前・値は数。効果の説明・理由づけ・勧めの文は書かない（作者 2026-10-27）。
/// 民営の工場の棟数は <see cref="World.FactoryCounter"/> と同じ述語・同じ数え方（画面の財カードの「10/12」と同じ数）。失業は画面の州の表（<see cref="PopTables.RegionLines"/>）と階級の表（<see cref="PopTables.ClassLines"/>）の失業者。
/// 最後通牒の中身は App の最後通牒のポップアップが読む本体の関数（<see cref="Diplomacy.TensionBreakdown"/>・<see cref="Diplomacy.WarOutlook"/>・係数）をそのまま読む。</summary>
internal static class PlayFacts
{
    /// <summary>州×財の工場の 1 行（数え方は <see cref="World.FactoryCounter"/> と同じ述語）。</summary>
    internal readonly record struct FactoryRow(int GoodId, string Region, string Recipe, int Private, int State, bool Bankrupt, double ProfitRate, double SmoothedMargin, double Mothball);

    /// <summary>自国の工場の行（技術の門が開いた生産者だけ・<see cref="World.FactoryCounter"/> と同じ述語）。利益の率＝直近の月の（収入−費用）÷費用（費用が 0 のときは NaN）。</summary>
    internal static List<FactoryRow> FactoryRows(World w)
    {
        var rows = new List<FactoryRow>();
        foreach (var r in w.Regions)
            foreach (var p in r.Producers)
            {
                if (!(Discovery.Gate(r, p) > ProductionDiag.TechGateClosed)) continue;   // 否定形＝NaN は数えない（FactoryCounter と同じ）
                int st = w.StateUnitsOf(p), units = w.PrivateUnitsOf(p), pv = p.Bankrupt ? 0 : units;
                if (st == 0 && units == 0) continue;   // 倒産中でも棟があった個体は 1 件として載せる（棟数の合計には入れない）
                double rate = p.LastCost > 1e-9 ? (p.LastRevenue - p.LastCost) / p.LastCost : double.NaN;
                rows.Add(new FactoryRow(p.GoodId, r.Name, p.RecipeName, pv, st, p.Bankrupt, rate, p.SmoothedMargin, p.MothballFrac.Value));
            }
        return rows;
    }

    private static string Pct(double v) => double.IsNaN(v) ? "－" : v.ToString("+0%;-0%;0%", CultureInfo.InvariantCulture);

    /// <summary>民営の工場（財ごとに 1 行・州の棟数と利益の率）。棟数の合計は <see cref="World.FactoryCounter"/> の民営・国営と一致する。</summary>
    internal static void AppendFactories(StringBuilder sb, World w)
    {
        var rows = FactoryRows(w);
        sb.AppendLine();
        int totPv = rows.Sum(x => x.Private), totSt = rows.Sum(x => x.State), bankrupt = rows.Count(x => x.Bankrupt);
        sb.AppendLine($"---- 工場（財ごと・州の棟数と直近の月の利益の率）　民営 {totPv} 棟・国営 {totSt} 棟・倒産中 {bankrupt} 件 ----");
        foreach (var grp in rows.GroupBy(x => x.GoodId).OrderBy(g => g.Key))
        {
            string gname = w.Region.GoodOf(grp.Key)?.Name ?? $"財{grp.Key}";
            int pv = grp.Sum(x => x.Private), st = grp.Sum(x => x.State);
            var sites = grp.OrderByDescending(x => x.Private + x.State).ThenBy(x => x.Region, StringComparer.Ordinal).ToList();
            const int Shown = 8;
            string list = string.Join(" ", sites.Take(Shown).Select(x =>
                $"{x.Region}{(x.Recipe.Length > 0 ? "・" + x.Recipe : "")} {(x.Bankrupt ? "倒産" : $"民{x.Private}国{x.State}")} {Pct(x.ProfitRate)}"));
            sb.AppendLine($"  {gname,-8} 民営 {pv,3} 国営 {st,3}　［{list}{(sites.Count > Shown ? $" ほか {sites.Count - Shown} 件" : "")}］");
        }
    }

    /// <summary>国内の失業（画面の州の表・階級の表と同じ失業者の数え方＝兵士を除く）。</summary>
    internal static void AppendUnemployment(StringBuilder sb, World w)
    {
        double popAll = 0, popCiv = 0;
        foreach (var p in w.AllPops()) { popAll += p.Size; if (!p.IsSoldier) popCiv += p.Size; }
        var regions = PopTables.RegionLines(w);
        double un = regions.Sum(x => x.Unemployed);
        sb.AppendLine();
        sb.AppendLine("---- 失業 ----");
        sb.AppendLine($"失業者 {un:N0}　失業率 {(popCiv > 1e-9 ? un / popCiv : 0):P1}（兵士を除く人口 {popCiv:N0} に対して）／{(popAll > 1e-9 ? un / popAll : 0):P1}（全人口 {popAll:N0} に対して）");
        var classes = PopTables.ClassLines(w).Where(c => c.UnempRate > 0.005).OrderByDescending(c => c.Unemployed).Take(8).ToList();
        sb.AppendLine("階級別（失業率 0.5% 超・失業者の多い順）： " + (classes.Count == 0 ? "なし" : string.Join("　", classes.Select(c => $"{c.Name} {c.Unemployed:N0}（{c.UnempRate:P1}）"))));
        var topR = regions.Where(x => x.Unemployed > 0.5 && x.Population > 1e-9).OrderByDescending(x => x.Unemployed).Take(5).ToList();
        sb.AppendLine("州別（失業者の多い順 上位 5・率の分母は州の全人口）： " + (topR.Count == 0 ? "なし" : string.Join("　", topR.Select(x => $"{x.Name} {x.Unemployed:N0}（{x.Unemployed / x.Population:P1}）"))));
    }

    /// <summary>最後通牒の中身（App のポップアップが出す数と同じ関数）：争点（緊張の内訳を表示名で束ねて大きい順）・緊張の今と受諾後・受諾で払う正統性・開戦したときの戦力と勝勢。数と名前だけ。</summary>
    internal static string UltimatumFacts(World w, Power sp, Power foe)
    {
        if (w.Diplomacy is not { } dip) return "";
        var issues = new List<(string Name, double V)>();
        foreach (var (reason, value) in dip.TensionBreakdown(sp, foe))
        {
            string nm = Diplomacy.TensionReasonJa(reason);
            if (nm.Length == 0) continue;
            int i = issues.FindIndex(x => x.Name == nm);
            if (i >= 0) issues[i] = (nm, issues[i].V + value); else issues.Add((nm, value));
        }
        issues = issues.OrderByDescending(x => x.V).ToList();
        var o = dip.WarOutlook(sp, foe, w.Coef);
        double tNow = dip.Tension(sp, foe), legCost = Math.Min(w.Coef.AISubmitLegitCost, w.Legitimacy);
        return $"争点 {(issues.Count == 0 ? "なし" : string.Join(" ", issues.Take(4).Select(x => $"{x.Name} {x.V:F0}")))}／緊張 {tNow:F0}→受諾後 {w.Coef.ConcedeTensionTarget:F0}／受諾で正統性 −{legCost:F2}／"
             + $"戦力の指数 自軍 陸{o.MyLand:N0} 海{o.MyNaval:N0} 空{o.MyAir:N0}・{foe.Name} 陸{o.EnLand:N0} 海{o.EnNaval:N0} 空{o.EnAir:N0}／勝勢 {o.WinShare:P0}";
    }

    /// <summary>参戦要請の中身：自軍と敵の戦力・断ったときの緊張の上乗せ（App のポップアップと同じ係数）。</summary>
    internal static string CallToArmsFacts(World w, Power sp, Power caller, Power enemy)
    {
        if (w.Diplomacy is not { } dip) return "";
        var o = dip.WarOutlook(sp, enemy, w.Coef);
        return $"戦力の指数 自軍 陸{o.MyLand:N0} 海{o.MyNaval:N0} 空{o.MyAir:N0}・{enemy.Name} 陸{o.EnLand:N0} 海{o.EnNaval:N0} 空{o.EnAir:N0}／勝勢 {o.WinShare:P0}／断ると {caller.Name} との緊張 +{w.Coef.AllyRefusalTension:F0}";
    }

    /// <summary>待っている問いの中身の節（最後通牒・参戦要請があるときだけ）。</summary>
    internal static void AppendPendingFacts(StringBuilder sb, World w, Power sp)
    {
        if (sp.PendingWar is null && sp.PendingCallToArms is null) return;
        sb.AppendLine();
        sb.AppendLine("---- 待っている問いの中身 ----");
        if (sp.PendingWar is { } foe) sb.AppendLine($"  最後通牒 {foe.Name}： {UltimatumFacts(w, sp, foe)}");
        if (sp.PendingCallToArms is { } ca) sb.AppendLine($"  参戦要請 {ca.Caller.Name}→{ca.Enemy.Name}： {CallToArmsFacts(w, sp, ca.Caller, ca.Enemy)}");
    }

    /// <summary>報告に足す節（数と状態だけ）をまとめて書く。</summary>
    internal static void Append(StringBuilder sb, World w, Power sp)
    {
        AppendPendingFacts(sb, w, sp);
        AppendFactories(sb, w);
        AppendUnemployment(sb, w);
    }

    /// <summary>`!callarms 受ける|断る 全て`（または `!answer 参戦要請=受ける:全て` と同じ展開）の形なら答え（受ける＝true）を返す。この呼び出しの間に立つ参戦要請へ同じ答えを置く指定
    /// （state には持ち越さない＝次の呼び出しでは効かない・行そのものは decision.raw.txt に残り、置いた答えは <see cref="PlayHarness.SkipCallToArms"/> が四半期スタンプつきで残す）。★(609) 合流：`!answer` の展開の行も読む（反証役 rpt〔v〕低 #3）。</summary>
    internal static bool? StandingCallArmsOf(string line)
    {
        string[] a = line.TrimStart('!').Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (a.Length == 3 && a[0] == "callarms" && a[2] == "全て" && a[1] is ("受ける" or "断る")) return a[1] == "受ける";
        if (a.Length > 1 && a[0] == "answer" && ExpandAnswer(a).Lines is { } lines)
            foreach (string l in lines)
                if (StandingCallArmsOf(l) is { } s) return s;
        return null;
    }

    /// <summary>`!answer 最後通牒=… 参戦要請=… 総選挙=…` を既存の動詞の行へ展開する（1 行で全ての待ちに答える）。値の中の `:` は空白に直す
    /// （`最後通牒=開戦:総動員`＝`!ultimatum 開戦 総動員`・`総選挙=2:0.3`＝`!election 2 0.3`）。書かなかった種類には触れない。展開した行を返す（書式不正は null と理由）。</summary>
    internal static (List<string>? Lines, string? Error) ExpandAnswer(string[] a)
    {
        var lines = new List<string>();
        for (int i = 1; i < a.Length; i++)
        {
            int eq = a[i].IndexOf('=');
            if (eq <= 0 || eq == a[i].Length - 1) return (null, $"⚠書式: `!answer 最後通牒=受諾|開戦[:総動員|:総動員なし] 参戦要請=受ける|断る 総選挙=番号|棄権[:割合]`（読めない語: {a[i]}）");
            string key = a[i][..eq], val = a[i][(eq + 1)..].Replace(':', ' ');
            string? verb = key switch { "最後通牒" => "ultimatum", "参戦要請" => "callarms", "総選挙" => "election", _ => null };
            if (verb is null) return (null, $"⚠答えられる待ちの種類が読めない: {key}（最後通牒|参戦要請|総選挙）");
            lines.Add($"!{verb} {val}");
        }
        return lines.Count == 0 ? (null, "⚠書式: `!answer 最後通牒=… 参戦要請=… 総選挙=…`（1 つ以上）") : (lines, null);
    }
}
