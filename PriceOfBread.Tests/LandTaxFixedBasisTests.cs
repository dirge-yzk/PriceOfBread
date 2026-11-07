using System.IO;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(937) 線 c82lt：土地税の算定の基準 A-2（旗 <see cref="Coefficients.LandTaxFixedBasis"/>・データ world.json の landTaxBasis＝"Fixed"・
/// 凍結の欄 <see cref="Producer.FrozenLandRentPerMonth"/>・差分 <see cref="Pop.LandTaxBasisAdjPerMonth"/>・読み手 <see cref="Fiscal.LandTaxBase"/>）の機構の番人。
/// 事前登録 measurements/m937_20261107_prereg_lt.txt の T1〜T11。期待は試験が手で置いた地代（1000・600・0）と計器の割り方の写し（<see cref="Instruments.LsRecipient"/>）から作る（エンジンの割り方を呼ばない）。
/// 試験の世界は構築の直後（1783 年の第 1 四半期・助走なし＝凍結の月）。旗は試験の世界の Coef だけ 1（World.Coef は国ごとの別のインスタンス）。</summary>
public class LandTaxFixedBasisTests
{
    private static MultiNationSim Sim(params string[] names)
        => new([.. names.Select(WorldData.ByName)], seed: 1783UL) { DiplomacyFrozen = true };

    private static (World W, Region R, Producer S) Rgo(MultiNationSim sim, int nation, bool colonial = false, bool nobleOwner = false)
    {
        var w = sim.Nations[nation].World;
        foreach (var r in w.Regions)
        {
            if (r.Id == 0 || (r.Tier == RegionTier.Colonial) != colonial) continue;
            foreach (var s in r.Producers)
                if (s.IsRgo && s.OwnerPopIndex >= 0 && s.OwnerPopIndex < r.Pops.Count && Colonial.AbsenteeRgo(r, s) == colonial
                    && (!nobleOwner || r.Pops[s.OwnerPopIndex].ClassId == PopClass.Noble))
                    return (w, r, s);
        }
        throw new Xunit.Sdk.XunitException("試験の前提：条件に合う RGO が無い");
    }

    private static void Zero(Region r)
    {
        foreach (var p in r.Pops) { p.LandRentPerMonth = new MoneyPerMonth(0); p.LandTaxBasisAdjPerMonth = new MoneyPerMonth(0); p.EarnedPerMonth = new MoneyPerMonth(0); }
    }

    /// <summary>計器の割り方の写しで、受け手 <paramref name="pop"/> の持ち分の和（null の欄は持ち主に 1）。</summary>
    private static double ShareOf(Region r, Producer s, int pop)
    {
        var ls = s.LandShares;
        if (ls is null) return pop == s.OwnerPopIndex ? 1.0 : 0.0;
        double sum = 0; long fb = 0;
        for (int k = 0; k < ls.Length; k++) if (ls[k] > 0 && Instruments.LsRecipient(r, s, k, ref fb) == pop) sum += ls[k];
        return sum;
    }

    private static double Base(Region r) => r.Pops.Sum(p => p.LandRentPerMonth.Value + p.LandTaxBasisAdjPerMonth.Value);

    /// <summary>T1（G7・M1・M2）：凍結の月は割る前の地代そのものを凍結し、差分は全ての受け手で 0（凍結の月は基数＝地代）。</summary>
    [Fact]
    public void T1_凍結の月は地代そのものを凍結し差分は0()
    {
        var (w, r, s) = Rgo(Sim("イギリス"), 0);
        Assert.True(w.LandTaxBasisFixed, "試験の前提：イギリスは world.json で定額の国");
        w.Coef.LandTaxFixedBasis = 1.0;
        Assert.Null(s.FrozenLandRentPerMonth);
        Zero(r);
        EconomyStep.PayOwnerRent(w, r, s, 1000.0, w.Coef);
        Assert.Equal(1000.0, s.FrozenLandRentPerMonth!.Value.Value);
        Assert.All(r.Pops, p => Assert.Equal(0.0, p.LandTaxBasisAdjPerMonth.Value));
        Assert.Equal(1000.0, r.Pops.Sum(p => p.LandRentPerMonth.Value), 9);
    }

    /// <summary>T2（G8a・G8b・M3・M4・M5）：凍結の後の月は凍結の値を書き直さず、受け手ごとの基数＝凍結×持ち分（地代 600 の月に基数の和 1000）。</summary>
    [Fact]
    public void T2_凍結の後は基数が凍結の値と持ち分()
    {
        var (w, r, s) = Rgo(Sim("イギリス"), 0);
        w.Coef.LandTaxFixedBasis = 1.0;
        Zero(r);
        EconomyStep.PayOwnerRent(w, r, s, 1000.0, w.Coef);
        w.Year = Calendar.StartYear + 1;
        Zero(r);
        EconomyStep.PayOwnerRent(w, r, s, 600.0, w.Coef);
        Assert.Equal(1000.0, s.FrozenLandRentPerMonth!.Value.Value);
        Assert.Equal(1000.0, Base(r), 9);
        Assert.Equal(600.0, r.Pops.Sum(p => p.LandRentPerMonth.Value), 9);
        for (int i = 0; i < r.Pops.Count; i++)
        {
            var p = r.Pops[i];
            Assert.Equal(1000.0 * ShareOf(r, s, i), Fiscal.LandTaxBase(w.Coef, p).Value, 9);
            Assert.Equal(600.0 * ShareOf(r, s, i), p.LandRentPerMonth.Value, 9);
        }
    }

