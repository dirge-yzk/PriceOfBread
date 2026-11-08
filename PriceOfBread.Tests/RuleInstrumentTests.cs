using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(842) 線 c68id：rule_s の末尾の 9 列（段 P1-0＝州ごとの治安の形 P1 の恒等の計器・事前登録 measurements/m842_20261105_prereg_id.txt の T4〜T7）の番人。
/// 期待は計器と別の出所から作る：m_r・m̄ は Politics.RegionMilitancy（エンジンの関数）を試験が足した値・R は PoliticalQuantities.EffectiveCoerce（エンジンの関数）と CrimeOrderPerPop から・
/// 遅れた R は publicOrderIndex の列（エンジンが Crime.Step で書いた w.PublicOrder）。
/// 突然変異（使い捨ての写し・measurements/m842_20261105/mutation.txt）：M3 m̄ から首都を外す→T5 が赤・M4 遅れた R に今の R を入れる→T7 が赤。</summary>
[Collection("Instruments")]
public class RuleInstrumentTests
{
    private static readonly string[] P1Cols = ["orderRawIndex", "militancyRegionIndex", "militancyNationIndex", "orderRegionIndex", "ungovTargetOldShare", "ungovTargetNewShare", "orderRawLagIndex", "orderRegionLagIndex", "ungovTargetNewLagShare"];

    private static double D(Dictionary<string, string> r, string c) => SubstInstrumentTests.D(r, c);

    private static double C01(double x) => !(x > 0) ? 0 : x > 1 ? 1 : x;

    private static List<Dictionary<string, string>> Read(string path)
    {
        var lines = System.IO.File.ReadAllLines(path);
        Assert.Equal(Instruments.RuleHeaderText, lines[0]);
        var h = lines[0].Split('\t');
        var res = new List<Dictionary<string, string>>();
        foreach (var l in lines.Skip(1))
        {
            var c = l.Split('\t');
            Assert.Equal(h.Length, c.Length);
            var d = new Dictionary<string, string>();
            for (int i = 0; i < h.Length; i++) d[h[i]] = c[i];
            res.Add(d);
        }
        return res;
    }

