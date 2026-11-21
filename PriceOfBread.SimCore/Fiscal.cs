namespace PriceOfBread.SimCore;

public static class Fiscal
{
    private const double ForeignBondYieldRatioCap = 3.0;
    private static readonly RatioToGdpYear DebtToGdpYearUnbounded = new(DebtRatioUnbounded);
    private const double DebtRatioUnbounded = 1e9;

    public static void Step(World w, Coefficients c, double spare = 0, double devalued = 0, double unopenedRoom = 0)
    {
        if (!w.FiscalEnabled) return;
        double payRate = w.FiscalFundingRatio.Value;
        bool underLimit = w.LastInterestPerMonth < c.MaxInterestShareOfRevenue * w.LastRevenuePerMonth;
        double gdp = w.GdpPerMonth.Value, nominalGdp = w.NominalOf(gdp);



        double revenue = 0;
        Instruments.LandsaleStep(w);
        var scan = TaxScan.Of(w, c);
        var pops = scan.Pops;
        List<double>? scopeInc = scan.ScopeInc; List<double> ruleMul = scan.RuleMul, ruleCut = scan.RuleCut; List<Pop?> ruleLord = scan.RuleLord;
        double crownShareForPrivilege = CrownShareForPrivilege(w, c);
        double titheRate = c.TitheRate.Value * Privileges.TitheDegree(w, c);
        List<Pop>? clergy = null;
        if (titheRate > 1e-9)
        {
            clergy = [];
            double clergySize = 0;
            foreach (var p in pops) if (p.ClassId == PopClass.Clergy && p.Size > 1e-9) { clergy.Add(p); clergySize += p.Size; }
            if (!(clergySize > 1e-9)) clergy = null;
        }
        double titheCollected = 0;
        w.LastRuleCut = 0;
        var taxPayers = new List<Pop>(); var taxClaim = new List<double>();
        double paidInc = 0, paidProg = 0, paidPoll = 0, paidLand = 0;
        var tithePayers = new List<Pop>(); var titheClaim = new List<double>();
        List<(int Pi, double V)>? landSales = null;
        for (int pi = 0; pi < pops.Count; pi++)
        {
            var p = pops[pi]; if (p.IsCriminal) continue;
            var asd = AssessPop(w, c, p, crownShareForPrivilege, scopeInc is null ? null : scopeInc[pi], w.TaxRate.Value, w.ProgressiveTax, w.PollTax, w.LandTax);
            double privBase = asd.PrivBase, consent = asd.Consent;
            double tax = asd.Total;
            double kInc = asd.Income, kProg = asd.Progressive, kPoll = asd.Poll, kLand = asd.Land;
            if (c.PrivilegeLedgerEnabled > 0 && privBase < 1.0)
                Privileges.Record(w, Privileges.TaxExemption, (w.TaxRate.Value * Math.Max(0, p.LastEarnedPerMonth.Value) * consent + w.LandTax * Math.Max(0, LandTaxBase(c, p).Value)) * (1.0 - privBase));
            double cut = 0; Pop? lord = null;
            {
                double full = tax;
                tax = full * ruleMul[pi];
                if (ruleLord[pi] is { } lp && !ReferenceEquals(lp, p) && full * ruleCut[pi] > 1e-12) { cut = full * ruleCut[pi]; lord = lp; }
            }
            double tithe = clergy is not null ? TitheClaim(p, titheRate) : 0;
            double claims = tax + cut + tithe;
            if (!(claims > 0)) continue;
            double rp = PayRate(p, claims);
            if (rp < 1.0 && kLand > 0 && c.LandShareTaxSale > 0) (landSales ??= []).Add((pi, kLand * ruleMul[pi] * (1.0 - rp)));
            tax *= rp; cut *= rp; tithe *= rp;
            { double f = ruleMul[pi] * rp; paidInc += kInc * f; paidProg += kProg * f; paidPoll += kPoll * f; paidLand += kLand * f; }
            if (lord is not null && cut > 0)
            {
                double moved = World.Transfer((w, [p]), (w, [lord], [1.0]), cut, income: true);
                w.LastRuleCut += moved;
                if (c.PrivilegeLedgerEnabled > 0) Privileges.Record(w, Privileges.Seigneurial, moved);
            }
            if (tax > 0) { taxPayers.Add(p); taxClaim.Add(tax); }
            if (tithe > 0) { tithePayers.Add(p); titheClaim.Add(tithe); }
            titheCollected += tithe;
            revenue += tax;
        }
        revenue = World.Transfer((w, taxPayers), (w, null, null), revenue, TreasuryCat.Tax, fromWeights: taxClaim);
        w.LastTaxIncomePerMonth = new MoneyPerMonth(paidInc); w.LastTaxProgressivePerMonth = new MoneyPerMonth(paidProg);
        w.LastTaxPollPerMonth = new MoneyPerMonth(paidPoll); w.LastTaxLandPerMonth = new MoneyPerMonth(paidLand);

        if (clergy is not null && titheCollected > 0) titheCollected = World.Transfer((w, tithePayers), (w, clergy, null), titheCollected, income: true, fromWeights: titheClaim);
        if (landSales is not null) LandSale.Resolve(w, c, pops, landSales, crownShareForPrivilege);
        if (c.PrivilegeLedgerEnabled > 0) Privileges.Record(w, Privileges.Tithe, titheCollected);
        if (c.PrivilegeLedgerEnabled > 0) Privileges.StepMonth(w, c);


        double rollover = PoliticalQuantities.Clamp01(c.BondRolloverFracPerMonth.Value);
        w.GovAvgCouponPerYear = (1.0 - rollover) * w.GovAvgCouponPerYear + rollover * w.GovBondRatePerYear;
        double interest = Calendar.PerYearToPerMonth(w.GovDebt.Value * w.GovAvgCouponPerYear.Value);
        double goldDebtBurden = 1.0;
        if (w.MonetaryLaw == MonetaryStandardLaw.Silver && w.WorldSilverPrice > 1e-9)
        {
            goldDebtBurden = GoldDebtBurden(Catalog.RefPrices[Catalog.SilverId], w.WorldSilverPrice);
        }
        double foreignShare = w.GovDebt.Value > 1e-9 ? PoliticalQuantities.Clamp01(w.ForeignDebt / w.GovDebt) : 0;
        if (goldDebtBurden > 1.0) interest *= 1.0 + foreignShare * (goldDebtBurden - 1.0);
        double indirectRevenue = Math.Max(0, w.LastTariffRevenuePerClearing) / w.Coef.MonthsPerClearing;
        foreach (var r in w.Regions) indirectRevenue -= r.StateSpent[(int)TreasuryCat.StateMonopoly];
        double budgetBase = revenue + indirectRevenue;
        double adminClaim = c.AdminBudgetFrac.Value * w.AdminSpending * budgetBase * AdminDistanceMean(w);
        double militaryClaim = Math.Max(0, w.LastMilitaryClaimPerMonth.Value);
        double surplus = Math.Max(0, budgetBase - interest - adminClaim - militaryClaim);
        if (Instruments.On) Instruments.Revenue(w, revenue, indirectRevenue);
        w.LastRevenuePerMonth = new MoneyPerMonth(budgetBase); w.LastInterestPerMonth = new MoneyPerMonth(interest);
        w.LastAdminClaimPerMonth = new MoneyPerMonth(adminClaim);
        w.FiscalCapacityPerMonth = new MoneyPerMonth(w.FiscalCapacityPerMonth.Value < 1e-9 ? surplus : w.FiscalCapacityPerMonth.Value + c.FiscalCapacitySmoothing * (surplus - w.FiscalCapacityPerMonth.Value));


        if (w.CurrencyReformCooldown > 0) w.CurrencyReformCooldown--;
        bool broken = CurrencyMemoryBroken(w.CumulativeDevaluation, nominalGdp, c.CapitalFlightDistrustThreshold);
        if (w.PendingCurrencyReform > 0)
        {
            double parity = w.PendingCurrencyReform;
            w.PendingCurrencyReform = 0;
            if (w.MonetaryEnabled && !w.OnGoldStandard && broken
                && w.NoDevaluationMonths >= c.CurrencyReformCalmMonths
                && w.FiscalFundingRatio >= c.GoldStandardMinFunding
                && w.CurrencyReformCooldown <= 0)
            {
                double sav = 0; foreach (var p in pops) sav += Math.Max(0, p.Savings.Value);
                double burnMem = c.CurrencyReformBurnShare.Value * w.CumulativeDevaluation;
                double kp = parity * Math.Max(1e-9, w.PriceLevel);
                double burnParity = (w.TotalMoney * Math.Max(0, 1.0 - 1.0 / Math.Max(1e-9, kp))).Value;
                double burn = Math.Min(Math.Max(burnMem, burnParity), sav * c.CurrencyReformBurnCap.Value);
                double lo = c.GoldCoverMin.Value * c.GoldRejoinCoverMargin;
                double loans = w.OutstandingLoans.Value, tre = Math.Max(0, w.Treasury.Value);
                if (loans > tre && lo < 1.0) burn = Math.Min(burn, Math.Max(0, w.TotalMoney.Value - (loans - tre) / (1.0 - lo)));
                if (burn > 1e-12 && sav > 1e-9)
                {
                    burn = World.Transfer((w, pops), (null, null, null), burn, outside: World.Outside.Fiat, fromHeld: sav);
                    double bite = burn / sav;
                    foreach (var p in pops) p.Militancy = Math.Min(1, p.Militancy + bite);
                }
                double forgiveRatio = nominalGdp > 1e-9 ? Math.Min(1.0, w.CumulativeDevaluation / nominalGdp) : 0;
                w.CumulativeDevaluation *= 1.0 - PoliticalQuantities.Clamp01(c.CurrencyReformForgiveShare.Value);
                double legBefore = w.Legitimacy;
                w.Legitimacy = Math.Max(0, w.Legitimacy - c.CurrencyReformLegitimacyHit * forgiveRatio);
                w.Explain?.Add("Legitimacy", "通貨改革(預金を焼いて記憶を消す)", w.Legitimacy - legBefore);
                w.GoldParity = parity;
                w.CurrencyReformCooldown = c.CurrencyReformCooldownMonths;
                w.LogAdd($"{w.Year} {w.SelfPower?.Name}が通貨改革（新平価{parity:F2}・焼却{burn:F0}）");
            }
        }

        double interestPaid = interest * payRate;
        double ForeignFrac(double share) => goldDebtBurden > 1.0
            ? share * goldDebtBurden / Math.Max(1e-12, 1.0 + share * (goldDebtBurden - 1.0))
            : share;
        double foreignDebtFrac = ForeignFrac(foreignShare);
        double foreignInt = interestPaid * foreignDebtFrac;
        double domInt = interestPaid - foreignInt;
        if (domInt > 1e-12) { var (holders, held) = BondHolders(pops); World.Transfer((w, null), (w, holders, held), domInt, TreasuryCat.Interest); }
        World.Transfer((w, null), (null, null, null), foreignInt, TreasuryCat.Interest);
        w.ForeignIntPaidThisQuarter += foreignInt;
        double unpaidInt = interest - interestPaid;
        if (unpaidInt > 1e-12 && !w.WarmupFrozen)
        {
            double unpaidForeign = unpaidInt * foreignDebtFrac;
            double unpaidDom = unpaidInt - unpaidForeign;
            var unpaidForeignStock = new MoneyStock(unpaidForeign);
            w.GovDebt += unpaidForeignStock; w.ForeignDebt += unpaidForeignStock;
            MoneyStock held = default; foreach (var p in pops) if (p.BondHoldings.Value > 0) held += p.BondHoldings;
            if (held.Value > 0 && unpaidDom > 0)
            {
                double grow = 1.0 + unpaidDom / held.Value;
                foreach (var p in pops) if (p.BondHoldings.Value > 0) p.BondHoldings *= grow;
                w.GovDebt += new MoneyStock(unpaidDom);
            }
        }



        if (w.GovDebt.Value > 1e-9 && payRate >= 1 && spare > 0 && !w.WarmupFrozen)
        {
            double floorRepay = Math.Min(w.GovDebt.Value * c.BondRepayFracPerMonth.Value, spare);
            double excess = w.Treasury.Value - UtilityAgent.OpulenceLineOf(w);
            if (!(excess > 0)) excess = 0;
            double repay = Math.Min(w.GovDebt.Value, Math.Min(spare, Math.Max(floorRepay, excess)));
            if (Instruments.On) Instruments.FsxRepay(w, floorRepay, repay, excess);
            double shareNow = PoliticalQuantities.Clamp01(w.ForeignDebt / w.GovDebt);
            double frForeign = repay * shareNow;
            MoneyStock domBefore = w.GovDebt - w.ForeignDebt;
            w.GovDebt -= new MoneyStock(repay); w.ForeignDebt = new MoneyStock(Math.Max(0, w.ForeignDebt.Value - frForeign));
            var (holders, held) = BondHolders(pops);
            double heldTotal = 0; foreach (var h in held) heldTotal += h;
            World.Transfer((w, null), (w, holders, held), Math.Max(0, repay - frForeign), TreasuryCat.DebtRepay);
            double principal = (domBefore - (w.GovDebt - w.ForeignDebt)).Value;
            if (principal > 0 && heldTotal > 0)
            {
                double keep = principal < heldTotal ? 1.0 - principal / heldTotal : 0.0;
                foreach (var p in pops) p.BondHoldings *= keep;
            }
            World.Transfer((w, null), (null, null, null), frForeign, TreasuryCat.DebtRepay);
            w.ForeignIntPaidThisQuarter += frForeign;
        }

        if (w.BorrowCooldownMonths > 0) w.BorrowCooldownMonths--;
        if (payRate < 1 && w.GovDebt.Value > 1e-9 && !underLimit && !w.WarmupFrozen) w.InsolvencyMonths += ArrearsOfMonth(w, c, payRate, spare, unopenedRoom); else w.InsolvencyMonths = 0;
        if (w.InsolvencyMonths >= c.DefaultArrearsMonths)
        {
            RepudiateDebt(w, c);
            w.InsolvencyMonths = 0; w.HadSovereignDefault = true; w.SovereignDefaultCount++;
            if (w.Bank is { } dbank) { dbank.Confidence = 0; dbank.Heat = 0; }
            double legBeforeDefault = w.Legitimacy;
            w.Legitimacy = Math.Max(0, w.Legitimacy - c.DefaultLegitimacyHit);
            w.Explain?.Add("Legitimacy", "国家破産（デフォルト）の信認崩壊", w.Legitimacy - legBeforeDefault);
        }
        MoneyPerYear gdpPerYear = Calendar.PerMonthToPerYear(new MoneyPerMonth(nominalGdp));
        double gdpDenom = gdpPerYear.Value;
        RatioToGdpYear dGdp = gdpDenom > 1e-9 ? Calendar.ToGdpYear(w.GovDebt, gdpPerYear) : (w.GovDebt.Value > 0 ? DebtToGdpYearUnbounded : default);
        if (!(dGdp <= DebtToGdpYearUnbounded)) dGdp = DebtToGdpYearUnbounded;
        if (w.MonetaryEnabled)
        {
            double coverage = w.BroadMoney.Value > 1e-9 ? w.BaseMoney / w.BroadMoney : 1.0;
            if (w.OnGoldStandard)
            {
                if (!w.MetallicStandardLaw || w.FiscalFundingRatio.Value < c.GoldSuspendMinFundingRatio || coverage < c.GoldCoverMin.Value)
                {
                    w.OnGoldStandard = false; w.GoldStandardSuspendedInCrisis = w.MetallicStandardLaw;
                    if (w.GoldStandardSuspendedInCrisis)
                    {
                        bool coverTrip = coverage < c.GoldCoverMin.Value;
                        w.GoldSuspensions++; if (coverTrip) w.GoldCoverSuspensions++;
                        w.LogAdd($"{w.Year} {w.SelfPower?.Name}が兌換停止（金本位から強制離脱・{(coverTrip ? $"正貨準備カバー率{coverage:P0}<{c.GoldCoverMin:P0}" : "財政不足")}）");
                    }
                }
            }
            else if (w.MetallicStandardLaw && w.FiscalFundingRatio >= c.GoldStandardMinFunding
                     && coverage >= c.GoldCoverMin.Value * c.GoldRejoinCoverMargin && !broken)
            { w.OnGoldStandard = true; w.GoldStandardSuspendedInCrisis = false; }
        }
        double fisher = Math.Max(c.FisherDeflationFloor, w.ExpectedInflationPerYear.Value) * c.FisherInflationWeight;
        double convDiscount = 1.0 - w.Convertibility * (1.0 - c.GoldStandardRateDiscount);
        double devalSpeed = devalued > 0 ? (gdpDenom > 1e-9 ? Calendar.PerMonthToPerYear(devalued) / gdpDenom : DebtRatioUnbounded) : 0;
        if (!(devalSpeed <= DebtRatioUnbounded)) devalSpeed = DebtRatioUnbounded;
        double distrust = c.BondRateDistrustScale * devalSpeed;
        double bondInside = c.BondBaseRatePerYear.Value + c.BondRateDebtScale * dGdp.Value + fisher + distrust + c.PolicyRateBondWeight * w.InterestRate;
        double reprDiscount = 1.0 - c.BondRateRepresentationDiscount.Value * PoliticalQuantities.Clamp01(w.ParliamentControl);
        double bondPre = bondInside * convDiscount * reprDiscount;
        w.GovBondRatePerYear = new RatePerYear(Math.Min(c.BondRateMaxPerYear.Value, bondPre));
        if (w.Explain is { } bl)
        {
            bl.Reset("BondRate");
            bl.Add("BondRate", "基礎金利", (c.BondBaseRatePerYear * convDiscount).Value);
            bl.Add("BondRate", "債務比プレミアム", c.BondRateDebtScale * dGdp.Value * convDiscount);
            bl.Add("BondRate", "インフレ期待", fisher * convDiscount);
            bl.Add("BondRate", "信認喪失（直近の切り下げ）", distrust * convDiscount);
            bl.Add("BondRate", "政策金利", c.PolicyRateBondWeight * w.InterestRate * convDiscount);
            bl.Add("BondRate", "金本位の信認割引", bondInside * (convDiscount - 1.0));
            bl.Add("BondRate", "議会の信認割引", bondInside * convDiscount * (reprDiscount - 1.0));
            bl.Add("BondRate", "上限クランプ", w.GovBondRatePerYear.Value - bondPre);
        }
        if (Instruments.On) Instruments.BondRate(w, (c.BondBaseRatePerYear * convDiscount).Value, c.BondRateDebtScale * dGdp.Value * convDiscount, fisher * convDiscount, distrust * convDiscount, c.PolicyRateBondWeight * w.InterestRate * convDiscount,
            bondInside * (convDiscount - 1.0), bondInside * convDiscount * (reprDiscount - 1.0), w.GovBondRatePerYear.Value - bondPre, dGdp.Value, convDiscount, reprDiscount);


        if (w.SocialSecurity > 1e-9)
        {
            var masses = new List<Pop>();
            foreach (var p in pops) if (!p.Closed && !p.IsModernElite) masses.Add(p);
            double massSize = 0; foreach (var p in masses) massSize += p.Size;
            double welfareReq = c.WelfareCostRatePerMonth * w.SocialSecurity * massSize;
            double welfareClaim = Math.Min(welfareReq, (c.WelfareRevenueShare * w.FiscalCapacityPerMonth).Value);
            double welfareCost = welfareClaim * payRate;
            w.LastWelfareCostPerMonth = new MoneyPerMonth(welfareClaim);
            World.Transfer((w, null), (w, masses, null), welfareCost, TreasuryCat.Welfare);
        }
        else w.LastWelfareCostPerMonth = new MoneyPerMonth(0);

        double pensionLevel = w.Pension switch { ReformLevel.Full => 1.0, ReformLevel.Limited => 0.5, _ => 0.0 };
        if (pensionLevel > 1e-9)
        {
            double oldSize = 0; foreach (var p in pops) oldSize += p.Size * p.AgeShare(60, double.PositiveInfinity);
            double pensionReq = c.PensionCostRatePerMonth * pensionLevel * oldSize;
            double pensionClaim = Math.Min(pensionReq, (c.WelfareRevenueShare * w.FiscalCapacityPerMonth).Value);
            double pensionCost = pensionClaim * payRate;
            w.LastPensionCostPerMonth = new MoneyPerMonth(pensionClaim);
            if (pensionCost > 0)
            {
                var elderly = new List<Pop>(); var elderW = new List<double>();
                double elderSize = 0, reachW = 0;
                foreach (var rg in w.Regions)
                {
                    double reach = Politics.RuleReach(rg);
                    foreach (var p in rg.Pops)
                    {
                        double os = p.AgeShare(60, double.PositiveInfinity);
                        if (os > 1e-9) { elderly.Add(p); elderW.Add(p.Size * os * reach); elderSize += p.Size * os; reachW += p.Size * os * reach; }
                    }
                }
                double reached = elderSize > 1e-9 ? pensionCost * (reachW / elderSize) : 0;
                if (Instruments.On && reachW > 1e-9)
                {
                    double soldierW = 0; for (int ei = 0; ei < elderly.Count; ei++) if (elderly[ei].IsSoldier) soldierW += elderW[ei];
                    w.DiagSfPensionToSoldiersCum += reached * (soldierW / reachW);
                }
                World.Transfer((w, null), (w, elderly, elderW), reached, TreasuryCat.Pension);
            }
        }
        else w.LastPensionCostPerMonth = new MoneyPerMonth(0);
    }

