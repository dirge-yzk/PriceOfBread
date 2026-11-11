using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(1038) 線 c98wr：戦争の入口の統合 段1〜3（旗 WarSurplusFromFill・WarTensionDiscountOpen・WarSubmitUnified）の機構の番人。
/// 事前登録 measurements/m1038_20261111_prereg_wr.txt の突然変異 M2〜M7 がここで赤になる。期待は盤面の値と作者の言葉（緊張 50%→要求 75%）から手で置く。</summary>
public class WarEntryUnifyTests
{
    private static MultiNationSim Make()
        => new([WorldData.ByName("イギリス"), WorldData.ByName("フランス"), WorldData.ByName("プロイセン")], 1789UL);

    private static Power P(MultiNationSim sim, string name)
        => sim.Diplomacy.Powers.First(p => p.Nation is not null && (p.BaseName.Length > 0 ? p.BaseName : p.Name) == name);

    private static readonly int[] NonQuarterMonths = [1, 2, 4, 5, 7, 8, 10, 11];

    /// <summary>M2：導く余剰は前線に縛られた割合を引く。交戦中で WarScale が正の国で、S＝陸の大きさ×(1−min(1,比))−必要量（比を外すと赤）。</summary>
    [Fact]
    public void 導く余剰は前線に縛られた割合を引く()
    {
        var sim = Make(); var d = sim.Diplomacy; var c = sim.Coef;
        var fr = P(sim, "フランス"); var pr = P(sim, "プロイセン");
        d.Wars.Add(new War { A = fr, B = pr });
        double r = d.WarScale(fr, c, out _);
        Assert.True(r > 0 && !double.IsNaN(r), $"前提が崩れた：交戦中の前線の比が正でない（{r}）");
        double land = ArmyShape.LandPotential(fr), need = fr.NeedLand + fr.NeedInternal;
        Assert.True(land > 1e-9, "前提が崩れた：陸の大きさが正でない");
        double b = Math.Min(1.0, r);
        Assert.Equal(land * (1 - b) - need, d.InvadeSurplus(fr, c), 12);
        Assert.True(d.InvadeSurplus(fr, c) < land - need, "前線に縛られた分が余剰から引かれていない");
    }

    /// <summary>M3・M4：緊張の割引は 1−τ²・τ＝緊張÷開戦の線（作者「緊張50%の場合…緊張0%の際の75%ぐらい」）。</summary>
    [Fact]
    public void 緊張の割引は一から緊張の比の二乗を引く()
    {
        Assert.Equal(0.75, Diplomacy.TensionDiscount(70, 140), 15);
        Assert.Equal(1.0, Diplomacy.TensionDiscount(0, 140), 15);
        Assert.Equal(1.0, Diplomacy.TensionDiscount(-10, 140), 15);
        Assert.Equal(1.0, Diplomacy.TensionDiscount(double.NaN, 140), 15);
        Assert.Equal(0.0, Diplomacy.TensionDiscount(140, 140), 15);
        Assert.Equal(0.0, Diplomacy.TensionDiscount(200, 140), 15);
        Assert.Equal(1 - 0.25 * 0.25, Diplomacy.TensionDiscount(35, 140), 15);
    }

    /// <summary>M3・M4：旗 WarTensionDiscountOpen の要求は、緊張 70 の組で旗 0 の要求の 0.75 倍。</summary>
    [Fact]
    public void 旗で要求に緊張の割引が掛かる()
    {
        var sim = Make(); var d = sim.Diplomacy; var c = sim.Coef;
        var self = P(sim, "イギリス"); var t = P(sim, "フランス");
        d.SeedTension(self, t, 70);
        Assert.Equal(70.0, d.Tension(self, t), 9);
        c.WarTensionDiscountOpen = 0; double n0 = d.InvadeNeed(self, t, c);
        c.WarTensionDiscountOpen = 1; double n1 = d.InvadeNeed(self, t, c);
        Assert.True(n0 > 0, "前提が崩れた：要求が正でない");
        Assert.Equal(0.75 * n0, n1, 12);
    }

    /// <summary>M5：旗 WarTensionDiscountOpen で〈弱小国が標的〉の門が外れる（旗 0 は残る＝対照）。</summary>
    [Fact]
    public void 旗で列強も侵略の標的になる()
    {
        var sim = Make(); var d = sim.Diplomacy; var c = sim.Coef;
        var w = sim.Nations[0].World; var self = w.SelfPower!; var t = P(sim, "フランス");
        Assert.True(d.RankOf(t) != PowerRank.Minor, "前提が崩れた：標的が弱小国");
        var wc = w.Coef;   // World.Coef は国ごとの別の写し＝門が読むのはこちら
        wc.WarTensionDiscountOpen = 0;
        w.CanInvade(t, out string r0);
        Assert.Equal("弱小国が標的", r0);
        wc.WarTensionDiscountOpen = 1;
        w.CanInvade(t, out string r1);
        Assert.NotEqual("弱小国が標的", r1);
    }

