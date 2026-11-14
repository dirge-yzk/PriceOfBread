using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (1124) 線 c109t0：貿易の置き換え T0a の計器 <c>s4cost_s</c>・<c>s7staple_s</c> と <c>prodcap_s</c> の末尾 4 列の自己検査。事前登録 measurements/m1124_20261114_prereg_t0.txt [3]。
/// T1＝P̂（計器が生の欄から書き直した式）＝SupplyTargetAt の産出（1e-12）。T2＝S4 の 3 項の和＝エンジンの単位費用×為替（相対 1e-9）。
/// T3＝S7 の合計＝nationgood_s の主食 7 財の行の和。T4＝prodcap_s の profitFactor の空の欄（眠り・倒産・Capacity 0 は空・0 にしない）。T5＝表の形。
/// 何もしない計器は 0 行で緑になる＝全ての検査に〈調べた行の数の下限〉を置く。期待値は式から作り、計器の合計の列を鵜呑みにしない。
/// </summary>
[Collection("Instruments")]
public class BuyShadowInstrumentTests
{
    private const ulong Seed = 1789UL;
    private static NationConfig[] Configs() => [NationConfig.France(), NationConfig.Prussia(), NationConfig.Britain()];

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
    private static bool Near(double a, double b, double tol) => System.Math.Abs(a - b) <= tol * System.Math.Max(1.0, System.Math.Max(System.Math.Abs(a), System.Math.Abs(b)));

