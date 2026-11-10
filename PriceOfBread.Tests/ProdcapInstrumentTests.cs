using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (1019) 線 c95pc：貿易の段0″ の計器 <c>prodcap_s</c>（年×州×生産者の播種の能力と賦存の級・読むだけ）の番人。事前登録 measurements/m1019_20261110_prereg_pc.txt [3]。
/// P1＝(年,国,財) の Σ output が nationgood_s の producedQty と合う（量の保存・Capacity を書くと赤）。P2＝書いた productivity が 財の生産性×7.0×endowFromProductivity と合う（級 X/E/Z/N/S/F の全てを含む）。
/// P3＝Mu5 を拾う：構築の後に Productivity を 2 倍にしても RoRead の級・規則値・豊富側の費用が動かない（級を Productivity から決める変異で赤）。P4＝見出しと年の規則（NgYear の年でなくても開幕の最初の年は書く）。
/// 何もしない計器は 0 行で緑になる＝全ての門に〈正の行が 1 つ以上ある〉対照を置く。
/// </summary>
[Collection("Instruments")]
public class ProdcapInstrumentTests
{
    private const ulong Seed = 1789UL;
    private static NationConfig Sicily() => WorldData.Nations().First(n => n.Provinces.Any(p => p.City == "パレルモ"));
    private static NationConfig[] Configs() => [NationConfig.France(), NationConfig.Prussia(), NationConfig.Britain(), NationConfig.Austria(), Sicily()];
    private static MultiNationSim Make() => new(Configs(), Seed, _ => new UtilityAgent(), null);

