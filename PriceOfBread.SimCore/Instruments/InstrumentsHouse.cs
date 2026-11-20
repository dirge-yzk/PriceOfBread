namespace PriceOfBread.SimCore;

// 力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない。〔履歴 InstrumentsHouse.cs#1〕
public static partial class Instruments
{
    // ここは列名の鍵だけ（同じ添字・数が違えば型の初期化で止まる）。〔履歴 InstrumentsHouse.cs#2〕
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
        if (!Tg.House) return;   // (1256) 書く表がどれも選ばれていなければ積算ごと飛ぶ
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
