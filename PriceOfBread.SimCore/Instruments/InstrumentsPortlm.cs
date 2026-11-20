namespace PriceOfBread.SimCore;

// 力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない（世界を書かない）。〔履歴 InstrumentsPortlm.cs#1〕
public static partial class Instruments
{
    internal const string PortlmHeaderText = "year\tnation\tregion\tlmRegion\tportGc\tlmGc\tportUsed\tlmUsed\tlandKmUsed\tgcKmUsed\tcrossLandmass"
        + "\tlmRegionPortCount\tfinitePortCount\tsameLmFinitePortCount\tgcKmMin"
        + "\tportFin\tlmFin\teffFinKm\tportSea\tlmSea\teffSeaKm\tportShore\tlmShore\teffShoreKm\tshoreName\tportDrop\tlmDrop\teffDropKm"
        + "\tportA\tlmA\teffA\tlandKmA\tportD\tlmD\teffD\tusedEqD";

    sealed class PortlmSink { public System.IO.StreamWriter? W; public bool Header; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, PortlmSink> PortlmSinks = new();

    /// <summary>州（座標）と使った港（座標）の陸塊の判定。札は <see cref="Landmass.Of"/>・陸路は <see cref="PathGraph.LandDistanceKm"/> の生の値（∞を大圏で退避しない）。
    /// Cross＝港の札が州の札と違う（陸路が有限でも札が違えば 1＝札で数える・陸路の有限は LandKm で別に読む）。</summary>
    internal static (string LmRegion, string LmPort, double LandKm, double GcKm, bool Cross) PortlmClassify((double Lat, double Lon) region, (double Lat, double Lon) port)
    {
        string lmR = Landmass.Of(region.Lat, region.Lon), lmP = Landmass.Of(port.Lat, port.Lon);
        double land = PathGraph.LandDistanceKm(region.Lat, region.Lon, port.Lat, port.Lon);
        double gc = Geo.DistanceKm(region.Lat, region.Lon, port.Lat, port.Lon);
        return (lmR, lmP, land, gc, lmP != lmR);
    }

