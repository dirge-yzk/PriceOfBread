using System;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(850) c69po 州ごとの治安 P1-1 の番人（事前登録 measurements/m850_20261105_prereg_po.txt の [7]）。
/// 旗 RuleOrderByRegion：0＝無統治の目標は国の秩序 Clamp01(w.PublicOrder)（今の式）・1＝州の秩序 order_r＝Clamp01(R×m̄÷m_r)（R＝今の Coercion の切る前の被覆）。
/// 機構の番人（向き・対照・旗 0 の不変）であって、創発の主張（税の届き・分離の数）は成績表と計器 secession_s・rule_s が見る。期待値は手で書いた数か、入力（闘争度・規模）の算術から作る（本体の関数から作らない）。</summary>
public class RuleOrderP1Tests
{
    private const double Reach = 0.93813;   // マルセイユ（パリから大圏 659.5km）の到達＝RuleSharesFormTests の D2 と同じ別の道具の値

    private static World France(double flag)
    {
        var w = new Simulation(OpeningFixture.Named("フランス").ToScenario(1789), new NullAgent()).World;
        w.Coef.RuleOrderByRegion = flag;
        w.Coef.RuleFromPower = 0;   // (1117) 旗 0 の経路の番人（★(832) ① の (a)・段 C′ の旗 1 では P1 の塊を通らない）
        foreach (var r in w.Regions) { r.Rule = [1.0, 0.0, 0.0, 0.0]; r.RuleInit = [1.0, 0.0, 0.0, 0.0]; }
        return w;
    }

    /// <summary>R（切る前の被覆）を置く＝Coercion×(総人口÷max(1, 基準人口))÷(総人口×CrimeOrderPerPop)＝Coercion÷(max(1, 基準人口)×CrimeOrderPerPop)。</summary>
    private static void SetRaw(World w, double raw) => w.Coercion = raw * Math.Max(1.0, w.Coef.PoliticalReferencePop) * w.Coef.CrimeOrderPerPop;

    private static void SetMilitancy(World w, Func<Region, double> m)
    {
        foreach (var r in w.Regions) foreach (var p in r.Pops) p.Militancy = m(r);
    }

    private static void Settle(World w, int quarters)
    {
        for (int i = 0; i < quarters; i++) Politics.CheckSecession(w, w.Coef, 0.0);
    }

    [Fact]
    public void P1_0_旗0は闘争度と今のCoercionを読まず国の秩序だけで無統治の目標を作る()
    {
        // 同じ盤面で、闘争度と Coercion だけを大きく変えた 2 つの世界が、旗 0 ではビットで同じ区分になる。
        var a = France(0.0); var b = France(0.0);
        a.PublicOrder = 0.5; b.PublicOrder = 0.5;
        SetRaw(a, 0.3); SetRaw(b, 5.0);
        SetMilitancy(a, r => r.Name == "マルセイユ" ? 0.8 : 0.01);
        SetMilitancy(b, r => 0.2);
        Settle(a, 40); Settle(b, 40);
        for (int i = 0; i < a.Regions.Count; i++)
            for (int k = 0; k < 4; k++)
                Assert.Equal(BitConverter.DoubleToInt64Bits(b.Regions[i].Rule[k]), BitConverter.DoubleToInt64Bits(a.Regions[i].Rule[k]));
        var mars = a.Regions.Single(r => r.Name == "マルセイユ");
        Settle(a, 360);
        Assert.Equal(0.5 * (1.0 - Reach), mars.Rule[3], 4);   // 旗 0 の目標＝(1−0.5)×(1−到達)
    }

    [Fact]
    public void P1_1_闘争度がどの州も同じなら州の秩序はRで旗1の目標は秩序Rの旧の式と同じ()
    {
        // m̄＝m_r（どの州も同じ闘争度）⇒ order_r＝Clamp01(R)。R＝0.5 なら旗 0 で w.PublicOrder＝0.5 と置いた D2 の値と同じ 0.5×(1−0.93813)。
        var w = France(1.0);
        w.PublicOrder = 1.0;   // 旗 1 は読まない（読めば無統治の目標は 0）
        SetRaw(w, 0.5);
        SetMilitancy(w, _ => 0.1);
        Settle(w, 400);
        var mars = w.Regions.Single(r => r.Name == "マルセイユ");
        Assert.Equal(0.5 * (1.0 - Reach), mars.Rule[3], 4);   // 0.030935
        Assert.Equal(1.0 - 0.5 * (1.0 - Reach), mars.Rule[0], 4);
    }

