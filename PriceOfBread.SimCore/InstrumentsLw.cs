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
//   enactedBindShare（法が施行されているとき、床〔係数の読みどおり〕が今の値段を上回る州の能力の割合・法が None なら 0・四半期末の断面で天井を知らない弱い読み＝記録だけ）／
//   ★(469) 追記 3 で足した 6 列：enactedNearFloorShare（法が施行されているとき、値段が〈機構と同じ天井で削った床〉×1.05 以下＝床に座るか床の下の州の能力の割合・N1）・
//   enactedBelowFloorShare（同じ床の 0.90 倍を値段が下回る州の能力の割合・N2）・{plain|farm}RealPriceMinIndex（値段÷州の物価水準の州の中の最小・発散の観測 (i)）・
//   {plain|farm}TinyPriceRegionShare（値段÷州の物価水準が 1e-3 未満の州の能力の割合・発散の観測 (i)）。
//   ★(481) 追記 6 列（N2＝施行行の床の下の診断・MarketFlow.StepDay が人力の労働の値段を書いた直後の値段とそのとき使った床を残し、四半期末の断面と並べる）：
//   stepBelowShare（最後の歩の直後に値段が〈その歩の床〉×0.90 未満だった州の能力の割合＝歩が床を守れていたか）・
//   floorEndOverStepIndex（四半期末の床÷最後の歩の床を能力で重み付け＝歩の後に床が上がったか）・priceEndOverStepIndex（四半期末の値段÷最後の歩の直後の値段＝歩の後に値段が動いたか）・
//   belowNoStepShare／belowPriceMovedShare／belowFloorMovedShare（四半期末に床×0.90 未満の州の能力を原因に分けた割合：最後の歩が無い〔印が古い・床なし〕／歩の直後は床×0.90 以上だったのに値段が落ちた／歩の直後の値段が四半期末の床×0.90 未満＝床が上がった）。
//   3 つの和は enactedBelowFloorShare と一致する。分母は enactedBelowFloorShare と同じ（法が施行されている州の能力）。
//   ⚠限界（追記 6 列）：歩の記録の印 Fresh は折り込みが下ろす＝その国が折り込まれない四半期（挙げた国以外の年末でない四半期）は印が残り、次の折り込みは数四半期ぶんの歩のうち最後の 1 回を見る／折り込みの途中で飛ばす州（能力 0・担い手なし・値段 0 以下）は印が下りず古い印のまま次へ持ち越す（belowNoStepShare を少なく数えうる）／歩の記録は工場労働（LaborId）だけ・農業労働は持たない。
// ⚠限界（計器が自己申告する）：四半期の末の断面／賃金の支払いは売れた量×値段の近似で、賃金→農民の所得の割合は class_s の農民の earnedPerMonth との比で読む／mc は市場の州の乗数まで含む自己費用の式をそのまま読む／人力の生産者が無い州は行に数えない。
public static partial class Instruments
{
    internal const string LwHeaderText = "quarter\tyear\tnation\tlawCode\t"
        + "plainCapPerMonth\tplainDemandPerMonth\tplainSoldPerMonth\tplainPriceOverMcIndex\tplainRealPriceIndex\tplainProfitMeanShare\tplainWageBillPerMonth\tplainUnemploymentShare\t"
        + "farmCapPerMonth\tfarmDemandPerMonth\tfarmSoldPerMonth\tfarmPriceOverMcIndex\tfarmRealPriceIndex\tfarmProfitMeanShare\tfarmWageBillPerMonth\tfarmUnemploymentShare\t"
        + "plainFloor0OverSubsIndex\tplainFloor0OverLivIndex\tplainFloor0OverSuffIndex\tplainFloor1OverSubsIndex\tplainFloor1OverLivIndex\tplainFloor1OverSuffIndex\tenactedBindShare"
        + "\tenactedNearFloorShare\tenactedBelowFloorShare\tplainRealPriceMinIndex\tplainTinyPriceRegionShare\tfarmRealPriceMinIndex\tfarmTinyPriceRegionShare"
        + "\tstepBelowShare\tfloorEndOverStepIndex\tpriceEndOverStepIndex\tbelowNoStepShare\tbelowPriceMovedShare\tbelowFloorMovedShare";

    /// <summary>★(481) N2 の診断の蓄え＝州ごとの最後の歩（人力の労働の値段を <c>MarketFlow.StepDay</c> が書いた直後）の値段とそのとき使った床（0＝床なし）。シミュの状態ではない（ConditionalWeakTable の先例・セーブに載らない）。
    /// Fresh＝前の四半期の折り込み以後に歩があった印（折り込みで下ろす）。</summary>
    private sealed class LwStepRec { public double Price, Floor; public bool Fresh; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Region, LwStepRec> LwSteps = new();

