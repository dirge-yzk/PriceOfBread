"""[計測の写しだけ] (178) 線D 前半の計器（読むだけ）＝賃金（労働の値段）と動力（今の労働力の財）の値段の作られ方。環境変数 POB_POW_DIR の下に t<seed>.txt。未設定なら1命令も走らない。
  P 行＝年に1回（q%4==3）の国ごと：年・国（BaseName）・人口・物価水準 PL（NominalOf(1)）・自己費用の物価・為替（金建て÷自国）・工業化度（州の平均）・
        3つの労働の財（労働力 L・農業労働力 F・高度労働力 A）ごとに：需要で重みを付けた名目の値段・働き手の人数で重みを付けた名目の値段・帯の位置（値段÷(参照×州の物価)＝需要の重み）・
        留保賃金（需要の重み・名目）・需要の和・産出の和・下の端に居る州の需要の割合（帯の位置<0.45）・上の端（>2）・州の値段の最小と最大（需要>0 の州）
        ・主食の値段（Staple の財の需要で重みを付けた名目÷PL）・麦と米（名目÷PL）・石炭・機械・蒸気機関の財・馬・電力（名目÷PL・需要の重み）
        ・労働力の財のレシピの産出（人力・畜力・蒸気機関・電動・水車）・工場の蒸気動力の割合（gate と同じ）・
        工場労働者（人数・月の所得の和・失業率の人数の重み）・農民（同）・人力の労働力の生産者の能力÷工場労働者の人数（1人あたりの動力）
        ・発見の値段（DiscoveryPrice の労働力）・蒸気の動力の費用（Evaluate の ratioOut）・蒸気機関の必要性・2値
使い方：python inj_pow178.py <木の根>"""
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

