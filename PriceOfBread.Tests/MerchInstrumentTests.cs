using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(1007) 線 c93ms：計器 <c>merch_s</c>（商人・職人の空と小営業の入口の錠 A・B の再計算・読むだけ）の番人。事前登録 measurements/m1007_20261110_prereg_ms.txt の T1〜T5。
/// 期待は手で置く（エンジンの関数は期待に使わない）：T1 は手の表・T2 は手で置いた貯蓄から手の式・T4 は別の計器 cpop_s・T5 はエンジン（Meritocracy.Step）の出力の差。
/// 突然変異（使い捨ての木・measurements/m1007_20261110_mutations.txt）：Mu1 lockBOpen の不等式を逆にする・Mu2 max(0,貯蓄) を外す・Mu3 merchantK に職人を足す・Mu4 植民地格だけを書く・Mu5 meritActive を外す。</summary>
[Collection("Instruments")]
public class MerchInstrumentTests
{
    private const double Rate = 0.02;

    private static MultiNationSim Two(ulong seed)
        => new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: seed) { DiplomacyFrozen = true };

    /// <summary>錠の 4 通りを作る州：先頭から全ての POP を頭数 10・貯蓄 100 にし、労働者（MeritFromIndex）と商人・職人だけ上書きする。</summary>
    private static Region Craft(MultiNationSim sim, double merchantSize, double workerSav, double artisanSize = 7)
    {
        var w = sim.Nations[0].World;
        Region? pick = null;
        foreach (var r in w.Regions)
            if (r.MeritFromIndex >= 0 && r.MeritToIndex >= 0 && r.MerchantPopIndex >= 0 && r.ArtisanPopIndex >= 0 && r.MeritFromIndex != r.MerchantPopIndex && r.MeritFromIndex != r.ArtisanPopIndex) { pick = r; break; }
        Assert.NotNull(pick);
        foreach (var p in pick!.Pops) { p.Size = 10; p.ApplyBakedSavings(100); }
        pick.Pops[pick.MeritFromIndex].ApplyBakedSavings(workerSav);
        pick.Pops[pick.MerchantPopIndex].Size = merchantSize;
        pick.Pops[pick.ArtisanPopIndex].Size = artisanSize;
        return pick;
    }

    /// <summary>T1：錠 A と錠 B の 4 通り（商人が 空／空でない × 労働者の元手 開／閉）。労働者の貯蓄 400（頭数あたり 40）は平均（約 10〜20）以上＝開・0 は未満＝閉。職人は常に 7＝商人の空に職人を足すと赤。</summary>
    [Theory]
    [InlineData(0.0, 400.0, true, true)]     // 空×開＝錠 A が効いて入れない
    [InlineData(0.0, 0.0, false, false)]     // 空×閉＝錠 B が先に閉じる
    [InlineData(5.0, 400.0, true, false)]    // 空でない×開＝入れる
    [InlineData(5.0, 0.0, false, false)]     // 空でない×閉
    public void 錠AとBの4通り(double merchantSize, double workerSav, bool wantLockB, bool wantBlockedMerchant)
    {
        var sim = Two(1789UL);
        var r = Craft(sim, merchantSize, workerSav);
        var m = Instruments.MerchLocks(r, Rate);
        Assert.True(m.MeritActive);
        Assert.Equal(wantLockB, m.LockBOpen);
        Assert.Equal(merchantSize <= 1e-9, m.MerchantEmpty);
        Assert.False(m.ArtisanEmpty);
        Assert.Equal(wantBlockedMerchant, m.LockBOpen && m.MerchantEmpty);
        Assert.Equal(merchantSize, m.MerchantK, 12);
        Assert.Equal(7.0, m.ArtisanK, 12);
        Assert.False(Instruments.MerchLocks(r, 0.0).LockBOpen);   // 入口の率が 0 なら錠 B は開かない
    }

    /// <summary>T2：負の貯蓄の POP を含む州の avgPerCap は max(0,貯蓄) の和÷頭数。n 個の POP のうち 1 つが −500・他は 100・頭数は全て 10＝(n−1)×100÷(10n)。</summary>
    [Fact]
    public void 負の貯蓄は平均から除く()
    {
        var sim = Two(1789UL);
        var r = Craft(sim, 5, 100);
        int n = r.Pops.Count;
        int neg = r.MerchantPopIndex;   // 商人の POP を −500 にする（労働者ではない）
        r.Pops[neg].ApplyBakedSavings(-500);
        var m = Instruments.MerchLocks(r, Rate);
        // 商人は頭数 5・職人は 7・他の n−2 個は 10：頭数の和＝10(n−2)+12・正の貯蓄の和＝(n−1)×100（商人の −500 は除く）
        double want = (n - 1) * 100.0 / (10.0 * (n - 2) + 12.0);
        Assert.Equal(want, m.AvgPerCap, 10);
        Assert.True(m.AvgPerCap > 0, "負の貯蓄を和に入れている");
    }

    /// <summary>英仏を n 四半期回し、出力のフォルダを返す。</summary>
    private static string Run(ulong seed, int quarters)
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_merch_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        var sim = Two(seed);
        try { for (int q = 0; q < quarters; q++) { sim.StepQuarter(); Instruments.Quarter(sim, seed, q); } }
        finally { Instruments.Close(sim); Instruments.Configure(null, null); }
        return dir;
    }

    /// <summary>T3：実走の行。cearn_s（全ての州×四半期）と (quarter,nation,region) で 1 対 1・Tier が複数・列の整合。</summary>
    [Fact]
    public void 実走の行は全ての州であり格が複数ある()
    {
        string dir = Run(1789UL, 4);
        var rows = SubstInstrumentTests.Rows(System.IO.Path.Combine(dir, "merch_s1789.tsv"), Instruments.MerchHeaderText);
        var ce = SubstInstrumentTests.Rows(System.IO.Path.Combine(dir, "cearn_s1789.tsv"), null);
        Assert.NotEmpty(rows);
        var keys = rows.Select(r => (r["quarter"], r["nation"], r["region"])).ToList();
        Assert.Equal(keys.Count, keys.Distinct().Count());
        Assert.Equal(ce.Select(r => (r["quarter"], r["nation"], r["region"])).OrderBy(k => k).ToList(), keys.OrderBy(k => k).ToList());
        Assert.True(rows.Select(r => r["tier"]).Distinct().Count() >= 2, "格が 1 種類しかない");
        foreach (var r in rows)
        {
            bool mEmpty = SubstInstrumentTests.D(r, "merchantK") <= 1e-9;
            Assert.Equal(mEmpty ? "1" : "0", r["merchantEmpty"]);
            if (r["lockBOpen"] == "1") Assert.Equal("1", r["meritActive"]);
            Assert.Equal(r["lockBOpen"] == "1" && mEmpty ? "1" : "0", r["lockABlockedMerchant"]);
            Assert.True(SubstInstrumentTests.D(r, "popK") + 1e-9 >= SubstInstrumentTests.D(r, "merchantK") + SubstInstrumentTests.D(r, "artisanK") + SubstInstrumentTests.D(r, "workerK") - 1e-6);
        }
    }

    /// <summary>T4（G1）：植民地格の州×四半期で、merch_s の merchantEmpty が cpop_s の商人の popK≦1e-9 と一致（ずれ 0）。上側＝本国格の行は cpop_s に無い＝混ぜると突き合わせが空振りする。</summary>
    [Fact]
    public void 植民地格の商人の空はcpop_sと一致する()
    {
        string dir = Run(1789UL, 6);
        var rows = SubstInstrumentTests.Rows(System.IO.Path.Combine(dir, "merch_s1789.tsv"), Instruments.MerchHeaderText);
        var cp = SubstInstrumentTests.Rows(System.IO.Path.Combine(dir, "cpop_s1789.tsv"), null);
        var cpMerchant = new Dictionary<(string, string, string), bool>();
        foreach (var r in cp) if (r["className"] == "商人") cpMerchant[(r["quarter"], r["nation"], r["region"])] = SubstInstrumentTests.D(r, "popK") <= 1e-9;
        int matched = 0, diff = 0, nonColonialWithCpop = 0, coreRows = 0;
        foreach (var r in rows)
        {
            bool colonial = r["tier"] == "Colonial";
            if (!colonial) coreRows++;
            if (cpMerchant.TryGetValue((r["quarter"], r["nation"], r["region"]), out bool cpEmpty))
            {
                if (!colonial) nonColonialWithCpop++;
                matched++;
                if (cpEmpty != (r["merchantEmpty"] == "1")) diff++;
            }
        }
        Assert.True(matched > 0, "植民地格の突き合わせた行が無い（前提が崩れた）");
        Assert.Equal(0, diff);
        Assert.Equal(0, nonColonialWithCpop);
        Assert.True(coreRows > 0, "本国格の行が無い（上側の前提が崩れた）");
    }

    /// <summary>T5（G2 の代え）：エンジンの出力を読む。商人が空でなく魅力が 1 を超える州で、労働者の元手の錠が閉（貯蓄 0）なら、入口の率が 0 の世界と商人の頭数が同じ（流れ 0）。錠が開（貯蓄 400）なら率が正の世界のほうが商人が増える（試験が感度を持つ確認）。</summary>
    [Theory]
    [InlineData(0.0, true)]
    [InlineData(400.0, false)]
    public void 錠Bが閉じていれば入口の流れは0(double workerSav, bool wantEqual)
    {
        double MerchantAfter(double rate)
        {
            var sim = Two(1789UL);
            sim.StepQuarter();
            var w = sim.Nations[0].World;
            var r = Craft(sim, 5, workerSav);
            r.Pops[r.MeritFromIndex].EarnedPerMonth = new MoneyPerMonth(10);        // 労働者の所得＝頭数あたり 1
            r.Pops[r.MerchantPopIndex].EarnedPerMonth = new MoneyPerMonth(100);     // 商人＝頭数あたり 20＝魅力が 1 を大きく超える
            w.Coef.PettyEntryRatePerQuarter = new RatePerQuarter(rate);
            Meritocracy.Step(w, w.Coef);
            return r.Pops[r.MerchantPopIndex].Size;
        }
        double off = MerchantAfter(0.0), on = MerchantAfter(Rate);
        if (wantEqual) Assert.Equal(off, on, 9);
        else Assert.True(on > off + 0.05, $"錠が開いているのに入口の流れが無い：{on} vs {off}");
    }
}
