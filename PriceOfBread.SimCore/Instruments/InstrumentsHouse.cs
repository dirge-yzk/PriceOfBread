namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (418) 農家の手仕事の表 house_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsAdmin.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない。INS=1 のときだけ動く（Instruments.Quarter から呼ばれる）。
// 目的＝事前登録 measurements/ce_20261019_prereg.txt の記録の線 R2・R5（手仕事の行が産出に占める割合・農民の能力の取り分・農業労働力の売れ方）を読む。旗 HouseholdProductionEnabled＝0 でも出す（A の表が〈旧の世界〉の表になる＝行が無いので手仕事の列は 0）。
// 行＝国×四半期（挙げた国は毎四半期・他の全ての国は年末）。人口が 0 の国は出さない。列（単位は接尾辞：Count＝件数・Share＝0〜1・PerMonth＝その月の産出の量）：
//   quarter／year／nation／homeRowsCount（活きた州の〈家内工業〉の行の数）／homeLaborWeightShare（人口加重・州ごとの〈家内工業〉の行の LaborWeight×Scale ÷ 農民の担う非役務の全ての行の LaborWeight×Scale＝能力を分け合う零和分割の取り分）／
//   財ごと 8 本の homeOutput<財>PerMonth（〈家内工業〉の行の Output の活きた州の和）・allOutput<財>PerMonth（その財の全ての行の Output の和）・homeShare<財>（前者÷後者・後者が 0 なら nan）／
//   farmLaborBoughtPerMonth（農業労働力の市場の LastSold の和）・farmLaborProducedPerMonth（同 LastProduced の和）・homeFarmLaborUsePerMonth（〈家内工業〉の行が買う農業労働力の推定＝Σ Output×農業労働力の投入量・買いの内訳を市場は分けないので産出から掛けた値）。
// ⚠限界（計器が自己申告する）：四半期の末の断面／州の単純和（人口加重は能力の取り分だけ）／LaborWeight は平滑の信念で、実際の fraction（prodW／laborTotalW）は労働の重みが 0 のとき 1／行の数に退く（その退きは読まない）／
//   農業労働力の LastSold は RGO と手仕事の行の買い分の合算（手仕事の行の買い分は homeFarmLaborUsePerMonth が産出からの推定で別に出す）／
//   homeLaborWeightShare の分母は眠った行を除かない（眠った行は LaborWeight を平滑のまま持つ＝取り分を低く読む側・手仕事の行は眠らない）。
public static partial class Instruments
{
    // ★(420) 反証役 ce〔bv〕低：財の並びは本体の 1 本（Simulation.HouseholdGoods）を読む＝写しを持たない。ここは列名の鍵だけ（同じ添字・数が違えば型の初期化で止まる）。
    static readonly string[] HouseKeys = ["Clothing", "Furniture", "PreservedMeat", "DriedFish", "Beer", "Spirits", "Cheese", "LeatherGoods"];
    // 計器が動くときだけ組む（型の初期化の順に触れない＝計器を出さない走りの初期化は旧と同じ）。
    static (string Key, int GoodId)[]? _houseGoods;
    static (string Key, int GoodId)[] HouseGoods => _houseGoods ??= HouseKeys.Length == Simulation.HouseholdGoods.Length
        ? HouseKeys.Select((k, i) => (k, Simulation.HouseholdGoods[i])).ToArray()
        : throw new System.InvalidOperationException("house_s の列名の鍵の数が Simulation.HouseholdGoods と違う");

    internal static string HouseHeaderText()
    {
        var cols = new List<string> { "quarter", "year", "nation", "homeRowsCount", "homeLaborWeightShare" };
        foreach (var (key, _) in HouseGoods) { cols.Add("homeOutput" + key + "PerMonth"); cols.Add("allOutput" + key + "PerMonth"); cols.Add("homeShare" + key); }
        cols.Add("farmLaborBoughtPerMonth"); cols.Add("farmLaborProducedPerMonth"); cols.Add("homeFarmLaborUsePerMonth");
        return string.Join('\t', cols);
    }

    static void HouseFold(Sink sk, ulong seed, World w, int q, bool picked, bool yearEnd)
    {
        if (!(picked || yearEnd) || !(w.TotalPopulation > 0)) return;
        int nGoods = HouseGoods.Length;
        var home = new double[nGoods]; var all = new double[nGoods];
        double den = 0, shareNum = 0, bought = 0, produced = 0, homeLabor = 0; int rows = 0;
        foreach (var r in w.Regions)
        {
            double pop = r.SumPopulation();
            if (!(pop > 0)) continue;
            int lp = -1;
            double homeW = 0, allW = 0;
            foreach (var pr in r.Producers)
            {
                bool isHome = pr.RecipeName == Simulation.HouseholdRecipeName;
                if (isHome) { rows++; lp = pr.LaborPopIndex; foreach (var inp in pr.Inputs) if (inp.GoodId == Catalog.FarmLaborId) homeLabor += pr.Output * inp.QtyPerOutput; }
                for (int k = 0; k < nGoods; k++)
                    if (pr.GoodId == HouseGoods[k].GoodId) { all[k] += pr.Output; if (isHome) home[k] += pr.Output; }
            }
            if (lp >= 0)
            {
                foreach (var pr in r.Producers)
                {
                    if (pr.LaborPopIndex != lp || pr.GoodId < 0 || pr.GoodId >= Catalog.Goods.Length || Catalog.Goods[pr.GoodId].Tier == GoodTier.Service) continue;
                    double wgt = pr.LaborWeight * pr.Scale; if (!(wgt > 0)) continue;
                    allW += wgt; if (pr.RecipeName == Simulation.HouseholdRecipeName) homeW += wgt;
                }
                if (allW > 0) { shareNum += pop * (homeW / allW); den += pop; }
            }
            if (Catalog.FarmLaborId < r.Markets.Length) { var m = r.Markets[Catalog.FarmLaborId]; bought += m.LastSold; produced += m.LastProduced; }
        }
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var cols = new List<string> { I(q), I(w.Year), NameOf(w), I(rows), F(den > 0 ? shareNum / den : 0.0) };
        for (int k = 0; k < nGoods; k++) { cols.Add(F(home[k])); cols.Add(F(all[k])); cols.Add(F(all[k] > 0 ? home[k] / all[k] : double.NaN)); }
        cols.Add(F(bought)); cols.Add(F(produced)); cols.Add(F(homeLabor));
        sk.House ??= Open("house", seed);
        if (!sk.HouseHeader) { sk.House.WriteLine(HouseHeaderText()); sk.HouseHeader = true; }
        sk.House.WriteLine(string.Join('\t', cols));
    }
}
