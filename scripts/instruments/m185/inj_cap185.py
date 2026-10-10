"""[計測の写しだけ] (185) 線B 前半の計器（読むだけ）＝工場の設備・労働の器と需要・賃金・値段の暴れ・工業の割合。環境変数 POB_CAP_DIR の下に t<seed>.txt。未設定なら1命令も走らない。
両方の木（腕A＝586b80e・段2）で同じ欄を出す＝両方にあるフィールドだけを読む（Producer.Scale の意味は木で違う＝段2 は職場の数・腕A は操業規模の重み）。
  C 行＝年に1回（q%4==3）の国ごと：
    年・国・人口・物価水準 PL・為替 fx・工場労働者の人数・その月の所得の和・失業率（人数の重み）・
    労働力の財：人力の生産者の能力の和（器）・全てのレシピの能力の和・需要の和・産出の和・需要で重みを付けた名目の値段・
    主食の値段（Staple の需要の重み・名目）・銀の値段（需要の重み・名目）・
    工場（労働を市場で買う RGO でない生産者）：数・Σ Scale・Σ BuiltScale・Σ能力・Σ産出・Σ産出×参照価格・Σ能力×参照価格・設備の無い（Scale≦1e-9）工場の産出の和（×参照価格）・
    RGO と職人の産出×参照価格（工業の割合の分母）・値段の暴れ（工場の財の州の値段の四半期の対数の変化の絶対値・前の四半期の産出×参照価格の重み・年の4四半期の平均）
使い方：python inj_cap185.py <木の根>"""
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

