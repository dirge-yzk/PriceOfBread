namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (550) 工場の建屋の財（建造物）の表 fb_s（読むだけ・事前登録 measurements/fb550_20261025_prereg.txt）--------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsWp.cs・InstrumentsIvf.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない。
// 旗 BuildingGoodBundle（既定 1）が正の国だけ行を出す（旗 0 の腕は表のファイルが出ない）。INS=1 のときだけ Quarter から呼ばれる。
// 行＝国×四半期（挙げた国は毎四半期・他の全ての国は年末）。本土＝州の格が植民地でない稼働中の州。重みは州の人口。
// 列：
//   outWood・outBrick・outCast・outSteel・outRC＝建造物の生産者の実産出 Output の和（本土・レシピごと＝木造・煉瓦造・鋳鉄の骨組み・鋼の骨組み・鉄筋コンクリート造。G3）／outTotal＝5 本の和
//   gateCast・gateSteel・gateRC＝その段の生産者で門が正の本土の州の数（鋳鉄の骨組み＝発見の行・鋼の骨組み＝転炉鋼の行・鉄筋コンクリート造＝住居の同名の行。番人②）／regions＝建造物の生産者がある本土の州の数
//   costWood・costBrick・costCast・costSteel・costRC＝首都州（w.Region）のその段の生産者の限界費用（自己費用＋投入費・今の価格・行を書く時点）＝門が閉じた段は nan（費用の安い段が産出を取るか＝G3e）
//   buildPxOverRefMain＝建造物の値段÷（参照価格×州の物価水準）の人口加重平均（G6）／buildStockMean＝建造物の在庫の州の単純平均（初期在庫 200 の食い潰しを読む）
//   buildDemand・buildProd＝本土の建造物の LastDemand／LastProduced の和／lumDemand・lumProd・limDemand・limProd・clayDemand・clayProd・cementDemand・cementProd＝製材・石灰石・粘土・セメントの同じ和（材料の足りなさ。鉄・鋼・機械は ivf_s）
// ⚠限界（計器が自分で申告する）：四半期の末の断面／outXxx は州の Output の和で操業規模・能力を割らない／gate は生産者の門（Discovery.Gate）で国の解禁の配列そのもの／建造物の市場が無い州（最小の世界）は数えない／
//   国別の絞り（POB_INSTRUMENTS_NATIONS）は IvfFold と同じ（picked）。助走の間（暦が進まない）は行を出さない。
public static partial class Instruments
{
    static readonly string[] FbRecipes = ["", "煉瓦造", "鋳鉄の骨組み", "鋼の骨組み", "鉄筋コンクリート造"];   // 先頭の空＝既定（木造・RecipeName が空）

    internal const string FbHeader = "quarter\tyear\tnation\tpopK\tmainPopK\tregions\toutWood\toutBrick\toutCast\toutSteel\toutRC\toutTotal\tgateCast\tgateSteel\tgateRC\t"
        + "costWood\tcostBrick\tcostCast\tcostSteel\tcostRC\tbuildPxOverRefMain\tbuildStockMean\tbuildDemand\tbuildProd\tlumDemand\tlumProd\tlimDemand\tlimProd\tclayDemand\tclayProd\tcementDemand\tcementProd";

    static void FbFold(Sink sk, ulong seed, World w, int q, bool picked, bool yearEnd)
    {
        if (!(w.Coef.BuildingGoodBundle > 0)) return;
        if (!(picked || yearEnd)) return;
        if (w.WarmupFrozen) return;
        double popK = w.TotalPopulation;
        if (!(popK > 0)) return;
        var out5 = new double[FbRecipes.Length];
        int gateCast = 0, gateSteel = 0, gateRC = 0, regions = 0, stockN = 0;
        double mainPop = 0, pxW = 0, px = 0, stockSum = 0, bD = 0, bP = 0, lumD = 0, lumP = 0, limD = 0, limP = 0, clD = 0, clP = 0, cmD = 0, cmP = 0;
        foreach (var r in w.Regions)
        {
            if (r.Tier == RegionTier.Colonial) continue;
            if (!(Catalog.BuildingId < r.Markets.Length)) continue;
            double pop = r.SumPopulation();
            mainPop += pop;
            bool has = false;
            foreach (var pr in r.Producers)
            {
                if (pr.GoodId != Catalog.BuildingId) continue;
                has = true;
                int k = System.Array.IndexOf(FbRecipes, pr.RecipeName);
                if (k >= 0) out5[k] += pr.Output;
                if (Discovery.Gate(r, pr) > 0) { if (k == 2) gateCast++; else if (k == 3) gateSteel++; else if (k == 4) gateRC++; }
            }
            if (has) regions++;
            var bm = r.Markets[Catalog.BuildingId];
            px += pop * bm.Price / System.Math.Max(1e-12, Catalog.RefPrices[Catalog.BuildingId] * r.PriceLevel); pxW += pop;
            stockSum += bm.Stock; stockN++;
            bD += bm.LastDemand; bP += bm.LastProduced;
            if (Catalog.LumberId < r.Markets.Length) { lumD += r.Markets[Catalog.LumberId].LastDemand; lumP += r.Markets[Catalog.LumberId].LastProduced; }
            if (Catalog.LimestoneId < r.Markets.Length) { limD += r.Markets[Catalog.LimestoneId].LastDemand; limP += r.Markets[Catalog.LimestoneId].LastProduced; }
            if (Catalog.ClayId < r.Markets.Length) { clD += r.Markets[Catalog.ClayId].LastDemand; clP += r.Markets[Catalog.ClayId].LastProduced; }
            if (Catalog.CementId < r.Markets.Length) { cmD += r.Markets[Catalog.CementId].LastDemand; cmP += r.Markets[Catalog.CementId].LastProduced; }
        }
        double tot = 0; foreach (double v in out5) tot += v;
        var cap = w.Region; var cost5 = new double[FbRecipes.Length]; System.Array.Fill(cost5, double.NaN);
        if (Catalog.BuildingId < cap.Markets.Length)
            foreach (var pr in cap.Producers)
            {
                if (pr.GoodId != Catalog.BuildingId) continue;
                int k = System.Array.IndexOf(FbRecipes, pr.RecipeName);
                if (k < 0 || !double.IsNaN(cost5[k]) || !(Discovery.Gate(cap, pr) > 0)) continue;
                cost5[k] = EconomyStep.MineralSelfCost(pr, cap, w.Coef, w) + EconomyStep.InputCostPerUnit(pr, cap, cap, w.Coef);
            }
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        sk.Fb ??= Open("fb", seed);
        if (!sk.FbHeader) { sk.Fb.WriteLine(FbHeader); sk.FbHeader = true; }
        sk.Fb.WriteLine(string.Join('\t', I(q), I(w.Year), NameOf(w), F(popK), F(mainPop), I(regions),
            F(out5[0]), F(out5[1]), F(out5[2]), F(out5[3]), F(out5[4]), F(tot), I(gateCast), I(gateSteel), I(gateRC),
            F(cost5[0]), F(cost5[1]), F(cost5[2]), F(cost5[3]), F(cost5[4]),
            F(pxW > 0 ? px / pxW : double.NaN), F(stockN > 0 ? stockSum / stockN : double.NaN), F(bD), F(bP), F(lumD), F(lumP), F(limD), F(limP), F(clD), F(clP), F(cmD), F(cmP)));
    }
}
