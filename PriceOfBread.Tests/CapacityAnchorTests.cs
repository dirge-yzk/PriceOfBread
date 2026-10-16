using System.Linq;
using System.Reflection;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(369) 扶養力の錨を州と一緒に動かす 段0 の形（休眠）の番人。事前登録 measurements/an_20261016_prereg.txt。
/// 形＝州の定数 <see cref="Region.AnchorPopulation"/>（構築の 1 回だけ写す）と、係数 <see cref="Coefficients.CapacityAnchorFromRegions"/>（既定 0＝旧の錨 pop − PopulationDelta）。
/// 期待値は本体の式から作らず、探りの数（measurements/an_20261016/probe0_open.txt）をリテラルで書く。旧の式との比べは相対 1e-9 の許容差
/// （州の和は pop − PopulationDelta とビット一致しえない＝pop は毎月州の和を取り直し・台帳は累積の演算）。
/// 突然変異で赤になる箇所：門の `&& !anchorFromRegions`（錨 0 の国が凍る）・capacity の門（耕地の無い世界が NaN になる）・旗 0 の読み（旗 0 でも欄を読むと軌道が動く）。</summary>
[Collection("Instruments")]
public class CapacityAnchorTests
{
    private static string Name(MultiNationSim sim, int i) => sim.Nations[i].World.SelfPower!.Name;
    private static World Of(MultiNationSim sim, string name) => sim.Nations.First(n => n.World.SelfPower!.Name == name).World;
    private static double AnchorSum(World w) { double s = 0; foreach (var r in w.Regions) s += r.AnchorPopulation; return s; }
    private static double OldAnchor(World w) => w.TotalPopulation - w.PopulationDelta;

    // ⚠MultiNationSim の係数は国の World.Coef に届かない（国ごとに別のインスタンス）＝旗は組んだ後に各国の World.Coef へ書く
    private static MultiNationSim Two(string a, string b, double flag, ulong seed = 83)
    {
        var sim = new MultiNationSim([WorldData.ByName(a), WorldData.ByName(b)], seed: seed) { DiplomacyFrozen = true };
        foreach (var n in sim.Nations) n.World.Coef.CapacityAnchorFromRegions = flag;
        return sim;
    }

    [Fact]
    public void 器の既定は休眠()
    {
        Assert.Equal(0.0, new Coefficients().CapacityAnchorFromRegions);
        Assert.Equal(0.0, new Region().AnchorPopulation);
    }

    [Fact]
    public void 開幕の61国_州の和が旧の錨に合い_植民地格の州は0()
    {
        var sim = new MultiNationSim(WorldData.Nations(), 1UL, _ => new UtilityAgent());
        Assert.Equal(61, sim.Nations.Count);
        int withColonies = 0;
        foreach (var n in sim.Nations)
        {
            var w = n.World; double old = OldAnchor(w), sum = AnchorSum(w);
            Assert.True(Math.Abs(sum - old) <= 1e-9 * Math.Max(1.0, old), $"{w.SelfPower!.Name}：州の和 {sum} が旧の錨 {old} に合わない");
            bool colony = false;
            foreach (var r in w.Regions)
            {
                if (r.Tier == RegionTier.Colonial) { Assert.Equal(0.0, r.AnchorPopulation); colony = true; }
                else Assert.True(r.AnchorPopulation > 0, $"{w.SelfPower!.Name}・{r.Name}：本国の州の錨が 0");
            }
            if (colony) withColonies++;
        }
        Assert.Equal(8, withColonies);   // 探りの数（仏・英・露・蘭・西・オマーン・デンマーク・葡）
        var pt = Of(sim, "ポルトガル");
        Assert.Equal(3218.4, AnchorSum(pt), 6);                                  // 探りの数（本国の 2 州）
        Assert.Equal(5668.4, pt.TotalPopulation, 6);                              // 植民地格の 9 州 2450 が台帳に入った総人口
        Assert.Equal(2450.0, pt.PopulationDelta, 6);
        Assert.Equal(2, pt.Regions.Count(r => r.AnchorPopulation > 0)); Assert.Equal(9, pt.Regions.Count(r => r.Tier == RegionTier.Colonial));
        var uk = Of(sim, "イギリス");
        Assert.Equal(16363.8, AnchorSum(uk), 6); Assert.Equal(31450.5, uk.PopulationDelta, 6);
    }

