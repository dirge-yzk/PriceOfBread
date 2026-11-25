using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (1358) 線 c143sg：計器 <c>tsreg_s</c>（州×主食の財の輸入の門の止まり方・読むだけ）の番人。事前登録 measurements/m1358_20261125_prereg_sg.txt [3]。
/// T1＝手で置いた盤面（内陸の州の容量が 0・港の州は 0 でない）で xCap が内陸の州にだけ付く。T2＝州ごとの和が tradeskip_s の国×財の行と一致（正の対照つき）。T3＝計器を切ると何も作らない。
/// </summary>
[Collection("Instruments")]
public class TsregInstrumentTests
{
    private const ulong Seed = 1789UL;
    private static readonly string[] SumCols = ["rows", "rowIdle", "rowNoNeed", "rowBudget0", "xSoldOut", "xCap", "xWedge", "xPass"];
    private static readonly string[] ExitCols = ["exPriceHigh", "exXPriceSmall", "exXNeedSmall", "exYZero", "exBudget", "exCapMid", "exDealt", "exGuard"];
    private static readonly string[] SkipExitCols = ["xpEndPriceHigh", "xpEndXPriceSmall", "xpEndXNeedSmall", "xpEndYZero", "xpEndBudget", "xpEndCapMid", "xpEndDealt", "xpEndGuard"];
    private static readonly string[] SkipSumCols = ["rows", "rowIdle", "rowNoNeed", "rowBudget0", "xSoldOut", "xCap", "xWedge", "xPass"];

