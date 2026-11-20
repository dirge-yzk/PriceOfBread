using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

// (1248) 国の消滅の 1 本（MultiNationSim.DissolveNation）を直に踏む番人。盤面は試験の中で置く。
public class DissolveNationTests
{
    private static double WorldBase(World[] ws) { double s = 0; foreach (var w in ws) s += w.BaseMoney.Value; return s; }
    private static double Sum(World[] ws, System.Func<World, double> f) { double s = 0; foreach (var w in ws) s += f(w); return s; }

    [Fact]
    public void 国の消滅の1本_債務と請求と準備金と世界の貨幣は前後で閉じ_名簿と行列の添字が詰まる()
    {
        var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };
        var pw = sim.Nations[0].World;
        pw.PendingRebelRegions = [pw.Regions[1]]; pw.PendingRebelPolity = Polity.Republic;
        sim.StepQuarter();
        Assert.True(sim.Nations.Count == 3, "前提が崩れた：反乱国家が建っていない");
        int pi = 0, ri = sim.Nations.Count - 1;
        var rw = sim.Nations[ri].World; var rebelP = rw.SelfPower!; var parentP = pw.SelfPower!;
        double rho = rw.FxRate / pw.FxRate;
        double scale = System.Math.Max(1.0, rw.TotalMoney.Value);
        double fd = 0.1 * scale;
        rw.ForeignDebt += new MoneyStock(fd); rw.GovDebt += new MoneyStock(fd); rw.PrevForeignDebt += new MoneyStock(fd);
        rw.ForeignBondClaims += new MoneyStock(0.03 * scale);
        rw.SilverReserveQty += 7.0; rw.GoldReserveQty += 3.0;
        // 行列の各要素を別の値に置く＝詰めの添字の取り違えが値で割れる
        int n = sim.Nations.Count;
        var q = new double[n][];
        for (int a = 0; a < n; a++) { q[a] = new double[n]; for (int b = 0; b < n; b++) q[a][b] = 10 * a + b + 1; }
        sim.Market.PairFlowQ = q;
        int powers0 = sim.Diplomacy.Powers.Count;

        var all0 = sim.Nations.ConvertAll(x => x.World).ToArray();
        double world0 = WorldBase(all0), fx0 = sim.Diplomacy.CumulativeFxResidual;
        double fdW0 = Sum(all0, w => w.ForeignDebt.Value * w.FxRate), clW0 = Sum(all0, w => w.ForeignBondClaims.Value * w.FxRate);
        double sil0 = Sum(all0, w => w.SilverReserveQty), gold0 = Sum(all0, w => w.GoldReserveQty);
        double pfd0 = pw.ForeignDebt.Value, pgd0 = pw.GovDebt.Value, pcl0 = pw.ForeignBondClaims.Value, psil0 = pw.SilverReserveQty, pgold0 = pw.GoldReserveQty;
        double rsil = rw.SilverReserveQty, rgold = rw.GoldReserveQty, rfd = rw.ForeignDebt.Value, rgd = rw.GovDebt.Value, rcl = rw.ForeignBondClaims.Value;

        sim.DissolveNation(parentP, rebelP, pi, ri, rebelWon: true, Polity.Republic, null);

