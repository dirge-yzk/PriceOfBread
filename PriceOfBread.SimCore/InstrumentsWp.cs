namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (537) 旧と新の武器の並行の表 wp_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsChem.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない。INS=1 のときだけ出す
// 目的＝事前登録 measurements/wp_20261024_prereg.txt の [3] ①〜④（追記 3 で直した形）を読む：旧が退場しないか・新の割合の伸び方・強さ1単位あたりの費用で選ぶか・装備の充足が上がる向きか・国営の持分が旧に付いたままか。
// wp_s：**全ての国**×四半期×{小銃・火砲・蒸気機関}（名前の並び＝<see cref="WpGoods"/>）。人口が 0 の国は出さない（POB_INSTRUMENTS_NATIONS の国の絞りは読まない＝この表は 3 財×国×四半期だけで小さい）。
//   列：quarter／year／nation／good／known（転炉鋼＝新世代を開く発見を国が解禁済みなら 1）／oldGateOpen（活きた州の旧のレシピの生産者で門が正の数）／oldOutputPerMonth（その実産出 Output の和）／
//   newOutputPerMonth（同じ財の旧以外の生産者の実産出の和）／newShare（新÷(旧＋新)・分母が 0 なら nan）／oldCostPerUnit／newCostPerUnit（資本州の最初の生産者の 自己費用＋投入費・名目・無ければ nan）／
//   goodPriceMean（活きた州のその財の市場の値段の単純平均・名目）／priceLevel／capitalEquipFill（資本州の装備充足 0〜1）／capitalReadiness（国の即応度）／
//   （追記 3 で足した列）oldBuilt／newBuilt（活きた州の BuiltScale の和＝国営の持分）／oldStateTarget／newStateTarget（StateTarget の和＝国営の意志）／oldScale／newScale（Scale の和）／
//   regionsBoth（旧と新の両方が正の門で並ぶ州の数）／regionsOldCheaper（そのうち旧の費用が新より安い州）／oldOutWhereOldCheaper／newOutWhereOldCheaper／oldOutWhereNewCheaper／newOutWhereNewCheaper（その州の産出の和）
// ⚠限界（計器が自己申告する）：四半期の末の断面／州の単純平均（産出で重み付けしない）／費用は資本州の 1 点と、並ぶ州ごとの 1 点（州の中の最初の旧と最初の新）／蒸気機関は新旧の強さの差が無い（産出の割合だけ読む）／
//   旧のレシピ＝名前で引く（マスケット・鋳鉄砲・鋳鉄の機関）＝それ以外の生産者は全て〈新〉に入る（旗 0 では門の閉じる旧が産出 0 に寄るので newShare は 1 に張り付く）／国の名前は表示名。
public static partial class Instruments
{
    // 財と旧のレシピの名前（番人が固定する）。
    static readonly (int Good, string Old)[] WpGoods = [(Catalog.RifleId, "マスケット"), (Catalog.ArtilleryId, "鋳鉄砲"), (Catalog.SteamEngineId, "鋳鉄の機関")];

    internal static string WpHeaderText()
        => "quarter\tyear\tnation\tgood\tknown\toldGateOpen\toldOutputPerMonth\tnewOutputPerMonth\tnewShare\toldCostPerUnit\tnewCostPerUnit\tgoodPriceMean\tpriceLevel\tcapitalEquipFill\tcapitalReadiness"
         + "\toldBuilt\tnewBuilt\toldStateTarget\tnewStateTarget\toldScale\tnewScale\tregionsBoth\tregionsOldCheaper\toldOutWhereOldCheaper\tnewOutWhereOldCheaper\toldOutWhereNewCheaper\tnewOutWhereNewCheaper";

    static void WpFold(Sink sk, ulong seed, World w, int q)
    {
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
                if (r.State != RegionState.Active) continue;
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
