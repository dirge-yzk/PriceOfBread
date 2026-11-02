# (793) 読み手 r793：アヘンの買い手の内訳と依存の更新の分子・分母を州×月で書く読むだけのプローブ（使い捨ての木だけ）。
# 力学の値を書かない・乱数を引かない。INS=1（Instruments.On）のときだけ動く。出力 <POB_INSTRUMENTS>/r793_s<seed>.tsv
import sys, os
root = sys.argv[1]
def sub(path, old, new, cnt=1):
    p = os.path.join(root, path)
    s = open(p, encoding='utf-8-sig').read()
    n = s.count(old)
    assert n == cnt, (path, old[:60], n)
    s = s.replace(old, new)
    open(p, 'w', encoding='utf-8').write(s)
E = 'PriceOfBread.SimCore/EconomyStep.cs'
sub(E, "                    demand = Math.Min(desired, affordable);\n",
       "                    demand = Math.Min(desired, affordable);\n"
       "                    if (insOn && habit.Forms) Instruments.R793Pop(region, coef, p, marketNeed, desired, affordable, demand, buyerPrice, lastSupply[pIdx]);\n")
sub(E, "                if (insOn && !world!.WarmupFrozen) Instruments.HabitCapStep(region",
       "                if (insOn) Instruments.R793Row(region, world!, good.RefPrice, popDemand, inputNeed[gi], investDemand[gi], militaryDemand[gi], rebelDemand[gi], inputTrickle[gi], effDemand[gi], coreSold, effSold, available, produced[gi], m.Stock, totalPop, region.OpiumAddiction, perCapita, usePrice, buyerPrice, priceFactor, ration[gi]);\n"
       "                if (insOn && !world!.WarmupFrozen) Instruments.HabitCapStep(region")
sub('PriceOfBread.SimCore/Instruments.cs', "        HabitbalTable(sim, seed, q);    // (787) アヘンの釣り合いの表\n",
    "        HabitbalTable(sim, seed, q);    // (787) アヘンの釣り合いの表\n        R793Table(sim, seed, q);\n")
code = r'''namespace PriceOfBread.SimCore;
public static partial class Instruments
{
    sealed class R793Acc
    {
        public double Size, Need, MarketNeed, Desired, Afford, Demand, Budget, Earned, Gate, BoundSize, SpendAff, NeedNoGate; public int Pops;
        public System.Text.StringBuilder Sb = new();
        public int M;
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Region, R793Acc> R793Accs = new();
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, HabitcapSink> R793Sinks = new();
    internal static void R793Pop(Region r, Coefficients c, Pop p, double marketNeed, double desired, double affordable, double demand, double buyerPrice, double lastSup)
    {
        var a = R793Accs.GetValue(r, _ => new R793Acc());
        double g = Math.Clamp(c.OpiumMiseryScale * (1.0 - lastSup), 0.0, 1.0);
        a.Pops++; a.Size += p.Size; a.Need += p.GoodNeed; a.MarketNeed += marketNeed; a.Desired += desired;
        a.Afford += Math.Min(affordable, 1e300); a.Demand += demand; a.Budget += p.RemainingBudget; a.Earned += p.LastEarnedPerMonth.Value;
        a.Gate += g * p.Size; if (demand < desired - 1e-12) a.BoundSize += p.Size;
    }
    internal static void R793Row(Region r, World w, double refPrice, double popDemand, double inputNeed, double invest, double mil, double rebel, double trickle, double eff,
        double coreSold, double effSold, double available, double produced, double stock, double totalPop, double addBefore, double pc, double usePrice, double buyerPrice, double priceFactor, double ration)
    {
        var a = R793Accs.GetValue(r, _ => new R793Acc());
        if (!w.WarmupFrozen && (addBefore > 0.1 || pc > 0.05))
        {
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            string F(double v) => v.ToString("G6", inv);
            double sold = coreSold + effSold;
            double popSold = popDemand * ration;
            a.Sb.Append(string.Join('\t', w.Year.ToString(inv), a.M.ToString(inv), NameOf(w), r.Name, F(totalPop), F(addBefore), F(pc), F(sold), F(popSold), F(popDemand), F(inputNeed), F(invest), F(mil + rebel), F(trickle), F(eff),
                F(coreSold), F(effSold), F(available), F(produced), F(stock), F(usePrice), F(buyerPrice), F(refPrice), F(priceFactor), F(ration),
                a.Pops.ToString(inv), F(a.Size), F(a.Need), F(a.MarketNeed), F(a.Desired), F(a.Demand), F(a.Budget), F(a.Earned), F(a.Size > 0 ? a.Gate / a.Size : double.NaN), F(a.Size > 0 ? a.BoundSize / a.Size : double.NaN))).Append('\n');
        }
        a.M++;
        a.Pops = 0; a.Size = a.Need = a.MarketNeed = a.Desired = a.Afford = a.Demand = a.Budget = a.Earned = a.Gate = a.BoundSize = 0;
    }
    const string R793Header = "year\tm\tnation\tregion\ttotalPop\tstockBefore\tpc\tsold\tpopSold\tpopDemand\tinputNeed\tinvest\tmilRebel\ttrickle\teff\tcoreSold\teffSold\tavailable\tproduced\tmktStock\tusePrice\tbuyerPrice\trefPrice\tpriceFactor\tration\tpops\tpopSize\tneed\tmarketNeed\tdesired\tdemand\tbudget\tearned\tgate\tboundShare";
    static void R793Table(MultiNationSim sim, ulong seed, int q)
    {
        var sk = R793Sinks.GetValue(sim, _ => new HabitcapSink());
        var sb = new System.Text.StringBuilder();
        if (!sk.Started) sb.Append("q\t").Append(R793Header).Append('\n');
        string qs = q.ToString(System.Globalization.CultureInfo.InvariantCulture);
        for (int i = 0; i < sim.Nations.Count; i++)
            foreach (var r in sim.Nations[i].World.Regions)
            {
                if (!R793Accs.TryGetValue(r, out var a)) continue;
                if (a.Sb.Length > 0)
                    foreach (var line in a.Sb.ToString().Split('\n')) if (line.Length > 0) sb.Append(qs).Append('\t').Append(line).Append('\n');
                a.Sb.Clear(); a.M = 0;
            }
        System.IO.Directory.CreateDirectory(Dir);
        string path = System.IO.Path.Combine(Dir, $"r793_s{seed}.tsv");
        var enc = new System.Text.UTF8Encoding(false);
        if (!sk.Started) { System.IO.File.WriteAllText(path, sb.ToString(), enc); sk.Started = true; }
        else System.IO.File.AppendAllText(path, sb.ToString(), enc);
    }
}
'''
open(os.path.join(root, 'PriceOfBread.SimCore/InstrumentsR793.cs'), 'w', encoding='utf-8').write(code)
print('ok')
