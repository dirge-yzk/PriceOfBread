using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// ★(395) 恒等の線 ic2 の計器の番人（読むだけ・軌道に触れない）。
/// ①州の分離の内訳 <c>secession_s</c>：組み直した圧力が本体の圧力と一致（pressureRebuildGapIndex＝0）・組み直した中央の保持力が本体の hold×共有分母に合う・
///   連続する 2 四半期の行で、支配度の増分が行の marginIndex×DominanceSpeed に合う（本体が独立に書いた r.Dominance との突き合わせ＝行の中だけで閉じない）・
///   最後の四半期の行の支配度と関税利害が世界の Region.Dominance／TariffInterest に合う。
/// ②労働の列を財ごとに：3 財の BoundDesired／BoundLoss／Need の和が既存の合算列に合い（同じ分類を財ごとに割っただけ）・plainLabor の市場の列が既存の労働力の列に合い・
///   最後の四半期の行が、番人の側で別の書き方（最小の配給率の財を直に探す）で数え直した値に合う。
/// ③法の列：最後の四半期の行が World の現在値に合い、国有化のマスクは 6 フラグから番人の側で別に組み立てた値に合う（フラグを立てた世界でも）。
/// ④計算量の台帳：AI の官僚制度の走査の行が名前・単価・件数の表に揃い、件数が計器の走査の呼びの数（burscan の calls）と一致する。
/// </summary>
[Collection("Instruments")]
public class SecessionLaborLawInstrumentTests
{
    private static MultiNationSim Make() => new([NationConfig.France(), NationConfig.Prussia(), NationConfig.Britain(), NationConfig.Austria()], 1789UL, _ => new UtilityAgent());

    private static (string[] Header, List<string[]> Rows) Table(string path)
    {
        var lines = System.IO.File.ReadAllLines(path);
        return (lines[0].Split('\t'), lines.Skip(1).Select(l => l.Split('\t')).ToList());
    }
    private static double Col(string[] h, string[] r, string name)
    {
        int i = Array.IndexOf(h, name); Assert.True(i >= 0, "列が無い " + name);
        return r[i] switch { "nan" => double.NaN, "" => double.NaN, "inf" => double.PositiveInfinity, "-inf" => double.NegativeInfinity, var s => double.Parse(s, System.Globalization.CultureInfo.InvariantCulture) };
    }
    private static void Near(double expect, double actual, double rel = 1e-8, string what = "") =>
        Assert.True(Math.Abs(expect - actual) <= rel * Math.Max(1.0, Math.Abs(expect)), $"{what} 期待 {expect}・実際 {actual}");
    private static string NameOfWorld(World w) => w.SelfPower is { } sp ? (sp.BaseName.Length > 0 ? sp.BaseName : sp.Name) : "(単国)";

