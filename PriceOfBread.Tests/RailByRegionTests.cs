using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (1097) 線 c105rr 支配の力の段 R（州の線路×支配度・旗 RailByRegion）の機構の番人。事前登録 measurements/m1097_20261113_prereg_rr.txt §3〜§4。
/// 旗 0：州の線路を書いても敷きも清算も読まない（M7）。G1：清算の陸の倍率＝12×[(1−φ)×馬＋φ×鉄道×0.25]（φ＝0 と φ＝1 の盤面・期待値は係数の表のリテラルの読みから）。
/// G2：敷いた線路×地形＝買えた単位（市場の在庫の減り）×TransportCapPerUnit。G7：need を超える州は置換が d×need まで＝線路が落ちる。G4：割譲で州の線路はそのまま動く。
/// G3：運賃の線路の分が払われ、計器の線路の分と合い、取り分の無い国へ払われない（計器つきの 4 四半期・trrail_s を読む）。
/// 突然変異（使い捨ての木・measurements/m1097_20261113/mut/）：M1→G1・M2→G4・M3→G7・M4→G2・M5→G3・M6→G1・M7→旗 0。
/// </summary>
[Collection("Instruments")]
public class RailByRegionTests
{
    private static World France(Coefficients coef) => new MultiNationSim([WorldData.ByName("フランス")], seed: 1789, coef: coef) { DiplomacyFrozen = true }.Nations[0].World;

    /// <summary>敷きの盤面：機関車は発見済み・船は買わない・線路と機関車の在庫と投資家の財布は潤沢・脅威と混雑の項は 0（目標＝人口×TransportTargetPerPop）。</summary>
    private static (World W, List<Region> Regs) LayBoard(Coefficients coef)
    {
        var sim = new MultiNationSim([WorldData.ByName("フランス")], seed: 1789, coef: coef) { DiplomacyFrozen = true };
        var w = sim.Nations[0].World; coef = w.Coef;   // 国ごとの Coef（構築で別のインスタンス）
        coef.RailByRegion = 1; coef.ThreatRailTargetWeight = 0; coef.TransportCongestionWeight = 0; coef.ShipFuelRate = 0;
        w.Discovered[Discovery.LocomotiveRow] = true; w.ShipCapacity = 1e18; w.ShipSteamShare = new Share(0);
        var regs = new List<Region>();
        int k = 0;
        foreach (var r in w.Regions)
        {
            r.LastTradePotential = ++k;   // 取り分は潜在量の比（州ごとに違う）
            if (r.InvestorPopIndex < 0) continue;
            regs.Add(r);
            foreach (int g in new[] { coef.RailGoodId, coef.LocoGoodId }) { r.Markets[g].Price = 1.0; r.Markets[g].Stock = 1e6; }
            r.Pops[r.InvestorPopIndex].SetSavings(1e9);
        }
        return (w, regs);
    }

    [Fact]
    public void 旗0では州の線路を書いても敷きは国の容量へ_州の線路は動かない()
    {
        var coef = new Coefficients();
        var w = France(coef); coef = w.Coef;
        coef.RailByRegion = 0;   // 旗 0 の経路の番人（既定は 2026-11-13 に 1）
        Assert.True(w.Regions.Count > 5, "州の数 " + w.Regions.Count);
        w.Discovered[Discovery.LocomotiveRow] = true; w.RailCapacity = 0;
        foreach (var r in w.Regions) r.RailTrack = 123.0;
        Transport.Step(w, coef);
        foreach (var r in w.Regions) Assert.Equal(123.0, r.RailTrack);   // 旗 0 は州の線路を減らさず積まない
        Assert.True(w.RailCapacity > 0, "前提が崩れた：旗 0 で国の容量に積んでいない");
        double sum = 0; foreach (var r in w.Regions) sum += r.RailTrack;
        Assert.NotEqual(sum, w.RailCapacity);   // 読み口は欄（州の和ではない）
    }

