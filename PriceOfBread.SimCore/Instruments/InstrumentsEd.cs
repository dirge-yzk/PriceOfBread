namespace PriceOfBread.SimCore;

// 力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない。〔履歴 InstrumentsEd.cs#1〕
public static partial class Instruments
{
    internal const string EdHeaderText = "quarter\tyear\tnation\tpopK\tfundingShare\tfundedRegionShare\tneedPerMonth\tprivateDemandPerMonth\tavailablePerMonth\tstateWantPerMonth\tstateTakePerMonth\t"
        + "stateWantMoneyPerMonth\tstateTakeMoneyPerMonth\tshortRegionShare\tdemandSignalPerMonth\tproducedPerMonth\tsoldPerMonth\tpriceMean\tpriceOverRefTimesLevelIndex\thigherWantPerMonth\thigherTakePerMonth"
        + "\thigherTuitionPerMonth\thigherSubsidyPerMonth\thigherSubsidyExpectedPerMonth\thigherCoverShare\thigherFillShare"   // thigherSubsidyExpectedPerMonth〔履歴 InstrumentsEd.cs#2〕
        + "\teduStage\teduStageMismatchShare";   // teduStageMismatchShare〔履歴 InstrumentsEd.cs#3〕

    /// <summary>
    /// 書くのは EconomyStep の INS=1 の月だけ・読むのは EdFold だけ。〔履歴 InstrumentsEd.cs#4〕
    /// </summary>
    internal sealed class EdDiag { public double Need, Priv, Avail, Want, Take, HigherWant, HigherTake, HigherGross, HigherSub; }   // EdDiag〔履歴 InstrumentsEd.cs#5〕
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Region, EdDiag> EdDiags = new();
    static readonly EdDiag EdZero = new();
    internal static EdDiag EdDiagOf(Region r) => EdDiags.TryGetValue(r, out var d) ? d : EdZero;
    /// <summary>教育の月の始め：必要量・民間の買いたい量・供給を置き、国庫の買いたい分と取れた分を 0 に戻す。</summary>
    internal static void EdBegin(Region r, double need, double priv, double avail) { var d = EdDiags.GetOrCreateValue(r); d.Need = need; d.Priv = priv; d.Avail = avail; d.Want = 0; d.Take = 0; }
    internal static void EdSet(Region r, double want, double take) { var d = EdDiags.GetOrCreateValue(r); d.Want = want; d.Take = take; }
    internal static void HigherEdSet(Region r, double want, double take) { var d = EdDiags.GetOrCreateValue(r); d.HigherWant = want; d.HigherTake = take; }
    /// <summary>
    /// 〔履歴 InstrumentsEd.cs#6〕
    /// </summary>
    internal static void HigherPaidBegin(Region r) { var d = EdDiags.GetOrCreateValue(r); d.HigherGross = 0; d.HigherSub = 0; }
    internal static void HigherPaid(Region r, double gross, double subsidy) { var d = EdDiags.GetOrCreateValue(r); d.HigherGross += gross; d.HigherSub += subsidy; }

    static void EdFold(Sink sk, ulong seed, World w, int q, bool picked, bool yearEnd)
    {
        if (!(picked || yearEnd) || !(w.TotalPopulation > 0)) return;
        double mmPop = 0; int natStage = Government.TaxStage(PolicyAxis.EducationSpending, w.EducationSpending);
        double popK = 0, fundW = 0, fundedPop = 0, shortPop = 0, need = 0, priv = 0, avail = 0, want = 0, take = 0, wantMoney = 0, takeMoney = 0, demand = 0, produced = 0, sold = 0, priceW = 0, relW = 0, priceDen = 0, hWant = 0, hTake = 0, hGross = 0, hSub = 0, hSubExp = 0, coverW = 0, fillNum = 0, fillDen = 0;
        bool haveEdu = Catalog.EducationId < Catalog.RefPrices.Length;
        double refP = haveEdu ? Catalog.RefPrices[Catalog.EducationId] : double.NaN;
        foreach (var r in w.Regions)
        {
            double rp = r.SumPopulation();
            var d = EdDiagOf(r);
            popK += rp; fundW += rp * r.CompulsoryEduFunding;
            if (Government.TaxStage(PolicyAxis.EducationSpending, r.EducationSpending) != natStage) mmPop += rp;   // EducationSpending〔履歴 InstrumentsEd.cs#7〕
            if (r.CompulsoryEduFunding > 1e-9)
            {
                fundedPop += rp;
                if (d.Want > d.Take * (1.0 + 1e-9) + 1e-12) shortPop += rp;   // 否定形でなく読むだけの診断＝NaN は数えない側
            }
            hGross += d.HigherGross; hSub += d.HigherSub; hSubExp += EconomyStep.ScholarshipCover(r, w.Coef) * d.HigherGross; coverW += rp * EconomyStep.ScholarshipCover(r, w.Coef);   // HigherGross〔履歴 InstrumentsEd.cs#8〕
            foreach (var p in r.Pops) { if (p.IsSoldier) continue; fillNum += p.Size * p.HigherEduFill; fillDen += p.Size; }
            need += d.Need; priv += d.Priv; avail += d.Avail; want += d.Want; take += d.Take; hWant += d.HigherWant; hTake += d.HigherTake;
            if (!haveEdu || Catalog.EducationId >= r.Markets.Length) continue;
            var m = r.Markets[Catalog.EducationId];
            demand += m.LastDemand; produced += m.LastProduced; sold += m.LastSold;
            wantMoney += d.Want * m.Price; takeMoney += d.Take * m.Price;
            if (rp > 0)
            {
                priceW += m.Price * rp; priceDen += rp;
                double den = refP * r.PriceLevel; if (den > 1e-12) relW += m.Price / den * rp;
            }
        }
        double Div(double a, double b) => b > 0 ? a / b : double.NaN;
        sk.Ed ??= Open("ed", seed);
        if (!sk.EdHeader) { sk.Ed.WriteLine(EdHeaderText); sk.EdHeader = true; }
        sk.Ed.WriteLine(string.Join('\t', q.ToString(System.Globalization.CultureInfo.InvariantCulture), w.Year.ToString(System.Globalization.CultureInfo.InvariantCulture), NameOf(w), F(popK), F(Div(fundW, popK)), F(Div(fundedPop, popK)),
            F(need), F(priv), F(avail), F(want), F(take), F(wantMoney), F(takeMoney), F(Div(shortPop, fundedPop)), F(demand), F(produced), F(sold), F(Div(priceW, priceDen)), F(Div(relW, priceDen)), F(hWant), F(hTake),
            F(hGross), F(hSub), F(hSubExp), F(Div(coverW, popK)), F(Div(fillNum, fillDen)), F(natStage), F(Div(mmPop, popK))));
    }
}