    [Fact]
    public void P1_2_警察が十分な州は旗1でも目標が旧と同じでビットで同じ区分になる()
    {
        // R＝10 で、どの州も m_r≦4×m̄ に収まる闘争度（0.01〜0.05）＝R×m̄÷m_r≧1＝order_r＝1。旧（w.PublicOrder＝1）も無統治 0。
        // 切った R（Clamp01(R)＝1）を使う誤り・order×(1−m_r) の誤り・Clamp01 を外す誤りなら、闘争度の高い州で目標が 0 でなくなる。
        var old = France(0.0); var neu = France(1.0);
        foreach (var w in new[] { old, neu })
        {
            w.PublicOrder = 1.0; SetRaw(w, 10.0);
            int i = 0; foreach (var r in w.Regions) { double m = 0.01 + 0.04 * (i++ % 5) / 4.0; foreach (var p in r.Pops) p.Militancy = m; }
        }
        Settle(old, 30); Settle(neu, 30);
        for (int i = 0; i < old.Regions.Count; i++)
            for (int k = 0; k < 4; k++)
                Assert.Equal(BitConverter.DoubleToInt64Bits(old.Regions[i].Rule[k]), BitConverter.DoubleToInt64Bits(neu.Regions[i].Rule[k]));
        Assert.All(neu.Regions, r => Assert.Equal(0.0, r.Rule[3]));
    }

    [Fact]
    public void P1_3_警察が足りないとき闘争度の高い州ほど無統治の目標が大きく低い州は小さい()
    {
        // R＝0.5。基準＝どの州も闘争度 0.1（order_r＝0.5）。マルセイユだけ 0.4 に上げると、マルセイユの order_r＝0.5×m̄÷0.4（m̄＞0.1 でも 0.5 未満）＝無統治が増え、
        // 他の州（0.1）は order_r＝0.5×m̄÷0.1＞0.5＝無統治が減る。m̄ は入力（大衆の規模と闘争度）の算術。
        var eq = France(1.0); var hi = France(1.0);
        foreach (var w in new[] { eq, hi }) { w.PublicOrder = 1.0; SetRaw(w, 0.5); }
        SetMilitancy(eq, _ => 0.1);
        SetMilitancy(hi, r => r.Name == "マルセイユ" ? 0.4 : 0.1);
        Settle(eq, 400); Settle(hi, 400);
        var marsEq = eq.Regions.Single(r => r.Name == "マルセイユ"); var marsHi = hi.Regions.Single(r => r.Name == "マルセイユ");
        Assert.True(marsHi.Rule[3] > marsEq.Rule[3] + 1e-3, $"闘争度の高い州で無統治が増えない {marsEq.Rule[3]} → {marsHi.Rule[3]}");
        double crowd = 0, crowdM = 0;
        foreach (var r in hi.Regions) foreach (var p in r.Pops) if (p.IsCommoner) { crowd += p.Size; if (r.Name == "マルセイユ") crowdM += p.Size; }
        double mbar = (0.1 * (crowd - crowdM) + 0.4 * crowdM) / crowd;
        Assert.Equal((1.0 - Math.Min(1.0, 0.5 * mbar / 0.4)) * (1.0 - Reach), marsHi.Rule[3], 3);
        var lyon = hi.Regions.Single(r => r.Name == "リヨン"); var lyonEq = eq.Regions.Single(r => r.Name == "リヨン");
        Assert.True(lyon.Rule[3] < lyonEq.Rule[3], $"闘争度の低い州で無統治が減らない {lyonEq.Rule[3]} → {lyon.Rule[3]}");
    }

    [Fact]
    public void P1_4_1州の国は旗1でも区分が動かない()
    {
        var w = France(1.0);
        w.Regions.RemoveRange(1, w.Regions.Count - 1);
        var cap = w.Regions[0];
        cap.Rule = [0.7, 0.1, 0.0, 0.2];
        SetRaw(w, 0.1); SetMilitancy(w, _ => 0.9);
        Settle(w, 10);
        Assert.Equal(new[] { 0.7, 0.1, 0.0, 0.2 }, cap.Rule);
    }

    /// <summary>(856) 旗 1 で秩序 0.5 を置く盤面：今の Coercion の被覆 R＝0.5・全ての POP の闘争度 0.1（m̄＝m_r＝order_r＝0.5）。w.PublicOrder は 1（旗 1 は読まない）。</summary>
    private static void HalfOrderFlag1(World w)
    {
        w.PublicOrder = 1.0;
        SetRaw(w, 0.5);
        SetMilitancy(w, _ => 0.1);
    }

