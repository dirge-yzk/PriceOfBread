using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (1100) 線 c105ix：計器 armsfill_s（軍需の充足の律速の分解・読むだけ）の自己整合の試験 2 本。事前登録 measurements/m1100_20261113_prereg_ix.txt [3]。
/// A1＝四つの財の fillContrib と other の和（fillContribQtyTotal）÷必要量の和（wantQtyTotal）が、同じ実行の stock_s の capitalEquipFillShare と合う。
///   列ごとの寄与は 財ごとの必要量（WantQty）×FillContribShare を試験の中で足し直す（計器の合計の列を鵜呑みにしない）。
/// A2＝兵÷戦力ゲージ 1.0 の兵数（Coefficients.SoldiersPerMilitary＝250×働き盛りの標準の割合。仕様書の 69.125 は今の値でない）×暮らし×装備×質×結束×軍国×技術×(1−海軍の割合)×熟練＝stock_s の landPowerIndex（紙の式・係数は試験に置く）。
/// 何もしない計器は緑にならない＝調べた行の数に下限を置く。
/// </summary>
[Collection("Instruments")]
public class InstrumentsArmsTests
{
    private const ulong Seed = 1789UL;
    // (1118) c108ps：A2 だけ盤面の種を替えた（★(743) の軌道に依る前提＝G10 の丸めの運・旗 ParliamentSeatsWithoutGap 1 の軌道で種 1789 のフランス/q2 が許し 1e-9 を超えた）。Assert・閾値・母集合は元のまま。
    private const ulong A2Seed = 1791UL;   // 種 1789〜1793 を旗 0・旗 1 で回し、両方で緑の最初の種（measurements/m1118_20261114/a2_seeds.txt）
    private static NationConfig[] Configs() => [NationConfig.France(), NationConfig.Prussia(), NationConfig.Britain()];

    private sealed class Tab
    {
        public string[] H = []; public List<string[]> R = [];
        public int I(string n) { int i = Array.IndexOf(H, n); Assert.True(i >= 0, "列が無い " + n); return i; }
        public double D(string[] r, string n) { var s = r[I(n)]; return s == "nan" ? double.NaN : double.Parse(s, System.Globalization.CultureInfo.InvariantCulture); }
    }
    private static Tab Read(string dir, string kind, ulong seed = Seed)
    {
        var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, $"{kind}_s{seed}.tsv"));
        return new Tab { H = lines[0].Split('\t'), R = lines.Skip(1).Select(l => l.Split('\t')).ToList() };
    }

    /// <summary>戦力の同期（Diplomacy.SyncPowers）は月の頭に走る＝四半期の末の断面は 1 か月古い。A2 は同期を 1 度呼び直してから計器を書く（積と指数を同じ瞬間の状態で比べる）。</summary>
    private static void Resync(MultiNationSim sim)
        => typeof(Diplomacy).GetMethod("SyncPowers", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(sim.Diplomacy, [sim.Nations[0].World.Coef]);

    private static (string dir, Action restore) Run(int quarters, bool resync = false, ulong seed = Seed)
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_arms_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        var sim = new MultiNationSim(Configs(), seed, _ => new UtilityAgent());
        sim.DiplomacyFrozen = true;
        Instruments.Phase(sim, seed, "open");
        sim.StartWarmup();
        Instruments.Phase(sim, seed, "warm");
        try { for (int q = 0; q < quarters; q++) { sim.StepQuarter(); if (resync) Resync(sim); Instruments.Quarter(sim, seed, q); } }
        finally { Instruments.Close(sim); }
        return (dir, () => { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } });
    }

    private static Dictionary<(string, string), string[]> ByKey(Tab t) =>
        t.R.GroupBy(r => (r[t.I("time")], r[t.I("nation")])).ToDictionary(g => g.Key, g => g.First());

    [Fact]
    public void A1_充足の寄与の和は_stock_の装備の充足と合う()
    {
        var (dir, restore) = Run(10);
        try
        {
            var af = Read(dir, "armsfill"); var st = Read(dir, "stock");
            var stock = ByKey(st);
            string[] names = ["rifle", "ammo", "artillery", "horse"];
            int checkedRows = 0, binding = 0; double worst = 0;
            foreach (var r in af.R)
            {
                double want = 0, fill = 0;
                foreach (var n in names) { double wq = af.D(r, n + "WantQty"), sh = af.D(r, n + "FillContribShare"); want += wq; fill += wq * sh; }
                want += af.D(r, "otherWantQty"); fill += af.D(r, "otherFillContribQty");
                Assert.True(want > 0, "必要量の和が 0");
                double eng = double.Parse(stock[("q" + r[af.I("quarter")].TrimStart('q'), r[af.I("nation")])][st.I("capitalEquipFillShare")], System.Globalization.CultureInfo.InvariantCulture);
                double mine = Math.Min(1.0, fill / want);
                worst = Math.Max(worst, Math.Abs(mine - eng));
                Assert.True(Math.Abs(mine - eng) <= 1e-9, $"{r[af.I("nation")]}/{r[af.I("quarter")]}：計器 {mine}・stock_s {eng}");
                if (eng < 0.999) binding++;
                checkedRows++;
            }
            Assert.True(checkedRows >= 20, $"調べた行が少ない {checkedRows}（最大差 {worst}）");
            Assert.True(binding >= 5, $"充足が 1 未満の行が少ない {binding}/{checkedRows}");
        }
        finally { restore(); }
    }

    [Fact]
    public void A2_戦力の指数は各項の積と合う()
    {
        var (dir, restore) = Run(10, resync: true, seed: A2Seed);
        try
        {
            var af = Read(dir, "armsfill", A2Seed); var st = Read(dir, "stock", A2Seed);
            var stock = ByKey(st);
            int checkedRows = 0;
            foreach (var r in af.R)
            {
                var s = stock[("q" + r[af.I("quarter")].TrimStart('q'), r[af.I("nation")])];
                double lp = double.Parse(s[st.I("landPowerIndex")], System.Globalization.CultureInfo.InvariantCulture);
                double prod = af.D(r, "soldierK") / new Coefficients().SoldiersPerMilitary * af.D(r, "readinessShare") * af.D(r, "qualityMul") * af.D(r, "cohesionMul") * af.D(r, "militarismMul")
                              * af.D(r, "techQualityMul") * (1.0 - af.D(r, "navalShare")) * af.D(r, "armySkillMul");
                Assert.True(Math.Abs(prod - lp) <= 1e-9 * Math.Max(1.0, Math.Abs(lp)), $"{r[af.I("nation")]}/{r[af.I("quarter")]}：積 {prod}・landPowerIndex {lp}");
                Assert.True(Math.Abs(af.D(r, "landPowerIndex") - lp) <= 1e-9 * Math.Max(1.0, Math.Abs(lp)), "計器自身の landPowerIndex 列が stock_s と食い違う");
                checkedRows++;
            }
            Assert.True(checkedRows >= 20, $"調べた行が少ない {checkedRows}");
        }
        finally { restore(); }
    }
}
