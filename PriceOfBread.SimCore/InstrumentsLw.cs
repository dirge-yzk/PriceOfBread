namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (459) 労働の値段の表 labor_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsEd.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しい状態を足さない。INS=1 のときだけ動く。旗 LaborFloorOffGate・LaborBandFloorOffGate・MinimumWageLawGate に依らず出す。
// 目的＝事前登録 measurements/lw459_20261020_prereg.txt の G1〜G6 と段0b（農民の所得の内訳の近似）・P5（水準の低下の帰結）を読む。
// 行＝国×四半期（挙げた国は毎四半期・他の全ての国は年末）。人口が 0 の国は出さない。値は州の和か、断りがなければ人力の生産者の能力の重み付き平均（直近の月の値）。
// 列（単位は接尾辞：Share＝0〜1・Index＝無次元の量・PerMonth＝月あたりの量〔数量は財の単位・Money は国の通貨〕）：
//   lawCode（World.MinimumWage の序数 0＝なし〜3＝充足）／
//   {plain|farm}CapPerMonth（人力の生産者の能力の和）・{plain|farm}DemandPerMonth（市場の LastDemand の和）・{plain|farm}SoldPerMonth（LastSold の和）・
//   {plain|farm}PriceOverMcIndex（値段÷自己費用 mc〔EconomyStep.ProfitFactor が読む mc と同じ式〕を能力で重み付け）・{plain|farm}RealPriceIndex（値段÷州の物価水準を能力で重み付け）・{plain|farm}ProfitMeanShare（EconomyStep.ProfitFactor を能力で重み付け）・
//   {plain|farm}WageBillPerMonth（売れた量×値段の和＝賃金の支払いの近似）・{plain|farm}UnemploymentShare（担い手 POP の失業率を人数で重み付け）／
//   plainFloor{0|1}Over{Subs|Liv|Suff}Index（法の床〔MinimumWageFloor の籠を段ごとに仮に置いたもの〕÷今の値段を能力で重み付け・0＝能力ベース・1＝実雇用ベース）／
//   enactedBindShare（法が施行されているとき、床〔係数の読みどおり〕が今の値段を上回る州の能力の割合・法が None なら 0）。
// ⚠限界（計器が自己申告する）：四半期の末の断面／賃金の支払いは売れた量×値段の近似で、賃金→農民の所得の割合は class_s の農民の earnedPerMonth との比で読む／mc は市場の州の乗数まで含む自己費用の式をそのまま読む／人力の生産者が無い州は行に数えない。
public static partial class Instruments
{
    internal const string LwHeaderText = "quarter\tyear\tnation\tlawCode\t"
        + "plainCapPerMonth\tplainDemandPerMonth\tplainSoldPerMonth\tplainPriceOverMcIndex\tplainRealPriceIndex\tplainProfitMeanShare\tplainWageBillPerMonth\tplainUnemploymentShare\t"
        + "farmCapPerMonth\tfarmDemandPerMonth\tfarmSoldPerMonth\tfarmPriceOverMcIndex\tfarmRealPriceIndex\tfarmProfitMeanShare\tfarmWageBillPerMonth\tfarmUnemploymentShare\t"
        + "plainFloor0OverSubsIndex\tplainFloor0OverLivIndex\tplainFloor0OverSuffIndex\tplainFloor1OverSubsIndex\tplainFloor1OverLivIndex\tplainFloor1OverSuffIndex\tenactedBindShare";

    private sealed class LwAcc
    {
        public double Cap, Demand, Sold, PxMcW, RealW, ProfW, Bill, UnW, UnDen, CapW;
    }