    [Fact]
    public void 割譲で錨は州と一緒に移り_旧の錨は動かない()
    {
        var sim = Two("イギリス", "フランス", 1.0);
        var uk = Of(sim, "イギリス"); var fr = Of(sim, "フランス");
        double ukSum = AnchorSum(uk), frSum = AnchorSum(fr), ukOld = OldAnchor(uk), frOld = OldAnchor(fr);
        var core = uk.Regions.First(r => r.Id != 0 && r.Tier == RegionTier.Core && r.State == RegionState.Active);
        double a = core.SumPopulation();                    // 構築直後＝州の開幕の人口（別の出所）
        Assert.Equal(a, core.AnchorPopulation, 9);
        Territory.TransferRegion(uk, fr, core, Territory.Rehoming.Ceded);
        Assert.Equal(ukSum - a, AnchorSum(uk), 6); Assert.Equal(frSum + a, AnchorSum(fr), 6);   // 州と一緒に移る
        Assert.Equal(ukOld, OldAnchor(uk), 6); Assert.Equal(frOld, OldAnchor(fr), 6);           // 旧の錨は不変（pop も台帳も同じ量だけ動く）
        // 植民地格の州は錨 0＝移しても和は動かない
        var col = uk.Regions.First(r => r.Tier == RegionTier.Colonial);
        double ukSum1 = AnchorSum(uk), frSum1 = AnchorSum(fr);
        Territory.TransferRegion(uk, fr, col, Territory.Rehoming.Ceded);
        Assert.Equal(ukSum1, AnchorSum(uk), 9); Assert.Equal(frSum1, AnchorSum(fr), 9);
    }

    [Fact]
    public void 建国の子の州は親の定数を持ち越し_構築で上書きしない()
    {
        var cfg = WorldData.ByName("フランス");
        var regions = (System.Collections.Generic.List<Region>)typeof(Simulation).GetMethod("BuildNation", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, [cfg, new Coefficients(), true])!;
        Assert.All(regions, r => Assert.True(r.AnchorPopulation == 0.0));   // 組んだだけでは書かない（Simulation の構築が写す）
        foreach (var r in regions) r.AnchorPopulation = 123.0;
        var sc = new Scenario { Seed = 1, Kind = ScenarioKind.Nation, NationConfig = cfg, PrebuiltRegions = regions, Coef = new Coefficients(), SharedWorld = true };
        var child = new Simulation(sc, new UtilityAgent());
        Assert.All(child.World.Regions, r => Assert.Equal(123.0, r.AnchorPopulation));
        // 対照：PrebuiltRegions でない構築は人口を写す
        var sc2 = new Scenario { Seed = 1, Kind = ScenarioKind.Nation, NationConfig = cfg, Coef = new Coefficients(), SharedWorld = true };
        var own = new Simulation(sc2, new UtilityAgent());
        Assert.All(own.World.Regions, r => Assert.Equal(r.SumPopulation(), r.AnchorPopulation, 9));
        Assert.True(own.World.Regions.All(r => r.AnchorPopulation > 0));
    }

    private static MultiNationSim PortugalRemainder(double flag)
    {
        var sim = Two("ポルトガル", "スペイン", flag);
        var pt = Of(sim, "ポルトガル"); var es = Of(sim, "スペイン");
        // 本国の 2 州と、植民地格の 9 州のうち最小の 1 州以外を譲る＝残りの国は小さい植民地の州 1 つ（(367) のクイアバ型）
        var keep = pt.Regions.Where(x => x.AnchorPopulation <= 0).OrderBy(x => x.SumPopulation()).First();
        foreach (var r in pt.Regions.Where(x => x != keep).ToList()) Territory.TransferRegion(pt, es, r, Territory.Rehoming.Ceded);
        Assert.Single(pt.Regions);
        return sim;
    }

