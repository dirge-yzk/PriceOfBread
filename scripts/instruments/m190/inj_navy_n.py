"""[計測の写しだけ] (190) 新しい木用の写し（FleetStock→FleetPowerValue・budget＝陸＋海・crew＝乗員〔兵士の単位〕・shape＝NavalSpendShareOf）。元は (152) 陸海の軍事費の分割・脅威の計器（読むだけ）。環境変数 POB_NAVY_DIR の下に n<seed>.txt。未設定なら1命令も走らない。
  Y 行＝年に1回（q%4==3）の国ごと：年・国（BaseName）・人口・兵士・Military・NavalShare・海軍の支出比（NavalSpendShare）・陸の維持費・海の維持費・
        艦隊ゲージ・海軍力・陸軍力・乗員の上限（Military×NavalShare）・理想の海の割合（首都州の ShapeNavalShare）・NeedLand・NeedSea・NeedInternal・
        即応度・軍需予算（州の和）・GDP・使える歳入・艦隊ストック（州の和）・陸の熟練の倍率・海の熟練の倍率・交戦中か・外洋の在（大西洋＋太平洋）
  WEV 行＝戦争1本ごと（Diplomacy.AddBelligerent の Wars.Add の直後に登録・書き出しは最後の四半期）＝月の刻み・年・内戦か・両者の Id・国か・戦死（両側の和）・月数
inj_s3a と inj_ghost を先に当てた木に当てる（SelfPlay の ghM の行の後ろに足す）。使い方：python inj_navy.py <木の根>"""
import sys
root = sys.argv[1]

def edit(rel, pairs, tail=''):
    p = root + '/' + rel
    raw = open(p, 'rb').read(); bom = raw.startswith(bytes([0xef, 0xbb, 0xbf])); s = raw.decode('utf-8-sig')
    CR, LF = chr(13), chr(10)
    crlf = (CR + LF) in s
    for old, new in pairs:
        if crlf: old = old.replace(LF, CR + LF); new = new.replace(LF, CR + LF)
        assert s.count(old) == 1, (rel, old[:90], s.count(old))
        s = s.replace(old, new)
    s += tail.replace(LF, CR + LF) if crlf else tail
    open(p, 'wb').write((b'\xef\xbb\xbf' if bom else b'') + s.encode('utf-8'))

