using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(910) 線 c78ep 案 B5：相続の前に人の居ない POP の債務を返す（<see cref="EmptyPopEstate"/>・旗 <see cref="Coefficients.EmptyPopDebtFirst"/>）の機構の番人。
/// 事前登録 measurements/m910_20261107_prereg_ep.txt [1]・[5]。期待は試験が自分で数える：債務と貯蓄は試験が置いた額（手で書いた数）・受け手の和は試験が Step の前に数えた和。
/// 突然変異の表は measurements/m910_20261107/mut_*.txt。</summary>
[Collection("Instruments")]
public class EmptyPopDebtFirstTests
{
    private static MultiNationSim Two(ulong seed)
        => new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: seed) { DiplomacyFrozen = true };

    /// <summary>国庫から 1 人の POP へ置く（重み 1＝人数 0 の POP でも受け取る）。</summary>
    private static void Put(World w, Pop p, double x) => Assert.Equal(x, World.Transfer((w, null), (w, [p], [1.0]), x));

    private static Region Pick(World w, int skip = 0) => w.Regions.Where(r => r.Pops.Count(p => p.Size > 1e-12) >= 6).Skip(skip).First();

    /// <summary>州の人の居る POP を 1 つ空にし、貯蓄 <paramref name="save"/> と手形の債務 <paramref name="debt"/> を持たせる（債務は Bank.Lend＝預金と与信が同じ額で立つ・試験が置いた額だけが債務）。</summary>
    private static Pop EmptyWithDebt(World w, Region r, double save, double debt, int k = 1)
    {
        var p = r.Pops.Where(x => x.Size > 1e-12 && x.Debt.Value == 0).ToList()[^k];
        p.Size = 0;
        double had = Math.Max(0, p.Savings.Value);
        if (had > 0) Assert.Equal(had, World.Transfer((w, [p]), (w, null, null), had));   // 盤面の貯蓄を国庫へ戻す（試験が置いた額だけにする）
        Assert.True(Math.Abs(p.Savings.Value) <= 1e-9, $"前提が崩れた：貯蓄が残る {p.Savings.Value}");
        if (save > 0) Put(w, p, save);
        Bank.Lend(w, p, debt);   // 貯蓄 save＋debt・債務 debt
        return p;
    }

    private static double HouseholdDebt(World w) { double d = 0; foreach (var r in w.Regions) foreach (var p in r.Pops) d += p.Debt.Value; return d; }

    private static void On(World w) { w.Coef.EmptyPopEstate = 1; w.Coef.EmptyPopDebtFirst = 1; w.Coef.EmptyPopDebtToHeirs = 0; }   // (936) 前提を既定に頼らず明示：B1（債務を相続人へ）は切る＝B5 だけを確かめる

    [Fact]
    public void 貯蓄が債務に足りれば相続の前に全額返し_残りだけが相続人へ()
    {
        var sim = Two(1910UL); var w = sim.Nations[0].World; On(w);
        var r = Pick(w);
        var a = EmptyWithDebt(w, r, 700, 300);   // 貯蓄 1000・債務 300
        var live = r.Pops.Where(p => p.Size > 1e-12).ToList();
        double liveSum0 = live.Sum(p => p.Savings.Value), h0 = w.Bank!.HouseholdLoans.Value, bm0 = w.BaseMoney.Value;
        EmptyPopEstate.Step(w);
        Assert.True(Math.Abs(a.Debt.Value) <= 1e-9, $"返していない：債務 {a.Debt.Value}");
        Assert.True(Math.Abs(a.Savings.Value) <= 1e-9, $"人の居ない POP に貯蓄が残る {a.Savings.Value}");
        Assert.True(Math.Abs(live.Sum(p => p.Savings.Value) - liveSum0 - 700) <= 1e-9 * 1000, $"相続人が受けた額 {live.Sum(p => p.Savings.Value) - liveSum0} ≠ 700（貯蓄−債務）");
        Assert.True(Math.Abs(w.Bank.HouseholdLoans.Value - (h0 - 300)) <= 1e-9 * 1000, $"家計の与信が返しの分だけ減らない {h0}→{w.Bank.HouseholdLoans.Value}");
        Assert.True(Math.Abs(w.BaseMoney.Value - bm0) <= 1e-9 * Math.Max(1, Math.Abs(bm0)), "返しが基礎貨幣を動かした");
    }

    /// <summary>(944) B1（<see cref="Coefficients.EmptyPopDebtToHeirs"/>）が 0 のときの B5 の番人（盤面の前提は <see cref="On"/> が明示）。
    /// 残りの債務を相続人へ移す規則の側は EmptyPopDebtToHeirsTests の T8（同じ盤面・債務 500・貯蓄 300）が持つ。</summary>
    [Fact]
    public void B1が0のとき貯蓄が債務に足りなければ貯蓄の全額を返し_残りの債務は人の居ないPOPに残る()
    {
        var sim = Two(1911UL); var w = sim.Nations[0].World; On(w);
        var r = Pick(w);
        var a = EmptyWithDebt(w, r, 0, 500);   // 貯蓄 500・債務 500
        Assert.Equal(300, World.Transfer((w, [a]), (w, null, null), 300));   // 貯蓄 200 に減らす（国庫へ）
        var live = r.Pops.Where(p => p.Size > 1e-12).ToList();
        double liveSum0 = live.Sum(p => p.Savings.Value);
        EmptyPopEstate.Step(w);
        Assert.True(Math.Abs(a.Debt.Value - 300) <= 1e-9 * 500, $"返した後の債務 {a.Debt.Value} ≠ 300（債務−貯蓄）");
        Assert.True(Math.Abs(a.Savings.Value) <= 1e-9, $"貯蓄が残る {a.Savings.Value}");
        Assert.True(a.Debt.Value >= 0, "返し過ぎ（負の債務）");
        Assert.True(Math.Abs(live.Sum(p => p.Savings.Value) - liveSum0) <= 1e-9 * 500, "返し切れないのに相続人へ移った");
    }

    [Fact]
    public void 返した後も家計の与信は国のPOPの債務の和に等しい()
    {
        var sim = Two(1912UL); var w = sim.Nations[0].World; On(w);
        EmptyWithDebt(w, Pick(w), 700, 300);
        EmptyWithDebt(w, Pick(w, 1), 0, 400);
        double before = HouseholdDebt(w);
        Assert.True(Math.Abs(w.Bank!.HouseholdLoans.Value - before) <= 1e-9 * Math.Max(1, before), "前提が崩れた：置いた後に与信＝債務の和でない");
        EmptyPopEstate.Step(w);
        double after = HouseholdDebt(w);
        Assert.True(before - after >= 700 - 1e-6, $"返した額 {before - after} が 700（300＋400）に届かない");
        Assert.True(Math.Abs(w.Bank.HouseholdLoans.Value - after) <= 1e-9 * Math.Max(1, before), $"家計の与信 {w.Bank.HouseholdLoans.Value} ≠ POP の債務の和 {after}");
    }

    [Fact]
    public void 人の居るPOPの債務と貯蓄は返しで動かない()
    {
        var sim = Two(1913UL); var w = sim.Nations[0].World; On(w);
        var r = Pick(w);
        EmptyWithDebt(w, r, 700, 300);
        var lp = r.Pops.Where(p => p.Size > 1e-12 && p.Debt.Value == 0).First();
        Bank.Lend(w, lp, 100);   // 人の居る POP の債務 100・貯蓄 +100
        double d0 = lp.Debt.Value, s0 = lp.Savings.Value;
        EmptyPopEstate.Step(w);
        Assert.Equal(d0, lp.Debt.Value);
        Assert.True(lp.Savings.Value >= s0, $"人の居る POP から出た {s0}→{lp.Savings.Value}");
    }

    [Fact]
    public void 人の居るPOPが無い州でも返し_残りは動かさない()
    {
        var sim = Two(1914UL); var w = sim.Nations[0].World; On(w);
        var r = Pick(w);
        var a = EmptyWithDebt(w, r, 600, 200);   // 貯蓄 800・債務 200
        foreach (var p in r.Pops) p.Size = 0;   // 州の全員が居ない＝相続人なし
        EmptyPopEstate.Step(w);
        Assert.True(Math.Abs(a.Debt.Value) <= 1e-9, $"相続人の居ない州で返していない：債務 {a.Debt.Value}");
        Assert.True(Math.Abs(a.Savings.Value - 600) <= 1e-9 * 800, $"返した後の貯蓄 {a.Savings.Value} ≠ 600（相続人が居ない＝動かさない）");
    }

    [Fact]
    public void 旗0では債務に触れず今の相続のまま()
    {
        var sim = Two(1915UL); var w = sim.Nations[0].World;
        w.Coef.EmptyPopEstate = 1; w.Coef.EmptyPopDebtFirst = 0; w.Coef.EmptyPopDebtToHeirs = 0;   // (936) 前提を既定に頼らず明示（B1 は切る）
        var r = Pick(w);
        var a = EmptyWithDebt(w, r, 700, 300);
        var live = r.Pops.Where(p => p.Size > 1e-12).ToList();
        double liveSum0 = live.Sum(p => p.Savings.Value), h0 = w.Bank!.HouseholdLoans.Value;
        EmptyPopEstate.Step(w);
        Assert.Equal(300.0, a.Debt.Value);
        Assert.Equal(h0, w.Bank.HouseholdLoans.Value);
        Assert.True(Math.Abs(live.Sum(p => p.Savings.Value) - liveSum0 - 1000) <= 1e-9 * 1000, "旗 0 の相続（貯蓄の全額）が変わった");
    }

    [Fact]
    public void 計器の列は返した額と商人の債務を読む()
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_epdebt_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        var sim = Two(1916UL);
        var w = sim.Nations[0].World;
        var r0 = Pick(w);
        var want = new Dictionary<string, double>();
        try
        {
            sim.StepQuarter(); Instruments.Quarter(sim, 1916UL, 0);   // 四半期 0 は旗 0 のまま（台帳を始める）
            On(w);
            sim.Nations[1].World.Coef.EmptyPopDebtFirst = 0;   // 国 1 は旗 0＝返さない
            EmptyWithDebt(w, r0, 900, 250);
            foreach (var n in sim.Nations) Instruments.Begin(n.World);   // 月初の登録
            foreach (var r in w.Regions)   // 期待＝試験が Step の前に数える（国 0 の州ごとの人の居ない POP の min(債務, max(0, 貯蓄)) の和）
            {
                double e = 0;
                foreach (var p in r.Pops) if (!(p.Size > 1e-12) && p.Debt.Value > 0) e += Math.Min(p.Debt.Value, Math.Max(0, p.Savings.Value));
                want[r.Name] = e;
            }
            foreach (var n in sim.Nations) EmptyPopEstate.Step(n.World);
            Instruments.Quarter(sim, 1916UL, 1);
        }
        finally { Instruments.Close(sim); Instruments.Configure(null, null); }
        try
        {
            var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, "cearn_s1916.tsv"));
            Assert.Equal(Instruments.CearnHeaderText, lines[0]);
            Assert.EndsWith("\tmerchantDebt\testateDebtSettledPerQuarter\tbillBlockedClearings\tbillBlockedDustClearings\temptyDebtAfterEstate\testateDebtMovedPerQuarter\theirDebtSettledPerQuarter", lines[0]);   // (927) (910) の型へ戻す＝尾の全部を EndsWith で確かめる（(920) の末尾 5 列まで・尾に列が増えたら赤で、この文字列を伸ばす）
            var h = lines[0].Split('\t');
            int Ix(string c) => Array.IndexOf(h, c);
            string nation0 = Instruments.NameOf(w);
            bool sawR0 = false; int rows1 = 0;
            foreach (var l in lines.Skip(1))
            {
                var c = l.Split('\t');
                if (c[Ix("quarter")] != "1") continue;
                Assert.Equal(h.Length, c.Length);
                double got = double.Parse(c[Ix("estateDebtSettledPerQuarter")], System.Globalization.CultureInfo.InvariantCulture);
                if (c[Ix("nation")] != nation0) { Assert.Equal(0.0, got); rows1++; continue; }   // 旗 0 の国は返さない
                double e = want[c[Ix("region")]];
                Assert.True(Math.Abs(got - e) <= 1e-9 * Math.Max(1, e), $"{c[Ix("region")]}：計器の返した額 {got} ≠ 試験が数えた {e}");
                if (c[Ix("region")] == r0.Name)
                {
                    sawR0 = true;
                    Assert.True(got >= 250 - 1e-6, $"置いた債務 250 が返った額に入っていない {got}");
                    var mp = r0.MerchantPop;
                    string md = c[Ix("merchantDebt")];
                    if (mp is null) Assert.Equal("nan", md, ignoreCase: true);
                    else Assert.Equal(mp.Debt.Value, double.Parse(md, System.Globalization.CultureInfo.InvariantCulture), 9);
                }
            }
            Assert.True(sawR0);
            Assert.True(rows1 > 0, "前提が崩れた：旗 0 の国の行が無い");
        }
        finally { try { System.IO.Directory.Delete(dir, true); } catch { } }
    }
}
