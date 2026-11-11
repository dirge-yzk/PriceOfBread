using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(821) 線 c65md：旗 <see cref="Coefficients.ForcedPeaceBlankOverCollapse"/>（列強の調停の戦争は崩壊の上書きを通らず白紙で終える）の番人＝機構と不変条件。
/// 盤面＝<see cref="CollapsePeaceTests"/> と同じ置き方（敗者イギリスが厭戦の崩壊の線・1 本目はフランスと〔イギリスは B〕・2 本目はスペインと〔イギリスは A〕）。
/// 期待値は賠償の式のリテラル：ReparationGdpFraction（0.2）×敗者の GdpPerMonth×戦勝点 1（崩壊の満額を [-1,1] に切った値）×支払い率 r（本体の式も計器の式も呼ばない）。
/// ⚠突然変異〈歩の分けを外す〉〈調停の戦争の崩壊を満額で上書きする〉〈調停の戦争を持つ敗者の戦争を前走査で全て数えない〉〈分けを調停でない戦争にも当てる〉で赤の見込み（measurements/m821_20261104_mutation.txt）。</summary>
[Collection("Instruments")]
public class ForcedPeaceBlankTests
{
    private static (MultiNationSim sim, Power fr, Power uk, Power es, War w1, War? w2) Board(double flag, bool forced1 = true, bool second = false)
    {
        var sim = new MultiNationSim(System.Array.ConvertAll(new[] { "フランス", "イギリス", "スペイン" }, OpeningFixture.Named), seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };
        var c = sim.Coef;
        c.ForcedPeaceBlankOverCollapse = flag;
        c.CollapsePeaceOneShare = 1;
        c.WarLuck = 0; c.TrenchDefenseWeight = 0; c.SeaInvasionControlMin = 0;
        c.TreatyPortWinnerDistanceKm = 0;          // 条約港の倍率（遠い勝者）を外す
        c.ConcertReparationRestraint = 0;          // 会議の削りを外す
        c.ColonyCaptureBudget = 0;                 // 植民地の奪取は見ない
        c.TerritoryCessionScore = 1e9;             // 本国の割譲は見ない
        var fr = sim.Nations[0].World.SelfPower!; var uk = sim.Nations[1].World.SelfPower!; var es = sim.Nations[2].World.SelfPower!;
        foreach (var p in new[] { fr, uk, es }) p.WarExhaustion = 0;
        uk.WarExhaustion = 1.0;                    // 敗者は崩壊（閾値以上）
        uk.Nation!.FiscalFundingRatio = new Share(0.8);
        var w1 = new War { A = fr, B = uk, PushA = 3e12, PushB = 1, ForcedPeace = forced1, Score = 0 };   // 調停＝戦勝点 0 で立てる（ExecuteMediation と同じ置き方）
        sim.Diplomacy.Wars.Add(w1);
        War? w2 = null;
        if (second) { w2 = new War { A = uk, B = es, PushA = 1, PushB = 1e12 }; sim.Diplomacy.Wars.Add(w2); }
        return (sim, fr, uk, es, w1, w2);
    }

    private static double Led(Power p) => p.Nation!.TreasuryLedger[(int)TreasuryCat.WarCost];
    private static void Close(double want, double got) => Assert.True(System.Math.Abs(want - got) <= 1e-9 * System.Math.Max(1.0, System.Math.Abs(want)), $"期待 {want:G17}・実 {got:G17}");
    private static void Step(MultiNationSim sim) => sim.Diplomacy.Step(sim.Coef, sim.Rng, month: 61);   // 四半期の AI・調停が走らない月

    [Fact]
    public void 旗0は調停の戦争も崩壊で上書きされ賠償を払う()
    {
        var (sim, fr, uk, _, w1, _) = Board(0);
        double f0 = Led(fr);
        Step(sim);
        Assert.Empty(sim.Diplomacy.Wars);
        Assert.True(System.Math.Abs(w1.Score) >= 1.0, $"旗 0 で調停の戦争が崩壊で上書きされていない（{w1.Score:G6}）");
        Assert.True(uk.GdpPerMonth.Value > 0, "前提が崩れた：敗者の GdpPerMonth が 0");
        Close(0.2 * uk.GdpPerMonth.Value * uk.Nation!.FiscalFundingRatio.Value, Led(fr) - f0);
    }