    static void LwFold(Sink sk, ulong seed, World w, int q, bool picked, bool yearEnd)
    {
        if (!(picked || yearEnd) || !(w.TotalPopulation > 0)) return;
        var c = w.Coef;
        var plain = new LwAcc(); var farm = new LwAcc();
        var fl = new double[6]; double flW = 0, bindW = 0, bindCap = 0;   // fl[0..2]＝能力ベースの 生存・生活・充足／fl[3..5]＝実雇用ベース
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active) continue;
            LwGood(r, w, c, Catalog.LaborId, plain, out double capP, out int lpP);
            LwGood(r, w, c, Catalog.FarmLaborId, farm, out _, out _);
            if (!(capP > 0) || lpP < 0 || lpP >= r.Pops.Count) continue;
            double price = r.Markets[Catalog.LaborId].Price;
            if (!(price > 0)) continue;
            double size = r.Pops[lpP].Size, sold = r.Markets[Catalog.LaborId].LastSold;
            for (int k = 0; k < 3; k++)
            {
                double basket = MinimumWageFloor.BasketPerPerson(r, (MinimumWageLaw)(k + 1));
                fl[k] += capP * (basket * size / capP) / price;
                if (sold > 1e-9) fl[3 + k] += capP * (basket * size / sold) / price;
            }
            flW += capP;
            if (w.MinimumWage != MinimumWageLaw.None)
            {
                bindCap += capP;
                if (MinimumWageFloor.PerUnit(r, Catalog.LaborId, w.MinimumWage) > price) bindW += capP;
            }
        }
        double Div(double a, double b) => b > 0 ? a / b : double.NaN;
        sk.Lab ??= Open("labor", seed);
        if (!sk.LabHeader) { sk.Lab.WriteLine(LwHeaderText); sk.LabHeader = true; }
        var cols = new List<string>
        {
            q.ToString(System.Globalization.CultureInfo.InvariantCulture), w.Year.ToString(System.Globalization.CultureInfo.InvariantCulture), NameOf(w), ((int)w.MinimumWage).ToString(System.Globalization.CultureInfo.InvariantCulture),
        };
        foreach (var a in new[] { plain, farm })
            cols.AddRange([F(a.Cap), F(a.Demand), F(a.Sold), F(Div(a.PxMcW, a.CapW)), F(Div(a.RealW, a.CapW)), F(Div(a.ProfW, a.CapW)), F(a.Bill), F(Div(a.UnW, a.UnDen))]);
        for (int k = 0; k < 6; k++) cols.Add(F(Div(fl[k], flW)));
        cols.Add(F(bindCap > 0 ? bindW / bindCap : 0.0));
        sk.Lab.WriteLine(string.Join('\t', cols));
    }

    /// <summary>1 州の 1 つの労働財（人力の生産者）を a に足す。cap＝その州の人力の能力の和・lp＝最初の人力の生産者の担い手 POP の添字（無ければ −1）。</summary>
    private static void LwGood(Region r, World w, Coefficients c, int goodId, LwAcc a, out double cap, out int lp)
    {
        cap = 0; lp = -1;
        if (goodId >= r.Markets.Length) return;
        var m = r.Markets[goodId];
        double price = m.Price;
        foreach (var s in r.Producers)
        {
            if (s.GoodId != goodId || !EconomyStep.IsHumanLaborSupply(s) || s.Bankrupt) continue;
            if (lp < 0) lp = s.LaborPopIndex;
            if (!(s.Capacity > 0)) continue;
            double mc = EconomyStep.MineralSelfCost(s, r, c, w) + EconomyStep.InputCostPerUnit(s, r, r, c);
            cap += s.Capacity; a.CapW += s.Capacity;
            if (mc > 0) a.PxMcW += s.Capacity * price / mc;
            if (r.PriceLevel > 0) a.RealW += s.Capacity * price / r.PriceLevel;
            a.ProfW += s.Capacity * EconomyStep.ProfitFactor(s, price, mc, c);
        }
        if (lp < 0) return;
        a.Cap += cap; a.Demand += m.LastDemand; a.Sold += m.LastSold; a.Bill += m.LastSold * price;
        if (lp < r.Pops.Count) { double sz = r.Pops[lp].Size; a.UnW += sz * r.Pops[lp].UnemploymentRate.Value; a.UnDen += sz; }
    }
}