    /// <summary>T3（M14＝Fiscal.cs の土地税の分岐を地代で判定する形）：凍結した RGO の地代が 0 の月も、査定は凍結×持ち分×率×privBase を請求する。</summary>
    [Fact]
    public void T3_地代が0の月も定額を請求する()
    {
        var (w, r, s) = Rgo(Sim("イギリス"), 0);
        w.Coef.LandTaxFixedBasis = 1.0;
        Zero(r);
        EconomyStep.PayOwnerRent(w, r, s, 1000.0, w.Coef);
        w.Year = Calendar.StartYear + 1;
        Zero(r);
        EconomyStep.PayOwnerRent(w, r, s, 0.0, w.Coef);
        var owner = r.Pops[s.OwnerPopIndex];
        Assert.Equal(0.0, owner.LandRentPerMonth.Value);
        var a = Fiscal.AssessPop(w, w.Coef, owner, 0.0, null, 0.0, 0.0, 0.0, 0.1);
        double share = ShareOf(r, s, s.OwnerPopIndex);
        Assert.True(share > 0, "試験の前提：持ち主の持ち分が 0");
        Assert.Equal(0.1 * 1000.0 * share * a.PrivBase, a.Land, 9);
    }

    /// <summary>T4（旗 0 の恒等）：旗 0 は凍結しない・差分は 0・基数は地代そのもの（ビットで同じ）。</summary>
    [Fact]
    public void T4_旗0は凍結せず基数は地代()
    {
        var (w, r, s) = Rgo(Sim("イギリス"), 0);
        w.Coef.LandTaxFixedBasis = 0.0;   // 旗 0 の盤面（既定は (937) で 1）
        Zero(r);
        EconomyStep.PayOwnerRent(w, r, s, 1000.0, w.Coef);
        Assert.Null(s.FrozenLandRentPerMonth);
        foreach (var p in r.Pops)
        {
            Assert.Equal(0.0, p.LandTaxBasisAdjPerMonth.Value);
            Assert.Equal(p.LandRentPerMonth, Fiscal.LandTaxBase(w.Coef, p));
        }
    }

    /// <summary>T5（名簿の外）：landTaxBasis の無い国（フランス）は旗 1 でも凍結しない。</summary>
    [Fact]
    public void T5_定額でない国は凍結しない()
    {
        var (w, r, s) = Rgo(Sim("フランス"), 0);
        Assert.False(w.LandTaxBasisFixed);
        w.Coef.LandTaxFixedBasis = 1.0;
        Zero(r);
        EconomyStep.PayOwnerRent(w, r, s, 1000.0, w.Coef);
        Assert.Null(s.FrozenLandRentPerMonth);
        Assert.All(r.Pops, p => Assert.Equal(0.0, p.LandTaxBasisAdjPerMonth.Value));
    }

    /// <summary>T6（M7′＝不在地主の RGO を凍結する形）：植民地格の州の不在地主の RGO は凍結しない（地代の送金が差分を取り残さない）。</summary>
    [Fact]
    public void T6_不在地主のRGOは凍結しない()
    {
        var (w, r, s) = Rgo(Sim("イギリス"), 0, colonial: true);
        w.Coef.LandTaxFixedBasis = 1.0;
        Zero(r);
        EconomyStep.PayOwnerRent(w, r, s, 1000.0, w.Coef);
        Assert.Null(s.FrozenLandRentPerMonth);
    }

    /// <summary>T7（M13・M8 の置き換え）：州の移転（割譲）で凍結は消える（受け手の国では収穫比例）。</summary>
    [Fact]
    public void T7_割譲で凍結は消える()
    {
        var sim = Sim("イギリス", "フランス");
        var (uk, r, s) = Rgo(sim, 0);
        uk.Coef.LandTaxFixedBasis = 1.0;
        EconomyStep.PayOwnerRent(uk, r, s, 1000.0, uk.Coef);
        Assert.NotNull(s.FrozenLandRentPerMonth);
        Territory.TransferRegion(uk, sim.Nations[1].World, r, Territory.Rehoming.Ceded);
        Assert.Null(s.FrozenLandRentPerMonth);
    }