    /// <summary>
    /// 形 b・c・c′・d の答え（反実仮想・純関数・同率は並びの先＝Simulation.PortChoice と同じ）。〔履歴 InstrumentsPortlm.cs#2〕
    /// </summary>
    internal static ((string Port, double EffKm) Fin, (string Port, double EffKm) Sea, (string Port, double EffKm) Shore, string ShoreName, (string Port, double EffKm) Drop,
        int LmPorts, int Finite, int SameLmFinite, double GcKmMin)
        PortlmShapes((double Lat, double Lon) r, string lmRegion, List<(double Lat, double Lon, string Name)> ports, List<(double Lat, double Lon, string Name)> shores, bool suez, bool panama)
    {
        int n = ports.Count;
        var gc = new double[n]; var land = new double[n];
        int lmPorts = 0, finite = 0, sameLmFinite = 0;
        int aBest = -1, gcBest = -1, finBest = -1;
        double aEffBest = double.MaxValue, gcMin = double.MaxValue, finMin = double.MaxValue;
        for (int i = 0; i < n; i++)
        {
            var p = ports[i];
            gc[i] = Geo.DistanceKm(r.Lat, r.Lon, p.Lat, p.Lon);
            land[i] = PathGraph.LandDistanceKm(r.Lat, r.Lon, p.Lat, p.Lon);
            bool fin = double.IsFinite(land[i]);
            bool sameLm = Landmass.Of(p.Lat, p.Lon) == lmRegion;
            if (sameLm) lmPorts++;
            if (fin) { finite++; if (sameLm) sameLmFinite++; }
            double e = fin ? land[i] : gc[i];
            if (e < aEffBest) { aEffBest = e; aBest = i; }
            if (gc[i] < gcMin) { gcMin = gc[i]; gcBest = i; }
            if (fin && land[i] < finMin) { finMin = land[i]; finBest = i; }
        }
        (string, double) A() => aBest < 0 ? ("", double.NaN) : (ports[aBest].Name, aEffBest);
        (string, double) fin_ = finBest >= 0 ? (ports[finBest].Name, finMin) : (gcBest < 0 ? ("", double.NaN) : (ports[gcBest].Name, gc[gcBest]));
        // (c) 最寄りの岸＝同じ国の有限の港の陸路の最小（＝finBest）
        (string, double) sea_;
        if (finBest < 0) sea_ = A();
        else
        {
            var q = ports[finBest]; int best = -1; double bm = double.MaxValue;
            for (int i = 0; i < n; i++)
            {
                double e = double.IsFinite(land[i]) ? land[i] : land[finBest] + Geo.SeaDistanceKm(q.Lat, q.Lon, ports[i].Lat, ports[i].Lon, suez, panama);
                if (e < bm) { bm = e; best = i; }
            }
            sea_ = (ports[best].Name, bm);
        }
        // (c′) 岸＝全ての国の港のうち陸路が有限で陸路の最小
        int sBest = -1; double sMin = double.MaxValue;
        for (int j = 0; j < shores.Count; j++)
        {
            double l = PathGraph.LandDistanceKm(r.Lat, r.Lon, shores[j].Lat, shores[j].Lon);
            if (double.IsFinite(l) && l < sMin) { sMin = l; sBest = j; }
        }
        (string, double) shore_; string shoreName = "";
        if (sBest < 0 || n == 0) shore_ = A();
        else
        {
            var s = shores[sBest]; shoreName = s.Name; int best = -1; double bm = double.MaxValue;
            for (int i = 0; i < n; i++)
            {
                double viaSea = sMin + (ports[i].Name == s.Name ? 0.0 : Geo.SeaDistanceKm(s.Lat, s.Lon, ports[i].Lat, ports[i].Lon, suez, panama));
                double e = double.IsFinite(land[i]) ? System.Math.Min(land[i], viaSea) : viaSea;
                if (e < bm) { bm = e; best = i; }
            }
            shore_ = (ports[best].Name, bm);
        }
        // (d) 札が同じで有限の港があれば∞を外す＝有限の港だけの (a) の eff の最小（＝陸路の最小）
        (string, double) drop_ = sameLmFinite > 0 ? (ports[finBest].Name, finMin) : A();
        return (fin_, sea_, shore_, shoreName, drop_, lmPorts, finite, sameLmFinite, gcBest < 0 ? double.NaN : gcMin);
    }

    /// <summary>
    /// (a)＝港ごとの eff〈陸路が有限なら陸路・∞なら大圏〉の最小（同率は並びの先）。〔履歴 InstrumentsPortlm.cs#3〕
    /// </summary>
    internal static (string PortA, double EffA, double LandA, string PortD, double EffD) PortlmAD((double Lat, double Lon) r, string lmRegion, List<(double Lat, double Lon, string Name)> ports)
    {
        var cand = new List<(string Name, double Eff, double Land, bool Fin)>();
        foreach (var p in ports)
        {
            double land = PathGraph.LandDistanceKm(r.Lat, r.Lon, p.Lat, p.Lon);
            double gcKm = Geo.DistanceKm(r.Lat, r.Lon, p.Lat, p.Lon);
            bool fin = double.IsFinite(land);
            cand.Add((p.Name, fin ? land : gcKm, land, fin));
        }
        var lmOf = new Dictionary<string, string>();
        foreach (var p in ports) lmOf[p.Name] = Landmass.Of(p.Lat, p.Lon);
        bool anySameFinite = false;
        foreach (var c in cand) if (c.Fin && lmOf[c.Name] == lmRegion) anySameFinite = true;
        (string Name, double Eff, double Land) a = ("", double.NaN, double.NaN), d = ("", double.NaN, double.NaN);
        foreach (var c in cand)
        {
            if (a.Name.Length == 0 || c.Eff < a.Eff) a = (c.Name, c.Eff, c.Land);
            if (anySameFinite && !c.Fin) continue;
            if (d.Name.Length == 0 || c.Eff < d.Eff) d = (c.Name, c.Eff, c.Land);
        }
        return (a.Name, a.Eff, a.Land, d.Name, d.Eff);
    }