TAIL = r'''

/// <summary>[計測の写しだけ] (152) 陸海の軍事費と脅威の計器（読むだけ）。</summary>
public static class NavyDiag
{
    public static readonly string? Dir = System.Environment.GetEnvironmentVariable("POB_NAVY_DIR");
    public static readonly bool On = Dir is { Length: > 0 };
    static string F(double v) => double.IsNaN(v) ? "nan" : v.ToString("G5", System.Globalization.CultureInfo.InvariantCulture);
    public sealed class Acc
    {
        public int Tick;
        public readonly System.Collections.Generic.List<(int t, int y, War w)> Ev = new();
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<object, Acc> ByDip = new();
    public static Acc For(object dip) => ByDip.GetValue(dip, _ => new Acc());
    public sealed class Match
    {
        readonly System.IO.StreamWriter W;
        public Match(ulong seed)
        {
            W = new System.IO.StreamWriter(System.IO.Path.Combine(Dir!, $"n{seed}.txt"), false, new System.Text.UTF8Encoding(false), 1 << 16);
            W.Write("#Y\tq\tyr\tnation\tpop\tsold\tmil\tnshare\tspendSh\tarmyUp\tnavyUp\tfleet\tnaval\tland\tcrew\tshape\tneedL\tneedS\tneedI\tready\tbudget\tgdp\trev\tfstock\tsklA\tsklN\twar\tocean\tseaTP\texp\timp\tcolOver\tcolLand\tseaSh\tnomGdp\n");
            W.Write("#B\tyr\tnation\tneighbor\tedges\tweight\n");
            W.Write("#WEV\tt\tyr\tcivil\ta\tb\tan\tbn\tdead\tmonths\n");
        }
        public void Quarter(MultiNationSim sim, int q, int quarters)
        {
            var ns = sim.Nations;
            if (q % 4 == 3)
            {
                for (int i = 0; i < ns.Count; i++)
                {
                    var w = ns[i].World;
                    if (w.SelfPower is not { } sp || !(w.TotalPopulation > 0)) continue;
                    string bn = sp.BaseName.Length > 0 ? sp.BaseName : sp.Name;
                    double budget = 0, fstock = 0;
                    foreach (var rg in w.Regions) { if (rg.State != RegionState.Active) continue; budget += rg.MilitaryBudget + rg.NavalBudget; } fstock = EconomyStep.FleetPowerValue(w);
                    bool war = false;
                    if (w.Diplomacy is { } dp) foreach (var wr in dp.Wars) if (wr.A == sp || wr.B == sp) { war = true; break; }
                    double ocean = sp.BasinPresence[(int)Basin.Atlantic] + sp.BasinPresence[(int)Basin.Pacific];
                    double seaTP = 0, colOver = 0, colLand = 0;
                    var capc = Climate.CityCoord(w.Region.Name); string capLm = Landmass.Of(capc.Lat, capc.Lon);
                    foreach (var rg in w.Regions)
                    {
                        if (rg.State != RegionState.Active) continue;
                        if (rg.IsPort) seaTP += rg.LastPortThroughput;
                        if (rg.Tier == RegionTier.Colonial) { var cc = Climate.CityCoord(rg.Name); if (Landmass.Of(cc.Lat, cc.Lon) == capLm) colLand += rg.SumPopulation(); else colOver += rg.SumPopulation(); }
                    }
                    W.Write($"Y\t{q + 1}\t{w.Year}\t{bn}\t{F(w.TotalPopulation)}\t{F(w.SoldierSize())}\t{F(sp.Military)}\t{F(sp.NavalShare)}\t{F(w.NavalSpendShare)}\t{F(w.ArmyUpkeep)}\t{F(w.NavyUpkeep)}\t{F(sp.FleetGauge)}\t{F(sp.NavalPower)}\t{F(sp.LandPower)}\t{F(sp.NavalShare * w.SoldierSize())}\t{F(ArmyShape.NavalSpendShareOf(sp))}\t{F(sp.NeedLand)}\t{F(sp.NeedSea)}\t{F(sp.NeedInternal)}\t{F(w.MilitaryReadiness)}\t{F(budget)}\t{F(sp.Gdp)}\t{F(w.SpendableRevenue())}\t{F(fstock)}\t{F(sp.ArmySkillMul)}\t{F(sp.NavySkillMul)}\t{(war ? 1 : 0)}\t{F(ocean)}\t{F(seaTP)}\t{F(w.LastExportValue)}\t{F(w.LastImportValue)}\t{F(colOver)}\t{F(colLand)}\t{F(seaTP / (w.FxRate * 3 * w.NominalGdp))}\t{F(w.NominalGdp)}\n");
                }
            }
            if (q % 4 == 3 && (ns[0].World.Year is 1790 or 1815 or 1860 or 1900 or 1914))
            {
                var nodes = PathGraph.Nodes; int nn = nodes.Length;
                var owner = new int[nn]; var terr = new double[nn];
                for (int k = 0; k < nn; k++) owner[k] = -1;
                var cf = ns[0].World.Coef;
                for (int i = 0; i < ns.Count; i++)
                    foreach (var rg in ns[i].World.Regions)
                    {
                        if (rg.State != RegionState.Active) continue;
                        var cc = Climate.CityCoord(rg.Name); if (cc.Lat == 0 && cc.Lon == 0) continue;
                        int nd = PathGraph.NearestNode(cc.Lat, cc.Lon); if (nd < 0) continue;
                        owner[nd] = i; int ti = (int)rg.Terrain; terr[nd] = ti < cf.TerrainHorseCost.Length ? cf.TerrainHorseCost[ti] : 1.0;
                    }
                var cnt = new System.Collections.Generic.Dictionary<(int, int), (int e, double wgt)>();
                var passN = new System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<int>>();
                void Add(int a, int b, double wgt) { if (a == b) return; var key = a < b ? (a, b) : (b, a); cnt.TryGetValue(key, out var v); cnt[key] = (v.e + 1, v.wgt + wgt); }
                foreach (var e in PathGraph.Edges)
                {
                    int oa = owner[e.A], ob = owner[e.B];
                    if (oa >= 0 && ob >= 0) Add(oa, ob, 2.0 / (terr[e.A] + terr[e.B]));
                    if (nodes[e.A].Kind == "pass" && ob >= 0) { if (!passN.TryGetValue(e.A, out var l)) passN[e.A] = l = new(); l.Add(e.B); }
                    if (nodes[e.B].Kind == "pass" && oa >= 0) { if (!passN.TryGetValue(e.B, out var l)) passN[e.B] = l = new(); l.Add(e.A); }
                }
                foreach (var l in passN.Values)
                    for (int x = 0; x < l.Count; x++) for (int y = x + 1; y < l.Count; y++)
                        if (owner[l[x]] != owner[l[y]]) Add(owner[l[x]], owner[l[y]], 2.0 / (terr[l[x]] + terr[l[y]]) / PathGraph.PassCostMult);
                foreach (var kv in cnt)
                {
                    var wa = ns[kv.Key.Item1].World; var wb = ns[kv.Key.Item2].World;
                    string na = wa.SelfPower is { } pa ? (pa.BaseName.Length > 0 ? pa.BaseName : pa.Name) : wa.Region.Name;
                    string nb = wb.SelfPower is { } pb ? (pb.BaseName.Length > 0 ? pb.BaseName : pb.Name) : wb.Region.Name;
                    W.Write($"B\t{ns[0].World.Year}\t{na}\t{nb}\t{kv.Value.e}\t{F(kv.Value.wgt)}\n");
                }
            }
            if (q == quarters - 1)
            {
                var acc = For(sim.Diplomacy);
                foreach (var (t, y, wr) in acc.Ev)
                    W.Write($"WEV\t{t}\t{y}\t{(wr.CivilWarParent is not null ? 1 : 0)}\t{wr.A.Id}\t{wr.B.Id}\t{(wr.A.Nation != null ? 1 : 0)}\t{(wr.B.Nation != null ? 1 : 0)}\t{F(wr.DeadA + wr.DeadB)}\t{wr.Months}\n");
                W.Dispose();
            }
        }
    }
}
'''
edit('PriceOfBread.SimCore/Discovery.cs', [], TAIL)
edit('PriceOfBread.SimCore/Diplomacy.cs', [
    ("    public void Step(Coefficients c, DeterministicRng rng, int month)\n    {\n",
     "    public void Step(Coefficients c, DeterministicRng rng, int month)\n    {\n        if (NavyDiag.On) NavyDiag.For(this).Tick++;\n"),
    ("        Wars.Add(war);\n        _embargo.Add(Key(war.A, war.B));",
     "        Wars.Add(war);\n        if (NavyDiag.On) NavyDiag.For(this).Ev.Add((NavyDiag.For(this).Tick, _year, war));\n        _embargo.Add(Key(war.A, war.B));"),
])
edit('PriceOfBread.SimCore/SelfPlay.cs', [
    ('        var ghM = GhostDiag.On ? new GhostDiag.Match(seed) : null; GhostDiag.Cur = ghM;\n',
     '        var ghM = GhostDiag.On ? new GhostDiag.Match(seed) : null; GhostDiag.Cur = ghM;\n        var navyM = NavyDiag.On ? new NavyDiag.Match(seed) : null;\n'),
    ('                ghM?.Quarter(sim, q, quarters);\n',
     '                ghM?.Quarter(sim, q, quarters);\n                navyM?.Quarter(sim, q, quarters);\n'),
])
print('ok', root)