    [Fact]
    public void P1_6_旗1でも無統治の目標は秩序と到達で決まり支配度に依らない()
    {
        // (856) RuleSharesFormTests の D2（旗 0 の経路の番人）と同じ主張を旗 1 で。マルセイユの到達 0.93813＝D2 と同じ別の道具の値。
        foreach (double dom in new[] { 1.0, 0.2 })
        {
            var w = France(1.0);
            HalfOrderFlag1(w);
            foreach (var r in w.Regions) r.Dominance = dom;
            Settle(w, 400);
            var mars = w.Regions.Single(r => r.Name == "マルセイユ");
            Assert.Equal(0.5 * (1.0 - 0.93813), mars.Rule[3], 4);   // 0.030935
            Assert.Equal(1.0 - 0.5 * (1.0 - 0.93813), mars.Rule[0], 4);
        }
    }

    [Fact]
    public void P1_7_旗1で1四半期だけ踏むと無統治の区分は到達の目標へ寄り支配度の低さでは増えない()
    {
        // (856) D2 の 1 四半期の主張を旗 1 で：0.025×0.5×(1−0.93813)＝約 0.00077。支配度 0.2 を読む旧の形なら約 0.0098。
        var w = France(1.0);
        HalfOrderFlag1(w);
        foreach (var r in w.Regions) r.Dominance = 0.2;
        Settle(w, 1);
        double ungov = w.Regions.Single(r => r.Name == "マルセイユ").Rule[3];
        Assert.InRange(ungov, 0.0006, 0.0009);
    }
}

/// <summary>(850) secession_s の末尾 10 列の番人：計器が自分で数えた州の秩序・目標と、エンジンが実際に寄せた目標（Rule の前と後から読む）が一致する。</summary>
[Collection("Instruments")]
public class RuleOrderP1InstrumentTests
{
    private static void Cleanup(string dir) { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    public void P1_5_計器の目標はエンジンが寄せた目標と一致し_旗0は旧の目標_旗1は新しい目標(double flag)
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_p1_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        try
        {
            var w = new Simulation(OpeningFixture.Named("フランス").ToScenario(1789), new NullAgent()).World;
            var c = w.Coef;
            c.RuleOrderByRegion = flag;
            c.RuleFromPower = 0;   // (1117) 旗 0 の経路の番人（★(832) ① の (a)）
            w.PublicOrder = 0.7;
            w.Coercion = 0.6 * Math.Max(1.0, c.PoliticalReferencePop) * c.CrimeOrderPerPop;   // R＝0.6
            int i = 0;
            foreach (var r in w.Regions) { r.Rule = [0.8, 0.1, 0.0, 0.1]; double m = 0.02 + 0.3 * (i++ % 4); foreach (var p in r.Pops) p.Militancy = m; }
            var before = w.Regions.ToDictionary(r => r.Name, r => r.Rule[3]);
            Politics.CheckSecession(w, c, 0.0);
            var rows = Instruments.SecRowsPending(w);
            Assert.NotNull(rows);
            var h = Instruments.SecessionHeaderText.Split('\t').Skip(3).ToArray();
            int Ix(string n) { int k = Array.IndexOf(h, n); Assert.True(k >= 0, n); return k; }
            double V(string[] r, string n) => double.Parse(r[Ix(n)], System.Globalization.CultureInfo.InvariantCulture);
            var parsed = rows!.Select(r => r.Split('\t')).ToList();
            Assert.True(parsed.Count >= 4, $"行が少ない {parsed.Count}");
            int checkedRows = 0, moved = 0;
            foreach (var r in parsed)
            {
                Assert.Equal(h.Length, r.Length);
                Assert.Equal(r[Ix("militancyMeanIndex")], r[Ix("militancyRegionOwnIndex")]);   // 同じ瞬間・同じ述語
                Assert.Equal(0.6, V(r, "orderRawNowIndex"), 9);
                Assert.Equal(0.7, V(r, "orderOldIndex"), 9);
                var reg = w.Regions.Single(x => x.Name == r[0]);
                Assert.Equal(before[reg.Name], V(r, "ruleUngovBeforeShare"), 9);
                double tEng = (reg.Rule[3] - before[reg.Name] * (1 - c.DominanceSpeed)) / c.DominanceSpeed;   // エンジンが実際に寄せた目標（外国の区分 0＝正規化は 1）
                double tOld = V(r, "ungovTargetOldShare"), tNew = V(r, "ungovTargetNewShare");
                Assert.Equal(0.3 * (1 - V(r, "reachOwnShare")), tOld, 8);
                Assert.Equal(flag > 0 ? tNew : tOld, tEng, 7);
                if (Math.Abs(tNew - tOld) > 1e-6) moved++;
                checkedRows++;
            }
            Assert.True(checkedRows >= 4);
            Assert.True(moved >= 2, $"旧と新の目標が違う州が少ない（盤面の前提が崩れた）{moved}");
        }
        finally { Cleanup(dir); }
    }
}