    private sealed class Tab
    {
        public string[] H = []; public List<string[]> R = [];
        public int I(string n) { int i = Array.IndexOf(H, n); Assert.True(i >= 0, "列が無い " + n); return i; }
        public double D(string[] r, string n) { var s = r[I(n)]; return s == "nan" ? double.NaN : double.Parse(s, System.Globalization.CultureInfo.InvariantCulture); }
    }
    private static Tab Read(string dir, string kind)
    {
        var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, $"{kind}_s{Seed}.tsv"));
        return new Tab { H = lines[0].Split('\t'), R = lines.Skip(1).Select(l => l.Split('\t')).ToList() };
    }

    private static (string dir, Action restore) Run(int quarters, int everyYears)
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_pc_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        int every0 = Instruments.NgEveryYears; Instruments.NgEveryYears = everyYears;
        var sim = Make();
        foreach (var n in sim.Nations) n.World.Coef.CivilWarLandslideRatio = 0;
        sim.DiplomacyFrozen = true;
        Instruments.Phase(sim, Seed, "open");
        sim.StartWarmup();
        Instruments.Phase(sim, Seed, "warm");
        try { for (int q = 0; q < quarters; q++) { sim.StepQuarter(); Instruments.Quarter(sim, Seed, q); } }
        finally { Instruments.Close(sim); }
        return (dir, () => { Instruments.NgEveryYears = every0; Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } });
    }

    /// <summary>P1：量の保存。nationgood_s にある年だけで (年,国,財) の Σ output が producedQty と合う（1e-9）。下側＝書かないと対照の件数で赤・上側＝Capacity を書くと赤。</summary>
    [Fact]
    public void P1_産出の和はnationgoodの産出と合う()
    {
        var (dir, restore) = Run(12, 1);
        try
        {
            var pc = Read(dir, "prodcap"); var ng = Read(dir, "nationgood");
            var sum = new Dictionary<(string, string, string), double>();
            int iy = pc.I("year"), inat = pc.I("ownerNation"), ig = pc.I("good");
            foreach (var r in pc.R) { var k = (r[iy], r[inat], r[ig]); sum[k] = sum.GetValueOrDefault(k) + pc.D(r, "output"); }
            int cmp = 0, pos = 0;
            foreach (var r in ng.R)
            {
                double want = ng.D(r, "producedQty"); var k = (r[ng.I("year")], r[ng.I("nation")], r[ng.I("good")]);
                double got = sum.GetValueOrDefault(k);
                Assert.True(Math.Abs(got - want) <= 1e-9 * Math.Max(Math.Abs(want), 1e-300) + 1e-12, $"{k}：計器 {got}・nationgood {want}");
                cmp++; if (want > 1e-6) pos++;
            }
            Assert.True(cmp >= 1000, $"調べた組が少ない {cmp}");
            Assert.True(pos >= 200, $"産出が正の組が少ない {pos}");
            Assert.True(pc.R.Count >= 20000, $"行が少ない {pc.R.Count}");
        }
        finally { restore(); }
    }

    /// <summary>P2：書いた productivity÷(財の生産性×7.0) が endowFromProductivity と合う（nan の行は級 F・役務・代替レシピ）。級 X・E・Z・N・S・F が全て現れる。</summary>
    [Fact]
    public void P2_生産性と賦存の倍率の突き合わせ()
    {
        var (dir, restore) = Run(4, 1);
        try
        {
            var pc = Read(dir, "prodcap");
            var prod = new Dictionary<string, double>();
            foreach (var g in Catalog.Goods) prod.TryAdd(g.Name, g.Productivity);
            int ig = pc.I("good"), ic = pc.I("class"); int cmp = 0, bad = 0; var cls = new HashSet<string>();
            foreach (var r in pc.R)
            {
                cls.Add(r[ic]);
                double e = pc.D(r, "endowFromProductivity");
                if (double.IsNaN(e)) continue;
                double want = pc.D(r, "productivity") / (prod[r[ig]] * 7.0);
                cmp++; if (Math.Abs(want - e) > 1e-9 * Math.Max(1.0, Math.Abs(e))) bad++;
            }
            Assert.Equal(0, bad);
            Assert.True(cmp >= 1000, $"比べた行が少ない {cmp}");
            foreach (var k in new[] { "X", "E", "Z", "N", "S", "F" }) Assert.Contains(k, cls);
        }
        finally { restore(); }
    }

    /// <summary>P3：Mu5 を拾う。構築の後に Productivity を 2 倍にしても、RoRead の級・規則値・豊富側の費用は動かない（動くのは endow だけ＝摂動が効いた対照）。級 X・E・N・S の各 1 個体。</summary>
    [Fact]
    public void P3_級はProductivityから決まらない()
    {
        var sim = Make();
        var found = new Dictionary<string, (World w, Region r, Producer p)>();
        foreach (var n in sim.Nations)
            foreach (var r in n.World.Regions)
                foreach (var p in r.Producers)
                {
                    if (!p.IsRgo) continue;
                    var x = Instruments.RoRead(n.World, r, p);
                    if ((x.Class is "X" or "E" or "N" or "S") && !found.ContainsKey(x.Class)) found[x.Class] = (n.World, r, p);
                }
        foreach (var k in new[] { "X", "E", "N", "S" }) Assert.True(found.ContainsKey(k), "級の対照が無い " + k);
        foreach (var (k, (w, r, p)) in found)
        {
            var before = Instruments.RoRead(w, r, p);
            double p0 = p.Productivity;
            p.Productivity = p0 * 2.0;
            try
            {
                var after = Instruments.RoRead(w, r, p);
                Assert.Equal(before.Class, after.Class);
                Assert.Equal(before.Rule, after.Rule);
                Assert.Equal(before.IfAbundance, after.IfAbundance);
                Assert.Equal(before.Endow * 2.0, after.Endow, 9);   // 摂動が実際に効いた（何も読まない読み手では緑にならない）
            }
            finally { p.Productivity = p0; }
        }
    }

    /// <summary>P4：見出しの宣言（15 列）と年の規則。NgYear の年が無い走りでも開幕の最初の q%4==3 の年を 1 回だけ書く。</summary>
    [Fact]
    public void P4_見出しと開幕の年()
    {
        var (dir, restore) = Run(12, 1000);   // 1000 年おき＝NgYear の年は 1913 だけ＝この走りでは開幕の年しか出ない
        try
        {
            var pc = Read(dir, "prodcap");
            Assert.Equal("year\tq\tregion\townerNation\tgood\trecipe\tisRgo\tclass\tproductivity\tendowFromProductivity\tcapacity\toutput\tmarginalCost\tsiteRow\tsiteFound", string.Join('\t', pc.H));
            Assert.True(pc.R.Count >= 10000, $"行が少ない {pc.R.Count}");
            var years = pc.R.Select(r => r[pc.I("year")]).Distinct().ToList();
            Assert.Single(years);
            Assert.All(pc.R, r => Assert.Equal("3", r[pc.I("q")]));
            Assert.Contains(pc.R, r => r[pc.I("isRgo")] == "1" && r[pc.I("siteFound")] == "0");
        }
        finally { restore(); }
    }
}
