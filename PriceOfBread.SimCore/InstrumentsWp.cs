namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (537) 旧と新の武器の並行の表 wp_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsChem.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない。INS=1 のときだけ出す
// 目的＝事前登録 measurements/wp_20261024_prereg.txt の [3] ①〜④（旧が退場しないか・新の割合が単調に増えるか・強さ1単位あたりの費用で選ぶか・装備の充足が上がる向きか）を読む。
// wp_s：挙げた国×四半期×{小銃・火砲・蒸気機関}（名前の並び＝<see cref="WpGoods"/>）。人口が 0 の国は出さない。
//   列：quarter／year／nation／good／known（転炉鋼＝新世代を開く発見を国が解禁済みなら 1）／oldGateOpen（活きた州の旧のレシピの生産者で門が正の数）／oldOutputPerMonth（その実産出 Output の和）／
//   newOutputPerMonth（同じ財の旧以外の生産者の実産出の和）／newShare（新÷(旧＋新)・分母が 0 なら nan）／oldCostPerUnit／newCostPerUnit（資本州の最初の生産者の 自己費用＋投入費・名目・無ければ nan）／
//   goodPriceMean（活きた州のその財の市場の値段の単純平均・名目）／priceLevel／capitalEquipFill（資本州の装備充足 0〜1）／capitalReadiness（国の即応度）
// ⚠限界（計器が自己申告する）：四半期の末の断面／州の単純平均（産出で重み付けしない）／費用は資本州の 1 点／蒸気機関は新旧の強さの差が無い（産出の割合だけ読む）／
//   旧のレシピ＝名前で引く（マスケット・鋳鉄砲・鋳鉄の機関）＝それ以外の生産者は全て〈新〉に入る（旗 0 では門の閉じる旧が産出 0 に寄るので newShare は 1 に張り付く）／国の名前は表示名。
public static partial class Instruments
{
    // 財と旧のレシピの名前（番人が固定する）。
    static readonly (int Good, string Old)[] WpGoods = [(Catalog.RifleId, "マスケット"), (Catalog.ArtilleryId, "鋳鉄砲"), (Catalog.SteamEngineId, "鋳鉄の機関")];

    internal static string WpHeaderText()
        => "quarter\tyear\tnation\tgood\tknown\toldGateOpen\toldOutputPerMonth\tnewOutputPerMonth\tnewShare\toldCostPerUnit\tnewCostPerUnit\tgoodPriceMean\tpriceLevel\tcapitalEquipFill\tcapitalReadiness";

    static void WpFold(Sink sk, ulong seed, World w, int q, bool picked)
    {
        if (!picked || !(w.TotalPopulation > 0)) return;
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var cap = w.Region;
        foreach (var (g, oldName) in WpGoods)
        {
            int open = Catalog.OpenedByRowOf(g);
            int gateOpen = 0; double oldOut = 0, newOut = 0, px = 0; int pxN = 0;
            foreach (var r in w.Regions)
            {
                if (r.State != RegionState.Active) continue;
                foreach (var pr in r.Producers)
                {
                    if (pr.GoodId != g) continue;
                    if (pr.RecipeName == oldName) { oldOut += pr.Output; if (Discovery.Gate(r, pr) > 0) gateOpen++; }
                    else newOut += pr.Output;
                }
                if (g < r.Markets.Length) { double p = r.Markets[g].Price; if (p > 0) { px += p; pxN++; } }
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
                F(tot > 0 ? newOut / tot : double.NaN), F(oldCost), F(newCost), F(pxN > 0 ? px / pxN : double.NaN), F(w.PriceLevel), F(cap.MilitaryEquipFill), F(w.MilitaryReadiness)));
        }
    }
}
