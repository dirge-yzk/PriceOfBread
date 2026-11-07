using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(936) 線 c82hd 案 B1：人の居ない POP の債務を相続人へ移す（<see cref="EmptyPopEstate"/>・旗 <see cref="Coefficients.EmptyPopDebtToHeirs"/>）の機構の番人 T1〜T8。
/// 事前登録 measurements/m936_20261107_prereg_hd.txt [4]・[5]。期待は試験が手で書いた数（債務 7・人数 2 と 1 なら 7/3 と 14/3 など）＝計器もエンジンの関数も読まない。
/// 盤面＝実の世界の 1 州を、試験が置いた人数の相続人だけが人の居る形へ組み直す（他の POP は人数 0・貯蓄 0）。突然変異の表は measurements/m936_20261107/mut_*.txt。</summary>
[Collection("Instruments")]
public class EmptyPopDebtToHeirsTests
{
    private static MultiNationSim Two(ulong seed)
        => new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: seed) { DiplomacyFrozen = true };

    private static void On(World w) { w.Coef.EmptyPopEstate = 1; w.Coef.EmptyPopDebtFirst = 0; w.Coef.EmptyPopDebtToHeirs = 1; }

    /// <summary>POP の正の貯蓄を国庫へ戻す（貯蓄 0 にする）。</summary>
    private static void Clear(World w, Pop p)
    {
        double had = Math.Max(0, p.Savings.Value);
        if (had > 0) Assert.Equal(had, World.Transfer((w, [p]), (w, null, null), had));
        Assert.True(Math.Abs(p.Savings.Value) <= 1e-9, $"前提が崩れた：貯蓄が残る {p.Savings.Value}");
    }

    /// <summary>国庫から 1 人の POP へ置く（重み 1）。</summary>
    private static void Put(World w, Pop p, double x) => Assert.Equal(x, World.Transfer((w, null), (w, [p], [1.0]), x));

    /// <summary>商人の居る州を選び、商人を人数 0・貯蓄 0 の送り手にし、相続人を <paramref name="sizes"/> の人数の POP だけにする（他は人数 0・貯蓄 0）。相続人の貯蓄は触らない。</summary>
    private static (Region R, Pop E, Pop[] H) Board(World w, params double[] sizes)
    {
        var r = w.Regions.First(x => x.MerchantPop is { } m && m.Size > 1e-12 && x.Pops.Count(p => p.Size > 1e-12 && !ReferenceEquals(p, m)) >= sizes.Length);
        foreach (var p in r.Pops) Assert.True(p.Debt.Value == 0, $"前提が崩れた：盤面の POP に債務 {p.Debt.Value}");
        var e = r.MerchantPop!;
        var h = r.Pops.Where(p => p.Size > 1e-12 && !ReferenceEquals(p, e)).Take(sizes.Length).ToArray();
        foreach (var p in r.Pops) if (!h.Contains(p)) { p.Size = 0; Clear(w, p); }
        for (int i = 0; i < h.Length; i++) h[i].Size = sizes[i];
        return (r, e, h);
    }

    /// <summary>手形の債務 <paramref name="d"/> を立て（Bank.Lend＝預金と与信が同じ額）、預金を国庫へ戻して貯蓄 0・債務 d にする。</summary>
    private static void Owe(World w, Pop e, double d) { Bank.Lend(w, e, d); Clear(w, e); Assert.Equal(d, e.Debt.Value); }

    private static double NationDebt(World w) { double d = 0; foreach (var r in w.Regions) foreach (var p in r.Pops) d += p.Debt.Value; return d; }

    private static void Rel(double want, double got, string what) => Assert.True(Math.Abs(got - want) <= 1e-12 * Math.Max(1, Math.Abs(want)), $"{what}：{got} ≠ {want}");

    [Fact]
    public void T1_送り手は厳密な0_相続人は人数の比()
    {
        var sim = Two(9361UL); var w = sim.Nations[0].World; On(w);
        var (_, e, h) = Board(w, 2.0, 1.0);
        Owe(w, e, 7);
        EmptyPopEstate.Step(w);
        Assert.Equal(0L, BitConverter.DoubleToInt64Bits(e.Debt.Value));   // 送り手は厳密な 0（塵を残さない＝:580 が開く）
        Rel(14.0 / 3, h[0].Debt.Value, "人数 2 の相続人の債務");
        Rel(7.0 / 3, h[1].Debt.Value, "人数 1 の相続人の債務");
    }

    [Fact]
    public void T1b_小さい相続人も人数の比で受ける_塵になる()
    {
        var sim = Two(9362UL); var w = sim.Nations[0].World; On(w);
        var (_, e, h) = Board(w, 2.0, 1.0, 2e-12);
        Owe(w, e, 1);
        EmptyPopEstate.Step(w);
        Assert.Equal(0L, BitConverter.DoubleToInt64Bits(e.Debt.Value));
        double small = h[2].Debt.Value, want = 2e-12 / 3.000000000002;
        Assert.True(Math.Abs(small - want) <= 1e-9 * want, $"小さい相続人の債務 {small} ≠ {want}");
        Assert.True(small > 0 && small <= 1e-12, $"小さい相続人の債務 {small} が塵（0＜債務≦1e-12）でない＝名乗りの前提が崩れた");
        Rel(1.0, h[0].Debt.Value + h[1].Debt.Value + h[2].Debt.Value, "相続人の債務の和");
    }

    [Fact]
    public void T2_国の債務の和と与信と基礎貨幣は動かない()
    {
        var sim = Two(9363UL); var w = sim.Nations[0].World; On(w);
        var (_, e, _) = Board(w, 2.0, 1.0);
        Owe(w, e, 7);
        double d0 = NationDebt(w); long h0 = BitConverter.DoubleToInt64Bits(w.Bank!.HouseholdLoans.Value), b0 = BitConverter.DoubleToInt64Bits(w.BaseMoney.Value);
        EmptyPopEstate.Step(w);
        Rel(d0, NationDebt(w), "国の Σ Pop.Debt");
        Assert.Equal(h0, BitConverter.DoubleToInt64Bits(w.Bank.HouseholdLoans.Value));
        Assert.Equal(b0, BitConverter.DoubleToInt64Bits(w.BaseMoney.Value));
    }

    [Fact]
    public void T3_貯蓄0で債務だけの人の居ないPOPも移る()
    {
        var sim = Two(9364UL); var w = sim.Nations[0].World; On(w);
        var (r, e, h) = Board(w, 1.0, 1.0);
        Owe(w, e, 4);
        foreach (var p in r.Pops) if (!(p.Size > 1e-12)) Assert.True(!(p.Savings.Value > 0), "前提が崩れた：人の居ない POP に貯蓄（貯蓄の送り手が居る州になる）");
        EmptyPopEstate.Step(w);
        Assert.Equal(0L, BitConverter.DoubleToInt64Bits(e.Debt.Value));
        Rel(4.0, h[0].Debt.Value + h[1].Debt.Value, "相続人の債務の和");
    }

    [Fact]
    public void T4_相続人の貯蓄が0でも移る()
    {
        var sim = Two(9365UL); var w = sim.Nations[0].World; On(w);
        var (_, e, h) = Board(w, 3.0, 1.0);
        foreach (var p in h) Clear(w, p);
        Owe(w, e, 8);
        EmptyPopEstate.Step(w);
        Assert.Equal(0L, BitConverter.DoubleToInt64Bits(e.Debt.Value));
        Rel(6.0, h[0].Debt.Value, "人数 3 の相続人の債務");
        Rel(2.0, h[1].Debt.Value, "人数 1 の相続人の債務");
    }

    [Fact]
    public void T5_重みは人数で貯蓄ではない()
    {
        var sim = Two(9366UL); var w = sim.Nations[0].World; On(w);
        var (_, e, h) = Board(w, 1.0, 3.0);
        Clear(w, h[0]); Clear(w, h[1]); Put(w, h[0], 300); Put(w, h[1], 100);   // 貯蓄は人数と逆の比
        Owe(w, e, 4);
        EmptyPopEstate.Step(w);
        Rel(1.0, h[0].Debt.Value, "人数 1（貯蓄 300）の相続人の債務");
        Rel(3.0, h[1].Debt.Value, "人数 3（貯蓄 100）の相続人の債務");
    }

    [Fact]
    public void T6_人の居るPOPが無い州では移らない()
    {
        var sim = Two(9367UL); var w = sim.Nations[0].World; On(w);
        var (r, e, _) = Board(w, 1.0, 1.0);
        Owe(w, e, 5);
        foreach (var p in r.Pops) p.Size = 0;   // 州の全員が居ない＝相続人なし
        EmptyPopEstate.Step(w);
        Assert.Equal(5.0, e.Debt.Value);
    }

    [Fact]
    public void T7_旗0では債務に触れない()
    {
        var sim = Two(9368UL); var w = sim.Nations[0].World; On(w); w.Coef.EmptyPopDebtToHeirs = 0;
        var (_, e, h) = Board(w, 2.0, 1.0);
        Owe(w, e, 7);
        EmptyPopEstate.Step(w);
        Assert.Equal(7.0, e.Debt.Value);
        Assert.Equal(0.0, h[0].Debt.Value); Assert.Equal(0.0, h[1].Debt.Value);
    }

    [Fact]
    public void T8_B5の返しの後の残りを相続人へ移す()
    {
        var sim = Two(9369UL); var w = sim.Nations[0].World; On(w); w.Coef.EmptyPopDebtFirst = 1;
        var (_, e, h) = Board(w, 1.0, 1.0);
        Owe(w, e, 500); Put(w, e, 300);   // 貯蓄 300・債務 500
        double h0 = w.Bank!.HouseholdLoans.Value;
        EmptyPopEstate.Step(w);
        Assert.Equal(0L, BitConverter.DoubleToInt64Bits(e.Debt.Value));
        Rel(200.0, h[0].Debt.Value + h[1].Debt.Value, "相続人の債務の和（債務 500−返した 300）");
        Assert.True(Math.Abs(w.Bank.HouseholdLoans.Value - (h0 - 300)) <= 1e-9 * 500, $"家計の与信が返しの分だけ減らない {h0}→{w.Bank.HouseholdLoans.Value}");
    }
}
