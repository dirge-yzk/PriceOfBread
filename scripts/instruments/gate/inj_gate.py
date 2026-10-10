"""[計測の写しだけ] (150) 世界のゲートの計器（読むだけ）。環境変数 POB_GATE_DIR の下に t<seed>.txt。未設定なら1命令も走らない。
inj_ghost（と inj_disc）を当てた木に当てる（SelfPlay の ghM の行の後ろに足す）。
  G 行＝年に1回（q%4==3）の国ごと：年・国（BaseName）・技術水準・旧の時計の値（蒸気 clamp((t-0.25)/0.5)・鉄道 clamp((t-0.35)/0.5)・港 t>=0.2）
        ・発見の2値（蒸気機関・機関車・冷蔵）・工場の蒸気動力の割合（SteamFill を産出×参照価格で重み付け・蒸気の判定の対象＝労働を買う工場〔石炭と電力を除く〕）
        ・蒸気を焚く工場の産出の割合（SteamFill>0.05）・鉄道の容量・船の容量・港の容量の和・冷蔵の旧のランプ（州の平均）・工業化度の州の平均・人口
⚠旧の時計の値は数を直書き（係数を消した木でも同じ計器が走る）。使い方：python inj_gate.py <木の根>"""
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

/// <summary>[計測の写しだけ] (150) 世界のゲートの計器（読むだけ）。</summary>
public static class GateDiag
{
    public static readonly string? Dir = System.Environment.GetEnvironmentVariable("POB_GATE_DIR");
    public static readonly bool On = Dir is { Length: > 0 };
    static string F(double v) => double.IsNaN(v) ? "nan" : v.ToString("G5", System.Globalization.CultureInfo.InvariantCulture);
    static int Row(string name) => System.Array.FindIndex(Catalog.Discoveries, x => x.Name == name);
    public sealed class Match
    {
        readonly System.IO.StreamWriter W;
        readonly int rS = Row("蒸気機関"), rL = Row("機関車"), rR = Row("冷蔵");
        public Match(ulong seed)
        {
            W = new System.IO.StreamWriter(System.IO.Path.Combine(Dir!, $"t{seed}.txt"), false, new System.Text.UTF8Encoding(false), 1 << 16);
            W.Write("#G\tq\tyr\tnation\ttech\toldSteam\toldRail\toldPort\tkSteam\tkLoco\tkReefer\tsteamFill\tsteamOn\trailCap\tshipCap\tportCap\toldReefer\tind\tpop\n");
        }
        public void Quarter(MultiNationSim sim, int q, int quarters)
        {
            if (q % 4 == 3)
            {
                var ns = sim.Nations; var rp = Catalog.RefPrices;
                for (int i = 0; i < ns.Count; i++)
                {
                    var w = ns[i].World;
                    string bn = w.SelfPower is { } sp0 && sp0.BaseName.Length > 0 ? sp0.BaseName : (w.SelfPower?.Name ?? w.Region.Name);
                    double t = w.TechLevel;
                    double oS = System.Math.Clamp((t - 0.25) / 0.5, 0, 1), oR = System.Math.Clamp((t - 0.35) / 0.5, 0, 1);
                    double sfW = 0, sfV = 0, onV = 0, port = 0, refr = 0, ind = 0, pop = 0; int nreg = 0;
                    foreach (var rg in w.Regions)
                    {
                        if (rg.State != RegionState.Active) continue;
                        nreg++;
                        port += rg.PortCapacity;
                        refr += System.Math.Clamp((rg.TotalIndustrialization - 1.0) / 1.5, 0, 1);
                        ind += rg.TotalIndustrialization;
                        foreach (var p in rg.Pops) pop += p.Size;
                        foreach (var s in rg.Producers)
                        {
                            if (!s.BuysLabor || s.GoodId == Catalog.CoalId || s.GoodId == Catalog.ElectricityId) continue;
                            double v = s.Output * (s.GoodId >= 0 && s.GoodId < rp.Length ? rp[s.GoodId] : 0);
                            if (!(v > 0)) continue;
                            sfW += v; sfV += v * s.SteamFill; if (s.SteamFill > 0.05) onV += v;
                        }
                    }
                    var k = w.Discovered;
                    W.Write($"G\t{q + 1}\t{w.Year}\t{bn}\t{F(t)}\t{F(oS)}\t{F(oR)}\t{(t >= 0.2 ? 1 : 0)}\t{(Discovery.Known(k, rS) ? 1 : 0)}\t{(Discovery.Known(k, rL) ? 1 : 0)}\t{(Discovery.Known(k, rR) ? 1 : 0)}\t{F(sfW > 0 ? sfV / sfW : 0)}\t{F(sfW > 0 ? onV / sfW : 0)}\t{F(w.RailCapacity)}\t{F(w.ShipCapacity)}\t{F(port)}\t{F(nreg > 0 ? refr / nreg : 0)}\t{F(nreg > 0 ? ind / nreg : 0)}\t{F(pop)}\n");
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
     '        var ghM = GhostDiag.On ? new GhostDiag.Match(seed) : null; GhostDiag.Cur = ghM;\n        var gateM = GateDiag.On ? new GateDiag.Match(seed) : null;\n'),
    ('                ghM?.Quarter(sim, q, quarters);\n',
     '                ghM?.Quarter(sim, q, quarters);\n                gateM?.Quarter(sim, q, quarters);\n'),
])
print('ok', root)
