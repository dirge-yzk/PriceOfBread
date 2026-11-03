namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (770) 内陸州の港の選び方の表 portch_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsLandfix.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない（世界を書かない）。INS=1（POB_INSTRUMENTS）のときだけ動く。
// 目的＝事前登録 measurements/m770_20261102_prereg_pr.txt の P1（配線）・P2（鉄道の減衰の新÷旧）・P3（港が変わる州の数）・U1（eff が旧より増えない）・U2（島の罠）を同じ盤面で読む。
// 取り方＝Instruments.Quarter の年末（q%4==3＝その四半期の港の張り直し <c>MultiNationSim.RewirePorts</c> の後）に全ての国の内陸州（港でない・座標のある州）を 1 巡し、
//   エンジンの港を選ぶ関数 <see cref="Simulation.PortChoice"/> は呼ばない（(785) で旗を焼き込み旧の経路〈大圏の最小〉が消えた）：portGc＝<see cref="PortchGcNearest"/>（大圏の最小の港＝旧の規則・計器の局所の書き方）。
//   portUsed・railUsedKm は州の欄の実際の値（エンジンが張った港と km）。(792) で列 portNew（<see cref="PortlmAD"/> の PortA＝形 (d) の前の (a) の反実仮想）を消した：portlm_s の portA と 10 種 202,943 行で全行一致（measurements/m792_20261103_portnew_cmp.txt）＝同じ純関数の写しだった。(a) の反実仮想の港は portlm_s の portA を読む。
//   landKmOld・landKmNew・gcKm は PortChoice の返り値を写さず、PathGraph.LandDistanceKm と Geo.DistanceKm を計器が直接引く（P1・U1 が PortChoice の中の誤りも拾えるように）。
// 行＝内陸州×年。列（接尾辞：Km・Index）：year・nation・region・portGc（大圏の最小の港＝旧の規則）・portUsed（実際に張った NearestPortName）・
//   gcKm（portGc までの大圏）・landKmOld（portGc の eff＝〈陸路が有限 ? 陸路 : 大圏〉＝旧の PortLandKm）・landKmNew（portUsed の eff）・railUsedKm（実際の NearestPortDistanceKm）・
//   railDecayOldIndex＝1/(1+gcKm/N)（旧の規則では NearestPortDistanceKm＝gcKm）・railDecayUsedIndex＝1/(1+railUsedKm/N)（N＝Diplomacy.DistanceNormKm・BilateralMarket.InlandRailAfterDecay と同じ式と門）・
//   basinOld（portGc の海盆）・basinNew（r.SeaBasin＝実際の海盆）・
//   fareLandOldIndex＝landKmOld×terrCost×landRatio・fareLandNewIndex＝landKmNew×terrCost×landRatio（BilateralMarket.cs:421 の陸の脚の運賃 landKm*terrCost*landRatio の同じ盤面の旧と新。km に線形＝新÷旧＝landKmNew÷landKmOld）。
// ⚠限界（計器が自己申告する）：年末の断面（四半期の途中の張り直しは見ない）／港の一覧の作り方（IsPort かつ座標あり）は Simulation.WirePortDistances の写し＝そこが替われば追いつかない／
//   railDecayOldIndex・landKmOld・fareLandOldIndex は旧の規則を今の盤面に当てた反実仮想（旗 1 の軌道では旧の規則の世界は走らない）／
//   terrCost・landRatio の式は BilateralMarket.cs の 308-316 行の写し（そこが替われば追いつかない・新÷旧は km の比なので写しの誤りには強い）。
public static partial class Instruments
{
    internal const string PortchHeaderText = "year\tnation\tregion\tportGc\tportUsed\tgcKm\tlandKmOld\tlandKmNew\trailUsedKm\trailDecayOldIndex\trailDecayUsedIndex\tbasinOld\tbasinNew\tfareLandOldIndex\tfareLandNewIndex";

    sealed class PortchSink { public System.IO.StreamWriter? W; public bool Header; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, PortchSink> PortchSinks = new();

