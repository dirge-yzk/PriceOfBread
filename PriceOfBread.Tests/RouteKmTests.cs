using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// ★(746) 州の組の経路の km の表 <see cref="RouteKm"/>（Geo.cs の末尾）と、それを引く計器（<c>migration_s</c> の列 routeKm・<c>xmig_s</c>）の番人。
/// 表の主張：①自分への距離は 0 ②大圏 ≦ 陸路・大圏 ≦ 港経由の海路・大圏 ≦ 選んだ経路 ③陸路と港経由の海路は対称（相対 1e-9・足す順で最後の桁がずれる）
/// ④選んだ経路の費用は選ばなかった物以下（貿易の PairKm と同じ比較）・陸直行と海経由の両方が実際に選ばれる ⑤海路の部品は <see cref="Geo.SeaDistanceKm"/> の港の座標どうしの値とビットで一致し、運河の 4 通りで値が分かれる
/// ⑥開幕の世界の全ての州が表に引け、港かどうかが州の構築と合い、座標表の全ての名前が表にある ⑦陸路は貿易の経路の表 <c>trroute_s</c> の realLandKm（陸直行の行）と一致＝貿易と同じ部品を引いている。
/// 計器の主張：⑧<c>migration_s</c> の routeKm は表を直接引いた値と一致し大圏以上 ⑨<c>xmig_s</c> の組の量の和 ＝ <c>region_s</c> の越境の出の和 ＝ 入の和（四半期ごと）・両端の国の名前は <c>region_s</c> の国と一致（別の経路）。
/// 何もしない番人は必ず緑になる＝陸直行と海経由が両方選ばれる・運河で値が変わる組がある・xmig_s に行が出る・migration_s に行が出る、を対照にする。
/// </summary>
[Collection("Instruments")]
public class RouteKmTests
{
    static readonly Coefficients C = new();

    static void Near(double expect, double actual, double rel = 1e-9) => Assert.True(Math.Abs(expect - actual) <= rel * Math.Max(1.0, Math.Abs(expect)), $"期待 {expect}・実際 {actual}");

    [Fact]
    public void 自分への距離は0で_大圏は全ての経路以下_陸路と港経由の海路は対称()
    {
        int m = RouteKm.Count;
        Assert.True(m >= 300, $"州の数 {m}（座標表の名前は 300 を超える）");
        for (int i = 0; i < m; i++)
        {
            Assert.Equal(0.0, RouteKm.GreatCircle(i, i)); Assert.Equal(0.0, RouteKm.Land(i, i));
            for (int cfg = 0; cfg < 4; cfg++) { Assert.Equal(0.0, RouteKm.SeaViaPort(i, i, cfg)); Assert.Equal(0.0, RouteKm.Km(i, i, cfg, C)); }
        }
        int finiteLand = 0, finiteSea = 0;
        for (int i = 0; i < m; i++)
            for (int j = 0; j < m; j++)
            {
                if (i == j) continue;
                double gc = RouteKm.GreatCircle(i, j), land = RouteKm.Land(i, j);
                double tol = 1e-9 * Math.Max(1.0, gc);
                Assert.True(gc >= 0);   // 同じ座標の名前の組（ボマ・コンゴ）は大圏 0
                if (double.IsFinite(land)) { finiteLand++; Assert.True(gc <= land + tol, $"大圏 {gc} が陸路 {land} を超えた {RouteKm.NameOf(i)}→{RouteKm.NameOf(j)}"); Near(land, RouteKm.Land(j, i)); }
                else Assert.True(double.IsPositiveInfinity(land));
                for (int cfg = 0; cfg < 4; cfg++)
                {
                    double sea = RouteKm.SeaViaPort(i, j, cfg);
                    if (double.IsFinite(sea)) { if (cfg == 0) finiteSea++; Assert.True(gc <= sea + tol, $"大圏 {gc} が港経由 {sea} を超えた {RouteKm.NameOf(i)}→{RouteKm.NameOf(j)} cfg {cfg}"); Near(sea, RouteKm.SeaViaPort(j, i, cfg)); }
                    double km = RouteKm.Km(i, j, cfg, C);
                    Assert.True(gc <= km + tol, $"大圏 {gc} が選んだ経路 {km} を超えた");
                }
            }
        Assert.True(finiteLand > m * m / 4 && finiteSea > m * m / 2, $"有限の組が少ない（陸 {finiteLand}・海 {finiteSea}）＝何も検査していない");
    }