    /// <summary>英仏を n 四半期回し（挙げた国は全て＝毎四半期の行）、rule_s の行と最後の四半期の世界を返す。</summary>
    private static (List<Dictionary<string, string>> Rows, MultiNationSim Sim, int LastQ) Run(ulong seed, int quarters, string nations = "*")
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_rulep1_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, nations);
        var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: seed) { DiplomacyFrozen = true };
        try { for (int q = 0; q < quarters; q++) { sim.StepQuarter(); Instruments.Quarter(sim, seed, q); } }
        finally { Instruments.Close(sim); Instruments.Configure(null, null); }
        try { return (Read(System.IO.Path.Combine(dir, $"rule_s{seed}.tsv")), sim, quarters - 1); }
        finally { try { System.IO.Directory.Delete(dir, true); } catch { } }
    }

    /// <summary>T4：見出しの末尾が 9 列（順序どおり）・全ての行の列の数が見出しと同じ・国の列（m̄）は同じ国×四半期の全ての州の行で同じ値。</summary>
    [Fact]
    public void P1の9列が末尾に順序どおりあり_国の列は州の行で同じ値()
    {
        var (rows, _, _) = Run(1797UL, 3);
        var head = Instruments.RuleHeaderText.Split('\t');
        Assert.Equal(P1Cols, head.TakeLast(9).ToArray());
        Assert.NotEmpty(rows);
        foreach (var g in rows.GroupBy(r => (r["quarter"], r["nation"])))
        {
            Assert.Single(g.Select(r => r["militancyNationIndex"]).Distinct());
            Assert.Single(g.Select(r => r["orderRawIndex"]).Distinct());
        }
    }

    /// <summary>T5：m_r は Politics.RegionMilitancy（平民の人口加重）から、m̄ は首都（Regions[0]）を含む全ての州の和から試験が数えた値に合う。首都を外した m̄ とは別の値＝首都を含むことを確かめる。</summary>
    [Fact]
    public void 州の闘争度と国の平均は首都を含めて試験が数えた値に合う()
    {
        var (rows, sim, lastQ) = Run(1797UL, 3);
        int checkedRows = 0;
        foreach (var n in sim.Nations)
        {
            var w = n.World; string nation = Instruments.NameOf(w);
            double allMil = 0, allSize = 0, noCapMil = 0, noCapSize = 0;
            for (int i = 0; i < w.Regions.Count; i++)
            {
                var (m, s) = Politics.RegionMilitancy(w.Regions[i]);
                allMil += m; allSize += s;
                if (i > 0) { noCapMil += m; noCapSize += s; }
            }
            double mBar = allMil / allSize, mBarNoCap = noCapMil / noCapSize;
            Assert.True(Math.Abs(mBar - mBarNoCap) > 1e-12, "前提が崩れた：首都を外しても m̄ が同じ＝首都を含むかを区別できない盤面");
            foreach (var r in rows.Where(r => r["quarter"] == lastQ.ToString() && r["nation"] == nation))
            {
                var reg = w.Regions.Single(x => x.Name == r["region"]);
                var (m, s) = Politics.RegionMilitancy(reg);
                double want = s > 1e-9 ? m / s : double.NaN;
                double got = D(r, "militancyRegionIndex");
                Assert.True(double.IsNaN(want) ? double.IsNaN(got) : Math.Abs(got - want) <= 1e-9 * Math.Max(1e-9, Math.Abs(want)), $"m_r が違う {nation} {reg.Name}：{got} ≠ {want}");
                Assert.True(Math.Abs(D(r, "militancyNationIndex") - mBar) <= 1e-9 * Math.Max(1e-9, mBar), $"m̄ が違う {nation}：{D(r, "militancyNationIndex")} ≠ {mBar}（首都を外した値 {mBarNoCap}）");
                checkedRows++;
            }
        }
        Assert.True(checkedRows > 0);
        Assert.True(checkedRows >= sim.Nations.Sum(n => n.World.Regions.Count) / 2, $"対照：突き合わせた州の行が少ない（{checkedRows}／州 {sim.Nations.Sum(n => n.World.Regions.Count)}）＝最後の四半期の行が世界の州の半分に届かない");   // (969) 対照の件数
    }

    /// <summary>T6：R・order_r・無統治の目標の恒等式。R は EffectiveCoerce と CrimeOrderPerPop から、order_r は R×m̄÷m_r の [0,1] クランプ、目標は (1−秩序)×(1−到達)。新−旧＝(旧の秩序−order_r)×(1−到達)。</summary>
    [Fact]
    public void 切る前のRと州の秩序と無統治の目標は恒等式どおり()
    {
        var (rows, sim, lastQ) = Run(1797UL, 3);
        int checkedRows = 0;
        foreach (var n in sim.Nations)
        {
            var w = n.World; var c = w.Coef; string nation = Instruments.NameOf(w);
            double pop = w.TotalPopulation;
            double rWant = PoliticalQuantities.EffectiveCoerce(w, c) / Math.Max(1e-9, pop * c.CrimeOrderPerPop);
            double po = Math.Clamp(w.PublicOrder, 0, 1);
            foreach (var r in rows.Where(r => r["quarter"] == lastQ.ToString() && r["nation"] == nation))
            {
                Assert.True(Math.Abs(D(r, "orderRawIndex") - rWant) <= 1e-9 * Math.Max(1, rWant), $"R が違う {nation}：{D(r, "orderRawIndex")} ≠ {rWant}");
                double mReg = D(r, "militancyRegionIndex"), mBar = D(r, "militancyNationIndex"), reach = D(r, "reachShare");
                double orderWant = !(mReg > 0) ? 1.0 : C01(rWant * mBar / mReg);
                Assert.True(Math.Abs(D(r, "orderRegionIndex") - orderWant) <= 1e-9, $"order_r が違う {nation} {r["region"]}");
                var reg = w.Regions.Single(x => x.Name == r["region"]);
                if (reg.Rule.Length != 4) { Assert.True(double.IsNaN(D(r, "ungovTargetOldShare")) && double.IsNaN(D(r, "ungovTargetNewShare"))); continue; }
                Assert.True(Math.Abs(D(r, "ungovTargetOldShare") - (1.0 - po) * (1.0 - reach)) <= 1e-9, $"旧の目標が違う {nation} {r["region"]}");
                Assert.True(Math.Abs(D(r, "ungovTargetNewShare") - (1.0 - orderWant) * (1.0 - reach)) <= 1e-9, $"新の目標が違う {nation} {r["region"]}");
                Assert.True(Math.Abs((D(r, "ungovTargetNewShare") - D(r, "ungovTargetOldShare")) - (po - orderWant) * (1.0 - reach)) <= 1e-9, $"新−旧が (旧の秩序−order_r)×(1−到達) でない {nation} {r["region"]}");
                checkedRows++;
            }
        }
        Assert.True(checkedRows > 0);
        Assert.True(checkedRows >= sim.Nations.Sum(n => n.World.Regions.Count) / 2, $"対照：恒等式を突き合わせた州の行が少ない（{checkedRows}／州 {sim.Nations.Sum(n => n.World.Regions.Count)}）");   // (969) 対照の件数
    }

    /// <summary>T7：遅れた R。最初の四半期は nan。以後、publicOrderIndex が 1 未満の行で orderRawLagIndex が publicOrderIndex に一致する（Crime.Step が前の四半期の Coercion で書いた w.PublicOrder）。
    /// 今の R orderRawIndex とは一致しない行がある＝遅れが無いと区別できない盤面でないこと（突然変異 M4＝遅れた R に今の R を入れる）を確かめる。同じ四半期の一致の件数を数えて申告する。</summary>
    [Fact]
    public void 遅れたRは1四半期前の末のRで_公開秩序が1未満の行ではその値に一致する()
    {
        var (rows, _, _) = Run(1797UL, 4);
        Assert.All(rows.Where(r => r["quarter"] == "0"), r => Assert.True(double.IsNaN(D(r, "orderRawLagIndex")) && double.IsNaN(D(r, "orderRegionLagIndex")) && double.IsNaN(D(r, "ungovTargetNewLagShare"))));
        int lagMatch = 0, nowMatch = 0, below = 0;
        foreach (var g in rows.Where(r => r["quarter"] != "0").GroupBy(r => (r["quarter"], r["nation"])))
        {
            var r = g.First();
            double po = D(r, "publicOrderIndex");
            if (!(po < 1 - 1e-9)) continue;
            below++;
            if (Math.Abs(D(r, "orderRawLagIndex") - po) <= 1e-9 * Math.Max(1, po)) lagMatch++;
            if (Math.Abs(D(r, "orderRawIndex") - po) <= 1e-9 * Math.Max(1, po)) nowMatch++;
        }
        Assert.True(below > 0, "前提が崩れた：公開秩序が 1 未満の国×四半期が無い");
        Assert.Equal(below, lagMatch);
        Assert.True(nowMatch < below, $"前提が崩れた：今の R がどの行でも公開秩序に一致する＝遅れの有無を区別できない（今の一致 {nowMatch}/{below}）");
    }

    /// <summary>(846) T8：遅れた R は挙げていない国でも毎四半期更新する（InstrumentsRule.cs の RuleFold の頭）。1 国だけ挙げると、もう一方は年末の四半期（q＝3・7）にしか行を出さない。
    /// その国の年末の行の orderRawLagIndex は 1 四半期前（q＝2・6）の末の R＝publicOrderIndex（＜1 の行）に一致する。`lagSt.Prev = rNow;` を早い return の後へ動かすと、挙げていない国の Prev は行を出した四半期でしか更新されず、
    /// q＝3 では nan・q＝7 では q＝3 の末の R になって一致しない（T7 は "*" で毎四半期の行が出る＝この穴を見ない・(842) の反証役 ③）。</summary>
    [Fact]
    public void 挙げていない国の遅れたRも毎四半期更新される_年末の行が1四半期前のRに一致する()
    {
        var (rows, _, _) = Run(1797UL, 8, "イギリス");
        var france = rows.Where(r => r["nation"] == "フランス").ToList();
        Assert.NotEmpty(france);
        Assert.All(france, r => Assert.True(r["quarter"] == "3" || r["quarter"] == "7", $"前提が崩れた：挙げていない国が年末以外の四半期（{r["quarter"]}）に行を出した"));
        int below = 0;
        foreach (var g in france.GroupBy(r => r["quarter"]))
        {
            var r = g.First();
            double po = D(r, "publicOrderIndex");
            if (!(po < 1 - 1e-9)) continue;
            below++;
            Assert.True(Math.Abs(D(r, "orderRawLagIndex") - po) <= 1e-9 * Math.Max(1, po), $"遅れた R が 1 四半期前の末の R でない（q={r["quarter"]}：{D(r, "orderRawLagIndex")} ≠ 公開秩序 {po}）");
        }
        Assert.True(below > 0, "前提が崩れた：挙げていない国の年末の行に公開秩序が 1 未満の行が無い");
    }
}