    internal static double AdminDistanceMul(World w, Region r)
    {
        var rc = Places.Coord(r.Name);
        if (rc.Lat == 0 && rc.Lon == 0) return 1.0;
        double km = Geo.DistanceKm(w.Latitude, w.Longitude, rc.Lat, rc.Lon);
        return AdminDistanceMulOfKm(km, w.Coef.DistanceWarDecay);
    }

    internal static double AdminDistanceMulOfKm(double km, double decay)
        => km > 0 && double.IsFinite(km) ? 1.0 + decay * km / Diplomacy.DistanceNormKm : 1.0;

    public static double AdminDistanceMean(World w)
    {
        double s = 0, n = 0;
        foreach (var r in w.Regions)
        {
            double rp = r.SumPopulation();
            if (!(rp > 0)) continue;
            s += rp * AdminDistanceMul(w, r); n += rp;
        }
        return n > 0 ? s / n : 1.0;
    }

    internal static double GoldDebtBurden(double refSilver, double worldSilverPrice)
    {
        double b = refSilver > 1e-9 ? refSilver / worldSilverPrice : 1.0;
        if (!(b >= 1.0 && b < double.PositiveInfinity)) b = 1.0;
        return b;
    }

    internal static bool BorrowGateOpen(World w, Coefficients c)
        => !(w.BorrowCooldownMonths > 0) && !w.WarmupFrozen && w.LastInterestPerMonth < c.MaxInterestShareOfRevenue * w.LastRevenuePerMonth;