    /// <summary>年末の四半期の末（<see cref="Quarter"/>）：全ての国の内陸州を 1 行ずつ書く。</summary>
    static void PortlmTable(MultiNationSim sim, ulong seed, int q)
    {
        if (!Tg.Portch) return;   // (1256) 書く表がどれも選ばれていなければ積算ごと飛ぶ
        if (q % Calendar.QuartersPerYear != Calendar.QuartersPerYear - 1) return;
        var sk = PortlmSinks.GetValue(sim, _ => new PortlmSink());
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        var shores = new List<(double Lat, double Lon, string Name)>();
        foreach (var n in sim.Nations)
            foreach (var r in n.World.Regions)
                if (r.IsPort) { var pc = Climate.CityCoord(r.Name); if (pc.Lat != 0 || pc.Lon != 0) shores.Add((pc.Lat, pc.Lon, r.Name)); }
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            var ports = new List<(double Lat, double Lon, string Name)>();
            foreach (var r in w.Regions)
                if (r.IsPort) { var pc = Climate.CityCoord(r.Name); if (pc.Lat != 0 || pc.Lon != 0) ports.Add((pc.Lat, pc.Lon, r.Name)); }
            if (ports.Count == 0) continue;
            bool suez = w.Diplomacy?.ChokeOpenFor(Diplomacy.SuezId, w.SelfPower) ?? false;
            bool panama = w.Diplomacy?.ChokeOpenFor(Diplomacy.PanamaId, w.SelfPower) ?? false;
            string nat = NameOf(w);
            foreach (var r in w.Regions)
            {
                if (r.IsPort) continue;
                var cc = Climate.CityCoord(r.Name);
                if (cc.Lat == 0 && cc.Lon == 0) continue;
                string gcPickName = PortchGcNearest(cc.Lat, cc.Lon, ports);
                var pg = Climate.CityCoord(gcPickName); var pu = Climate.CityCoord(r.NearestPortName);
                var used = PortlmClassify((cc.Lat, cc.Lon), (pu.Lat, pu.Lon));
                string lmGc = Landmass.Of(pg.Lat, pg.Lon);
                var sh = PortlmShapes((cc.Lat, cc.Lon), used.LmRegion, ports, shores, suez, panama);
                var ad = PortlmAD((cc.Lat, cc.Lon), used.LmRegion, ports);
                string Lm(string port) { var c = Climate.CityCoord(port); return port.Length == 0 ? "" : Landmass.Of(c.Lat, c.Lon); }
                sk.W ??= Open("portlm", seed);
                if (!sk.Header) { sk.W.WriteLine(PortlmHeaderText); sk.Header = true; }
                bool noPort = r.NearestPortName.Length == 0;   // 張り直しで港が付かなかった州（港の無い国は上で飛ばす＝通常は来ない）＝札と km を空・crossLandmass 0
                sk.W.WriteLine(string.Join('\t', w.Year.ToString(inv), nat, r.Name, used.LmRegion, gcPickName, lmGc, r.NearestPortName, noPort ? "" : used.LmPort,
                    noPort ? "" : PortchR(used.LandKm), noPort ? "" : PortchR(used.GcKm), !noPort && used.Cross ? "1" : "0",
                    sh.LmPorts.ToString(inv), sh.Finite.ToString(inv), sh.SameLmFinite.ToString(inv), PortchR(sh.GcKmMin),
                    sh.Fin.Port, Lm(sh.Fin.Port), PortchR(sh.Fin.EffKm),
                    sh.Sea.Port, Lm(sh.Sea.Port), PortchR(sh.Sea.EffKm),
                    sh.Shore.Port, Lm(sh.Shore.Port), PortchR(sh.Shore.EffKm), sh.ShoreName,
                    sh.Drop.Port, Lm(sh.Drop.Port), PortchR(sh.Drop.EffKm),
                    ad.PortA, Lm(ad.PortA), PortchR(ad.EffA), PortchR(ad.LandA), ad.PortD, Lm(ad.PortD), PortchR(ad.EffD), noPort ? "" : (r.NearestPortName == ad.PortD ? "1" : "0")));
            }
        }
    }

    /// <summary>試合の終わり（<see cref="Close"/>）：書き出しを閉じる。</summary>
    static void PortlmClose(MultiNationSim sim)
    {
        if (!Tg.Portch) return;   // (1256) 書く表がどれも選ばれていなければ積算ごと飛ぶ
        if (PortlmSinks.TryGetValue(sim, out var sk)) { sk.W?.Dispose(); sk.W = null; }
    }
}