    private static (string dir, MultiNationSim sim, Action restore) Run(int quarters)
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_bs_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        int every0 = Instruments.NgEveryYears; Instruments.NgEveryYears = 1;
        var sim = new MultiNationSim(Configs(), Seed, _ => new UtilityAgent());
        sim.DiplomacyFrozen = true;
        Instruments.Phase(sim, Seed, "open");
        sim.StartWarmup();
        Instruments.Phase(sim, Seed, "warm");
        try { for (int q = 0; q < quarters; q++) { sim.StepQuarter(); Instruments.Quarter(sim, Seed, q); } }
        finally { Instruments.Close(sim); }
        return (dir, sim, () => { Instruments.NgEveryYears = every0; Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } });
    }

    private static IEnumerable<(World W, Region R, Producer P)> AllProducers(MultiNationSim sim)
    {
        foreach (var n in sim.Nations) foreach (var r in n.World.Regions) foreach (var p in r.Producers)
            if (p.GoodId >= 0 && p.GoodId < r.Markets.Length) yield return (n.World, r, p);
    }

    [Fact]
    public void T1_P_hat_は供給の産出と合う()
    {
        var (dir, sim, restore) = Run(12);
        try
        {
            // 値段と在庫を市場ごとに散らして、採算の上・境・下と在庫の不足・過剰を全て通す（読むだけの検査の後なので世界は進めない）。
            int k = 0;
            foreach (var n in sim.Nations) foreach (var r in n.World.Regions) foreach (var m in r.Markets)
            {
                k++;
                m.Price *= 0.4 + (k % 7) * 0.25;
                m.Stock *= (k % 3) == 0 ? 0.0 : (k % 3) == 1 ? 1.0 : 25.0;
            }
            int rows = 0, partial = 0, moved = 0, positive = 0;
            foreach (var (w, r, p) in AllProducers(sim))
            {
                var c = w.Coef;
                bool isLabor = p.GoodId is Catalog.LaborId or Catalog.FarmLaborId;
                bool baumol = Catalog.Goods[p.GoodId].Tier == GoodTier.Service && !isLabor;
                double gate = Discovery.Gate(r, p);
                double got = EconomyStep.SupplyTargetAt(p, r, c, w, baumol, gate, out double pf);
                double hat = Instruments.BsPHat(p, r, c, w, baumol, gate, p.Output);
                Assert.True(Near(got, hat, 1e-12), $"P̂ {hat} ≠ 供給 {got}（{Catalog.Goods[p.GoodId].Name}）");
                rows++; if (pf > 0 && pf < 1) partial++; if (!Near(got, p.Output, 1e-9)) moved++; if (got > 0) positive++;
            }
            Assert.True(rows > 1000, "調べた行が少ない " + rows);
            Assert.True(partial >= 20, "採算が境の行が少ない " + partial);
            Assert.True(moved >= 100 && positive >= 100, $"動く行・正の行が少ない {moved}/{positive}");
        }
        finally { restore(); }
    }

    [Fact]
    public void T2_S4の3項の和は単位費用と合う()
    {
        var (dir, sim, restore) = Run(12);
        try
        {
            var t = Read(dir, "s4cost");
            int rows = 0, fxMoved = 0;
            foreach (var r in t.R)
            {
                double eng = t.D(r, "unitCostFxSumEngine"), sum = t.D(r, "selfFxCostSum") + t.D(r, "gradeFxCostSum") + t.D(r, "inputFxCostSum");
                Assert.True(eng >= 0, "単位費用が負 " + eng);
                if (eng > 0) rows++;
                Assert.True(System.Math.Abs(sum - eng) <= 1e-9 * System.Math.Max(1.0, eng), $"3 項の和 {sum} ≠ 単位費用 {eng}（{r[t.I("nation")]} {r[t.I("year")]}）");
                if (System.Math.Abs(t.D(r, "fxRate") - 1.0) > 1e-6) fxMoved++;
            }
            Assert.True(rows >= 6, "正の行が少ない " + rows);
            Assert.True(fxMoved >= 1, "為替が 1 でない行が無い（為替を掛け忘れる誤りを拾えない）");
        }
        finally { restore(); }
    }

    [Fact]
    public void T3_S7の合計はnationgoodの主食の行の和と合う()
    {
        var (dir, sim, restore) = Run(12);
        try
        {
            var s7 = Read(dir, "s7staple"); var ng = Read(dir, "nationgood");
            var staple = new HashSet<string>();
            for (int g = 0; g < Catalog.Goods.Length; g++) if (Catalog.Goods[g].Tier == GoodTier.Staple || Catalog.Goods[g].GrainChain) staple.Add(Catalog.Goods[g].Name);
            Assert.Equal(7, staple.Count);
            var sum = new Dictionary<(string, string), (double Imp, double Dem, double Sh)>();
            foreach (var r in ng.R)
            {
                if (!staple.Contains(r[ng.I("good")])) continue;
                var key = (r[ng.I("year")], r[ng.I("nation")]);
                sum.TryGetValue(key, out var a);
                sum[key] = (a.Imp + ng.D(r, "crossImportQty"), a.Dem + ng.D(r, "demandQty"), a.Sh + ng.D(r, "shortageQty"));
            }
            int rows = 0, imports = 0;
            foreach (var r in s7.R)
            {
                if (int.Parse(r[s7.I("q")]) % 4 != 3) continue;
                Assert.True(sum.TryGetValue((r[s7.I("year")], r[s7.I("nation")]), out var e), "nationgood_s に行が無い " + r[s7.I("nation")]);
                double imp = s7.D(r, "importQty"), dem = s7.D(r, "demandQty"), sh = s7.D(r, "shortageQty");
                Assert.True(Near(imp, e.Imp, 1e-9) && Near(dem, e.Dem, 1e-9) && Near(sh, e.Sh, 1e-9), $"S7 {imp}/{dem}/{sh} ≠ nationgood {e.Imp}/{e.Dem}/{e.Sh}（{r[s7.I("nation")]} {r[s7.I("year")]}）");
                rows++; if (e.Imp > 0) imports++;
            }
            Assert.True(rows >= 6, "年の末の行が少ない " + rows);
            Assert.True(imports >= 1, "主食の輸入が正の行が無い（財を落とす誤りを拾えない）");
            Assert.True(s7.R.Count >= 30, "四半期の行が少ない " + s7.R.Count);
        }
        finally { restore(); }
    }

    [Fact]
    public void T4_profitFactorは眠り_倒産_Capacity0で空()
    {
        var (dir, sim, restore) = Run(12);
        try
        {
            int empty = 0, valued = 0, sleeping = 0;
            foreach (var (w, r, p) in AllProducers(sim))
            {
                string cell = Instruments.BsProfitFactorCell(p, r, w.Coef);
                bool shouldEmpty = p.Bankrupt || p.Capacity == 0.0 || EconomyStep.SleepState(r, p, w.Coef) == 1;
                if (EconomyStep.SleepState(r, p, w.Coef) == 1) sleeping++;
                if (shouldEmpty) { Assert.Equal("", cell); empty++; }
                else
                {
                    Assert.True(cell.Length > 0, "値の行が空");
                    Assert.True(Near(double.Parse(cell, System.Globalization.CultureInfo.InvariantCulture), p.DesiredOutput / p.Capacity, 1e-12));
                    valued++;
                }
            }
            Assert.True(empty >= 10 && valued >= 100 && sleeping >= 1, $"空 {empty}・値 {valued}・眠り {sleeping}");
            // 眠り・倒産は Capacity が 0 の行と重なって見えにくい＝能力が正の盤面を作って、眠りだけ・倒産だけで空になることを通す（M5＝眠りに 0 を書く誤りの穴）
            int forcedSleep = 0, forcedBankrupt = 0;
            foreach (var (w, r, p) in AllProducers(sim))
            {
                if (EconomyStep.SleepState(r, p, w.Coef) == 1 && forcedSleep < 20)
                {
                    p.Capacity = 5.0; p.DesiredOutput = 3.0;
                    Assert.Equal(1, EconomyStep.SleepState(r, p, w.Coef));
                    Assert.Equal("", Instruments.BsProfitFactorCell(p, r, w.Coef)); forcedSleep++;
                }
                else if (EconomyStep.SleepState(r, p, w.Coef) == 0 && !p.Bankrupt && p.Capacity > 0 && forcedBankrupt < 20)
                {
                    Assert.NotEqual("", Instruments.BsProfitFactorCell(p, r, w.Coef));
                    p.Bankrupt = true;
                    Assert.Equal("", Instruments.BsProfitFactorCell(p, r, w.Coef)); forcedBankrupt++;
                }
            }
            Assert.True(forcedSleep >= 1 && forcedBankrupt >= 1, $"作った眠り {forcedSleep}・倒産 {forcedBankrupt}");
            // 書かれた表でも、Capacity が 0 の行の profitFactor は空（0 でない）
            var pc = Read(dir, "prodcap"); int capZero = 0;
            foreach (var r in pc.R)
                if (pc.D(r, "capacity") == 0.0) { Assert.Equal("", r[pc.I("profitFactor")]); capZero++; }
            Assert.True(capZero >= 1, "Capacity 0 の行が表に無い");
        }
        finally { restore(); }
    }

    [Fact]
    public void T5_表の形()
    {
        var (dir, sim, restore) = Run(8);
        try
        {
            foreach (var kind in new[] { "s4cost", "s7staple", "prodcap" })
            {
                var t = Read(dir, kind);
                Assert.True(t.R.Count > 0, kind + " が 0 行");
                foreach (var r in t.R) Assert.Equal(t.H.Length, r.Length);
            }
            var p = Read(dir, "prodcap");
            Assert.Equal(["laborPopIndex", "scale", "lastInputFill", "profitFactor"], p.H.Skip(p.H.Length - 4).ToArray());
            Assert.Equal(Instruments.S4costHeaderText, string.Join('\t', Read(dir, "s4cost").H));
        }
        finally { restore(); }
    }
}