    [Fact]
    public void 選んだ経路の費用は選ばなかった物以下で_陸直行と海経由の両方が選ばれ_陸路の旗が0なら陸は選ばれない()
    {
        int m = RouteKm.Count; double r = C.LandSeaCostRatio;
        int landPicked = 0, seaPicked = 0, bothInf = 0;
        for (int i = 0; i < m; i++)
            for (int j = 0; j < m; j++)
            {
                if (i == j) continue;
                int cfg = 0;
                double land = RouteKm.Land(i, j), so = RouteKm.SeaBetweenPorts(i, j, cfg), legs = RouteKm.Leg(i) + RouteKm.Leg(j);
                double costLand = land * r, costSea = so + legs * r;   // 貿易の viaLand（地形と比を 1 に置く）・viaSea の式（別の列の組み合わせから書いた期待）
                double km = RouteKm.Km(i, j, cfg, C); bool isLand = RouteKm.IsLand(i, j, cfg, C);
                if (!double.IsFinite(costLand) && !double.IsFinite(costSea)) { bothInf++; Assert.True(double.IsPositiveInfinity(km)); continue; }
                if (isLand) { landPicked++; Assert.True(costLand < costSea, $"陸を選んだが費用が安くない {RouteKm.NameOf(i)}→{RouteKm.NameOf(j)}"); Assert.Equal(land, km); }
                else { seaPicked++; Assert.True(!(costLand < costSea)); Assert.Equal(RouteKm.Leg(i) + so + RouteKm.Leg(j), km); }
            }
        Assert.True(landPicked > 100 && seaPicked > 100, $"陸 {landPicked}・海 {seaPicked}＝片方しか選ばれない盤面は何も検査していない");
        var noLand = new Coefficients { LandRouteEnabled = 0 };
        for (int i = 0; i < m; i += 7) for (int j = 0; j < m; j += 5) Assert.False(RouteKm.IsLand(i, j, 0, noLand));
    }

    [Fact]
    public void 海路の部品は港の座標どうしのGeoのSeaDistanceKmとビットで一致し_運河の4通りで値が分かれる()
    {
        int m = RouteKm.Count;
        var ports = Enumerable.Range(0, m).Where(RouteKm.IsPort).ToList();
        Assert.True(ports.Count >= 40, $"港の州が {ports.Count}");
        double suezGain = 0, panamaGain = 0; int checkedPairs = 0;
        for (int a = 0; a < ports.Count; a += 3)
            for (int b = 0; b < ports.Count; b += 2)
            {
                int i = ports[a], j = ports[b]; if (i == j) continue;
                var (la, lo) = Places.Coord(RouteKm.NameOf(i)); var (lb, lb2) = Places.Coord(RouteKm.NameOf(j));
                Assert.Equal(i, RouteKm.PortOf(i)); Assert.Equal(0.0, RouteKm.Leg(i));
                for (int cfg = 0; cfg < 4; cfg++)
                {
                    double expect = Geo.SeaDistanceKm(la, lo, lb, lb2, (cfg & 2) != 0, (cfg & 1) != 0);
                    Assert.Equal(expect, RouteKm.SeaBetweenPorts(i, j, cfg));
                    checkedPairs++;
                }
                suezGain = Math.Max(suezGain, RouteKm.SeaBetweenPorts(i, j, 0) - RouteKm.SeaBetweenPorts(i, j, 2));
                panamaGain = Math.Max(panamaGain, RouteKm.SeaBetweenPorts(i, j, 0) - RouteKm.SeaBetweenPorts(i, j, 1));
            }
        Assert.True(checkedPairs > 1000);
        Assert.True(suezGain > 3000, $"スエズを通れると短くなる組が無い（最大の短縮 {suezGain:F0} km）");
        Assert.True(panamaGain > 3000, $"パナマを通れると短くなる組が無い（最大の短縮 {panamaGain:F0} km）");
    }