    internal static double DevaluationAllowed(double room, double cap)
    {
        if (!(cap > 0) || !(room > cap)) return Math.Min(room, cap);
        return room;
    }

    internal static double FiatPressureOfLedger(double devalue, double savingsBefore, double issue)
    {
        double den = savingsBefore + issue;
        return den > 0 ? devalue / den : 0.0;
    }

    internal static bool CurrencyMemoryBroken(double cumulativeDevaluation, double nominalGdpPerMonth, double distrustThresholdPerMonthGdp)
        => nominalGdpPerMonth > 1e-9
           && Calendar.ToGdpYear(new MoneyStock(cumulativeDevaluation), Calendar.PerMonthToPerYear(new MoneyPerMonth(nominalGdpPerMonth))).Value
              > Calendar.PerYearToPerMonth(distrustThresholdPerMonthGdp);

    internal static double BorrowTicket(double nominalGdp, Coefficients c) => Math.Max(0, nominalGdp) * c.BorrowFractionOfGdp.Value;

    internal static double Borrow(World w, Coefficients c, double spare)
    {
        double shortfall = spare < 0 ? -spare : 0;
        if (Instruments.On && shortfall > 0 && !BorrowGateOpen(w, c)) Instruments.Gate(w, shortfall);
        if (!(shortfall > 0) || !BorrowGateOpen(w, c)) return 0;
        double nominalGdp = w.NominalGdpPerMonth.Value;
        double borrowWant = Math.Min(BorrowTicket(nominalGdp, c), shortfall);
        var capitalists = new List<Pop>();
        foreach (var (_, p) in w.AllPopsWithRegion()) if (CapitalMarket.IsInvestor(p)) capitalists.Add(p);
        double capSavings = 0; foreach (var p in capitalists) capSavings += Math.Max(0, p.Savings.Value);
        double openness = w.CapitalControlLaw ? PoliticalQuantities.Clamp01(c.CapitalControlOpenness.Value) : 1.0;
        double domesticPool = capSavings * c.BondAppetite.Value;
        double foreignPool = openness * c.ForeignBondCapacity.Value * Math.Max(0, nominalGdp) * Math.Clamp(c.BondBaseRatePerYear.Value / Math.Max(1e-9, w.GovBondRatePerYear.Value), 0, ForeignBondYieldRatioCap);
        double foreignPoolRaw = foreignPool;
        foreignPool = Math.Min(foreignPool, Math.Max(0.0, w.ForeignBondInvestorPool));
        double pool = domesticPool + foreignPool;
        double borrowed = Math.Min(borrowWant, pool);
        if (Instruments.On) Instruments.Borrow(w, shortfall, BorrowTicket(nominalGdp, c), domesticPool, foreignPool, borrowed, foreignPoolRaw);
        if (!(borrowed > 1e-9)) return 0;
        double foreignPortion = pool > 1e-9 ? borrowed * (foreignPool / pool) : 0;
        double domesticPortion = borrowed - foreignPortion;
        double domesticMoved = 0;
        if (domesticPortion > 0 && capSavings > 0)
        {
            var paid = new List<double>(capitalists.Count);
            domesticMoved = World.Transfer((w, capitalists), (w, null, null), domesticPortion, TreasuryCat.Borrow, taken: paid);
            for (int i = 0; i < paid.Count; i++) capitalists[i].BondHoldings += new MoneyStock(paid[i]);
        }
        World.Transfer((null, null), (w, null, null), foreignPortion, TreasuryCat.Borrow);
        w.PendingForeignBorrow += foreignPortion;
        double added = domesticMoved + foreignPortion;
        w.GovDebt += new MoneyStock(added); var foreignPortionStock = new MoneyStock(foreignPortion); w.ForeignDebt += foreignPortionStock;
        w.PrevForeignDebt += foreignPortionStock;
        { double debtBefore = w.GovDebt.Value - added; w.GovAvgCouponPerYear = (debtBefore * w.GovAvgCouponPerYear + added * w.GovBondRatePerYear) / System.Math.Max(1e-9, w.GovDebt.Value); }
        return domesticMoved > 0 ? borrowed : foreignPortion;
    }

