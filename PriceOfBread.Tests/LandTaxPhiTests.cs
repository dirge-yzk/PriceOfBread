using System.IO;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(990) 線 c90px：土地税の基数の定額の度合い φ*（<see cref="World.LandTaxBasisPhi"/>・旗 <see cref="Coefficients.LandTaxPhiMove"/>・動かす関数 <see cref="Government.MoveLandTaxPhi"/>）の器の番人。
/// 事前登録 measurements/m990_20261109_prereg_px.txt の [3]。期待は試験が手で置いた地代（1000・1500・600）と式 φ×凍結＋(1−φ)×地代 から作る（エンジンの割り方を写さない）。
/// 政治を有効にした盤面（<see cref="World.PoliticsEnabled"/>＝無効な世界では余白が 0 になり mandate の有無が区別できない）。目標は関数の引数（T の欄は作らない）。</summary>
public class LandTaxPhiTests
{
    private static MultiNationSim Sim(params string[] names)
        => new([.. names.Select(WorldData.ByName)], seed: 1783UL) { DiplomacyFrozen = true };

    private static World Board(MultiNationSim sim, int nation, double stock)
    {
        var w = sim.Nations[nation].World;
        w.PoliticsEnabled = true; w.PoliticalLockEnabled = true;
        w.Coef.LandTaxFixedBasis = 1.0; w.Coef.LandTaxPhiMove = 1.0;
        w.PoliticalCapitalMax = stock; w.MandateSpent = 0;
        return w;
    }

    private static (Region R, Producer S) Rgo(World w)
    {
        foreach (var r in w.Regions)
        {
            if (r.Id == 0 || r.Tier == RegionTier.Colonial) continue;
            foreach (var s in r.Producers)
                if (s.IsRgo && s.OwnerPopIndex >= 0 && s.OwnerPopIndex < r.Pops.Count && !Colonial.AbsenteeRgo(r, s)) return (r, s);
        }
        throw new Xunit.Sdk.XunitException("試験の前提：条件に合う RGO が無い");
    }

    private static void Zero(Region r)
    {
        foreach (var p in r.Pops) { p.LandRentPerMonth = new MoneyPerMonth(0); p.LandTaxBasisAdjPerMonth = new MoneyPerMonth(0); p.EarnedPerMonth = new MoneyPerMonth(0); }
    }

    private static void Freeze(World w, Region r, Producer s, double rent)
    {
        w.WarmupFrozen = true;
        try { EconomyStep.PayOwnerRent(w, r, s, rent, w.Coef); }
        finally { w.WarmupFrozen = false; }
    }

    private static double Base(World w, Region r) => r.Pops.Sum(p => Fiscal.LandTaxBase(w.Coef, p).Value);

    /// <summary>インフレの下（今の地代 1500 ＞ 凍結 1000）で比例へ戻すと、基数＝請求の元が上がる。期待は φ×1000＋(1−φ)×1500。</summary>
    [Fact]
    public void インフレの下で比例へ戻すと請求が上がる()
    {
        var w = Board(Sim("イギリス"), 0, 1e6);
        var (r, s) = Rgo(w);
        Assert.Equal(1.0, w.LandTaxBasisPhi);
        Freeze(w, r, s, 1000.0);
        Zero(r); EconomyStep.PayOwnerRent(w, r, s, 1500.0, w.Coef);
        double before = Base(w, r);
        Assert.Equal(1000.0, before, 9);
        int n = 0;
        while (w.LandTaxBasisPhi > 0.1 && n++ < 2000) Government.MoveLandTaxPhi(w, w.Coef, 0.0);
        double phi = w.LandTaxBasisPhi;
        Assert.InRange(phi, 1e-9, 0.1);
        Zero(r); EconomyStep.PayOwnerRent(w, r, s, 1500.0, w.Coef);
        double after = Base(w, r);
        Assert.Equal(phi * 1000.0 + (1 - phi) * 1500.0, after, 6);
        Assert.True(after > before + 400.0, $"比例へ戻したのに基数が上がらない before={before} after={after}");
    }