    /// <summary>★(481) 計器の hook（読むだけ）：<c>MarketFlow.StepDay</c> の人力の労働（工場労働）の枝の最後で、書いた直後の値段 <paramref name="price"/> と使った床 <paramref name="floor"/>（法が None なら 0）を残す。</summary>
    internal static void LwStep(Region r, int goodId, double price, double floor)
    {
        if (goodId != Catalog.LaborId) return;
        var rec = LwSteps.GetOrCreateValue(r);
        rec.Price = price; rec.Floor = floor; rec.Fresh = true;
    }

    private sealed class LwAcc
    {
        public double Cap, Demand, Sold, PxMcW, RealW, ProfW, Bill, UnW, UnDen, CapW;
        public double MinReal = double.PositiveInfinity, TinyCap;   // ★(469) 追記 3：州の中の実質の値段の最小・実質の値段が 1e-3 未満の州の能力
    }

    static void LwFold(Sink sk, ulong seed, World w, int q, bool picked, bool yearEnd)
    {
        if (!(picked || yearEnd) || !(w.TotalPopulation > 0)) return;
        var c = w.Coef;
        var plain = new LwAcc(); var farm = new LwAcc();
        var fl = new double[6]; double flW = 0, bindW = 0, bindCap = 0, nearW = 0, belowW = 0;
        double stBelowW = 0, stFloorCap = 0, stFloorRatioW = 0, stPriceRatioW = 0, stSeenCap = 0, noStepW = 0, priceMovedW = 0, floorMovedW = 0;   // ★(481) N2 の診断   // fl[0..2]＝能力ベースの 生存・生活・充足／fl[3..5]＝実雇用ベース
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
                // ★(469) 追記 3：床は機構（MarketFlow.StepDay）と同じ天井で削った値（AbsFloor・天井＝参照価格×物価水準×EffWageMax）で読む。縛り＝値段が床の 1.05 倍以下／床の下＝値段が床の 0.90 倍未満
                double ceil = r.GoodOf(Catalog.LaborId)!.RefPrice * r.PriceLevel * MarketFlow.EffWageMax(r, c);
                double fl0 = MinimumWageFloor.AbsFloor(r, Catalog.LaborId, ceil, w.MinimumWage);
                if (fl0 > 0 && price <= fl0 * 1.05) nearW += capP;
                if (fl0 > 0 && price < fl0 * 0.90) belowW += capP;
                // ★(481) N2 の診断：最後の歩の直後の値段と床を、四半期末の断面（price・fl0）と並べる
                bool seen = LwSteps.TryGetValue(r, out var srec) && srec.Fresh;
                if (seen)
                {
                    stSeenCap += capP;
                    if (srec!.Floor > 0) { stFloorCap += capP; if (srec.Price < srec.Floor * 0.90) stBelowW += capP; stFloorRatioW += capP * (fl0 / srec.Floor); }
                    if (srec.Price > 0) stPriceRatioW += capP * (price / srec.Price);
                }
                if (fl0 > 0 && price < fl0 * 0.90)
                {
                    if (!seen || !(srec!.Floor > 0)) noStepW += capP;
                    else if (srec.Price >= fl0 * 0.90) priceMovedW += capP;
                    else floorMovedW += capP;
                }
            }
            if (LwSteps.TryGetValue(r, out var srec2)) srec2.Fresh = false;
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
        cols.Add(F(bindCap > 0 ? nearW / bindCap : 0.0));
        cols.Add(F(bindCap > 0 ? belowW / bindCap : 0.0));
        var tailCols = new[] { F(bindCap > 0 ? stBelowW / bindCap : 0.0), F(Div(stFloorRatioW, stFloorCap)), F(Div(stPriceRatioW, stSeenCap)), F(bindCap > 0 ? noStepW / bindCap : 0.0), F(bindCap > 0 ? priceMovedW / bindCap : 0.0), F(bindCap > 0 ? floorMovedW / bindCap : 0.0) };
        foreach (var a in new[] { plain, farm })
        {
            cols.Add(F(a.MinReal));
            cols.Add(F(Div(a.TinyCap, a.CapW)));
        }
        cols.AddRange(tailCols);
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
        if (cap > 0 && r.PriceLevel > 0)
        {   // ★(469) 追記 3：州の粒度の実質の値段（発散の観測 (i)）
            double real = price / r.PriceLevel;
            if (real < a.MinReal) a.MinReal = real;
            if (real < 1e-3) a.TinyCap += cap;
        }
        a.Cap += cap; a.Demand += m.LastDemand; a.Sold += m.LastSold; a.Bill += m.LastSold * price;
        if (lp < r.Pops.Count) { double sz = r.Pops[lp].Size; a.UnW += sz * r.Pops[lp].UnemploymentRate.Value; a.UnDen += sz; }
    }
}