    internal sealed class TaxScan
    {
        internal readonly List<Pop> Pops = [];
        internal List<double>? ScopeInc;
        internal readonly List<double> RuleMul = [], RuleCut = [];
        internal readonly List<Pop?> RuleLord = [];

        internal static TaxScan Of(World w, Coefficients c)
        {
            var s = new TaxScan();
            bool scopeOn = c.TaxConsentWeight > 0;
            double ruleDeliver = 1.0 - PoliticalQuantities.Clamp01(c.PrivilegeTaxDiscount.Value);
            if (scopeOn) s.ScopeInc = [];
            foreach (var r in w.Regions)
            {
                Pop? lord = null;
                foreach (var lp in r.Pops) if (lp.ClassId == PopClass.Noble && !lp.IsSoldier && lp.Size > 1e-9) { lord = lp; break; }
                double mul = Politics.RuleDelivery(r, c);
                double cut = r.Rule.Length == 4
                    ? (PoliticalQuantities.Clamp01(r.Rule[1]) + PoliticalQuantities.Clamp01(r.Rule[0]) * (1.0 - PoliticalQuantities.Clamp01(r.AdminFill))) * (1.0 - ruleDeliver)
                    : 0;
                foreach (var p in r.Pops)
                {
                    s.Pops.Add(p);
                    s.ScopeInc?.Add(InterestGroups.SuffrageInclusion(w, r, p));
                    s.RuleMul.Add(mul); s.RuleCut.Add(lord is not null ? cut : 0); s.RuleLord.Add(lord);
                }
            }
            return s;
        }
    }