    /// <summary>T8（M9＝予算表の予想を地代のまま）：同じ盤面で旗 1 の予想の土地税は旗 0 より大きい（凍結 1000・地代 600 の RGO が 1 つ＝差は正）。</summary>
    [Fact]
    public void T8_予想も同じ基数を読む()
    {
        var (w, r, s) = Rgo(Sim("イギリス"), 0);
        w.Coef.LandTaxFixedBasis = 1.0;
        EconomyStep.PayOwnerRent(w, r, s, 1000.0, w.Coef);
        w.Year = Calendar.StartYear + 1;
        Zero(r);
        EconomyStep.PayOwnerRent(w, r, s, 600.0, w.Coef);
        double on = Fiscal.ForecastTax(w, w.Coef, 0.0, 0.0, 0.0, 0.1).Land;
        w.Coef.LandTaxFixedBasis = 0.0;
        double off = Fiscal.ForecastTax(w, w.Coef, 0.0, 0.0, 0.0, 0.1).Land;
        Assert.True(on > off + 1e-9, $"予想の土地税 旗1 {on} ≦ 旗0 {off}");
    }

    /// <summary>T9（M10＝特権台帳を地代のまま）：同じ構築の 2 つの清（専制＝身分制免税あり）で、凍結 1000・地代 600 の RGO を置き、特権台帳の免税の記録は旗 1 の方が大きい。</summary>
    [Fact]
    public void T9_特権台帳も同じ基数を読む()
    {
        double Take(double flag)
        {
            var (w, r, s) = Rgo(Sim("清"), 0, nobleOwner: true);
            Assert.True(w.LandTaxBasisFixed, "試験の前提：清は world.json で定額の国");
            w.Coef.PrivilegeLedgerEnabled = 1.0;
            w.Coef.LandTaxFixedBasis = 1.0;
            EconomyStep.PayOwnerRent(w, r, s, 1000.0, w.Coef);
            w.Year = Calendar.StartYear + 1;
            Zero(r);
            EconomyStep.PayOwnerRent(w, r, s, 600.0, w.Coef);
            w.Coef.LandTaxFixedBasis = flag;
            Assert.True(w.LandTax > 1e-9, "試験の前提：清の土地税の率が 0");
            Assert.True(w.FiscalEnabled, "試験の前提：清の財政が無効");
            Assert.True(Privileges.TaxAssessment(w, w.Coef, r.Pops[s.OwnerPopIndex], 0.0) < 1.0, $"試験の前提：清の貴族に身分制免税が無い（政体 {w.Polity}）");
            System.Array.Clear(w.PrivilegeTakeMonth);
            double y0 = w.PrivilegeTakeYear[Privileges.TaxExemption];
            Fiscal.Step(w, w.Coef);   // 月の取り分は Fiscal.Step の末で年の指数平均へ畳まれる（Privileges.StepMonth）＝年の和の増分を読む
            return w.PrivilegeTakeYear[Privileges.TaxExemption] - y0 * (1.0 - 1.0 / Calendar.MonthsPerYear);
        }
        double on = Take(1.0), off = Take(0.0);
        Assert.True(on > off + 1e-9, $"免税の記録 旗1 {on} ≦ 旗0 {off}");
    }

    /// <summary>T10（M12＝選好を地代のまま）：選好の土台の土地税の基数の和は、地代の和より凍結の差（1000−600）だけ大きい。地代の和は変わらない。</summary>
    [Fact]
    public void T10_選好の土地税の基数は凍結の差だけ大きい()
    {
        var (w, r, s) = Rgo(Sim("イギリス"), 0);
        w.Coef.LandTaxFixedBasis = 1.0;
        EconomyStep.PayOwnerRent(w, r, s, 1000.0, w.Coef);
        w.Year = Calendar.StartYear + 1;
        Zero(r);
        EconomyStep.PayOwnerRent(w, r, s, 600.0, w.Coef);
        var on = PolicyPreferences.ComputeBuildSums(w);
        Assert.Equal(400.0, on.Tl - on.Tr, 6);
        w.Coef.LandTaxFixedBasis = 0.0;
        var off = PolicyPreferences.ComputeBuildSums(w);
        Assert.Equal(off.Tr, off.Tl);
        Assert.Equal(on.Tr, off.Tr);
    }

    /// <summary>T11（セーブ）：凍結の欄（MoneyPerMonth?・値と null）は同じ版の往復でバイトが同じで、値も戻る。</summary>
    [Fact]
    public void T11_凍結の欄はセーブで往復する()
    {
        var sim = Sim("イギリス");
        var (w, r, s) = Rgo(sim, 0);
        w.Coef.LandTaxFixedBasis = 1.0;
        EconomyStep.PayOwnerRent(w, r, s, 1234.5, w.Coef);
        int ri = w.Regions.IndexOf(r), si = r.Producers.IndexOf(s);
        using var ms1 = new MemoryStream();
        SaveGame.Save(sim, ms1);
        ms1.Position = 0;
        var sim2 = SaveGame.Load(ms1);
        using var ms2 = new MemoryStream();
        SaveGame.Save(sim2, ms2);
        Assert.Equal(ms1.ToArray(), ms2.ToArray());
        var w2 = sim2.Nations[0].World;
        Assert.Equal(1234.5, w2.Regions[ri].Producers[si].FrozenLandRentPerMonth!.Value.Value);
        Assert.True(w2.LandTaxBasisFixed);
        Assert.Contains(w2.Regions.SelectMany(x => x.Producers), p => p.FrozenLandRentPerMonth is null);
    }
}