    /// <summary>助走（WarmupFrozen）の 20 四半期の間は φ* が 1 ビットも動かない（目標が 0 でも）。</summary>
    [Fact]
    public void 助走の20四半期でphiが動かない()
    {
        var w = Board(Sim("イギリス"), 0, 1e6);
        w.WarmupFrozen = true;
        for (int q = 0; q < 20; q++) Government.MoveLandTaxPhi(w, w.Coef, 0.0);
        Assert.Equal(1.0, w.LandTaxBasisPhi);
        w.WarmupFrozen = false;
        Government.MoveLandTaxPhi(w, w.Coef, 0.0);
        Assert.True(w.LandTaxBasisPhi < 1.0, "助走の外では動く（歯止めが助走だけに掛かっていること）");
    }

    /// <summary>建国の子は旗 1 のとき親の φ* を継ぐ。旗 0 では写さない（今の子は比例から始まる）。</summary>
    [Fact]
    public void 建国の子のphiが親と同じ_旗0では写さない()
    {
        var sim = Sim("イギリス", "フランス");
        var parent = sim.Nations[0].World; var child = sim.Nations[1].World;
        parent.LandTaxBasisPhi = 0.37;
        child.LandTaxBasisPhi = 0.0;
        child.Coef.LandTaxPhiMove = 0.0;
        child.CopyLawsFrom(parent);
        Assert.Equal(0.0, child.LandTaxBasisPhi);   // 旗 0＝写さない
        child.Coef.LandTaxPhiMove = 1.0;
        child.CopyLawsFrom(parent);
        Assert.Equal(0.37, child.LandTaxBasisPhi);  // 旗 1＝親と同じ
    }

    /// <summary>純反対（上げる向きの純支持が負）でも、政治資本の在庫があれば φ* が動く（mandate＝true）。</summary>
    [Fact]
    public void 地主が純反対でも在庫があればphiが動く()
    {
        var w = Board(Sim("フランス"), 0, 1e6);
        Assert.Equal(0.0, w.LandTaxBasisPhi);
        var e = PolicyPreferences.Build(w);
        Assert.True(PolicyPreferences.NetSupport(w, PolicyAxis.LandTax, true, e) < 0, "試験の前提：定額へ向かう（土地税の軸を上げる）向きは純反対");
        Government.MoveLandTaxPhi(w, w.Coef, 1.0);
        Assert.True(w.LandTaxBasisPhi > 0.0, "在庫があるのに純反対で止まった（錠）");
    }

    /// <summary>φ* を動かした後、四半期の途中（月の地代を払った後）のセーブの往復で基数が一致する。</summary>
    [Fact]
    public void 四半期の途中のセーブの往復で請求が一致する()
    {
        var sim = Sim("イギリス");
        var w = Board(sim, 0, 1e6);
        var (r, s) = Rgo(w);
        Freeze(w, r, s, 1000.0);
        for (int i = 0; i < 5; i++) Government.MoveLandTaxPhi(w, w.Coef, 0.0);
        Assert.InRange(w.LandTaxBasisPhi, 1e-9, 0.99);
        Zero(r); EconomyStep.PayOwnerRent(w, r, s, 600.0, w.Coef);
        using var ms = new MemoryStream();
        SaveGame.Save(sim, ms);
        ms.Position = 0;
        var sim2 = SaveGame.Load(ms);
        var w2 = sim2.Nations[0].World;
        Assert.Equal(w.LandTaxBasisPhi, w2.LandTaxBasisPhi);
        var (r2, s2) = Rgo(w2);
        Assert.Equal(r.Id, r2.Id);
        Zero(r); EconomyStep.PayOwnerRent(w, r, s, 600.0, w.Coef);
        Zero(r2); EconomyStep.PayOwnerRent(w2, r2, s2, 600.0, w2.Coef);
        Assert.Equal(Base(w, r), Base(w2, r2), 9);
        double phi = w.LandTaxBasisPhi;
        Assert.Equal(phi * 1000.0 + (1 - phi) * 600.0, Base(w2, r2), 6);
    }
}