    [Fact]
    public void 本国を失った残りの国は旧では1783年の錨のまま_旗1では人口の段が土地の扶養力で回る()
    {
        // 旧（旗 0）：錨は 3218.4 のまま＝下限 0.3×錨 の 965.52 以上の扶養力
        var off = PortugalRemainder(0.0); var wo = Of(off, "ポルトガル");
        Assert.Equal(0, wo.Regions.Count(r => r.AnchorPopulation > 0));
        Assert.Equal(3218.4, OldAnchor(wo), 6);
        off.Nations.First(n => n.World == wo).DemographicStep();
        Assert.True(wo.DiagCapacity >= 0.3 * 3218.4 - 1e-6, $"旧の扶養力 {wo.DiagCapacity} が下限 965.52 を下回る");
        // 新（旗 1）：錨 0・扶養力は Σ耕地で決まる（LandAnchorCapacityWeight=1）。人口の段は飛ばない
        var on = PortugalRemainder(1.0); var wn = Of(on, "ポルトガル");
        double arable = wn.Regions.Where(r => r.State == RegionState.Active).Sum(r => r.ArableLand);
        Assert.True(arable > 1 && arable < 1e8, "前提：植民地の州の耕地は有限");
        double p0 = wn.TotalPopulation;
        on.Nations.First(n => n.World == wn).DemographicStep();
        Assert.Equal(arable, wn.DiagLandAnchor, 9);                                  // 耕地の和（別の出所）
        Assert.Equal(arable, wn.DiagCapacityBaseline, 9);                            // 錨 0 ＋ 1.0×(Σ耕地 − 0)
        Assert.True(wn.DiagCapacity > 0 && wn.DiagCapacity < 0.3 * 3218.4, $"旗 1 の扶養力 {wn.DiagCapacity} が土地で決まらない");
        Assert.NotEqual(p0, wn.TotalPopulation);                                    // 凍っていない（出生と死が動いた）
        Assert.True(double.IsFinite(wn.TotalPopulation));
    }

    [Fact]
    public void 旗1で錨も耕地も無い世界は人口の段を飛ばし_NaNにならない()
    {
        var c = new Coefficients { CapacityAnchorFromRegions = 1.0 };
        var sim = new Simulation(new Scenario { Seed = 1, Kind = ScenarioKind.RegionalFamine, Coef = c }, new NullAgent());
        foreach (var r in sim.World.Regions) { r.AnchorPopulation = 0.0; r.ArableLand = 1e9; }   // 較正の小世界と同じ（耕地の集計から外れる）
        double p0 = sim.World.TotalPopulation, d0 = sim.World.PopulationDelta;
        sim.DemographicStep();
        Assert.Equal(p0, sim.World.TotalPopulation); Assert.Equal(d0, sim.World.PopulationDelta);
        Assert.True(double.IsFinite(sim.World.TotalPopulation));
    }

    private static (long Pop, long Money)[] Digest(MultiNationSim sim)
        => [.. sim.Nations.Select(n => (BitConverter.DoubleToInt64Bits(n.World.TotalPopulation), BitConverter.DoubleToInt64Bits(n.World.TotalMoney.Value)))];

    [Fact]
    public void 旗0は欄を読まない_旗1は読む()
    {
        const int quarters = 4;
        (long, long)[] Run(double flag, double scale)
        {
            var sim = Two("イギリス", "フランス", flag);
            foreach (var n in sim.Nations) foreach (var r in n.World.Regions) r.AnchorPopulation *= scale;
            for (int q = 0; q < quarters; q++) sim.StepQuarter();
            return Digest(sim);
        }
        Assert.Equal(Run(0.0, 1.0), Run(0.0, 2.0));       // 旗 0：欄を 2 倍にしても軌道はビット一致（誰も読まない）
        Assert.NotEqual(Run(1.0, 1.0), Run(1.0, 2.0));    // 旗 1：錨が倍＝扶養力が動く（何もしない機構が緑にならない）
    }