    private static (string[] H, List<string[]> R) Read(string dir, string kind)
    {
        var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, $"{kind}_s{Seed}.tsv"));
        return (lines[0].Split('\t'), lines.Skip(1).Select(l => l.Split('\t')).ToList());
    }
    private static bool IsStaple(string goodName) => Catalog.Goods.Any(g => g.Name == goodName && (g.Tier == GoodTier.Staple || g.GrainChain));

    [Fact]
    public void T1_内陸の州の容量切れは内陸の州の_xCap_に付き港の州には付かない()
    {
        int gc = Catalog.Goods.Length, bread = Catalog.BreadId;
        Assert.True(IsStaple(Catalog.Goods[bread].Name));
        var inland = new Region { Name = "内陸", IsPort = false }; var port = new Region { Name = "港", IsPort = true }; var seller = new Region { Name = "売り手", IsPort = true };
        var tr = new (int nation, Region r, double lat, double lon, double landKm, double access, double cap, bool linked, double ownLat, double ownLon, bool seaOk)[]
        {
            (0, inland, 0, 0, 0, 1, 1, true, 0, 0, true), (0, port, 0, 0, 0, 1, 1, true, 0, 0, true), (1, seller, 0, 0, 0, 1, 1, true, 0, 0, true),
        };
        var a = new Instruments.TsAcc { Nn = 2, Gc = gc, Names = ["甲", "乙"], NoSell = new int[gc] };
        a.C = new int[2 * gc * (int)Instruments.TsCol.Count];
        a.GoodBegin(1); a.AddSeller(2, 1.0);
        var m = new Market { LastDemand = 3, LastImport = 1, LastShortage = 2 };
        double[] left = [5.0]; double[] capLeft = [0.0, 10.0, 10.0];
        // 内陸の行：買い手の容量 0 → xCap
        a.Row(0, bread, inland, m); a.Scan(tr, 0, 100.0, left, capLeft, 1.0, 0.0, 0.0, null); a.Exit(Instruments.TsExit.CapMid);
        // 港の行：容量あり・売り手の値段が高く楔を通る（xPass）
        a.Row(0, bread, port, m); a.Scan(tr, 1, 100.0, left, capLeft, 1.0, 0.0, 0.0, null); a.Exit(Instruments.TsExit.Dealt);
        // 主食でない財の行は州の記録を作らない
        int other = Enumerable.Range(0, gc).First(g => !IsStaple(Catalog.Goods[g].Name));
        a.Row(0, other, inland, m); a.Scan(tr, 0, 100.0, left, capLeft, 1.0, 0.0, 0.0, null); a.Exit(Instruments.TsExit.CapMid);
        Assert.Equal(2, a.Regs.Count);
        var ri = a.Regs.Single(r => r.R == inland); var rp = a.Regs.Single(r => r.R == port);
        Assert.Equal(1, ri.C[(int)Instruments.RgCol.XCap]); Assert.Equal(0, ri.C[(int)Instruments.RgCol.XPass]); Assert.False(ri.Port);
        Assert.Equal(0, rp.C[(int)Instruments.RgCol.XCap]); Assert.Equal(1, rp.C[(int)Instruments.RgCol.XPass]); Assert.True(rp.Port);
        Assert.Equal(1, ri.C[(int)Instruments.RgCol.Rows]); Assert.Equal(3.0, ri.Dem); Assert.Equal(2.0, ri.Short);
        Assert.Equal(1, rp.C[(int)Instruments.RgCol.ExDealt] > 0 ? 1 : 0);   // 港の行の出口は xPass の組の数で重みづけて Dealt へ
        // tradeskip の国×財の列は同じ盤面で xCap=1（内陸）＋ xCap=1（主食でない財は別の財の列）＝州の記録が既存の蓄えを変えていない
        Assert.Equal(1, a.At(0, bread, Instruments.TsCol.XCap)); Assert.Equal(1, a.At(0, other, Instruments.TsCol.XCap));
    }

    [Fact]
    public void T2_州ごとの和はtradeskipの国と財の行と一致する()
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_tsreg_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*"); int every0 = Instruments.NgEveryYears; Instruments.NgEveryYears = 1;
        var sim = new MultiNationSim([NationConfig.France(), NationConfig.Prussia(), NationConfig.Britain(), NationConfig.Austria()], Seed, _ => new UtilityAgent());
        foreach (var n in sim.Nations) n.World.Coef.CivilWarLandslideRatio = 0;
        sim.DiplomacyFrozen = true;
        try
        {
            Instruments.Phase(sim, Seed, "open"); sim.StartWarmup(); Instruments.Phase(sim, Seed, "warm");
            for (int q = 0; q < 24; q++) { sim.StepQuarter(); Instruments.Quarter(sim, Seed, q); }
            Instruments.Close(sim);
            var ts = Read(dir, "tradeskip"); var rg = Read(dir, "tsreg");
            int Ti(string n) { int i = Array.IndexOf(ts.H, n); Assert.True(i >= 0, n); return i; }
            int Ri(string n) { int i = Array.IndexOf(rg.H, n); Assert.True(i >= 0, n); return i; }
            long Sum(IEnumerable<string[]> rows, int col) => rows.Sum(r => long.Parse(r[col]));
            var byKey = rg.R.GroupBy(r => (r[Ri("year")], r[Ri("nation")], r[Ri("good")])).ToDictionary(g => g.Key, g => g.ToList());
            int cmp = 0; long xcapInland = 0, rowsInland = 0, exitSum = 0; var ports = new HashSet<string>();
            foreach (var r in ts.R)
            {
                string nat = r[Ti("nation")], good = r[Ti("good")];
                if (nat == "ALL" || !IsStaple(good)) continue;
                byKey.TryGetValue((r[Ti("year")], nat, good), out var mine); mine ??= [];
                for (int k = 0; k < SumCols.Length; k++) Assert.True(long.Parse(r[Ti(SkipSumCols[k])]) == Sum(mine, Ri(SumCols[k])), $"{r[0]} {nat} {good} {SumCols[k]}");
                for (int k = 0; k < ExitCols.Length; k++) Assert.True(long.Parse(r[Ti(SkipExitCols[k])]) == Sum(mine, Ri(ExitCols[k])), $"{r[0]} {nat} {good} {ExitCols[k]}");
                cmp++;
            }
            foreach (var r in rg.R)
            {
                ports.Add(r[Ri("portFlag")]);
                if (r[Ri("portFlag")] == "0") { xcapInland += long.Parse(r[Ri("xCap")]); rowsInland += long.Parse(r[Ri("rows")]); }
                for (int k = 0; k < ExitCols.Length; k++) exitSum += long.Parse(r[Ri(ExitCols[k])]);
            }
            Assert.True(cmp >= 4 * 5 * 3, "対照：比べた国×財の行が少ない");
            Assert.True(ports.Contains("0") && ports.Contains("1"), "対照：港でない州と港の州の両方の行がある");
            Assert.True(rowsInland > 0 && exitSum > 0, "対照：港でない州の行と出口の数が正");
            Assert.True(rg.R.Sum(r => long.Parse(r[Ri("xCap")])) > 0, "対照：容量で止まった組が 1 つ以上ある");
        }
        finally { Instruments.NgEveryYears = every0; Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }

    [Fact]
    public void T3_計器を切ると州の表も蓄えも作られない()
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_tsreg_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(null, null);
        var sim = new MultiNationSim([NationConfig.France(), NationConfig.Prussia()], Seed, _ => new UtilityAgent());
        sim.DiplomacyFrozen = true; sim.StartWarmup();
        for (int q = 0; q < 8; q++) sim.StepQuarter();
        Assert.Null(Instruments.TsSnapshot(sim.Market));
        Assert.False(System.IO.File.Exists(System.IO.Path.Combine(dir, $"tsreg_s{Seed}.tsv")));
    }
}
