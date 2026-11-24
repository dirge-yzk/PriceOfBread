namespace PriceOfBread.SimCore;

// 力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない（世界を書かない）。〔履歴 InstrumentsPortch.cs#1〕
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
        if (!Tg.Portch) return;   // (1256) 書く表がどれも選ばれていなければ積算ごと飛ぶ
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
            double landRatio = c.LandSeaCostRatio * (1.0 - railGate * (1.0 - PoliticalQuantities.Clamp01(c.RailCostFraction)));   // BilateralMarket.StepBilateral の landRatio の式の写し
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
                // BilateralMarket.StepBilateral の terrCost の段の写し
                int ti = (int)r.Terrain;
                double horse = ti < c.TerrainHorseCost.Length ? c.TerrainHorseCost[ti] : 1.0;
                double rail = ti < c.TerrainRailCost.Length ? c.TerrainRailCost[ti] : 1.0;
                double terrCost = horse + railGate * (rail - horse);
                if (r.Terrain == Terrain.WaterMajor && c.TerrainWaterMajorCost > 0) terrCost = c.TerrainWaterMajorCost;
                else if (r.Terrain == Terrain.WaterMinor && c.TerrainWaterMinorCost > 0) terrCost = c.TerrainWaterMinorCost;
                // BilateralMarket.InlandRailAfterDecay と A1PHat の距離ゼロの門の写し（距離が 0 に近い州は減衰なし＝1）
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
        if (!Tg.Portch) return;   // (1256) 書く表がどれも選ばれていなければ積算ごと飛ぶ
        if (PortchSinks.TryGetValue(sim, out var sk)) { sk.W?.Dispose(); sk.W = null; }
    }
}
