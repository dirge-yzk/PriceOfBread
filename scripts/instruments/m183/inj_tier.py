"""[計測の写しだけ] (183) 州の格の計器（読むだけ）。環境変数 POB_TIER_DIR の下に t<seed>.txt。未設定なら1命令も走らない。
  T 行＝年に1回（q%4==3）の国ごと：年・国（BaseName）・本土 Core・編入 Integrated・植民地 Colonial・海の向こうの本国の格（首都と別の陸塊で Core/Integrated）・
        そのうち 1783 の持ち主が自分でない州・首都の陸塊の本国の格
  E 行＝州の格が動く出来事（移し替え Territory.TransferRegion の Rehome の前後・独立の確定 RegisterIndependence の Sovereign・植民地の昇格）＝
        年・種類・州・前の格・後の格・送り手・受け手・州の 1783 の持ち主が受け手か・受け手の首都と同じ陸塊か
inj_navy を先に当てた木に当てる（SelfPlay の navyM の行の後ろに足す）。使い方：python inj_tier.py <木の根>"""
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

/// <summary>[計測の写しだけ] (183) 州の格の計器（読むだけ）。</summary>
public static class TierDiag
{
    public static readonly string? Dir = System.Environment.GetEnvironmentVariable("POB_TIER_DIR");
    public static readonly bool On = Dir is { Length: > 0 };
    public sealed class Acc { public readonly System.Collections.Generic.List<string> Ev = new(); }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<object, Acc> ByDip = new();
    public static Acc? For(object? dip) => dip is null ? null : ByDip.GetValue(dip, _ => new Acc());
    static string Nm(World? w) => w?.SelfPower is { } sp ? (sp.BaseName.Length > 0 ? sp.BaseName : sp.Name) : "-";
    static string Lm(Region r) { var cc = Climate.CityCoord(r.Name); return Landmass.Of(cc.Lat, cc.Lon); }
    /// <summary>出来事を1行（州の格の前後）。owner＝出来事の後の持ち主（null＝建国の器がまだ無い）。</summary>
    public static void Event(object? dip, int year, string kind, Region r, RegionTier before, World? from, World? owner)
    {
        if (!On || For(dip) is not { } acc) return;
        int originSelf = owner?.SelfPower is { } op && op.Id == r.OriginPowerId ? 1 : 0;
        int sameLm = owner is not null && owner.Regions.Count > 0 && owner.Regions[0] != r ? (Lm(owner.Regions[0]) == Lm(r) ? 1 : 0) : -1;
        lock (acc) acc.Ev.Add($"E\t{year}\t{kind}\t{r.Name}\t{before}\t{r.Tier}\t{Nm(from)}\t{Nm(owner)}\t{originSelf}\t{sameLm}");
    }
    public sealed class Match
    {
        readonly System.IO.StreamWriter W;
        public Match(ulong seed)
        {
            W = new System.IO.StreamWriter(System.IO.Path.Combine(Dir!, $"t{seed}.txt"), false, new System.Text.UTF8Encoding(false), 1 << 16);
            W.Write("#T\tyr\tnation\tcore\tinteg\tcol\thomeOver\thomeOverForeign\thomeCapLm\n");
            W.Write("#E\tyr\tkind\tregion\tbefore\tafter\tfrom\tto\toriginTo\tsameLm\n");
        }
        public void Quarter(MultiNationSim sim, int q, int quarters)
        {
            var ns = sim.Nations;
            if (q % 4 == 3)
                for (int i = 0; i < ns.Count; i++)
                {
                    var w = ns[i].World;
                    if (w.SelfPower is not { } sp || !(w.TotalPopulation > 0) || w.Regions.Count == 0) continue;
                    string capLm = Lm(w.Regions[0]);
                    int core = 0, integ = 0, col = 0, over = 0, overF = 0, capH = 0;
                    foreach (var r in w.Regions)
                    {
                        if (r.State != RegionState.Active) continue;
                        if (r.Tier == RegionTier.Core) core++; else if (r.Tier == RegionTier.Integrated) integ++; else col++;
                        if (r.Tier == RegionTier.Colonial) continue;
                        if (Lm(r) != capLm) { over++; if (r.OriginPowerId != sp.Id) overF++; } else capH++;
                    }
                    W.Write($"T\t{w.Year}\t{Nm(w)}\t{core}\t{integ}\t{col}\t{over}\t{overF}\t{capH}\n");
                }
            if (q == quarters - 1)
            {
                if (For(sim.Diplomacy) is { } acc) foreach (var s in acc.Ev) W.Write(s + "\n");
                W.Dispose();
            }
        }
    }
}
'''
edit('PriceOfBread.SimCore/Discovery.cs', [], TAIL)
edit('PriceOfBread.SimCore/Territory.cs', [
    ("    internal static (double Rail, double Ship) TransferRegion(World from, World? to, Region r, Rehoming how, bool sameCore = false)\n    {\n",
     "    internal static (double Rail, double Ship) TransferRegion(World from, World? to, Region r, Rehoming how, bool sameCore = false)\n    {\n        var tierDiagBefore = r.Tier;\n"),
    ("        return (rail, ship);   // 建国",
     "        if (TierDiag.On) TierDiag.Event(from.Diplomacy, from.Year, how.ToString(), r, tierDiagBefore, from, to);\n        return (rail, ship);   // 建国"),
])
edit('PriceOfBread.SimCore/Diplomacy.cs', [
    ("                Territory.Rehome(r, Territory.Rehoming.Sovereign);\n",
     "                var tierDiagB = r.Tier;\n                Territory.Rehome(r, Territory.Rehoming.Sovereign);\n                if (TierDiag.On) TierDiag.Event(this, _year, \"Sovereign\", r, tierDiagB, parent.Nation, cw);\n"),
])
edit('PriceOfBread.SimCore/Colonial.cs', [
    ("                    cr.Tier = RegionTier.Integrated;\n",
     "                    cr.Tier = RegionTier.Integrated;\n                    if (TierDiag.On) TierDiag.Event(w.Diplomacy, w.Year, \"Promote\", cr, RegionTier.Colonial, w, w);\n"),
])
edit('PriceOfBread.SimCore/SelfPlay.cs', [
    ('        var navyM = NavyDiag.On ? new NavyDiag.Match(seed) : null;\n',
     '        var navyM = NavyDiag.On ? new NavyDiag.Match(seed) : null;\n        var tierM = TierDiag.On ? new TierDiag.Match(seed) : null;\n'),
    ('                navyM?.Quarter(sim, q, quarters);\n',
     '                navyM?.Quarter(sim, q, quarters);\n                tierM?.Quarter(sim, q, quarters);\n'),
])
print('ok', root)