/// <summary>[計測の写しだけ] (185) 工場の設備と労働の器の計器（読むだけ）。</summary>
public static class Cap185Diag
{
    public static readonly string? Dir = System.Environment.GetEnvironmentVariable("POB_CAP_DIR");
    public static readonly bool On = Dir is { Length: > 0 };
    static string F(double v) => double.IsNaN(v) ? "nan" : v.ToString("G6", System.Globalization.CultureInfo.InvariantCulture);
    public sealed class Match
    {
        readonly System.IO.StreamWriter W;
        // 値段の暴れ：州×財の前の四半期の値段と重み（計器だけの記憶）
        readonly System.Collections.Generic.Dictionary<Region, double[]> _prev = new();
        readonly System.Collections.Generic.Dictionary<Region, double[]> _prevW = new();
        readonly double[] _volN = new double[512], _volD = new double[512];
        public Match(ulong seed)
        {
            W = new System.IO.StreamWriter(System.IO.Path.Combine(Dir!, $"t{seed}.txt"), false, new System.Text.UTF8Encoding(false), 1 << 16);
            W.Write("#C\tq\tyr\tnation\tpop\tPL\tfx\twkN\twkEarn\twkU\tlabCapH\tlabCapAll\tlabDem\tlabOut\tlabPx\tfoodPx\tsilverPx\tnFac\tsumK\tsumB\tsumCap\tsumOut\tfacOutV\tfacCapV\tnoKOutV\totherOutV\tpxVol\n");
        }
        public void Quarter(MultiNationSim sim, int q, int quarters)
        {
            var ns = sim.Nations; var rp = Catalog.RefPrices;
            int ng = Catalog.Goods.Length;
            for (int i = 0; i < ns.Count && i < _volN.Length; i++)
            {
                var w = ns[i].World;
                foreach (var rg in w.Regions)
                {
                    if (rg.State != RegionState.Active) continue;
                    if (!_prev.TryGetValue(rg, out var pv)) { pv = new double[ng]; _prev[rg] = pv; _prevW[rg] = new double[ng]; }
                    var pw = _prevW[rg];
                    var facOut = new double[ng];
                    foreach (var s in rg.Producers) if (s.BuysLabor && !s.IsRgo && s.GoodId < ng) facOut[s.GoodId] += s.Output;
                    for (int g = 0; g < ng && g < rg.Markets.Length; g++)
                    {
                        double p = rg.Markets[g].Price;
                        if (pv[g] > 1e-12 && p > 1e-12 && pw[g] > 0) { _volN[i] += System.Math.Abs(System.Math.Log(p / pv[g])) * pw[g]; _volD[i] += pw[g]; }
                        pv[g] = p; pw[g] = facOut[g] * rp[g];
                    }
                }
            }
            if (q % 4 == 3)
            {
                for (int i = 0; i < ns.Count; i++)
                {
                    var w = ns[i].World;
                    string bn = w.SelfPower is { } sp0 && sp0.BaseName.Length > 0 ? sp0.BaseName : (w.SelfPower?.Name ?? w.Region.Name);
                    double pl = w.NominalOf(1.0);
                    double pop = 0, wkN = 0, wkE = 0, wkU = 0, capH = 0, capA = 0, ldem = 0, lout = 0, lpx = 0, fN = 0, fD = 0, sN = 0, sD = 0;
                    double nFac = 0, sK = 0, sB = 0, sC = 0, sO = 0, fOV = 0, fCV = 0, noK = 0, oOV = 0;
                    foreach (var rg in w.Regions)
                    {
                        if (rg.State != RegionState.Active) continue;
                        foreach (var p in rg.Pops)
                        {
                            pop += p.Size;
                            if (p.ClassId == PopClass.Worker) { wkN += p.Size; wkE += p.Earned; wkU += p.Size * p.UnemploymentRate; }
                        }
                        if (Catalog.LaborId < rg.Markets.Length) { var m = rg.Markets[Catalog.LaborId]; ldem += m.LastDemand; lout += m.LastProduced; lpx += m.Price * m.LastDemand; }
                        for (int g = 0; g < ng && g < rg.Markets.Length; g++)
                            if (Catalog.Goods[g].Tier == GoodTier.Staple) { fN += rg.Markets[g].Price * rg.Markets[g].LastDemand; fD += rg.Markets[g].LastDemand; }
                        if (Catalog.SilverId < rg.Markets.Length) { sN += rg.Markets[Catalog.SilverId].Price * rg.Markets[Catalog.SilverId].LastDemand; sD += rg.Markets[Catalog.SilverId].LastDemand; }
                        foreach (var s in rg.Producers)
                        {
                            if (s.GoodId == Catalog.LaborId) { capA += s.Capacity; if (s.RecipeName.Length == 0) capH += s.Capacity; continue; }
                            if (s.GoodId < 0 || s.GoodId >= ng || Catalog.Goods[s.GoodId].Tier == GoodTier.Service) continue;
                            double r = rp[s.GoodId];
                            if (s.BuysLabor && !s.IsRgo)
                            {
                                nFac++; sK += s.Scale; sB += s.BuiltScale; sC += s.Capacity; sO += s.Output; fOV += s.Output * r; fCV += s.Capacity * r;
                                if (!(s.Scale > 1e-9)) noK += s.Output * r;
                            }
                            else oOV += s.Output * r;
                        }
                    }
                    double vol = i < _volN.Length && _volD[i] > 0 ? _volN[i] / _volD[i] : double.NaN;
                    if (i < _volN.Length) { _volN[i] = 0; _volD[i] = 0; }
                    W.Write($"C\t{q + 1}\t{w.Year}\t{bn}\t{F(pop)}\t{F(pl)}\t{F(w.FxRate)}\t{F(wkN)}\t{F(wkE)}\t{F(wkN > 0 ? wkU / wkN : 0)}\t{F(capH)}\t{F(capA)}\t{F(ldem)}\t{F(lout)}\t{F(ldem > 0 ? lpx / ldem : double.NaN)}\t{F(fD > 0 ? fN / fD : double.NaN)}\t{F(sD > 0 ? sN / sD : double.NaN)}\t{F(nFac)}\t{F(sK)}\t{F(sB)}\t{F(sC)}\t{F(sO)}\t{F(fOV)}\t{F(fCV)}\t{F(noK)}\t{F(oOV)}\t{F(vol)}\n");
                }
            }
            if (q == quarters - 1) W.Dispose();
        }
    }
}
'''
edit('PriceOfBread.SimCore/Discovery.cs', [], TAIL)
edit('PriceOfBread.SimCore/SelfPlay.cs', [
    ('        sim.StartWarmup();\n', '        sim.StartWarmup();\n        var cap185M = Cap185Diag.On ? new Cap185Diag.Match(seed) : null;\n'),
    ('sim.StepQuarter();\n', 'sim.StepQuarter();\n                cap185M?.Quarter(sim, q, quarters);\n'),
])
print('ok', root)