    internal static TaxAssessed AssessPop(World w, Coefficients c, Pop p, double crownShare, double? scopeInc, double incomeRate, double progressiveRate, double pollRate, double landRate)
    {
        double privBase = Privileges.TaxAssessment(w, c, p, crownShare);
        double consent = 1.0;
        if (c.TaxConsentWeight > 0)
        {
            double voice = InterestGroups.FranchiseVoice(p, w.Suffrage, c);
            if (scopeInc is { } si) voice *= si;
            consent = 1.0 - c.TaxConsentWeight * (1.0 - voice);
        }
        double tax = incomeRate * Math.Max(0, p.LastEarnedPerMonth.Value) * privBase * consent;
        double kInc = tax, kProg = 0, kPoll = 0, kLand = 0;
        if (progressiveRate > 1e-9 && (p.IsModernElite || p.Closed) && !p.IsSoldier)
        { kProg = progressiveRate * Math.Max(0, p.LastEarnedPerMonth.Value) * consent; tax += kProg; }
        if (pollRate > 1e-9 && p.IsCommoner)
        { kPoll = c.PollTaxRate * pollRate * p.Size; tax += kPoll; }
        var landBase = LandTaxBase(c, p);
        if (landRate > 1e-9 && landBase.Value > 0)
        { kLand = (landRate * landBase * privBase).Value; tax += kLand; }
        return new TaxAssessed(privBase, consent, kInc, kProg, kPoll, kLand, tax);
    }

