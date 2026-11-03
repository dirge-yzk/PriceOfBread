using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (449) 線 sp の番人（事前登録 measurements/sp_20261020_prereg.txt [6]）＝国力の物差し（世界の総量に対する本国の割合）。★(456) 旗 ScoreFromWorldShare を焼き込んで旧の腕を消した。
/// ①国力の読み手が 1 本（旧の 4 項・旧の腕・旗の復活を禁じる）／②内訳の 1 本（6 項の点の和が国力）／③開幕のランクの取り直し（助走の直後に 6 項とも総量が正）／
/// ④条約港の風化は列強の中央値を基準にする／⑤総量の集合は実在の国〔Power.Nation が非 null〕だけで、抽象の勢力が盤に居ても動かない（(465) 作り直し）。
/// 期待値は手計算のリテラルか独立の式で書く（実装の関数の出力から作らない＝同語反復にしない）。
/// </summary>
public class ScoreReaderTests
{
    private static MultiNationSim Four()
        => new([NationConfig.France(), NationConfig.Japan(), NationConfig.Britain(), NationConfig.America()], seed: 1789);

    // ================= ① 読み手は 1 本 =================

    // ★(451) 合流：出力の場所（AppContext.BaseDirectory）から上へ探す形は隔離ビルド（--artifacts-path がリポジトリの外）で null になって赤だった＝既存の 1 本（ソースの場所から引く）を呼ぶ。
    private static string RepoRoot() => GoodIdGuardTests.RepoRoot();

    /// <summary>コメント〔行頭の // と行末の //〕を落とした行（文字列の中に // は無い）。</summary>
    private static string CodeOf(string line)
    {
        int i = line.IndexOf("//", StringComparison.Ordinal);
        return i < 0 ? line : line[..i];
    }

