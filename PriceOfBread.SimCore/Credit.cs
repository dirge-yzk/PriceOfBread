namespace PriceOfBread.SimCore;

public static class Credit
{
    private const double HeatClampMax = 1.5;

    public static void Step(World w, Coefficients c, (double NominalGdp, double AvgSat)? month = null, int days = 1)
    {
        var bank = w.Bank;
        if (bank is null) return;

        double hoardRaw = c.CurrencyHoldingRatio.Value * (1.0 + c.HoardPanicWeight * (1.0 - bank.Confidence));
        double hoardRatio = !double.IsFinite(hoardRaw) ? c.CurrencyHoldingRatio.Value : hoardRaw < 0 ? 0 : hoardRaw;   // 上限は置かない（設計思想⑧）・非有限は既定の退蔵率へ（否定形）
        bank.Gold = new MoneyStock(Math.Max(0, w.BaseMoney.Value) * (1.0 - hoardRatio));

        double avgSat = month?.AvgSat ?? NationalScore.MeanSatisfaction(w);
        List<Pop>? popsLazy = null;
        List<Pop> Pops()
        {
            if (popsLazy is null)
            {
                popsLazy = [];
                foreach (var r0 in w.Regions) popsLazy.AddRange(r0.Pops);
            }
            return popsLazy;
        }
        double fiscalDefault = w.FiscalEnabled ? c.FiscalDefaultPanic * Math.Max(0, 1.0 - w.FiscalFundingRatio.Value) : 0;
        double debtDeflation = w.MonetaryEnabled ? c.DebtDeflationWeight * Math.Max(0, (-w.ExpectedInflationPerYear).Value) * bank.Heat : 0;
        double bondLoss = w.FiscalEnabled ? c.BondLossConfWeight * Math.Max(0, 1.0 - w.BondPrice) * PoliticalQuantities.Clamp01(Calendar.ToGdpYear(w.GovDebt, new MoneyPerYear(Math.Max(1.0, Calendar.PerMonthToPerYear(month?.NominalGdp ?? w.NominalGdpPerMonth.Value)))).Value) : 0;
        double overheatFragility = c.OverheatFragility * Math.Max(0, bank.Heat - c.OverheatFragilityThreshold);
        double targetConf = PoliticalQuantities.Clamp01(1.0 - c.PanicSensitivity * Math.Max(0, c.ConfidencePivot - avgSat) - c.DrawdownPanic * w.GdpDrawdown.Value - fiscalDefault - debtDeflation - bondLoss - overheatFragility);
        double div = Calendar.DaysPerMonth;
        double spd = (targetConf < bank.Confidence ? c.PanicSpeed : c.ConfidenceSpeed) / div;
        if (days > 1) spd = Calendar.OverSteps(spd, days);
        double confStart = bank.Confidence;
        bank.Confidence = PoliticalQuantities.Clamp01(PoliticalQuantities.Toward(bank.Confidence, targetConf, spd));
        double confL = days > 1 ? 0.5 * (confStart + bank.Confidence) : bank.Confidence;

        double appetite = PoliticalQuantities.Clamp01(1.0 - c.InterestRateEffect * w.InterestRate);
        double maxLoans = (bank.Gold * (c.CreditMultiplier - 1)).Value;
        if (c.GoldCoverCreditRestraint > 0 && w.OnGoldStandard && w.BroadMoney.Value > 1e-9 && w.GoldStandardLaw)
        {
            double gcCoverage = w.BaseMoney / w.BroadMoney;
            double gcFloor = c.GoldCoverMin.Value * (1.0 + c.GoldCoverCreditRestraint);
            if (gcFloor > 1e-9 && gcCoverage < gcFloor) maxLoans *= gcCoverage / gcFloor;
        }
        var home = w.Region;
        double repayable = double.PositiveInfinity;
        if (home.InvestorPopIndex >= 0 && home.InvestorPopIndex < home.Pops.Count)
            repayable = Calendar.PerMonthToPerYear(Math.Max(0, home.Pops[home.InvestorPopIndex].LastEarnedPerMonth.Value)) * c.CreditRepayableYears;
        if (Instruments.On) Instruments.CreditCap(w, maxLoans, repayable, days);
        if (repayable < maxLoans) maxLoans = repayable;
        double run = PoliticalQuantities.ClampOr(c.BankRunSensitivity * Math.Max(0, c.ConfidencePivot - avgSat), 0, c.BankRunMax.Value, 0);
        bool crisis = bank.Heat > c.LolrHeatTrigger || w.GdpDrawdown.Value > c.LolrDrawdownTrigger.Value;
        if (crisis && w.FiscalFundingRatio > c.LolrMinFunding) run *= 1.0 - c.LolrStrength.Value;
        double lolrBoost = crisis && w.FiscalFundingRatio > c.LolrMinFunding ? c.LolrReserveBoost : 0;
        double effMaxLoans = maxLoans * (1.0 - run) * (1.0 + lolrBoost);
        if (repayable < effMaxLoans) effMaxLoans = repayable;
        bank.Heat = maxLoans > 1e-9 ? PoliticalQuantities.ClampOr(bank.Loans.Value / maxLoans, 0, HeatClampMax, 0) : 0;
        double targetLoans = effMaxLoans * confL * appetite;
        if (w.Explain is { } exCredit)
        {
            exCredit.Reset("CreditTarget");
            exCredit.Add("CreditTarget", "与信の上限（準備×信用乗数と返せる額の小さい方）", maxLoans);
            exCredit.Add("CreditTarget", "銀行取り付けと最後の貸し手", effMaxLoans - maxLoans);
            exCredit.Add("CreditTarget", "銀行の信頼（ミンスキー）", effMaxLoans * confL - effMaxLoans);
            exCredit.Add("CreditTarget", "政策金利（貸出意欲）", targetLoans - effMaxLoans * confL);
        }
        double adjSpd = c.CreditAdjustSpeed / div;
        if (days > 1) adjSpd = Calendar.OverSteps(adjSpd, days);
        double delta = adjSpd * (targetLoans - bank.Loans.Value);
        if (bank.Loans.Value + delta < 0) delta = -bank.Loans.Value;

        if (home.InvestorPopIndex >= 0 && home.InvestorPopIndex < home.Pops.Count)
        {
            var inv = home.Pops[home.InvestorPopIndex];
            double lent = Math.Max(delta, -Math.Max(0, inv.Savings.Value));
            if (lent > 0) World.Transfer((null, null), (w, [inv], [1.0]), lent, outside: World.Outside.Credit);
            else if (lent < 0) World.Transfer((w, [inv]), (null, null, null), -lent, outside: World.Outside.Credit);
        }
        else
        {
            var pops = Pops();
            var held = new List<double>(pops.Count); double heldSum = 0;
            foreach (var p in pops) { double h = Math.Max(0, p.Savings.Value); held.Add(h); heldSum += h; }
            double lent = Math.Max(delta, -heldSum);
            if (lent > 0) World.Transfer((null, null), (w, pops, held), lent, outside: World.Outside.Credit);
            else if (lent < 0) World.Transfer((w, pops), (null, null, null), -lent, outside: World.Outside.Credit);
        }

        double loanRate = w.LoanRatePerYear.Value;
        if (loanRate > 1e-12 && bank.Loans.Value > 1e-9)
        {
            double interestDue = Calendar.PerYearToPerDay(bank.Loans.Value * loanRate) * days;
            double paid;
            Pop? borrower = null;
            if (home.InvestorPopIndex >= 0 && home.InvestorPopIndex < home.Pops.Count)
            {
                borrower = home.Pops[home.InvestorPopIndex];
                double profitDaily = Calendar.PerMonthToPerDay(Math.Max(0, (borrower.EarnedPerMonth - borrower.PaidPerMonth).Value)) * days;
                paid = Math.Min(interestDue, Math.Min(profitDaily, Math.Max(0, borrower.Savings.Value)));
            }
            else
            {
                var pops = Pops();
                double tot = 0, profitTot = 0;
                foreach (var p in pops) { tot += Math.Max(0, p.Savings.Value); profitTot += Math.Max(0, (p.EarnedPerMonth - p.PaidPerMonth).Value); }
                paid = Math.Min(interestDue, Math.Min(Calendar.PerMonthToPerDay(profitTot) * days, tot));
            }
            double unpaidInterest = interestDue - paid;
            double capitalized = c.InterestCapitalization.Value > 0 && unpaidInterest > 1e-12 ? c.InterestCapitalization.Value * unpaidInterest : 0;
            if (paid > 0 || capitalized > 0)
            {
                var pops = Pops();
                var held = new List<double>(pops.Count); double heldSum = 0;
                foreach (var p in pops) { double h = Math.Max(0, ReferenceEquals(p, borrower) ? p.Savings.Value - paid : p.Savings.Value); held.Add(h); heldSum += h; }
                var weights = heldSum > 1e-9 ? held : null;
                if (paid > 0) World.Transfer((w, borrower is not null ? [borrower] : pops), (w, pops, weights), paid);
                if (capitalized > 0) World.Transfer((null, null), (w, pops, weights), capitalized, outside: World.Outside.Credit);
            }
        }
    }
}