    internal static MoneyPerMonth LandTaxBase(Coefficients c, Pop p) => c.LandTaxFixedBasis > 0 ? p.LandRentPerMonth + p.LandTaxBasisAdjPerMonth : p.LandRentPerMonth;

    internal readonly record struct TaxAssessed(double PrivBase, double Consent, double Income, double Progressive, double Poll, double Land, double Total);

    internal static double CrownShareForPrivilege(World w, Coefficients c)
        => c.PrivilegeFromCrownShare.Value > 0 && c.PrivilegeTaxDiscount.Value > 0 ? PowerAffinity.Shares(w)[0] : 0;

    internal static double TitheClaim(Pop p, double titheRate)
        => Politics.IsMassPop(p) && p.LastEarnedPerMonth.Value > 0 ? (titheRate * p.LastEarnedPerMonth).Value : 0;

    internal static double PayRate(Pop p, double claims)
    {
        double held = Math.Max(0, p.Savings.Value);
        return held >= claims ? 1.0 : held / claims;
    }

    internal static (double Income, double Progressive, double Poll, double Land) ForecastTax(World w, Coefficients c, double incomeRate, double progressiveRate, double pollRate, double landRate)
    {
        var scan = TaxScan.Of(w, c);
        var pops = scan.Pops;
        double crownShare = CrownShareForPrivilege(w, c);
        double titheRate = c.TitheRate.Value * Privileges.TitheDegree(w, c);
        bool clergy = false;
        if (titheRate > 1e-9) foreach (var p in pops) if (p.ClassId == PopClass.Clergy && p.Size > 1e-9) { clergy = true; break; }
        double paidInc = 0, paidProg = 0, paidPoll = 0, paidLand = 0;
        for (int pi = 0; pi < pops.Count; pi++)
        {
            var p = pops[pi]; if (p.IsCriminal) continue;
            var a = AssessPop(w, c, p, crownShare, scan.ScopeInc is null ? null : scan.ScopeInc[pi], incomeRate, progressiveRate, pollRate, landRate);
            double tax = a.Total * scan.RuleMul[pi], cut = 0;
            if (scan.RuleLord[pi] is { } lp && !ReferenceEquals(lp, p) && a.Total * scan.RuleCut[pi] > 1e-12) cut = a.Total * scan.RuleCut[pi];
            double claims = tax + cut + (clergy ? TitheClaim(p, titheRate) : 0);
            if (!(claims > 0)) continue;
            double f = scan.RuleMul[pi] * PayRate(p, claims);
            paidInc += a.Income * f; paidProg += a.Progressive * f; paidPoll += a.Poll * f; paidLand += a.Land * f;
        }
        return (paidInc, paidProg, paidPoll, paidLand);
    }