/// <summary>[計測の写しだけ] (178) 賃金と動力の値段の計器（読むだけ）。</summary>
public static class Pow178Diag
{
    public static readonly string? Dir = System.Environment.GetEnvironmentVariable("POB_POW_DIR");
    public static readonly bool On = Dir is { Length: > 0 };
    static string F(double v) => double.IsNaN(v) ? "nan" : v.ToString("G5", System.Globalization.CultureInfo.InvariantCulture);
    public sealed class Match
    {
        readonly System.IO.StreamWriter W;
        public Match(ulong seed)
        {
            W = new System.IO.StreamWriter(System.IO.Path.Combine(Dir!, $"t{seed}.txt"), false, new System.Text.UTF8Encoding(false), 1 << 16);
            var h = new System.Text.StringBuilder("#P\tq\tyr\tnation\tpop\tPL\tcostPL\tfx\tind");
            foreach (var g in new[] { "L", "F", "A" }) h.Append($"\t{g}pxD\t{g}pxW\t{g}band\t{g}rw\t{g}dem\t{g}prod\t{g}lowSh\t{g}hiSh\t{g}min\t{g}max");
            h.Append("\tfood\twheat\trice\tcoal\tmach\tengine\thorse\telec\tlHuman\tlAnimal\tlSteam\tlElec\tlWater\tsteamFill\twkN\twkEarn\twkU\tpsN\tpsEarn\tpsU\tcapPerWk\tdpLabor\tsteamCost\tsteamNeed\tkSteam\n");
            W.Write(h.ToString());
        }
        public void Quarter(MultiNationSim sim, int q, int quarters)
        {
            if (q % 4 == 3)
            {
                var ns = sim.Nations; var rp = Catalog.RefPrices;
                int ng = Catalog.Goods.Length;
                int[] lab = { Catalog.LaborId, Catalog.FarmLaborId, Catalog.AdvLaborId };
                int[] oth = { Catalog.WheatId, Catalog.RiceId, Catalog.CoalId, Catalog.MachineryId, Catalog.SteamEngineId, Catalog.HorseId, Catalog.ElectricityId };
                for (int i = 0; i < ns.Count; i++)
                {
                    var w = ns[i].World; var c = w.Coef;
                    string bn = w.SelfPower is { } sp0 && sp0.BaseName.Length > 0 ? sp0.BaseName : (w.SelfPower?.Name ?? w.Region.Name);
                    double pl = w.NominalOf(1.0);
                    double pop = 0, ind = 0; int nreg = 0;
                    var pxD = new double[3]; var pxWn = new double[3]; var pxWd = new double[3]; var band = new double[3]; var rwv = new double[3]; var dem = new double[3]; var prod = new double[3];
                    var low = new double[3]; var hi = new double[3]; var mn = new double[] { double.MaxValue, double.MaxValue, double.MaxValue }; var mx = new double[3];
                    double foodN = 0, foodD = 0; var oN = new double[oth.Length]; var oD = new double[oth.Length];
                    double lH = 0, lA = 0, lS = 0, lE = 0, lW = 0, sfW = 0, sfV = 0;
                    double wkN = 0, wkE = 0, wkU = 0, psN = 0, psE = 0, psU = 0, capH = 0;
                    foreach (var rg in w.Regions)
                    {
                        if (rg.State != RegionState.Active) continue;
                        nreg++; ind += rg.TotalIndustrialization;
                        double wkReg = 0;
                        foreach (var p in rg.Pops)
                        {
                            pop += p.Size;
                            if (p.ClassId == PopClass.Worker) { wkN += p.Size; wkE += p.Earned; wkU += p.Size * p.UnemploymentRate; wkReg += p.Size; }
                            else if (p.ClassId == PopClass.Peasant) { psN += p.Size; psE += p.Earned; psU += p.Size * p.UnemploymentRate; }
                        }
                        for (int k = 0; k < 3; k++)
                        {
                            int g = lab[k]; if (g >= rg.Markets.Length) continue;
                            var m = rg.Markets[g];
                            double d = m.LastDemand;
                            double wrp = rp[g] * rg.PriceLevel;
                            double pos = wrp > 0 ? m.Price / wrp : double.NaN;
                            pxD[k] += m.Price * d; band[k] += pos * d; rwv[k] += m.ReservationWage * d; dem[k] += d; prod[k] += m.LastProduced;
                            if (pos < 0.45) low[k] += d; if (pos > 2.0) hi[k] += d;
                            if (d > 1e-9) { if (m.Price < mn[k]) mn[k] = m.Price; if (m.Price > mx[k]) mx[k] = m.Price; }
                            double wt = k == 1 ? 0 : wkReg;   // 働き手の重み（農業労働力は農民で下）
                            if (k == 1) foreach (var p in rg.Pops) if (p.ClassId == PopClass.Peasant) wt += p.Size;
                            pxWn[k] += m.Price * wt; pxWd[k] += wt;
                        }
                        for (int g = 0; g < System.Math.Min(ng, rg.Markets.Length); g++)
                            if (Catalog.Goods[g].Tier == GoodTier.Staple) { foodN += rg.Markets[g].Price * rg.Markets[g].LastDemand; foodD += rg.Markets[g].LastDemand; }
                        for (int k = 0; k < oth.Length; k++) { int g = oth[k]; if (g >= rg.Markets.Length) continue; oN[k] += rg.Markets[g].Price * rg.Markets[g].LastDemand; oD[k] += rg.Markets[g].LastDemand; }
                        foreach (var s in rg.Producers)
                        {
                            if (s.GoodId == Catalog.LaborId)
                            {
                                switch (s.RecipeName)
                                {
                                    case "畜力": lA += s.Output; break;
                                    case "蒸気機関": lS += s.Output; break;
                                    case "電動": lE += s.Output; break;
                                    case "水車": lW += s.Output; break;
                                    default: lH += s.Output; capH += s.Capacity; break;
                                }
                                continue;
                            }
                            if (!s.BuysLabor || s.GoodId == Catalog.CoalId || s.GoodId == Catalog.ElectricityId) continue;
                            double v = s.Output * (s.GoodId >= 0 && s.GoodId < rp.Length ? rp[s.GoodId] : 0);
                            if (!(v > 0)) continue;
                            sfW += v; sfV += v * s.SteamFill;
                        }
                    }
                    var sb = new System.Text.StringBuilder();
                    sb.Append($"P\t{q + 1}\t{w.Year}\t{bn}\t{F(pop)}\t{F(pl)}\t{F(w.CostPriceLevel)}\t{F(w.FxRate)}\t{F(nreg > 0 ? ind / nreg : 0)}");
                    for (int k = 0; k < 3; k++)
                        sb.Append($"\t{F(dem[k] > 0 ? pxD[k] / dem[k] : double.NaN)}\t{F(pxWd[k] > 0 ? pxWn[k] / pxWd[k] : double.NaN)}\t{F(dem[k] > 0 ? band[k] / dem[k] : double.NaN)}\t{F(dem[k] > 0 ? rwv[k] / dem[k] : double.NaN)}\t{F(dem[k])}\t{F(prod[k])}\t{F(dem[k] > 0 ? low[k] / dem[k] : double.NaN)}\t{F(dem[k] > 0 ? hi[k] / dem[k] : double.NaN)}\t{F(mn[k] < double.MaxValue ? mn[k] : double.NaN)}\t{F(mx[k])}");
                    sb.Append($"\t{F(foodD > 0 && pl > 0 ? foodN / foodD / pl : double.NaN)}");
                    for (int k = 0; k < oth.Length; k++) sb.Append($"\t{F(oD[k] > 0 && pl > 0 ? oN[k] / oD[k] / pl : double.NaN)}");
                    sb.Append($"\t{F(lH)}\t{F(lA)}\t{F(lS)}\t{F(lE)}\t{F(lW)}\t{F(sfW > 0 ? sfV / sfW : 0)}\t{F(wkN)}\t{F(wkE)}\t{F(wkN > 0 ? wkU / wkN : 0)}\t{F(psN)}\t{F(psE)}\t{F(psN > 0 ? psU / psN : 0)}\t{F(wkN > 0 ? capH / wkN : 0)}");
                    int nr = Catalog.Discoveries.Length, rS = Discovery.SteamRow;
                    var ro = new (double Replaced, double Cost)[nr];
                    var t = Discovery.Evaluate(w, w.LastDiscoveryView, c, includeKnown: true, ratioOut: ro);
                    double dpl = w.DiscoveryPrice is { } dp && Catalog.LaborId < dp.Length ? dp[Catalog.LaborId] : double.NaN;
                    sb.Append($"\t{F(dpl)}\t{F(rS >= 0 ? ro[rS].Cost : double.NaN)}\t{F(rS >= 0 ? t[rS].Need : double.NaN)}\t{(Discovery.Known(w.Discovered, rS) ? 1 : 0)}\n");
                    W.Write(sb.ToString());
                }
            }
            if (q == quarters - 1) W.Dispose();
        }
    }
}
'''
edit('PriceOfBread.SimCore/Discovery.cs', [], TAIL)
edit('PriceOfBread.SimCore/SelfPlay.cs', [
    ('        sim.StartWarmup();\n', '        sim.StartWarmup();\n        var pow178M = Pow178Diag.On ? new Pow178Diag.Match(seed) : null;\n'),
    ('sim.StepQuarter();\n', 'sim.StepQuarter();\n                pow178M?.Quarter(sim, q, quarters);\n'),
])
print('ok', root)
