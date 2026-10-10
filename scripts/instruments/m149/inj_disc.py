"""[計測の写しだけ] (149) 合流の基線の計器（読むだけ）：発見の年と国・装備の充足と即応度と陸海の戦力。
環境変数 POB_DISC_DIR の下に d<seed>.txt。未設定なら1命令も走らない。inj_ghost を当てた木に当てる（SelfPlay の ghM の行の後ろに足す）。
  D 行＝国がその四半期に初めて既知にした発見の行（四半期0＝開幕の既知を全て）：q・年・国・行・分野
  M 行＝年に1回（q%4==3）の国ごと：年・国・即応度・装備の充足（首都州）・陸軍力・海軍力・兵士・人口
使い方：python inj_disc.py <木の根>"""
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

/// <summary>[計測の写しだけ] (149) 発見の年と装備の充足の計器（読むだけ）。</summary>
public static class DiscDiag
{
    public static readonly string? Dir = System.Environment.GetEnvironmentVariable("POB_DISC_DIR");
    public static readonly bool On = Dir is { Length: > 0 };
    static string F(double v) => double.IsNaN(v) ? "nan" : v.ToString("G5", System.Globalization.CultureInfo.InvariantCulture);
    public sealed class Match
    {
        readonly System.IO.StreamWriter W;
        readonly System.Collections.Generic.Dictionary<World, bool[]> Seen = new();
        public Match(ulong seed)
        {
            W = new System.IO.StreamWriter(System.IO.Path.Combine(Dir!, $"d{seed}.txt"), false, new System.Text.UTF8Encoding(false), 1 << 16);
            W.Write("#D\tq\tyr\tnation\trow\tfield\n");
            W.Write("#M\tq\tyr\tnation\treadiness\tequipfill\tland\tnaval\tsoldiers\tpop\n");
        }
        public void Quarter(MultiNationSim sim, int q, int quarters)
        {
            var ns = sim.Nations; int yr = ns[0].World.Year;
            for (int i = 0; i < ns.Count; i++)
            {
                var w = ns[i].World;
                string nn = w.SelfPower?.Name ?? w.Region.Name;
                string bn = w.SelfPower is { } sp0 && sp0.BaseName.Length > 0 ? sp0.BaseName : nn;
                var d = w.Discovered;
                if (!Seen.TryGetValue(w, out var seen)) { seen = new bool[d.Length]; Seen[w] = seen; }
                for (int r = 0; r < d.Length && r < Catalog.Discoveries.Length; r++)
                    if (d[r] && !seen[r]) { seen[r] = true; W.Write($"D\t{q + 1}\t{yr}\t{nn}\t{Catalog.Discoveries[r].Name}\t{Catalog.Discoveries[r].Field}\n"); }
                if (q % 4 == 3)
                {
                    double sold = 0, pop = 0;
                    foreach (var rg in w.Regions) { if (rg.State != RegionState.Active) continue; foreach (var p in rg.Pops) { pop += p.Size; if (p.IsSoldier) sold += p.Size; } }
                    double land = w.SelfPower?.LandPower ?? double.NaN, naval = w.SelfPower?.NavalPower ?? double.NaN;
                    W.Write($"M\t{q + 1}\t{yr}\t{bn}\t{F(w.MilitaryReadiness)}\t{F(w.Region.MilitaryEquipFill)}\t{F(land)}\t{F(naval)}\t{F(sold)}\t{F(pop)}\n");
                }
            }
            if (q == quarters - 1) W.Dispose();
        }
    }
}
'''
edit('PriceOfBread.SimCore/Discovery.cs', [], TAIL)
edit('PriceOfBread.SimCore/SelfPlay.cs', [
    ('        var ghM = GhostDiag.On ? new GhostDiag.Match(seed) : null; GhostDiag.Cur = ghM;\n',
     '        var ghM = GhostDiag.On ? new GhostDiag.Match(seed) : null; GhostDiag.Cur = ghM;\n        var discM = DiscDiag.On ? new DiscDiag.Match(seed) : null;\n'),
    ('                ghM?.Quarter(sim, q, quarters);\n',
     '                ghM?.Quarter(sim, q, quarters);\n                discM?.Quarter(sim, q, quarters);\n'),
])
print('ok', root)
