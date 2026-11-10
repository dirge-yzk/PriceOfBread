using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(1014) 線 c94pe：旗 ColonyPettyEntryEmptyTarget（小営業の入口 ①* で空の商人の行き先を飛ばさない）の器の番人。事前登録 measurements/m1014_20261110_prereg_pe.txt [7] の T1〜T4。
/// 期待は手で置く（エンジンの関数は期待に使わない）：錠 B の開閉は計器 <see cref="Instruments.MerchLocks"/> で先に確かめ、Step の後の頭数を手の数と比べる。
/// 突然変異（使い捨ての木・measurements/m1014_20261110/mut_*.txt）：M1 旗を読まない・M2 空の行き先の魅力を 0・M3 率の上限を外す・M4 錠 B も外す・M5 :132 も外す・M6 職人の空の行き先も開く。</summary>
[Collection("Instruments")]
public class ColonyPettyEntryTests
{
    private const double Rate = 0.02;   // PettyEntryRatePerQuarter の既定（Coefficients.cs）＝試験が明示で置く

    private static MultiNationSim Two(ulong seed)
        => new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: seed) { DiplomacyFrozen = true };

    /// <summary>州を 1 つ選び、全ての POP を頭数 10・貯蓄 100 にし、労働者・商人・職人・専門職（MeritToIndex）だけ上書きする。所得は頭数あたり 労働者 1・商人 20・職人 20・専門職 100。</summary>
    private static (World w, Region r) Craft(double flag, double merchantSize, double workerSav, double artisanSize = 7, double meritToSize = 10)
    {
        var sim = Two(1789UL);
        sim.StepQuarter();
        var w = sim.Nations[0].World;
        Region? pick = null;
        foreach (var x in w.Regions)
            if (x.MeritFromIndex >= 0 && x.MeritToIndex >= 0 && x.MerchantPopIndex >= 0 && x.ArtisanPopIndex >= 0
                && x.MeritFromIndex != x.MerchantPopIndex && x.MeritFromIndex != x.ArtisanPopIndex && x.MeritToIndex != x.MerchantPopIndex && x.MeritToIndex != x.ArtisanPopIndex) { pick = x; break; }
        Assert.NotNull(pick);   // 対照：選べる州が 1 つはある
        var r = pick!;
        foreach (var p in r.Pops) { p.Size = 10; p.ApplyBakedSavings(100); }
        r.Pops[r.MeritFromIndex].ApplyBakedSavings(workerSav);
        r.Pops[r.MerchantPopIndex].Size = merchantSize;
        r.Pops[r.ArtisanPopIndex].Size = artisanSize;
        r.Pops[r.MeritToIndex].Size = meritToSize;
        r.Pops[r.MeritFromIndex].EarnedPerMonth = new MoneyPerMonth(10);
        r.Pops[r.MerchantPopIndex].EarnedPerMonth = new MoneyPerMonth(100);
        r.Pops[r.ArtisanPopIndex].EarnedPerMonth = new MoneyPerMonth(100);
        r.Pops[r.MeritToIndex].EarnedPerMonth = new MoneyPerMonth(1000);
        w.Coef.PettyEntryRatePerQuarter = new RatePerQuarter(Rate);
        w.Coef.ColonyPettyEntryEmptyTarget = flag;   // World.Coef は国ごと別インスタンス
        return (w, r);
    }

    /// <summary>T1：空の商人（頭数 0・稼ぎ 100）・錠 B 開（労働者の貯蓄 400＝頭数あたり 40 ≧ 州の平均）。旗 0 なら Step の後も商人は 0、旗 1 なら商人が入る。</summary>
    [Theory]
    [InlineData(0.0, false)]
    [InlineData(1.0, true)]
    public void 空の商人の行き先は旗1でだけ入る(double flag, bool wantEntry)
    {
        var (w, r) = Craft(flag, 0, 400);
        var locks = Instruments.MerchLocks(r, Rate);
        Assert.True(locks.LockBOpen, "錠 B が開いていない（盤面の前提が崩れた）");
        Assert.True(locks.MerchantEmpty, "商人が空でない（盤面の前提が崩れた）");
        Meritocracy.Step(w, w.Coef);
        double after = r.Pops[r.MerchantPopIndex].Size;
        if (wantEntry) Assert.True(after > 1e-6, $"旗 1 で空の商人の行き先に入りが無い：{after}");
        else Assert.Equal(0.0, after);
    }

    /// <summary>T2（反証役 ③ の形）：旗 1 でも、錠 B が閉（労働者の貯蓄 0）なら Step の後の商人は 0。錠の開閉は計器で先に確かめる。</summary>
    [Fact]
    public void 錠Bが閉じていれば旗1でも空の商人は0のまま()
    {
        var (w, r) = Craft(1.0, 0, 0);
        var locks = Instruments.MerchLocks(r, Rate);
        Assert.False(locks.LockBOpen, "錠 B が閉じていない（盤面の前提が崩れた）");
        Assert.True(locks.MeritActive, "社会移動の節に着かない州（対照が効かない）");
        Meritocracy.Step(w, w.Coef);
        Assert.Equal(0.0, r.Pops[r.MerchantPopIndex].Size);
    }

    /// <summary>T3：旗 1 の空の行き先の流れは率の上限の内（魅力は 1e16 の桁でも min(魅力−1,1)＝1）＝増えは 0.02×労働者の頭数（Step の前の 10）の 0.25〜2 倍。</summary>
    [Fact]
    public void 空の行き先の流れは率の上限の内()
    {
        var (w, r) = Craft(1.0, 0, 400);
        double worker0 = r.Pops[r.MeritFromIndex].Size;
        Assert.Equal(10.0, worker0);
        Meritocracy.Step(w, w.Coef);
        double gain = r.Pops[r.MerchantPopIndex].Size;
        Assert.True(gain <= 2 * Rate * worker0, $"入りが率の上限を超えた：{gain} > {2 * Rate * worker0}");
        Assert.True(gain >= 0.25 * Rate * worker0, $"入りが率の上限より桁で小さい：{gain} < {0.25 * Rate * worker0}");
    }

    /// <summary>T4：旗 1 でも、空の職人の行き先と空の専門職（① の行き先）は旗 0 と同じ頭数（持ち場の外へ広げない）。対照：同じ盤面で商人は旗 1 でだけ入る。</summary>
    [Fact]
    public void 旗1でも職人と専門職の空の行き先は開かない()
    {
        double Art(double flag, out double merit, out double merch)
        {
            var (w, r) = Craft(flag, 0, 400, artisanSize: 0, meritToSize: 0);
            Assert.True(Instruments.MerchLocks(r, Rate).LockBOpen, "錠 B が開いていない（盤面の前提が崩れた）");
            Meritocracy.Step(w, w.Coef);
            merit = r.Pops[r.MeritToIndex].Size; merch = r.Pops[r.MerchantPopIndex].Size;
            return r.Pops[r.ArtisanPopIndex].Size;
        }
        double a0 = Art(0.0, out double m0, out double c0), a1 = Art(1.0, out double m1, out double c1);
        Assert.Equal(a0, a1);
        Assert.Equal(m0, m1);
        Assert.True(c1 > c0 + 1e-6, $"対照が効かない（旗 1 で商人に入りが無い）：{c1} vs {c0}");
    }
}
