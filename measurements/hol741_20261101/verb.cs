// ★使い捨て（hol52・push しない）：州の組の素の経路 km を書き出す（シミュを回さない）。
if (args.Length > 0 && args[0] == "routedump")
{
    var rsim = new MultiNationSim(WorldData.Nations(), 1UL, _ => new UtilityAgent());
    var regs = new List<(string Name, double Lat, double Lon, bool Port)>();
    var seenR = new HashSet<string>();
    foreach (var n in rsim.Nations) foreach (var r in n.World.Regions)
    { var (la, lo) = Places.Coord(r.Name); if ((la != 0 || lo != 0) && seenR.Add(r.Name)) regs.Add((r.Name, la, lo, r.IsPort)); }
    int m = regs.Count, N = PathGraph.Nodes.Length;
    var LM = PathGraph.LandMatrix();
    var node = new int[m]; var graft = new double[m];
    for (int i = 0; i < m; i++) { node[i] = PathGraph.NearestNode(regs[i].Lat, regs[i].Lon); graft[i] = node[i] < 0 ? double.NaN : Geo.DistanceKm(regs[i].Lat, regs[i].Lon, PathGraph.Nodes[node[i]].Lat, PathGraph.Nodes[node[i]].Lon); }
    double Land(int i, int j) => node[i] < 0 || node[j] < 0 ? double.PositiveInfinity : (i == j ? 0 : LM[node[i] * N + node[j]] + graft[i] + graft[j]);
    var leg = new double[m]; var lp = new int[m];
    for (int i = 0; i < m; i++)
    {
        if (regs[i].Port) { leg[i] = 0; lp[i] = i; continue; }
        double best = double.PositiveInfinity; int bj = -1;
        for (int j = 0; j < m; j++) if (regs[j].Port) { double d = Land(i, j); if (d < best) { best = d; bj = j; } }
        if (bj < 0) { for (int j = 0; j < m; j++) if (regs[j].Port) { double d = Geo.DistanceKm(regs[i].Lat, regs[i].Lon, regs[j].Lat, regs[j].Lon); if (d < best) { best = d; bj = j; } } }
        leg[i] = best; lp[i] = bj;
    }
    var o = new System.IO.StreamWriter(args.Length > 1 ? args[1] : "routedump.tsv");
    o.WriteLine("a\tb\tportA\tportB\tgc\tland\tseaGc00\tseaGc10\tseaGc11\tlegA\tlegB\tseaPort00\tseaPort10\tseaPort11");
    string F(double x) => x.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
    (bool S, bool P)[] cs = [(false, false), (true, false), (true, true)];
    for (int i = 0; i < m; i++) for (int j = 0; j < m; j++)
    {
        if (i == j) continue;
        var a = regs[i]; var b = regs[j];
        double gc = Geo.DistanceKm(a.Lat, a.Lon, b.Lat, b.Lon), land = Land(i, j);
        var sg = new double[3]; var sp = new double[3];
        for (int k = 0; k < 3; k++)
        {
            sg[k] = Geo.SeaDistanceKm(a.Lat, a.Lon, b.Lat, b.Lon, cs[k].S, cs[k].P);
            var pa = regs[lp[i]]; var pb = regs[lp[j]];
            sp[k] = leg[i] + (lp[i] == lp[j] ? 0 : Geo.SeaDistanceKm(pa.Lat, pa.Lon, pb.Lat, pb.Lon, cs[k].S, cs[k].P)) + leg[j];
        }
        o.WriteLine(string.Join('\t', a.Name, b.Name, regs[lp[i]].Name, regs[lp[j]].Name, F(gc), F(land), F(sg[0]), F(sg[1]), F(sg[2]), F(leg[i]), F(leg[j]), F(sp[0]), F(sp[1]), F(sp[2])));
    }
    o.Close();
    Console.WriteLine($"routedump {m} 州・港 {regs.Count(x => x.Port)}");
    return;
}