        Assert.DoesNotContain(sim.Nations, x => ReferenceEquals(x.World, rw));
        Assert.Equal(n - 1, sim.Nations.Count);
        Assert.Equal(powers0 - 1, sim.Diplomacy.Powers.Count);   // 名簿から外れる（RemovePower）
        Assert.DoesNotContain(rebelP, sim.Diplomacy.Powers);
        var all1 = sim.Nations.ConvertAll(x => x.World).ToArray();
        double tol = 1e-9 * System.Math.Max(1.0, world0);
        double fxMoved = sim.Diplomacy.CumulativeFxResidual - fx0;
        Assert.True(System.Math.Abs(WorldBase(all1) - world0 - fxMoved) <= tol, $"世界の基礎貨幣が換算の台帳の外で {WorldBase(all1) - world0 - fxMoved} 動いた");
        // 債務・請求は親の通貨へ換算して渡る
        Assert.True(System.Math.Abs(pw.ForeignDebt.Value - pfd0 - rfd * rho) <= tol, $"親の対外債の増え {pw.ForeignDebt.Value - pfd0} ≠ {rfd * rho}");
        Assert.True(System.Math.Abs(pw.GovDebt.Value - pgd0 - rgd * rho) <= tol, $"親の政府債務の増え {pw.GovDebt.Value - pgd0} ≠ {rgd * rho}");
        Assert.True(System.Math.Abs(pw.ForeignBondClaims.Value - pcl0 - rcl * rho) <= tol, $"親の債権の増え {pw.ForeignBondClaims.Value - pcl0} ≠ {rcl * rho}");
        Assert.True(System.Math.Abs(Sum(all1, w => w.ForeignDebt.Value * w.FxRate) - fdW0) <= tol, "対外債の金の値打ちの和が動いた");
        Assert.True(System.Math.Abs(Sum(all1, w => w.ForeignBondClaims.Value * w.FxRate) - clW0) <= tol, "債権の金の値打ちの和が動いた");
        // 準備金は現物＝換算せずそのまま親へ
        Assert.Equal(psil0 + rsil, pw.SilverReserveQty, 9);
        Assert.Equal(pgold0 + rgold, pw.GoldReserveQty, 9);
        Assert.True(System.Math.Abs(Sum(all1, w => w.SilverReserveQty) - sil0) <= 1e-9 * System.Math.Max(1.0, sil0), "銀の準備の和が動いた");
        Assert.True(System.Math.Abs(Sum(all1, w => w.GoldReserveQty) - gold0) <= 1e-9 * System.Math.Max(1.0, gold0), "金の準備の和が動いた");
        // 行列の添字詰め（消えた国の行と列を除く）
        var pfq = sim.Market.PairFlowQ!;
        Assert.Equal(n - 1, pfq.Length);
        for (int a = 0; a < n - 1; a++) { Assert.Equal(n - 1, pfq[a].Length); for (int b = 0; b < n - 1; b++) Assert.Equal(10 * a + b + 1, pfq[a][b]); }
    }

    // (1262) c129sb ex の穴：消える国が名簿の最後でない（中ほど）の対照。最後の国を溶かす上の試験では、行列の最後の行と列を落とす書き方（Take(n-1)）でも値が合ってしまう。
    [Fact]
    public void 国の消滅の1本_中ほどの国を溶かすと行列は消えた国の行と列だけを除いて詰まる()
    {
        var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };
        var pw = sim.Nations[0].World;
        pw.PendingRebelRegions = [pw.Regions[1]]; pw.PendingRebelPolity = Polity.Republic;
        sim.StepQuarter();
        Assert.True(sim.Nations.Count == 3, "前提が崩れた：反乱国家が建っていない");
        int pi = 0, ri = 1;   // 中ほど（フランス）を親（イギリス）へ溶かす＝最後（反乱国家）は残って添字が 1 つ前へ詰まる
        var rw = sim.Nations[ri].World; var lastW = sim.Nations[2].World;
        var rebelP = rw.SelfPower!; var parentP = pw.SelfPower!;
        int n = sim.Nations.Count;
        var q = new double[n][];
        for (int a = 0; a < n; a++) { q[a] = new double[n]; for (int b = 0; b < n; b++) q[a][b] = 10 * a + b + 1; }
        sim.Market.PairFlowQ = q;
        sim.DissolveNation(parentP, rebelP, pi, ri, rebelWon: true, Polity.Republic, null);
        Assert.Equal(n - 1, sim.Nations.Count);
        Assert.DoesNotContain(sim.Nations, x => ReferenceEquals(x.World, rw));
        Assert.Same(lastW, sim.Nations[1].World);   // 最後の国が 1 つ前へ詰まった
        var pfq = sim.Market.PairFlowQ!;
        Assert.Equal(n - 1, pfq.Length);
        int[] old = [0, 2];   // 詰めた後の添字 → 溶かす前の添字（消えた 1 を飛ばす）
        for (int a = 0; a < n - 1; a++) { Assert.Equal(n - 1, pfq[a].Length); for (int b = 0; b < n - 1; b++) Assert.Equal(10 * old[a] + old[b] + 1, pfq[a][b]); }
    }
}