    /// <summary>M6：旗 WarSubmitUnified の侵略の屈服は 1 本の判定（陣営の陸×射程）に従う。旧の規則（陸軍力×射程≧3×軍事ゲージ）と食い違う盤面で確かめる。</summary>
    [Fact]
    public void 旗で侵略の屈服は一本の判定に従う()
    {
        var sim = Make(); sim.StartWarmup(); var d = sim.Diplomacy; var c = sim.Coef;   // 陸の大きさ（兵の頭数×質）は試走の後に立つ
        var me = P(sim, "イギリス");
        var target = d.Powers.Find(p => p.Nation is null && p.ColonyList.Count > 0 && d.RankOf(p) == PowerRank.Minor && d.Suzerain(p) is null);
        Assert.NotNull(target);
        target!.Military = 0.01; c.ColonizeSubmitFloorMilitary = 0.001;   // 標的の陣営の陸が床より上（遠い標的＝射程 約 0.24 でも 1 本の判定で届く盤面）
        me.Military = 1e-9;           // 旧の規則（陸軍力×射程）では届かない
        bool oldRule = d.ColonizeForce(me, target, c) >= d.SubmitLine(target, c);
        Assert.False(oldRule, "前提が崩れた：旧の規則でも屈服する盤面");
        Assert.True(d.SubmitYields(me, target, c), $"前提が崩れた：1 本の判定で屈服しない盤面（英の陸 {ArmyShape.LandPotential(me)}・射程 {d.Projection(me, target, c)}・標的の陣営の陸 {d.CampLand(target, me, c)}）");
        c.WarSubmitUnified = 1;
        Assert.True(d.Subjugate(me, target, c), "旗 WarSubmitUnified なのに侵略の屈服が 1 本の判定に従っていない");
    }

    /// <summary>M6 の対照：旗 0 は旧の規則のまま（同じ盤面で開戦）。</summary>
    [Fact]
    public void 旗ゼロの侵略の屈服は旧の規則のまま()
    {
        var sim = Make(); sim.StartWarmup(); var d = sim.Diplomacy; var c = sim.Coef;   // 陸の大きさ（兵の頭数×質）は試走の後に立つ
        var me = P(sim, "イギリス");
        var target = d.Powers.Find(p => p.Nation is null && p.ColonyList.Count > 0 && d.RankOf(p) == PowerRank.Minor && d.Suzerain(p) is null);
        Assert.NotNull(target);
        target!.Military = 0.01; c.ColonizeSubmitFloorMilitary = 0.001; me.Military = 1e-9;
        Assert.True(d.SubmitYields(me, target, c), "前提が崩れた");
        Assert.False(d.Subjugate(me, target, c), "旗 0 なのに旧の規則（陸軍力×射程≧屈服線）で判定されていない");
    }

    /// <summary>M7：旗 3 本を 1 にしても、緊張が開戦の線を WarTriggerMonths 続けば AI の組は開戦する（屈服しない比の盤面＝AISubmitMilRatio を大きく）。旗 0 が対照。</summary>
    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    public void 緊張が開戦の線を続けば旗に関わらず開戦する(double flag)
    {
        var sim = Make(); var d = sim.Diplomacy; var c = sim.Coef;
        c.WarSurplusFromFill = flag; c.WarTensionDiscountOpen = flag; c.WarSubmitUnified = flag;
        c.AISubmitMilRatio = 1e9;   // どちらの規則でも屈服しない
        var fr = P(sim, "フランス"); var pr = P(sim, "プロイセン");
        Assert.False(d.IsAtWar(fr, pr), "前提が崩れた：開幕から交戦中");
        for (int i = 0; i < c.WarTriggerMonths + 1 && !d.IsAtWar(fr, pr); i++)
        {
            double tt = d.Tension(fr, pr);
            if (tt < 190) d.AddTension(fr, pr, 190 - tt, TensionReason.Threat);
            d.Step(c, sim.Rng, NonQuarterMonths[i % NonQuarterMonths.Length]);
        }
        Assert.True(d.IsAtWar(fr, pr), $"緊張が開戦の線を {c.WarTriggerMonths} か月続いたのに開戦しない（旗 {flag}）");
    }
}