    [Fact]
    public void 計器anchor_sの列が定義どおりに出る()
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_anchor_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        var sim = Two("イギリス", "フランス", 0.0, 1789UL);
        try
        {
            for (int q = 0; q < 3; q++) { sim.StepQuarter(); Instruments.Quarter(sim, 1789UL, q); }
        }
        finally { Instruments.Close(sim); Instruments.Configure(null, null); }
        try
        {
            var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, "anchor_s1789.tsv"));
            Assert.Equal(Instruments.AnchorHeader, lines[0]);
            Assert.Equal(1 + 2 * 3, lines.Length);   // 挙げた国（*）は毎四半期・2 国×3 四半期
            var h = lines[0].Split('\t');
            int Ix(string n) => Array.IndexOf(h, n);
            foreach (var row in lines.Skip(1).Select(l => l.Split('\t')))
            {
                Assert.Equal(h.Length, row.Length);
                double D(string n) => double.Parse(row[Ix(n)], System.Globalization.CultureInfo.InvariantCulture);
                Assert.Equal(0.0, D("flag")); Assert.True(Math.Abs(D("anchorUsed") - D("anchorOld")) <= 1e-9 * D("anchorOld"));   // 旗 0＝旧の錨を使う（人口の段が使った実値・月初と四半期末で丸めが別＝相対 1e-9）
                Assert.True(Math.Abs(D("anchorRegions") - D("anchorOld")) <= 1e-9 * D("anchorOld"));
                Assert.True(D("capacity") > 0);
                if (row[Ix("nation")] == "イギリス") { Assert.Equal(31.0, D("regionsN")); Assert.Equal(5.0, D("anchorRegionsN")); Assert.Equal(16363.8, D("anchorRegions"), 4); }
                if (row[Ix("nation")] == "フランス") { Assert.Equal(13.0, D("regionsN")); Assert.Equal(7.0, D("anchorRegionsN")); }
            }
        }
        finally { try { System.IO.Directory.Delete(dir, true); } catch { } }
    }

    [Fact]
    public void 計器anchor_sのanchorUsedは人口の段が実際に使った値を読む()
    {
        // 旗 1 で州の欄を 2 倍にすると、人口の段の錨は anchorOld（旧の式）の約 2 倍になる。anchorUsed が旗から再計算した値なら、人口の段の足し算を消しても緑になる（何もしない機構）
        // ＝人口の段が書く純記帳 World.DiagAnchorBaseline を読む。突然変異：DemographicStep の `baseline += ar.AnchorPopulation` を外すと anchorUsed が 0 になり赤。
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_anchor_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        var sim = Two("イギリス", "フランス", 1.0, 1790UL);
        foreach (var n in sim.Nations) foreach (var r in n.World.Regions) r.AnchorPopulation *= 2.0;
        try
        {
            for (int q = 0; q < 3; q++) { sim.StepQuarter(); Instruments.Quarter(sim, 1790UL, q); }
        }
        finally { Instruments.Close(sim); Instruments.Configure(null, null); }
        try
        {
            var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, "anchor_s1790.tsv"));
            var h = lines[0].Split('	');
            int Ix(string n) => Array.IndexOf(h, n);
            foreach (var row in lines.Skip(1).Select(l => l.Split('	')))
            {
                double D(string n) => double.Parse(row[Ix(n)], System.Globalization.CultureInfo.InvariantCulture);
                Assert.Equal(1.0, D("flag"));
                Assert.True(Math.Abs(D("anchorUsed") - D("anchorRegions")) <= 1e-9 * D("anchorRegions"));   // 人口の段が使った錨＝州の欄の和
                Assert.True(D("anchorUsed") > 1.9 * D("anchorOld"));                                          // 旧の錨の約 2 倍（欄を 2 倍にした）
            }
        }
        finally { try { System.IO.Directory.Delete(dir, true); } catch { } }
    }
}
