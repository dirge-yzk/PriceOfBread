namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (770) 内陸州の港の選び方の表 portch_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsLandfix.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない（世界を書かない）。INS=1（POB_INSTRUMENTS）のときだけ動く。
// 目的＝事前登録 measurements/m770_20261102_prereg_pr.txt の P1（配線）・P2（鉄道の減衰の新÷旧）・P3（港が変わる州の数）・U1（eff が旧より増えない）・U2（島の罠）を同じ盤面で読む。
// 取り方＝Instruments.Quarter の年末（q%4==3＝その四半期の港の張り直し <c>MultiNationSim.RewirePorts</c> の後）に全ての国の内陸州（港でない・座標のある州）を 1 巡し、
//   港を選ぶ純関数 <see cref="Simulation.PortChoice"/> を byLand=false（旧の規則）と true（新の規則）の両方で呼ぶ（portGc・portNew）。portUsed・railUsedKm は州の欄の実際の値（エンジンが張った港と km）。
//   landKmOld・landKmNew・gcKm は PortChoice の返り値を写さず、PathGraph.LandDistanceKm と Geo.DistanceKm を計器が直接引く（P1・U1 が PortChoice の中の誤りも拾えるように）。
// 行＝内陸州×年。列（接尾辞：Km・Index）：year・nation・region・portGc（大圏の最小の港＝旧の規則）・portNew（PortChoice(true) の答え）・portUsed（実際に張った NearestPortName）・
//   gcKm（portGc までの大圏）・landKmOld（portGc の eff＝〈陸路が有限 ? 陸路 : 大圏〉＝旧の PortLandKm）・landKmNew（portUsed の eff）・railUsedKm（実際の NearestPortDistanceKm）・
//   railDecayOldIndex＝1/(1+k·gcKm/N)（旧の規則では NearestPortDistanceKm＝gcKm）・railDecayUsedIndex＝1/(1+k·railUsedKm/N)（k＝InlandPortDistanceDecay・N＝Diplomacy.DistanceNormKm・BilateralMarket.cs:272-273 と同じ式と門）・
//   basinOld（portGc の海盆）・basinNew（r.SeaBasin＝実際の海盆）・
//   fareLandOldIndex＝landKmOld×terrCost×landRatio・fareLandNewIndex＝landKmNew×terrCost×landRatio（BilateralMarket.cs:421 の陸の脚の運賃 landKm*terrCost*landRatio の同じ盤面の旧と新。km に線形＝新÷旧＝landKmNew÷landKmOld）。
// ⚠限界（計器が自己申告する）：年末の断面（四半期の途中の張り直しは見ない）／港の一覧の作り方（IsPort かつ座標あり）は Simulation.WirePortDistances の写し＝そこが替われば追いつかない／
//   railDecayOldIndex・landKmOld・fareLandOldIndex は旧の規則を今の盤面に当てた反実仮想（旗 1 の軌道では旧の規則の世界は走らない）／
//   terrCost・landRatio の式は BilateralMarket.cs の 308-316 行の写し（そこが替われば追いつかない・新÷旧は km の比なので写しの誤りには強い）／portNew は WirePortDistances が呼ぶのと同じ純関数＝P1 は配線しか拾わない。
public static partial class Instruments
{
    internal const string PortchHeaderText = "year\tnation\tregion\tportGc\tportNew\tportUsed\tgcKm\tlandKmOld\tlandKmNew\trailUsedKm\trailDecayOldIndex\trailDecayUsedIndex\tbasinOld\tbasinNew\tfareLandOldIndex\tfareLandNewIndex";

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
            double k = c.InlandPortDistanceDecay, norm = Diplomacy.DistanceNormKm;
            double railGate = Discovery.RailTransport(w.Discovered) ? 1.0 : 0.0;
            double landRatio = c.LandSeaCostRatio * (1.0 - railGate * (1.0 - PoliticalQuantities.Clamp01(c.RailCostFraction)));   // BilateralMarket.cs:308 の写し
            string nat = NameOf(w);
            foreach (var r in w.Regions)
            {
                if (r.IsPort) continue;
                var cc = Climate.CityCoord(r.Name);
                if (cc.Lat == 0 && cc.Lon == 0) continue;
                var gcPick = Simulation.PortChoice(cc.Lat, cc.Lon, ports, false);
                var newPick = Simulation.PortChoice(cc.Lat, cc.Lon, ports, true);
                var pOld = Climate.CityCoord(gcPick.Name); var pUsed = Climate.CityCoord(r.NearestPortName);
                var (gcKm, landKmOld) = PortchEff((cc.Lat, cc.Lon), (pOld.Lat, pOld.Lon));
                var (_, landKmNew) = PortchEff((cc.Lat, cc.Lon), (pUsed.Lat, pUsed.Lon));
                // BilateralMarket.cs:309-316 の写し（terrCost）
                int ti = (int)r.Terrain;
                double horse = ti < c.TerrainHorseCost.Length ? c.TerrainHorseCost[ti] : 1.0;
                double rail = ti < c.TerrainRailCost.Length ? c.TerrainRailCost[ti] : 1.0;
                double terrCost = horse + railGate * (rail - horse);
                if (r.Terrain == Terrain.WaterMajor && c.TerrainWaterMajorCost > 0) terrCost = c.TerrainWaterMajorCost;
                else if (r.Terrain == Terrain.WaterMinor && c.TerrainWaterMinorCost > 0) terrCost = c.TerrainWaterMinorCost;
                // BilateralMarket.cs:272-273 の写し（距離が 0 に近い・係数 0 の州は減衰なし＝1）
                double decayOld = k > 1e-12 && gcKm > 1e-9 ? 1.0 / (1.0 + k * gcKm / norm) : 1.0;
                double decayUsed = k > 1e-12 && r.NearestPortDistanceKm > 1e-9 ? 1.0 / (1.0 + k * r.NearestPortDistanceKm / norm) : 1.0;
                sk.W ??= Open("portch", seed);
                if (!sk.Header) { sk.W.WriteLine(PortchHeaderText); sk.Header = true; }
                sk.W.WriteLine(string.Join('\t', w.Year.ToString(inv), nat, r.Name, gcPick.Name, newPick.Name, r.NearestPortName,
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
