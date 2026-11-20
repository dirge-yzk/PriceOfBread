namespace PriceOfBread.SimCore;

// 力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない。〔履歴 InstrumentsWp.cs#1〕
public static partial class Instruments
{
    // 財と旧のレシピの名前（番人が固定する）。
    static readonly (int Good, string Old)[] WpGoods = [(Catalog.RifleId, "マスケット"), (Catalog.ArtilleryId, "鋳鉄砲"), (Catalog.SteamEngineId, "鋳鉄の機関")];

    internal static string WpHeaderText()
        => "quarter\tyear\tnation\tgood\tknown\toldGateOpen\toldOutputPerMonth\tnewOutputPerMonth\tnewShare\toldCostPerUnit\tnewCostPerUnit\tgoodPriceMean\tpriceLevel\tcapitalEquipFill\tcapitalReadiness"
         + "\toldBuilt\tnewBuilt\toldStateTarget\tnewStateTarget\toldScale\tnewScale\tregionsBoth\tregionsOldCheaper\toldOutWhereOldCheaper\tnewOutWhereOldCheaper\toldOutWhereNewCheaper\tnewOutWhereNewCheaper";

    static void WpFold(Sink sk, ulong seed, World w, int q)
    {
        if (!Tg.Wp) return;   // (1256) 書く表がどれも選ばれていなければ積算ごと飛ぶ
        if (!(w.TotalPopulation > 0)) return;
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var cap = w.Region;
        foreach (var (g, oldName) in WpGoods)
        {
            int open = Catalog.OpenedByRowOf(g);
            int gateOpen = 0; double oldOut = 0, newOut = 0, px = 0; int pxN = 0;
            double oldBuilt = 0, newBuilt = 0, oldStateTarget = 0, newStateTarget = 0, oldScale = 0, newScale = 0;
            int both = 0, oldCheaper = 0; double oOldC = 0, nOldC = 0, oNewC = 0, nNewC = 0;
            foreach (var r in w.Regions)
            {
                Producer? firstOld = null, firstNew = null;
                double rOldOut = 0, rNewOut = 0;
                foreach (var pr in r.Producers)
                {
                    if (pr.GoodId != g) continue;
                    if (pr.RecipeName == oldName)
                    {
                        oldOut += pr.Output; rOldOut += pr.Output; if (Discovery.Gate(r, pr) > 0) gateOpen++;
                        oldBuilt += pr.BuiltScale; oldStateTarget += pr.StateTarget; oldScale += pr.Scale;
                        if (firstOld is null && Discovery.Gate(r, pr) > 0) firstOld = pr;
                    }
                    else
                    {
                        newOut += pr.Output; rNewOut += pr.Output;
                        newBuilt += pr.BuiltScale; newStateTarget += pr.StateTarget; newScale += pr.Scale;
                        if (firstNew is null && Discovery.Gate(r, pr) > 0) firstNew = pr;
                    }
                }
                if (g < r.Markets.Length) { double p = r.Markets[g].Price; if (p > 0) { px += p; pxN++; } }
                if (firstOld is not null && firstNew is not null)
                {
                    double oc = EconomyStep.MineralSelfCost(firstOld, r, w.Coef, w) + EconomyStep.InputCostPerUnit(firstOld, r, r, w.Coef);
                    double nc = EconomyStep.MineralSelfCost(firstNew, r, w.Coef, w) + EconomyStep.InputCostPerUnit(firstNew, r, r, w.Coef);
                    if (oc > 0 && nc > 0)
                    {
                        both++;
                        if (oc < nc) { oldCheaper++; oOldC += rOldOut; nOldC += rNewOut; }
                        else { oNewC += rOldOut; nNewC += rNewOut; }
                    }
                }
            }
            double oldCost = double.NaN, newCost = double.NaN;
            foreach (var pr in cap.Producers)
            {
                if (pr.GoodId != g) continue;
                double mc = EconomyStep.MineralSelfCost(pr, cap, w.Coef, w) + EconomyStep.InputCostPerUnit(pr, cap, cap, w.Coef);
                if (pr.RecipeName == oldName) { if (double.IsNaN(oldCost)) oldCost = mc; }
                else if (double.IsNaN(newCost)) newCost = mc;
            }
            double tot = oldOut + newOut;
            sk.Wp ??= Open("wp", seed);
            if (!sk.WpHeader) { sk.Wp.WriteLine(WpHeaderText()); sk.WpHeader = true; }
            sk.Wp.WriteLine(string.Join('\t', I(q), I(w.Year), NameOf(w), Catalog.Goods[g].Name, open >= 0 && Discovery.Known(w.Discovered, open) ? "1" : "0", I(gateOpen), F(oldOut), F(newOut),
                F(tot > 0 ? newOut / tot : double.NaN), F(oldCost), F(newCost), F(pxN > 0 ? px / pxN : double.NaN), F(w.PriceLevel), F(cap.MilitaryEquipFill), F(w.MilitaryReadiness),
                F(oldBuilt), F(newBuilt), F(oldStateTarget), F(newStateTarget), F(oldScale), F(newScale), I(both), I(oldCheaper), F(oOldC), F(nOldC), F(oNewC), F(nNewC)));
        }
    }
}