    private static (string dir, MultiNationSim sim) Run(int quarters, Action<MultiNationSim, int>? afterStep = null)
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_sl_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        var sim = Make();
        Instruments.Phase(sim, 1789UL, "open");
        sim.StartWarmup();
        Instruments.Phase(sim, 1789UL, "warm");
        try { for (int q = 0; q < quarters; q++) { sim.StepQuarter(); afterStep?.Invoke(sim, q); Instruments.Quarter(sim, 1789UL, q); } }
        finally { Instruments.Close(sim); }
        return (dir, sim);
    }
    private static void Cleanup(string dir) { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }

    [Fact]
    public void 州の分離の内訳は組み直した圧力が本体に合い_支配度の増分が行の余裕に合い_最後の行は世界に合う()
    {
        var (dir, sim) = Run(36);
        try
        {
            var (h, rows) = Table(System.IO.Path.Combine(dir, "secession_s1789.tsv"));
            Assert.Equal(Instruments.SecessionHeaderText.Split('\t'), h);
            Assert.All(rows, r => Assert.Equal(h.Length, r.Length));
            Assert.True(rows.Count > 100, $"行が少ない {rows.Count}＝CheckSecession の hook が呼ばれていない？");
            double speed = sim.Nations[0].World.Coef.DominanceSpeed;
            int nonZeroPressure = 0, withMargin = 0;
            foreach (var r in rows)
            {
                Assert.True(Math.Abs(Col(h, r, "pressureRebuildGapIndex")) <= 1e-12, $"組み直した圧力が本体とずれた {r[Array.IndexOf(h, "nation")]}/{r[Array.IndexOf(h, "region")]} 差 {Col(h, r, "pressureRebuildGapIndex")}");
                Assert.True(Math.Abs(Col(h, r, "holdBaseRebuildGapIndex")) <= 1e-9, $"中央の保持力の組み直しが本体とずれた {Col(h, r, "holdBaseRebuildGapIndex")}");
                double dom = Col(h, r, "dominanceShare"); Assert.True(dom >= 0 && dom <= 1, $"支配度が [0,1] の外 {dom}");
                Assert.True(Col(h, r, "crowdFactorShare") >= 0 && Col(h, r, "crowdFactorShare") < 1, "大衆の飽和が [0,1) の外");
                if (Col(h, r, "pressureIndex") > 0) nonZeroPressure++;
                if (Col(h, r, "marginIndex") != 0) withMargin++;
            }
            Assert.True(nonZeroPressure > 20, $"圧力が正の行が少ない {nonZeroPressure}（対照＝全て 0 なら列が空）");
            // 連続する 2 四半期（同じ国・同じ州）：支配度の増分＝次の行の marginIndex × DominanceSpeed（クランプにかからない範囲）。本体が書いた r.Dominance の差との突き合わせ。
            var byKey = rows.GroupBy(r => (Nation: r[Array.IndexOf(h, "nation")], Region: r[Array.IndexOf(h, "region")]))
                            .ToDictionary(g => g.Key, g => g.OrderBy(r => Col(h, r, "quarter")).ToList());
            int checkedPairs = 0;
            foreach (var kv in byKey)
            {
                var list = kv.Value;
                for (int i = 1; i < list.Count; i++)
                {
                    if (Col(h, list[i], "quarter") != Col(h, list[i - 1], "quarter") + 1) continue;
                    double d0 = Col(h, list[i - 1], "dominanceShare"), d1 = Col(h, list[i], "dominanceShare");
                    if (!(d0 > 1e-6 && d0 < 1 - 1e-6 && d1 > 1e-6 && d1 < 1 - 1e-6)) continue;   // クランプにかかった四半期は式が成り立たない
                    Near(d0 + Col(h, list[i], "marginIndex") * speed, d1, 1e-9, $"{kv.Key} 支配度の増分");
                    checkedPairs++;
                }
            }
            Assert.True(checkedPairs >= 20, $"支配度の増分を突き合わせた組が少ない {checkedPairs}（対照）");
            // 最後の四半期の行：世界の Region.Dominance／TariffInterest（CheckSecession が最後に書いた値）と一致
            var last = rows.Where(r => Col(h, r, "quarter") == 35).ToList();
            Assert.NotEmpty(last);
            int matched = 0;
            foreach (var n in sim.Nations)
            {
                var w = n.World; string nm = NameOfWorld(w);
                foreach (var r in last.Where(r => r[Array.IndexOf(h, "nation")] == nm))
                {
                    string rn = r[Array.IndexOf(h, "region")];
                    var reg = w.Regions.FirstOrDefault(x => x.Name == rn);
                    if (reg is null) continue;   // 四半期の後半で州が離れた
                    Near(reg.Dominance, Col(h, r, "dominanceShare"), 1e-9, nm + "/" + rn + " 支配度");
                    Near(reg.TariffInterest, Col(h, r, "tariffInterestIndex"), 1e-9, nm + "/" + rn + " 関税利害");
                    matched++;
                }
            }
            Assert.True(matched >= 4, $"世界と突き合わせた行が少ない {matched}");
        }
        finally { Cleanup(dir); }
    }

    [Fact]
    public void 同時に閾値を越えた州が複数あれば共有分母が立ち_行の保持力は共有分母で割った値になる()
    {
        // 直に CheckSecession を踏む（RevoltIgnitionTests の幽霊の国の根と同じ作法）。英の盤面で大衆のいる州を全て離れる向きに置き、圧力の尺度を上げて閾値を越えた州を 2 つ以上にする。
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_sl2_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        try
        {
            var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
            var w = sim.World; var c = w.Coef;
            c.SecessionPressureScale = 50.0;
            w.Tariff = 1.0; w.Legitimacy = 0.02;
            var targets = w.Regions.Where(x => x.Id != 0 && x.State == RegionState.Active && Politics.RegionCrowd(x) > 1).ToList();
            Assert.True(targets.Count >= 2, $"前提が崩れた：大衆のいる非首都の州が {targets.Count} 件");
            foreach (var q in w.Regions) foreach (var p in q.Pops) p.Militancy = 0;
            foreach (var r in targets) foreach (var p in r.Pops) { p.Militancy = 1.0; p.CoreEthnicShare = new Share(0.0); p.NationalConsciousness = 0.0; }
            Politics.CheckSecession(w, c, 0.0);
            var rows = Instruments.SecRowsPending(w);
            Assert.NotNull(rows);
            var h = Instruments.SecessionHeaderText.Split('	').Skip(3).ToArray();   // 行は region から始まる（quarter／year／nation は Fold が前に付ける）
            int Ix(string n) { int i = Array.IndexOf(h, n); Assert.True(i >= 0, n); return i; }
            double V(string[] r, string n) => double.Parse(r[Ix(n)], System.Globalization.CultureInfo.InvariantCulture);
            var parsed = rows!.Select(r => r.Split('	')).ToList();
            Assert.All(parsed, r => Assert.Equal(h.Length, r.Length));
            int restive = parsed.Count(r => V(r, "pressureIndex") > c.SecessionThreshold);   // 番人の側で数える（内戦の戦線は無い盤面）
            Assert.True(restive >= 2, $"前提が崩れた：閾値を越えた州が {restive} 件");
            double expectDiv = 1.0 + c.SecessionHoldSharedArmy * Math.Max(0, restive - 1);
            Assert.True(c.SecessionHoldSharedArmy > 0 && expectDiv > 1.0, "共有分母が 1 のまま（SecessionHoldSharedArmy＝0？）");
            foreach (var r in parsed)
            {
                Near(expectDiv, V(r, "sharedDivIndex"), 1e-9, "共有分母");
                Assert.Equal(restive, (int)V(r, "restiveCount"));
                Near(V(r, "effHoldBeforeShareIndex") / expectDiv, V(r, "holdShareIndex"), 1e-9, "共有分母で割った保持力");
                Near(V(r, "holdShareIndex") - V(r, "pressureIndex"), V(r, "marginIndex"), 1e-9, "余裕");
            }
        }
        finally { Cleanup(dir); }
    }

    [Fact]
    public void 労働の財ごとの列は3財の和が既存の合算列に合い_最後の行は番人の側の書き方で数え直した値に合う()
    {
        var (dir, sim) = Run(24);
        try
        {
            var (h, rows) = Table(System.IO.Path.Combine(dir, "factory_s1789.tsv"));
            Assert.Equal(4 + 33 + 27, h.Length);   // 既存の 33 列は動かさず末尾に 27 列
            Assert.All(rows, r => Assert.Equal(h.Length, r.Length));
            string[] pre = ["plainLabor", "advLabor", "farmLabor"];
            double advSupplyMax = 0, advNeedMax = 0, farmSupplyMax = 0, advBoundMax = 0;
            foreach (var r in rows)
            {
                double Sum(string suffix) => pre.Sum(p => Col(h, r, p + suffix));
                Near(Col(h, r, "laborBoundDesiredRefValuePerMonth"), Sum("BoundDesiredRefValuePerMonth"), 1e-9, "律速の作りたい量の和");
                Near(Col(h, r, "laborBoundLossRefValuePerMonth"), Sum("BoundLossRefValuePerMonth"), 1e-9, "律速で失った量の和");
                Near(Col(h, r, "laborNeedAtCapacityUnitsPerMonth"), Sum("NeedAtCapacityUnitsPerMonth"), 1e-9, "能力に要る労働の和");
                Near(Col(h, r, "laborNeedAtDesiredUnitsPerMonth"), Sum("NeedAtDesiredUnitsPerMonth"), 1e-9, "作りたい量に要る労働の和");
                Near(Col(h, r, "laborSupplyCapacityUnitsPerMonth"), Col(h, r, "plainLaborSupplyCapacityUnitsPerMonth"), 1e-12, "労働力の供給の器");
                Near(Col(h, r, "laborMarketDemandUnitsPerMonth"), Col(h, r, "plainLaborMarketDemandUnitsPerMonth"), 1e-12, "労働力の需要");
                Near(Col(h, r, "laborMarketProducedUnitsPerMonth"), Col(h, r, "plainLaborMarketProducedUnitsPerMonth"), 1e-12, "労働力の産出");
                double ra0 = Col(h, r, "laborRationMeanShare"), ra1 = Col(h, r, "plainLaborRationMeanShare");
                Assert.True((double.IsNaN(ra0) && double.IsNaN(ra1)) || Math.Abs(ra0 - ra1) <= 1e-12, $"労働力の配給率 {ra0} {ra1}");
                advSupplyMax = Math.Max(advSupplyMax, Col(h, r, "advLaborSupplyCapacityUnitsPerMonth"));
                advNeedMax = Math.Max(advNeedMax, Col(h, r, "advLaborNeedAtDesiredUnitsPerMonth"));
                farmSupplyMax = Math.Max(farmSupplyMax, Col(h, r, "farmLaborSupplyCapacityUnitsPerMonth"));
                advBoundMax = Math.Max(advBoundMax, Col(h, r, "advLaborBoundDesiredRefValuePerMonth"));
            }
            Assert.True(advSupplyMax > 0 && advNeedMax > 0 && farmSupplyMax > 0, $"高度労働力・農業労働力の列が 0 のまま（財の取り違え？）adv供給 {advSupplyMax} adv需要 {advNeedMax} farm供給 {farmSupplyMax}");

            // 最後の四半期を、番人の側で別の書き方に直して数え直す（最小の配給率の労働財を直接探す）
            var last = rows.Where(r => Col(h, r, "quarter") == 23).ToDictionary(r => r[Array.IndexOf(h, "nation")]);
            int[] gids = [Catalog.LaborId, Catalog.AdvLaborId, Catalog.FarmLaborId];
            foreach (var n in sim.Nations)
            {
                var w = n.World; string nm = NameOfWorld(w); var r = last[nm];
                var sup = new double[3]; var supPop = new double[3]; var needDes = new double[3]; var dem = new double[3]; var prod = new double[3]; var raW = new double[3];
                var boundD = new double[3]; var boundL = new double[3];
                foreach (var rg in w.Regions)
                {
                    if (rg.State != RegionState.Active) continue;
                    for (int k = 0; k < 3; k++) { var m = rg.Markets[gids[k]]; dem[k] += m.LastDemand; prod[k] += m.LastProduced; raW[k] += m.LastDemand * m.LastRation.Value; }
                    var seen = new HashSet<(int, int)>();
                    foreach (var s in rg.Producers)
                    {
                        int gk = Array.IndexOf(gids, s.GoodId);
                        if (gk >= 0 && !s.Bankrupt) { sup[gk] += s.Capacity; if (seen.Add((gk, s.LaborPopIndex))) supPop[gk] += rg.Pops[s.LaborPopIndex].Size; }
                        if (!s.BuysLabor || s.Bankrupt || (s.Scale == 0 && s.Output == 0)) continue;
                        double rp = Catalog.RefPrices[s.GoodId];
                        var labIn = s.Inputs.Where(x => Array.IndexOf(gids, x.GoodId) >= 0).ToList();
                        foreach (var x in labIn) needDes[Array.IndexOf(gids, x.GoodId)] += s.DesiredOutput * x.QtyPerOutput;
                        if (s.LastInputFill < 1.0 - 1e-9 && labIn.Count > 0)
                        {
                            // 労働財の中の最小の配給率を持つ財（並びの先を優先）
                            double mn = double.PositiveInfinity; int who = -1;
                            foreach (var x in labIn) { double ra = rg.Markets[x.GoodId].LastRation.Value; if (ra < mn) { mn = ra; who = Array.IndexOf(gids, x.GoodId); } }
                            if (Math.Abs(mn - s.LastInputFill) <= 1e-12) { boundD[who] += s.DesiredOutput * rp; boundL[who] += s.DesiredOutput * (1.0 - s.LastInputFill) * rp; }
                        }
                    }
                }
                for (int k = 0; k < 3; k++)
                {
                    string p = pre[k];
                    Near(sup[k], Col(h, r, p + "SupplyCapacityUnitsPerMonth"), 1e-8, nm + " " + p + " 供給の器");
                    Near(supPop[k], Col(h, r, p + "SupplierPopK"), 1e-8, nm + " " + p + " 担い手の人数");
                    Near(needDes[k], Col(h, r, p + "NeedAtDesiredUnitsPerMonth"), 1e-8, nm + " " + p + " 作りたい量に要る量");
                    Near(dem[k], Col(h, r, p + "MarketDemandUnitsPerMonth"), 1e-8, nm + " " + p + " 市場の需要");
                    Near(prod[k], Col(h, r, p + "MarketProducedUnitsPerMonth"), 1e-8, nm + " " + p + " 市場の産出");
                    if (dem[k] > 0) Near(raW[k] / dem[k], Col(h, r, p + "RationMeanShare"), 1e-8, nm + " " + p + " 配給率");
                    Near(boundD[k], Col(h, r, p + "BoundDesiredRefValuePerMonth"), 1e-8, nm + " " + p + " 律速の作りたい量");
                    Near(boundL[k], Col(h, r, p + "BoundLossRefValuePerMonth"), 1e-8, nm + " " + p + " 律速で失った量");
                }
            }
        }
        finally { Cleanup(dir); }
    }

    [Fact]
    public void 法の列は世界の現在値に合い_国有化のマスクは6フラグから別に組み立てた値に合う()
    {
        // 四半期の末に法を直に立ててから計器を読む（本体が次の四半期に戻すのは読む後）。フラグを立てた世界で、マスクのビットの対応が合う。
        var (dir, sim) = Run(6, (s, q) =>
        {
            if (q != 5) return;
            var w = s.Nations[0].World;
            w.NationalizeRailways = true; w.NationalizeOil = true; w.NationalizeAll = true;
            w.Antitrust = AntitrustLaw.Trustbust; w.SocialSecurityLaw = SocialSecurityLaw.Unemployment; w.ProgressiveTax = 0.35;
            var w2 = s.Nations[1].World; w2.NationalizeMines = true; w2.NationalizeMilitary = true; w2.NationalizeElectricity = true;
        });
        try
        {
            var (h, rows) = Table(System.IO.Path.Combine(dir, "scalars_s1789.tsv"));
            Assert.Equal(23 + 4, h.Length);   // 既存の 23 列は動かさず末尾に 4 列
            Assert.Equal(["socialSecurityLawCode", "antitrustLawCode", "progressiveTaxShare", "nationalizedMaskCode"], h.Skip(23).ToArray());
            var last = rows.Where(r => Col(h, r, "quarter") == 5).ToDictionary(r => r[Array.IndexOf(h, "nation")]);
            foreach (var n in sim.Nations)
            {
                var w = n.World; var r = last[NameOfWorld(w)];
                Assert.Equal((int)w.SocialSecurityLaw, (int)Col(h, r, "socialSecurityLawCode"));
                Assert.Equal((int)w.Antitrust, (int)Col(h, r, "antitrustLawCode"));
                Near(w.ProgressiveTax, Col(h, r, "progressiveTaxShare"), 1e-9, "累進課税");
                // 6 フラグから別に組み立てる（NationalizedSector の番号が立つ位置：鉄道0・鉱山1・軍需2・石油4・電力5・全て6）
                int mask = (w.NationalizeRailways ? 1 : 0) | (w.NationalizeMines ? 2 : 0) | (w.NationalizeMilitary ? 4 : 0) | (w.NationalizeOil ? 16 : 0) | (w.NationalizeElectricity ? 32 : 0) | (w.NationalizeAll ? 64 : 0);
                Assert.Equal(mask, (int)Col(h, r, "nationalizedMaskCode"));
            }
            // 対照：立てた 2 国の値が実際にその値（0 のままの列でないこと）
            var w0 = sim.Nations[0].World; var w1 = sim.Nations[1].World;
            Assert.Equal(1 | 16 | 64, (int)Col(h, last[NameOfWorld(w0)], "nationalizedMaskCode"));
            Assert.Equal(2 | 4 | 32, (int)Col(h, last[NameOfWorld(w1)], "nationalizedMaskCode"));
            Assert.Equal((int)SocialSecurityLaw.Unemployment, (int)Col(h, last[NameOfWorld(w0)], "socialSecurityLawCode"));
            Assert.Equal((int)AntitrustLaw.Trustbust, (int)Col(h, last[NameOfWorld(w0)], "antitrustLawCode"));
            Near(0.35, Col(h, last[NameOfWorld(w0)], "progressiveTaxShare"), 1e-9, "累進課税（立てた値）");
        }
        finally { Cleanup(dir); }
    }

    [Fact]
    public void 計算量の台帳のAIの官僚制度の走査の行は表に揃い_件数は計器の走査の呼びの数に合う()
    {
        Assert.Equal(CostLedger.Count, CostLedger.Names.Length);
        Assert.Equal(CostLedger.Count, CostLedger.UnitMicros.Length);
        Assert.Equal(CostLedger.AiBureaucracyScans + 1, CostLedger.Count);   // 新しい行は表の最後
        Assert.Equal(CostLedger.AiBureaucracyScanUnitMicros, CostLedger.UnitMicros[CostLedger.AiBureaucracyScans]);
        Assert.True(CostLedger.UnitMicros.All(u => u > 0 && double.IsFinite(u)), "単価が 0 以下か有限でない行がある");
        var (dir, sim) = Run(8);
        try
        {
            var total = sim.CostTotals();
            long scans = total[CostLedger.AiBureaucracyScans];
            Assert.True(scans > 0, "走査が 1 回も数えられていない");
            // 計器の走査の呼びの数（burscan の calls＝GateAllLawTargets が Instruments.On のときに数える別の数）と、台帳の件数が一致
            var (h, rows) = Table(System.IO.Path.Combine(dir, "burscan_s1789.tsv"));
            Assert.Single(rows);
            Assert.Equal(scans, (long)Col(h, rows[0], "calls"));
        }
        finally { Cleanup(dir); }
    }
}