    [Fact]
    public void 旗1は調停の戦争を白紙で終え賠償も勝敗も動かない()
    {
        var (sim, fr, uk, _, w1, _) = Board(1);
        double f0 = Led(fr), u0 = Led(uk); int lost0 = uk.WarsLost, won0 = fr.WarsWon;
        Step(sim);
        Assert.Empty(sim.Diplomacy.Wars);   // 調停の戦争はその月に講和する
        Assert.True(System.Math.Abs(w1.Score) < sim.Coef.WhitePeaceScore, $"調停の戦争の戦勝点が白紙の線を超えた（{w1.Score:G6}）");
        Assert.Equal(0.0, Led(fr) - f0);
        Assert.Equal(0.0, Led(uk) - u0);
        Assert.Equal(lost0, uk.WarsLost);
        Assert.Equal(won0, fr.WarsWon);
    }

    [Fact]
    public void 旗1で同じ敗者の調停でない崩壊の戦争は単独の満額を払う()
    {
        var (sim, fr, uk, es, w1, w2) = Board(1, second: true);
        double f0 = Led(fr), e0 = Led(es);
        Step(sim);
        Assert.Empty(sim.Diplomacy.Wars);
        Assert.True(System.Math.Abs(w2!.Score) >= 1.0, $"調停でない戦争の崩壊の上書きが消えた（{w2.Score:G6}）");
        Assert.True(System.Math.Abs(w1.Score) < sim.Coef.WhitePeaceScore, $"調停の戦争の戦勝点が白紙の線を超えた（{w1.Score:G6}）");
        Assert.Equal(0.0, Led(fr) - f0);
        Close(0.2 * uk.GdpPerMonth.Value * uk.Nation!.FiscalFundingRatio.Value, Led(es) - e0);   // 集合は 1 本＝単独の満額（取り分で割らない）
    }

    [Fact]
    public void 旗1で調停の戦争があっても同じ敗者の調停でない崩壊の戦争2本は1回分を分ける()
    {
        // 4 国：調停の戦争 フランス×イギリス・調停でない崩壊の戦争 イギリス×スペイン・ロシア×イギリス。イギリスが 3 本とも崩壊の線。
        // 調停の戦争は白紙＝集合に入らない・調停でない 2 本は (791) の集合で 1 回分（取り分の和 1）＝勝者の受け取りの和は単独の満額 1 本分。
        var sim = new MultiNationSim(System.Array.ConvertAll(new[] { "フランス", "イギリス", "スペイン", "ロシア" }, OpeningFixture.Named), seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };
        var c = sim.Coef;
        c.ForcedPeaceBlankOverCollapse = 1; c.CollapsePeaceOneShare = 1;
        c.WarLuck = 0; c.TrenchDefenseWeight = 0; c.SeaInvasionControlMin = 0;
        c.TreatyPortWinnerDistanceKm = 0; c.ConcertReparationRestraint = 0; c.ColonyCaptureBudget = 0; c.TerritoryCessionScore = 1e9;
        var fr = sim.Nations[0].World.SelfPower!; var uk = sim.Nations[1].World.SelfPower!; var es = sim.Nations[2].World.SelfPower!; var ru = sim.Nations[3].World.SelfPower!;
        foreach (var p in new[] { fr, uk, es, ru }) p.WarExhaustion = 0;
        uk.WarExhaustion = 1.0;
        uk.Nation!.FiscalFundingRatio = new Share(0.8);
        var w1 = new War { A = fr, B = uk, PushA = 3e12, PushB = 1, ForcedPeace = true, Score = 0 };
        var w2 = new War { A = uk, B = es, PushA = 1, PushB = 1e12 };
        var w3 = new War { A = ru, B = uk, PushA = 2e12, PushB = 1 };
        sim.Diplomacy.Wars.Add(w1); sim.Diplomacy.Wars.Add(w2); sim.Diplomacy.Wars.Add(w3);
        double f0 = Led(fr), e0 = Led(es), r0 = Led(ru);
        Step(sim);
        Assert.Empty(sim.Diplomacy.Wars);
        Assert.True(System.Math.Abs(w2.Score) >= 1.0 && System.Math.Abs(w3.Score) >= 1.0, $"前提が崩れた：調停でない戦争が崩壊の満額でない（{w2.Score:G6}/{w3.Score:G6}）");
        Assert.Equal(0.0, Led(fr) - f0);
        double ge = Led(es) - e0, gr = Led(ru) - r0;
        Assert.True(ge > 0 && gr > 0, $"取り分が 0 の勝者が出た（{ge:G6}/{gr:G6}）");
        Close(0.2 * uk.GdpPerMonth.Value * uk.Nation!.FiscalFundingRatio.Value, ge + gr);   // 1 回分（2 本で 2 倍にならない）
    }