    private static IEnumerable<(string Name, int Line, string Code)> SourceLines()
    {
        var root = RepoRoot();
        foreach (var dir in new[] { "PriceOfBread.SimCore", "PriceOfBread.App", "PriceOfBread.Runner" })
            foreach (var f in Directory.EnumerateFiles(Path.Combine(root, dir), "*.cs", SearchOption.AllDirectories))
            {
                if (f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar) || f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)) continue;
                string name = Path.GetFileName(f);
                int ln = 0;
                foreach (var line in File.ReadLines(f)) { ln++; yield return (name, ln, CodeOf(line)); }
            }
    }

    [Fact]
    public void 旧の国力の式と旗は復活しない_旧の列を読むのは計器だけである()
    {
        // ★(456) 旗 ScoreFromWorldShare（0＝旧の和）と旧の腕〔OfOld・TreatyStrengthOld・LegacyLines〕は焼き込んで消した。旧の 4 項の式（Instruments.OldScore）と計器 score_s の旧の列〔scoreOld・rankOld・isGreatOld〕は (798) で消した。
        //   NationalScore に旧の項を戻す・旗を戻す・OldScore を戻す・旧の項の宣言を戻す、のどれも（どのファイルでも）赤にする。
        //   金融〔NationalScore.Finance〕は画面の与信の履歴の系列〔GameViewModel.Header.cs〕だけが読む＝国力の読み手ではない（その 1 つだけ許す・(805) で計器 InstrumentsScore.cs の例外を外した）。
        var forbidden = new Regex(@"\b(OfOld|TreatyStrengthOld|LegacyLines|LegacyLabels|ScoreFromWorldShare)\b|NationalScore\.(Military|Wealth|Tech|WealthParts)\b");
        var decl = new Regex(@"\bstatic\s+[\w<>\[\](),?\s]*?\b(Military|Wealth|Tech|WealthParts)\s*\(");   // ★(457) 修飾なしで呼ぶ旧の項の宣言も禁じる（反証役 hy3〔v〕低#3・(805) で計器の例外を外した）
        var finance = new Regex(@"NationalScore\.Finance\b");
        var oldScore = new Regex(@"\bOldScore\b");
        var bad = new List<string>();
        foreach (var (name, ln, code) in SourceLines())
        {
            foreach (Match m in forbidden.Matches(code)) bad.Add($"{name}:{ln} {m.Value}");
            foreach (Match m in decl.Matches(code)) bad.Add($"{name}:{ln} {m.Value}");
            if (finance.IsMatch(code) && name != "GameViewModel.Header.cs") bad.Add($"{name}:{ln} NationalScore.Finance");
            if (oldScore.IsMatch(code)) bad.Add($"{name}:{ln} OldScore");
        }
        Assert.True(bad.Count == 0, "旧の国力の式か旗が戻っている：" + string.Join(" / ", bad));
    }

    // ================= ② 内訳の 1 本 =================

    [Fact]
    public void 内訳は6項の点で_その和が国力で_首位との差も同じ物差しである()
    {
        var sim = Four();
        sim.Run(2);
        var ws = sim.Nations.ConvertAll(n => n.World);
        // 独立の式：項ごとの総量 tot_t・使う項の数 used から 100×素の量÷総量÷used
        var terms = ws.Select(NationalScore.TermsOf).ToArray();
        var tot = new double[6];
        foreach (var t in terms) for (int k = 0; k < 6; k++) tot[k] += t[k];
        int used = tot.Count(x => x > 0);
        Assert.Equal(6, used);   // 2 四半期走らせた世界では 6 項とも総量が正（退化していない前提）
        var all = NationalScore.Breakdowns(ws);
        var sc = NationalScore.Scores(ws);
        for (int i = 0; i < ws.Count; i++)
        {
            Assert.Equal(["兵の数", "軍事費", "鉄の産出", "動力の消費", "都市の人口", "総人口"], all[i].Select(l => l.Label));
            for (int k = 0; k < 6; k++) Assert.Equal(100.0 * terms[i][k] / tot[k] / used, all[i][k].Points, 9);
            Assert.Equal(sc[i], all[i].Sum(l => l.Points), 9);
            Assert.Equal(sc[i], NationalScore.BreakdownOf(ws[i]).Sum(l => l.Points), 9);   // 国ごとに引く道も同じ
        }
        // ホバーの文字列：6 項の行と、合計＝国力・首位との差＝同じ Scores の最大との差
        var text = PlayText.ScoreBreakdownText(ws[0]);
        foreach (var lab in new[] { "兵の数", "軍事費", "鉄の産出", "動力の消費", "都市の人口", "総人口" }) Assert.Contains(lab + " ", text);
        Assert.DoesNotContain("生産の豊かさ", text);
        Assert.Contains($"合計 {sc[0]:F1}", text);
        double topV = sc.Max();
        if (topV > sc[0] + 1e-9) Assert.Contains($"との差 {topV - sc[0]:F1}", text); else Assert.EndsWith("首位", text);
    }

    // ================= ③ 開幕のランクの取り直し =================

    [Fact]
    public void 助走の直後にランクを取り直し_その時点で6項とも総量が正である()
    {
        // 61 カ国の全体で読む（定員制の分岐＝real.Count＞12）。
        var sim = new MultiNationSim(WorldData.Nations(), 1789UL, _ => new NullAgent());
        var ws = sim.Nations.ConvertAll(n => n.World);
        int UsedTerms()
        {
            var tot = new double[6];
            foreach (var t in ws.Select(NationalScore.TermsOf)) for (int k = 0; k < 6; k++) tot[k] += t[k];
            return tot.Count(x => x > 0);
        }
        Assert.True(UsedTerms() < 6, "構築時に 6 項そろっている＝取り直しの根拠が崩れた（市場が回る前は流れの項が 0 のはず）");
        var atBuild = sim.Diplomacy.Powers.Where(p => p.Nation != null).ToDictionary(p => p.Id, p => sim.Diplomacy.RankOf(p));   // 構築時のランク（コンストラクタが置いたもの）
        sim.StartWarmup();
        Assert.Equal(6, UsedTerms());
        // 独立の再述：国力の降順（同点は Id）で上位 GreatPowerCount＝列強・次の SecondPowerCount＝準列強・次の MiddlePowerCount＝中堅・残り＝弱小
        var c = sim.Coef;
        var sc = NationalScore.ScoresByPower(sim.Diplomacy);
        var order = sim.Diplomacy.Powers.Where(p => p.Nation != null).OrderByDescending(p => sc[p.Id]).ThenBy(p => p.Id).ToList();
        int diffFromBuild = 0;
        for (int i = 0; i < order.Count; i++)
        {
            var want = i < c.GreatPowerCount ? PowerRank.Great
                     : i < c.GreatPowerCount + c.SecondPowerCount ? PowerRank.Secondary
                     : i < c.GreatPowerCount + c.SecondPowerCount + c.MiddlePowerCount ? PowerRank.Middle : PowerRank.Minor;
            Assert.Equal(want, sim.Diplomacy.RankOf(order[i]));
            if (atBuild[order[i].Id] != want) diffFromBuild++;
        }
        Assert.True(diffFromBuild > 0, "助走の前後でランクが 1 つも動かない＝この世界では取り直しの効きを見分けられない");
    }

    // ================= ④ 条約港の風化の基準 =================

    [Fact]
    public void 条約港の風化は列強の中央値を基準にする()
    {
        // 事前登録 sp_20261020 [1] D4：比の基準＝列強の強さの中央値（上側の中央・添字 n÷2）。最大のままなら首位の 1 国が飛び抜けて他の比が沈む。
        var sim = new MultiNationSim(WorldData.Nations(), 1789UL, _ => new NullAgent());
        sim.StartWarmup();   // 助走の直後（流れの項が出そろった盤面・開幕のランクの取り直しもここ）
        var d = sim.Diplomacy;
        var sc = NationalScore.ScoresByPower(d);   // 世界の総量に対する割合の国力
        var greatS = d.Powers.Where(p => p.Nation != null && d.RankOf(p) == PowerRank.Great).Select(p => sc[p.Id]).OrderBy(x => x).ToList();
        double median = greatS[greatS.Count / 2], max = greatS[^1];
        Assert.True(max > median * 1.2, "列強の最大と中央値が離れている世界でなければ、基準の違いを見分けられない");
        // 列強でない国のうち、比（÷中央値）が 0.45 未満で最大のものを 1 国選ぶ（比÷0.5 が 1 に張り付かない）
        var pick = d.Powers.Where(p => p.Nation != null && d.RankOf(p) != PowerRank.Great && sc[p.Id] / median < 0.45).OrderByDescending(p => sc[p.Id]).First();
        var region = pick.Nation!.Regions[0];
        region.ForeignRulers = ["測定用"]; region.ForeignShares = [0.8];
        double ratio = sc[pick.Id] / median;
        double expected = 0.8 * (1.0 - Math.Min(1.0, ratio / sim.Coef.TreatyPortLapseRatio.Value) * sim.Coef.DominanceSpeed);
        double withMax = 0.8 * (1.0 - Math.Min(1.0, sc[pick.Id] / max / sim.Coef.TreatyPortLapseRatio.Value) * sim.Coef.DominanceSpeed);
        Assert.True(Math.Abs(expected - withMax) > 1e-6, "最大を基準にした値と中央値を基準にした値が違う盤面");
        d.TreatyPortLapse(sim.Coef, 3);
        Assert.Equal(expected, region.ForeignShares[0], 12);
    }

    // ================= ⑤ 総量の集合は 1 つ =================

    [Fact]
    public void 国力の総量の集合は実在の国だけで_抽象の勢力が盤に居ても動かない()
    {
        // ★(465) 作り直し（旧は ScoresByPower と Scores を同じ集合から呼ぶ同語反復だった＝反証役 hy3〔v〕低#4）。盤に Power.Nation が null の勢力〔抽象的な外国＝大きな軍事力〕を足し、
        //   総量の集合（外交のランク・画面・play・Runner の score／scorebreak が読む NationalScore.NationWorldsOf）が実在の 4 国だけであること・抽象の勢力が総量にも一覧にも混ざらないこと・
        //   国力が独立の式（各国の項の素の量 TermsOf から 100×量÷総量÷使う項の数）と一致することを押さえる。期待は実装の集合の関数から作らない（盤の Powers を直に走査する）。
        var sim = Four();
        sim.Run(2);
        var d = sim.Diplomacy;
        var real = sim.Nations.ConvertAll(n => n.World);
        var abs = new Power { Id = d.Powers.Max(p => p.Id) + 1, Name = "抽象", BaseName = "抽象", Military = 1e6 };   // Nation == null
        d.Powers.Add(abs);
        Assert.Null(abs.Nation);   // 前提：盤に実在でない勢力が居る
        Assert.True(d.Powers.Count > real.Count);

        // 集合：盤の Powers を直に走査した実在の国と同じ順序・同じ顔ぶれ
        var expectedSet = new List<World>();
        foreach (var p in d.Powers) if (p.Nation != null) expectedSet.Add(p.Nation);
        Assert.Equal(4, expectedSet.Count);
        var got = NationalScore.NationWorldsOf(real[0]);
        Assert.Equal(expectedSet.Count, got.Count);
        for (int i = 0; i < got.Count; i++) Assert.Same(expectedSet[i], got[i]);

        // 独立の式
        var terms = expectedSet.Select(NationalScore.TermsOf).ToArray();
        var tot = new double[6];
        foreach (var t in terms) for (int k = 0; k < 6; k++) tot[k] += t[k];
        int used = tot.Count(x => x > 0);
        Assert.True(used > 0);
        var byPower = NationalScore.ScoresByPower(d);
        var bdByPower = NationalScore.BreakdownsByPower(d);
        Assert.DoesNotContain(abs.Id, byPower.Keys);
        Assert.DoesNotContain(abs.Id, bdByPower.Keys);
        Assert.Equal(expectedSet.Count, byPower.Count);
        double sum = 0;
        for (int i = 0; i < expectedSet.Count; i++)
        {
            double want = 0; for (int k = 0; k < 6; k++) want += 100.0 * terms[i][k] / tot[k] / used;
            int id = expectedSet[i].SelfPower!.Id;
            Assert.Equal(want, byPower[id], 9);
            Assert.Equal(want, NationalScore.Of(expectedSet[i]), 9);
            Assert.Equal(want, bdByPower[id].Sum(l => l.Points), 9);
            Assert.Contains($"合計 {want:F1}", PlayText.ScoreBreakdownText(expectedSet[i]));
            sum += want;
        }
        Assert.Equal(100.0, sum, 6);   // 全国の合計は 100（総量が正の項だけを均等に平均するため）
        // 盤に居ない世界（単独の試験・較正）は自分だけを総量にする＝盤の国は混ざらない
        var lone = new World(new List<Region>(), new Coefficients());
        var loneSet = NationalScore.NationWorldsOf(lone);
        Assert.Single(loneSet);
        Assert.Same(lone, loneSet[0]);
    }
}