    [Fact]
    public void 開幕の世界の全ての州が表に引け_港かどうかは州の構築と合い_座標表の全ての名前が引ける()
    {
        var sim = new MultiNationSim(WorldData.Nations(), 1UL, _ => new UtilityAgent());
        var seen = new Dictionary<string, bool>();
        foreach (var n in sim.Nations) foreach (var r in n.World.Regions)
        {
            var (la, lo) = Places.Coord(r.Name);
            if (la == 0 && lo == 0) continue;
            seen.TryAdd(r.Name, r.IsPort);
        }
        foreach (var (name, isPort) in seen)
        {
            int i = RouteKm.IndexOf(name);
            Assert.True(i >= 0, $"{name} が表に無い");
            Assert.Equal(isPort, RouteKm.IsPort(i));
        }
        Assert.True(seen.Count >= 300 && seen.Count <= RouteKm.Count);
        Assert.Equal(Places.All.Count(p => p.Lat != 0 || p.Lon != 0), RouteKm.Count);   // 座標表の全ての名前（後から得る植民地・標的も）が引ける
        Assert.Equal(-1, RouteKm.IndexOf("存在しない州の名前"));
    }

    // ---------------------------------------------------------------- 計器（migration_s・xmig_s）と貿易の経路の表との突き合わせ ----------------------------------------------------------------

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
    private static string Str(string[] h, string[] r, string name) { int i = Array.IndexOf(h, name); Assert.True(i >= 0, "列が無い " + name); return r[i]; }

    /// <summary>(1277) 越境の移民を 1 回置く（k＝0 仏→普・1 普→英・2 墺→仏・各国の最初の州の労働者の 1%）。置けたら true。</summary>
    private static bool PlaceCrossBorder(MultiNationSim sim, int k)
    {
        int[] from = [0, 1, 3], to = [1, 2, 0];   // 盤面の国の並び＝仏 0・普 1・英 2・墺 3（MultiNationSim の作りの順）
        if (sim.Nations.Count < 4) return false;
        var src = sim.Nations[from[k]].World; var dst = sim.Nations[to[k]].World;
        if (src.Regions.Count == 0 || dst.Regions.Count == 0) return false;
        var sp = src.Regions[0].PopOf(PopClass.Worker);
        if (sp is null || !(sp.Size > 1e-6)) return false;
        double em = src.LastEmigration;
        sim.MoveMigrants(src, dst, src.Regions[0], dst.Regions[0], PopClass.Worker, 0.01 * sp.Size);
        return src.LastEmigration > em;
    }