    [Fact]
    public void 旗1は人間の勝者でもAIの勝者でも同じ()
    {
        var a = Board(1, second: true);
        double af0 = Led(a.fr), ae0 = Led(a.es);
        Step(a.sim);
        var b = Board(1, second: true);
        b.fr.IsPlayer = true;
        double bf0 = Led(b.fr), be0 = Led(b.es);
        Step(b.sim);
        Assert.Equal(Led(a.fr) - af0, Led(b.fr) - bf0);
        Assert.Equal(Led(a.es) - ae0, Led(b.es) - be0);
        Assert.Equal(a.w1.Score, b.w1.Score);
    }

    [Fact]
    public void 旗1でも調停でない崩壊の戦争の上書きと賠償は旗0と同じ()
    {
        double Run(double flag, out double score)
        {
            var (sim, fr, _, _, w1, _) = Board(flag, forced1: false);
            double f0 = Led(fr);
            Step(sim);
            Assert.Empty(sim.Diplomacy.Wars);
            score = w1.Score;
            return Led(fr) - f0;
        }
        double g0 = Run(0, out double s0), g1 = Run(1, out double s1);
        Assert.True(System.Math.Abs(s0) >= 1.0, $"前提が崩れた：調停でない戦争が崩壊で上書きされていない（{s0:G6}）");
        Assert.Equal(s0, s1);
        Assert.Equal(g0, g1);
        Assert.True(g0 > 0, "前提が崩れた：賠償が動いていない");
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    public void 計器cpmed_sは調停の戦争の講和を1行に書き旗1は移った額が0で反実仮想が旗0の払いに一致する(double flag)
    {
        var (sim, fr, uk, _, _, _) = Board(flag);
        var dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_cpmed_" + System.Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        try
        {
            Step(sim);
            Assert.Empty(sim.Diplomacy.Wars);
            var rows = Instruments.CmRowsPending(sim.Diplomacy);
            Assert.Single(rows);
            var h = Instruments.CpmedHeaderText.Split('\t');
            var r = rows[0].Split('\t');
            Assert.Equal(h.Length, r.Length);
            string Col(string n) { int i = System.Array.IndexOf(h, n); Assert.True(i >= 0, "列が無い " + n); return r[i]; }
            double Num(string n) => double.Parse(Col(n), System.Globalization.CultureInfo.InvariantCulture);
            double pay = 0.2 * uk.GdpPerMonth.Value * uk.Nation!.FiscalFundingRatio.Value;   // 旧の規則の満額（戦勝点 1）
            Assert.Equal("1", Col("forcedFlag"));
            Assert.Equal("0", Col("civilFlag"));
            Assert.Equal("1", Col("collapsedIterFlag"));
            Close(pay, Num("repWouldOldIndex"));    // 反実仮想は旗に依らない
            if (flag > 0)
            {
                Assert.Equal("0", Col("hitFlag"));
                Assert.Equal("1", Col("blankFlag"));
                Assert.Equal("0", Col("overwriteMatchFlag"));
                Assert.Equal(0.0, Num("repMovedIndex"));
                Assert.Equal(0.0, Num("ledgerAbsIndex"));
            }
            else
            {
                Assert.Equal("1", Col("hitFlag"));
                Assert.Equal("0", Col("blankFlag"));
                Assert.Equal("1", Col("overwriteMatchFlag"));
                Close(pay, Num("repMovedIndex"));
                Assert.True(Num("ledgerAbsIndex") > 0, "台帳が動いていない");
            }
        }
        finally { Instruments.Configure(null, null); }
    }
}