    [Fact]
    public void 旗0の清算は州の線路を読まない()
    {
        static double[] RunOne(bool track)
        {
            var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };
            foreach (var n in sim.Nations) { n.World.Coef.RailByRegion = 0; n.World.Discovered[Discovery.LocomotiveRow] = true; n.World.ShipCapacity = 1e9; if (track) foreach (var r in n.World.Regions) r.RailTrack = 1e12; }   // 船腹を大きく＝内陸の州の容量が線路で決まる盤面
            sim.StepQuarter();
            var o = new List<double>();
            foreach (var n in sim.Nations) foreach (var r in n.World.Regions) { o.Add(r.LastTradeThrottle); o.Add(r.LastTradePotential); o.Add(r.LastTransportCap); foreach (var m in r.Markets) { o.Add(m.Stock); o.Add(m.Price); } }
            return o.ToArray();
        }
        var a = RunOne(false); var b = RunOne(true);
        Assert.True(a.Length > 30, "調べた値の数 " + a.Length);
        for (int i = 0; i < a.Length; i++) Assert.Equal(BitConverter.DoubleToInt64Bits(a[i]), BitConverter.DoubleToInt64Bits(b[i]));
    }

    [Fact]
    public void G1_清算の陸の倍率は州の線路の充足で馬と鉄道を混ぜる()
    {
        foreach (bool full in new[] { false, true })
        {
            string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_rr_" + Guid.NewGuid().ToString("N"));
            Instruments.Configure(dir, "*"); Instruments.SelectTables("rulepower_s");
            var sim = new MultiNationSim([NationConfig.France(), NationConfig.Prussia(), NationConfig.Britain(), NationConfig.Austria()], 1789UL, _ => new UtilityAgent(), null);
            sim.DiplomacyFrozen = true;
            foreach (var n in sim.Nations)
            {
                var c = n.World.Coef; c.RailByRegion = 1; c.TransportBuildRate = 0; c.CivilWarLandslideRatio = 0;
                n.World.Discovered[Discovery.LocomotiveRow] = true;
                foreach (var r in n.World.Regions) r.RailTrack = full ? 1e15 : 0.0;
            }
            try { sim.StepQuarter(); }
            finally { Instruments.SelectTables(null); Instruments.Configure(null, null); }
            try
            {
                var s = Instruments.RpSnapOf(sim.Diplomacy);
                Assert.NotNull(s);
                var c0 = sim.Nations[0].World.Coef;
                double[] horse = [1, 0.8, 0.9, 1.5, 3, 2.5, 8], rail = [1, 1, 1, 1.5, 1.2, 5, 3];   // 地形の表（Plain・WaterMajor・WaterMinor・Hill・Forest・Mountain・Barren）のリテラル＝期待値はエンジンの式から作らない
                Assert.Equal(horse, c0.TerrainHorseCost); Assert.Equal(rail, c0.TerrainRailCost);
                Assert.Equal(12.0, c0.LandSeaCostRatio); Assert.Equal(0.25, c0.RailCostFraction);
                int seen = 0;
                for (int t = 0; t < s!.Tc; t++)
                {
                    var tg = s.Tr[t].r.Terrain;
                    if (tg == Terrain.WaterMajor || tg == Terrain.WaterMinor) continue;   // 水運の州は母集合の外
                    int ti = (int)tg;
                    double got = s.TerrCost[t] * s.LandRatio[t];
                    double want = full ? 12.0 * 0.25 * rail[ti] : 12.0 * horse[ti];
                    Assert.True(Math.Abs(got - want) <= 1e-12 * want, $"{s.Tr[t].r.Name}（{tg}）：陸の倍率 {got}・期待 {want}（線路 {(full ? "満" : "0")}）");
                    seen++;
                }
                Assert.True(seen > 50, "調べた州の数 " + seen);
            }
            finally { try { System.IO.Directory.Delete(dir, true); } catch { } }
        }
    }

    [Fact]
    public void G2_敷いた線路と地形の積は買えた単位と容量の単位の積()
    {
        var coef = new Coefficients();
        var (w, regs) = LayBoard(coef);
        coef = w.Coef;
        Assert.True(regs.Count >= 3, "前提：投資家の居る州 " + regs.Count);
        regs[0].Terrain = Terrain.Mountain; regs[1].Terrain = Terrain.Hill; regs[2].Terrain = Terrain.Forest;   // 地形の敷く費用が 1 でない州
        foreach (var r in w.Regions) r.RailTrack = 0;
        var stock0 = regs.Select(r => r.Markets[coef.RailGoodId].Stock + r.Markets[coef.LocoGoodId].Stock).ToArray();
        Transport.Step(w, coef);
        int checkedN = 0;
        for (int i = 0; i < regs.Count; i++)
        {
            var r = regs[i];
            double units = stock0[i] - (r.Markets[coef.RailGoodId].Stock + r.Markets[coef.LocoGoodId].Stock);   // 買えた単位＝市場の在庫の減り（エンジンの足し算から作らない）
            double terr = r.Terrain switch { Terrain.Mountain => 5.0, Terrain.Hill => 1.5, Terrain.Forest => 1.2, Terrain.Barren => 3.0, _ => 1.0 };
            if (!(units > 0)) continue;
            Assert.True(Math.Abs(r.RailTrack * terr - units * 30.0) <= 1e-9 * units * 30.0, $"{r.Name}（{r.Terrain}）：線路×地形 {r.RailTrack * terr}・買えた単位×30 {units * 30.0}");
            checkedN++;
        }
        Assert.True(checkedN >= 3, "線路を敷いた州 " + checkedN);
        Assert.True(regs[0].RailTrack > 0, "前提：山の州にも敷いた");
    }

    [Fact]
    public void G7_needを超える州は置換がd掛けるneedまで_線路は落ちる()
    {
        var coef = new Coefficients();
        var (w, regs) = LayBoard(coef);
        coef = w.Coef;
        double target = w.TotalPopulation * coef.TransportTargetPerPop;   // 脅威と混雑の項は 0（LayBoard）
        foreach (var r in w.Regions) r.RailTrack = 1e9;   // 全ての州で need を大きく超える
        Assert.True(1e9 > 100 * target, $"前提：線路が目標の 100 倍を超える（目標 {target}）");
        Transport.Step(w, coef);
        int n = 0;
        foreach (var r in regs)
        {
            double tPre = 1e9 * (1 - coef.TransportDecay);   // 減耗の後
            double laid = r.RailTrack - tPre;
            Assert.True(laid <= coef.TransportDecay * target * (1 + 1e-9) + 1e-6, $"{r.Name}：need を超える州で積んだ {laid}＞d×目標 {coef.TransportDecay * target}（置換の項が錠になっている）");
            Assert.True(r.RailTrack < 1e9, $"{r.Name}：線路が落ちていない");
            n++;
        }
        Assert.True(n >= 3, "調べた州 " + n);
        double laidSum = 0; foreach (var r in regs) laidSum += r.RailTrack - 1e9 * (1 - coef.TransportDecay);
        Assert.True(laidSum > 0, "前提が崩れた：置換の買いが 1 件も無い");
    }

    [Fact]
    public void G4_割譲で州の線路はそのまま州について行く()
    {
        var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };
        var uk = sim.Nations[0].World; var fr = sim.Nations[1].World;
        uk.Coef.RailByRegion = 1; fr.Coef.RailByRegion = 1;
        int k = 0; foreach (var q in uk.Regions) { q.LastTradePotential = ++k; q.RailTrack = 10.0 * k * k; }   // 線路は潜在量の比と違う形（k²）＝旧の規則（送り手の容量×取り分）と見分ける
        foreach (var q in fr.Regions) q.RailTrack = 7.0;
        var r = uk.Regions.Find(x => x.Id != 0)!;
        double tr0 = r.RailTrack, uk0 = uk.RailCapacity, fr0 = fr.RailCapacity;
        Assert.True(tr0 > 0 && tr0 < uk0, "前提");
        double sh = Transport.RegionShare(Transport.ShareBasis(uk, uk.Coef), r);
        Assert.True(Math.Abs(tr0 - uk0 * sh) > 1e-6, $"前提が崩れた：州の線路 {tr0} が送り手の容量×取り分 {uk0 * sh} と同じ");
        Territory.TransferRegion(uk, fr, r, Territory.Rehoming.Ceded);
        Assert.Equal(tr0, r.RailTrack);
        Assert.Equal(uk0 - tr0, uk.RailCapacity, 9);
        Assert.Equal(fr0 + tr0, fr.RailCapacity, 9);
        Assert.Equal(uk0 + fr0, uk.RailCapacity + fr.RailCapacity, 9);
    }

    [Fact]
    public void G3_運賃の線路の分は払われ計器の分と合い_取り分の無い国へ行かない()
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_rr3_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*"); Instruments.SelectTables("trrail_s");
        var sim = new MultiNationSim([NationConfig.France(), NationConfig.Prussia(), NationConfig.Britain(), NationConfig.Austria()], 1789UL, _ => new UtilityAgent(), null);
        sim.DiplomacyFrozen = true;
        foreach (var n in sim.Nations)
        {
            var c = n.World.Coef; c.RailByRegion = 1; c.RailNeedByOutput = 0; c.CivilWarLandslideRatio = 0;   // (1137) 旗 0 の経路の番人（g1FormulaViol は潜在量の比の式）。旗 1 の同じ Assert の束は RailNeedByOutputTests の G_F の Theory
            n.World.Discovered[Discovery.LocomotiveRow] = true;
            int i = 0; foreach (var r in n.World.Regions) r.RailTrack = (i++ % 2 == 0) ? 1e15 : 0.0;
        }
        try
        {
            Instruments.Phase(sim, 1789UL, "open");
            for (int q = 0; q < 4; q++) { sim.StepQuarter(); Instruments.Quarter(sim, 1789UL, q); }
        }
        finally { Instruments.Close(sim); Instruments.SelectTables(null); Instruments.Configure(null, null); }
        try
        {
            var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, "trrail_s1789.tsv"));
            var h = lines[0].Split('\t'); int Ix(string x) => Array.IndexOf(h, x);
            double paid = 0, ins = 0, maxRel = 0; long noShare = 0, g1z = 0, g1zv = 0, g1p = 0, g1fv = 0, g2 = 0, g2v = 0, g7l = 0, rows = 0;
            foreach (var ln in lines.Skip(1))
            {
                var f = ln.Split('\t'); rows++;
                double p = double.Parse(f[Ix("railFreightPaidGoldPerYear")], System.Globalization.CultureInfo.InvariantCulture);
                double s = double.Parse(f[Ix("railFreightInsGoldPerYear")], System.Globalization.CultureInfo.InvariantCulture);
                paid += p; ins += s;
                if (f[Ix("waterFlag")] == "0") maxRel = Math.Max(maxRel, Math.Abs(p - s) / Math.Max(1e-6, Math.Max(p, s)));
                noShare += long.Parse(f[Ix("g3NoShareRows")]); g1z += long.Parse(f[Ix("g1ZeroSides")]); g1zv += long.Parse(f[Ix("g1ZeroViol")]);
                g1p += long.Parse(f[Ix("g1PosSides")]); g1fv += long.Parse(f[Ix("g1FormulaViol")]); g2 += long.Parse(f[Ix("g2Rows")]); g2v += long.Parse(f[Ix("g2Viol")]); g7l += long.Parse(f[Ix("g7LockRows")]);
            }
            Assert.True(rows > 50, "行の数 " + rows);
            Assert.True(paid > 0, "運賃の線路の分が 1 度も払われていない（船の持ち主へ戻っている）");
            Assert.True(maxRel <= 1e-6, $"払った線路の分と計器の線路の分の差（州×年の相対の最大）{maxRel}");
            string all = $"G3 取り分の無い国 {noShare}・G1 T＝0 {g1z} 破れ {g1zv}・T＞0 {g1p} 破れ {g1fv}・G2 {g2} 破れ {g2v}・G7 錠 {g7l}";
            Assert.True(noShare == 0 && g1zv == 0 && g1fv == 0 && g2v == 0 && g7l == 0, all);
            Assert.True(g1z > 0 && g1p > 0 && g2 > 0, all);
        }
        finally { try { System.IO.Directory.Delete(dir, true); } catch { } }
    }
}