    [Fact]
    public void 陸路は貿易の経路の表と一致し_migration_sのrouteKmは表と合い_xmig_sは区域の越境の量と閉じる()
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_rk_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        int snap = Instruments.TrSnapEveryYears; Instruments.TrSnapEveryYears = 1;
        var sim = new MultiNationSim([NationConfig.France(), NationConfig.Prussia(), NationConfig.Britain(), NationConfig.Austria()], 1789UL, _ => new UtilityAgent());
        try
        {
            Instruments.Phase(sim, 1789UL, "open");
            sim.StartWarmup();
            Instruments.Phase(sim, 1789UL, "warm");
            // (880) 盤面の四半期の数 40→60（Assert の式とリテラルは不変）＝旗 0・ImportPayerLive＝1・SubsistenceNoBoughtTech＝1 の 3 つで xmig_s の行が 20 以上になる {40,60,80} の最小（事前登録 measurements/m880_20261106_prereg_id.txt [5]・数は measurements/m880_20261106/routekm880.txt）
            // ★(1277) c131pf 試験だけの直し（㊴・区分＝軌道に依る前提・(1181) の PlaceWar と同じ型）：越境の移民を試験の中で置く（4 四半期おきに 仏→普・普→英・墺→仏 の順で労働者の 1%）＝旗 CapSkipSellerPotential 1 の木で 60 四半期の自然の越境が対照に届かなかった（種 1789〜1792）。突き合わせと対照の Assert の文と閾値は変えない。
            int placed = 0;
            try { for (int q = 0; q < 60; q++) { sim.StepQuarter(); if (q % 4 == 0 && PlaceCrossBorder(sim, q / 4 % 3)) placed++; Instruments.Quarter(sim, 1789UL, q); } }
            finally { Instruments.Close(sim); }
            string P(string f) => System.IO.Path.Combine(dir, f + "_s1789.tsv");

            // 貿易の経路の表：陸直行の行の realLandKm ＝ 表の陸路（別の経路で同じ部品を引いている）
            var (th, tr) = Table(P("trroute"));
            int landRows = 0;
            foreach (var r in tr)
            {
                if (Str(th, r, "routeCode") != "0") continue;
                int i = RouteKm.IndexOf(Str(th, r, "sellerRegion")), j = RouteKm.IndexOf(Str(th, r, "buyerRegion"));
                if (i < 0 || j < 0) continue;
                Near(Col(th, r, "realLandKm"), RouteKm.Land(i, j)); landRows++;
            }
            Assert.True(landRows >= 1, "陸直行の行が 1 本も無い＝貿易の陸路との突き合わせが何も検査していない");

            // migration_s の routeKm
            var (mh, mr) = Table(P("migration"));
            Assert.Equal("routeKm", mh[^1]); Assert.Equal(5, mh.Length);
            int mfinite = 0;
            foreach (var r in mr)
            {
                Assert.Equal(mh.Length, r.Length);
                int i = RouteKm.IndexOf(Str(mh, r, "fromRegion")), j = RouteKm.IndexOf(Str(mh, r, "toRegion"));
                Assert.True(i >= 0 && j >= 0);
                double rk = Col(mh, r, "routeKm");
                Assert.True(rk >= RouteKm.GreatCircle(i, j) * (1 - 1e-9), $"routeKm {rk} が大圏 {RouteKm.GreatCircle(i, j)} を下回った");
                var exp = RouteKm.Km(i, j, RouteKm.CfgOf(sim.Nations[0].World), C);   // 1789〜1830 年代は運河なし（cfg 0）＝国に依らない
                if (double.IsFinite(rk)) { mfinite++; Near(exp, rk); }
            }
            Assert.True(mfinite >= 20, $"migration_s の有限の routeKm が {mfinite} 行");

            // xmig_s：四半期ごとに region_s の越境の列と閉じる・国の名前は region_s の別の経路と合う
            var (xh, xr) = Table(P("xmig"));
            var (rh, rr) = Table(P("region"));
            Assert.Equal(8, xh.Length);
            Assert.True(xr.Count >= 10, $"xmig_s の行が {xr.Count}＝越境の移民が起きた対照が無い");
            var nationOf = new Dictionary<(string Q, string Reg), string>();
            var outSum = new Dictionary<string, double>(); var inSum = new Dictionary<string, double>();
            foreach (var r in rr)
            {
                string q = Str(rh, r, "quarter");
                nationOf[(q, Str(rh, r, "region"))] = Str(rh, r, "nation");
                outSum[q] = outSum.GetValueOrDefault(q) + Col(rh, r, "crossBorderOutKPerQuarter");
                inSum[q] = inSum.GetValueOrDefault(q) + Col(rh, r, "crossBorderInKPerQuarter");
            }
            var xSum = new Dictionary<string, double>(); var xNations = new HashSet<string>();
            foreach (var r in xr)
            {
                Assert.Equal(xh.Length, r.Length);
                string q = Str(xh, r, "quarter"); double v = Col(xh, r, "movedKPerQuarter");
                Assert.True(v > 0);
                xSum[q] = xSum.GetValueOrDefault(q) + v;
                Assert.NotEqual(Str(xh, r, "fromNation"), Str(xh, r, "toNation"));
                xNations.Add(Str(xh, r, "fromNation")); xNations.Add(Str(xh, r, "toNation"));
                Assert.Equal(nationOf[(q, Str(xh, r, "fromRegion"))], Str(xh, r, "fromNation"));
                Assert.Equal(nationOf[(q, Str(xh, r, "toRegion"))], Str(xh, r, "toNation"));
                int i = RouteKm.IndexOf(Str(xh, r, "fromRegion")), j = RouteKm.IndexOf(Str(xh, r, "toRegion"));
                double gc = Col(xh, r, "greatCircleKm"), rk = Col(xh, r, "routeKm");
                Near(RouteKm.GreatCircle(i, j), gc);
                Assert.True(rk >= gc * (1 - 1e-9));
            }
            foreach (var q in outSum.Keys)
            {
                double o = outSum[q], x = xSum.GetValueOrDefault(q);
                Near(o, x); Near(inSum[q], x);
            }
            Assert.True(placed >= 10, $"越境を置けた回数が {placed}（盤面の前提の破れ）");
            Assert.True(xSum.Count >= 5, "越境の移民が起きた四半期が少ない");
            Assert.True(xNations.Count >= 3, $"(982) 対照：越境の移民に関わった国が {xNations.Count}（3 未満）＝越境の突き合わせが 1 組の国の対に偏っている");
        }
        finally { Instruments.TrSnapEveryYears = snap; Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }
}