    private static (List<Pop> Holders, List<double> Held) BondHolders(List<Pop> pops)
    {
        var holders = new List<Pop>(); var held = new List<double>();
        foreach (var p in pops) if (p.BondHoldings.Value > 0) { holders.Add(p); held.Add(p.BondHoldings.Value); }
        return (holders, held);
    }

    internal static void BookForeignDebt(Pop? p, MoneyStock h, World issuer, World holderNation)
    {
        if (!(h.Value > 0)) return;
        if (p is null) issuer.GovDebt += h; else p.BondHoldings -= h;
        issuer.ForeignDebt += h; issuer.PrevForeignDebt += h;
        holderNation.ForeignBondClaims += h * issuer.FxRate / holderNation.FxRate;
    }

    /// <summary>(1250) 不履行の累計に積むその月の未払い（月の請求の割合）。旧＝1−r。旗 ArrearsBeyondOpenedRoom＞0 で金本位でない月は、
    /// 払えない額（−spare＝切り下げの後）のうち、兌換の遅れでまだ開いていない切り下げの余地（unopenedRoom＝deficit−余地）の分を数えない
    /// ＝1−r に (1−unopenedRoom÷払えない額) を掛ける（＝余地−切り下げ額 の分だけ）。unopenedRoom 0 は旧と同じ値。負は 0・NaN は旧の 1−r。</summary>
    internal static double ArrearsOfMonth(World w, Coefficients c, double payRate, double spare, double unopenedRoom)
    {
        double unpaid = 1.0 - payRate;
        if (!(c.ArrearsBeyondOpenedRoom > 0) || w.OnGoldStandard || !(-spare > 0) || !(unopenedRoom >= 0)) return unpaid;
        double left = 1.0 - unopenedRoom / -spare;
        return left > 0 ? unpaid * left : 0;
    }

    internal static void RepudiateDebt(World w, Coefficients c)
    {
        w.GovDebt = new MoneyStock(0); w.ForeignDebt = default;
        foreach (var (_, p) in w.AllPopsWithRegion()) p.BondHoldings = default;
        w.OnGoldStandard = false; w.GoldStandardSuspendedInCrisis = true;
        w.BorrowCooldownMonths = c.DefaultCooldownMonths;
    }

    internal static void SeedBondHoldings(World w)
    {
        MoneyStock dom = w.GovDebt - w.ForeignDebt, held = default; double invSav = 0, size = 0;
        foreach (var (_, p) in w.AllPopsWithRegion()) { held += p.BondHoldings; size += p.Size; if (CapitalMarket.IsInvestor(p)) invSav += Math.Max(0, p.Savings.Value); }
        if (held.Value > 0 || !(dom.Value > 0) || BondLedger.Balanced(w, held)) return;
        foreach (var r in w.Regions)
            foreach (var p in r.Pops)
                p.BondHoldings += dom * (invSav > 0 ? (CapitalMarket.IsInvestor(p) ? Math.Max(0, p.Savings.Value) / invSav : 0) : (size > 0 ? p.Size / size : 0));
    }
}