    /// <summary>往復で崩れない文字列（"R"）。ビット一致を文字列の一致で読むために G10 の F は使わない。</summary>
    static string PortchR(double v) => double.IsPositiveInfinity(v) ? "inf" : v.ToString("R", System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>州から港までの eff＝〈陸グラフの距離が有限 ? 陸路 : 大圏〉。PortChoice を通さず直接引く。</summary>
    static (double Gc, double Eff) PortchEff((double Lat, double Lon) from, (double Lat, double Lon) to)
    {
        double d = Geo.DistanceKm(from.Lat, from.Lon, to.Lat, to.Lon);
        double l = PathGraph.LandDistanceKm(from.Lat, from.Lon, to.Lat, to.Lon);
        return (d, double.IsFinite(l) ? l : d);
    }

    /// <summary>州（座標）から見た大圏の最小の港の名前（同率は並びの先＝旧の規則と同じ strict &lt;）。portch_s と portlm_s の portGc（旧の規則の港）の局所の書き方。ports が空なら ""。</summary>
    internal static string PortchGcNearest(double lat, double lon, List<(double Lat, double Lon, string Name)> ports)
    {
        double best = double.MaxValue; string name = "";
        foreach (var p in ports) { double d = Geo.DistanceKm(lat, lon, p.Lat, p.Lon); if (d < best) { best = d; name = p.Name; } }
        return name;
    }

    /// <summary>年末の四半期の末（<see cref="Quarter"/>）：全ての国の内陸州を 1 行ずつ書く。</summary>
    static void PortchTable(MultiNationSim sim, ulong seed, int q)
    {
        if (q % Calendar.QuartersPerYear != Calendar.QuartersPerYear - 1) return;
        var sk = PortchSinks.GetValue(sim, _ => new PortchSink());
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            var ports = new List<(double Lat, double Lon, string Name)>();
            foreach (var r in w.Regions)
                if (r.IsPort) { var pc = Climate.CityCoord(r.Name); if (pc.Lat != 0 || pc.Lon != 0) ports.Add((pc.Lat, pc.Lon, r.Name)); }
            if (ports.Count == 0) continue;
            var c = w.Coef;
            double norm = Diplomacy.DistanceNormKm;
            double railGate = Discovery.RailTransport(w.Discovered) ? 1.0 : 0.0;
            double landRatio = c.LandSeaCostRatio * (1.0 - railGate * (1.0 - PoliticalQuantities.Clamp01(c.RailCostFraction)));   // BilateralMarket.cs:308 の写し
            string nat = NameOf(w);
            foreach (var r in w.Regions)
            {
                if (r.IsPort) continue;
                var cc = Climate.CityCoord(r.Name);
                if (cc.Lat == 0 && cc.Lon == 0) continue;
                string gcPickName = PortchGcNearest(cc.Lat, cc.Lon, ports);
                var pOld = Climate.CityCoord(gcPickName); var pUsed = Climate.CityCoord(r.NearestPortName);
                var (gcKm, landKmOld) = PortchEff((cc.Lat, cc.Lon), (pOld.Lat, pOld.Lon));
                var (_, landKmNew) = PortchEff((cc.Lat, cc.Lon), (pUsed.Lat, pUsed.Lon));
                // BilateralMarket.cs:309-316 の写し（terrCost）
                int ti = (int)r.Terrain;
                double horse = ti < c.TerrainHorseCost.Length ? c.TerrainHorseCost[ti] : 1.0;
                double rail = ti < c.TerrainRailCost.Length ? c.TerrainRailCost[ti] : 1.0;
                double terrCost = horse + railGate * (rail - horse);
                if (r.Terrain == Terrain.WaterMajor && c.TerrainWaterMajorCost > 0) terrCost = c.TerrainWaterMajorCost;
                else if (r.Terrain == Terrain.WaterMinor && c.TerrainWaterMinorCost > 0) terrCost = c.TerrainWaterMinorCost;
                // BilateralMarket.cs:277-278 の門と InlandRailAfterDecay の写し（距離が 0 に近い州は減衰なし＝1）
                double decayOld = gcKm > 1e-9 ? 1.0 / (1.0 + gcKm / norm) : 1.0;
                double decayUsed = r.NearestPortDistanceKm > 1e-9 ? 1.0 / (1.0 + r.NearestPortDistanceKm / norm) : 1.0;
                sk.W ??= Open("portch", seed);
                if (!sk.Header) { sk.W.WriteLine(PortchHeaderText); sk.Header = true; }
                sk.W.WriteLine(string.Join('\t', w.Year.ToString(inv), nat, r.Name, gcPickName, r.NearestPortName,
                    PortchR(gcKm), PortchR(landKmOld), PortchR(landKmNew), PortchR(r.NearestPortDistanceKm),
                    PortchR(decayOld), PortchR(decayUsed),
                    SeaGraph.BasinAt(pOld.Lat, pOld.Lon).ToString(), r.SeaBasin.ToString(),
                    PortchR(landKmOld * terrCost * landRatio), PortchR(landKmNew * terrCost * landRatio)));
            }
        }
    }

    /// <summary>試合の終わり（<see cref="Close"/>）：書き出しを閉じる。</summary>
    static void PortchClose(MultiNationSim sim)
    {
        if (PortchSinks.TryGetValue(sim, out var sk)) { sk.W?.Dispose(); sk.W = null; }
    }
}
