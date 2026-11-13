using System.Collections.Generic;

namespace PriceOfBread.SimCore;

public static class CapitalMarket
{
    private static readonly RatioToGdpYear NoGdpDistressDebtRatio = new(9);
    private const double BondConfMin = 0.2;

    internal static double Headroom(Coefficients c, World w)
        => c.CapitalHeadroomFromReturn > 0 ? c.CapitalIndTarget * w.ModernizeReturnShare()
                                           : System.Math.Max(0, c.CapitalIndTarget - w.MeanIndustrialization);

    public static void Step(IReadOnlyList<Simulation> nations, Coefficients c)
    {
        int n = nations.Count;
        if (n < 2) return;
        var w = new World[n];
        double[]? probeD = null;
        void ProbeSnap()
        {
            if (!Simulation.ConservationProbe) return;
            probeD ??= new double[n];
            for (int i = 0; i < n; i++) probeD[i] = (w[i].BaseMoney - w[i].CumulativeTradeFlow - w[i].CumulativeGoldMinted - w[i].CumulativeFiatIssued).Value;
        }
        void ProbeDiff(string label)
        {
            if (!Simulation.ConservationProbe || probeD is null) return;
            for (int i = 0; i < n; i++)
            {
                double now = (w[i].BaseMoney - w[i].CumulativeTradeFlow - w[i].CumulativeGoldMinted - w[i].CumulativeFiatIssued).Value;
                if (System.Math.Abs(now - probeD[i]) > 1e-4)
                    System.Console.Error.WriteLine($"[PROBE金融] {label} {w[i].Region.Name} {w[i].Year}年 ΔD={now - probeD[i]:F6}");
                probeD[i] = now;
            }
        }
        var ret = new double[n];
        var cred = new double[n];
        var ngdp = new double[n];
        var e = new double[n];
        double fx = 0;
        for (int i = 0; i < n; i++)
        {
            w[i] = nations[i].World;
            e[i] = w[i].FxRate;
            ngdp[i] = w[i].NominalGdpPerMonth.Value;
            RatioToGdpYear debtR = ngdp[i] > 1e-9 ? Calendar.ToGdpYear(w[i].GovDebt, Calendar.PerMonthToPerYear(new MoneyPerMonth(ngdp[i]))) : (w[i].GovDebt.Value > 0 ? NoGdpDistressDebtRatio : default);
            if (!(debtR <= NoGdpDistressDebtRatio)) debtR = NoGdpDistressDebtRatio;
            double bondSpread = System.Math.Max(0, (w[i].GovBondRatePerYear - c.BondBaseRatePerYear).Value);
            double bondConf = System.Math.Clamp(1.0 - c.BondYieldFlightWeight * bondSpread, BondConfMin, 1.0);
            double fxCredit = System.Math.Clamp(w[i].ExchangeRate, c.ExchangeRateFloor, 1.0);
            double cr = PoliticalQuantities.Clamp01(1.0 - debtR / c.DebtDistressRatio)
                      * (c.OffGoldCapitalPenalty + (1.0 - c.OffGoldCapitalPenalty) * w[i].Convertibility)
                      * fxCredit
                      * bondConf;
            cred[i] = cr;
            w[i].CreditRating = cr;
            w[i].CreditDebtTerm = PoliticalQuantities.Clamp01(1.0 - debtR / c.DebtDistressRatio);
            w[i].CreditGoldTerm = c.OffGoldCapitalPenalty + (1.0 - c.OffGoldCapitalPenalty) * w[i].Convertibility;
            w[i].CreditFxTerm = fxCredit;
            w[i].CreditBondTerm = bondConf;
            double headroom = Headroom(c, w[i]);
            double abs = c.CapitalCreditFloor + (1.0 - c.CapitalCreditFloor) * TechEducation.AbsorptionOf(w[i], c);
            headroom *= 1.0 + (abs - 1.0);
            ret[i] = headroom * (c.CapitalCreditFloor + (1.0 - c.CapitalCreditFloor) * cr) + c.PolicyRateCapitalDraw * w[i].InterestRate * cr;
            if (w[i].Explain is { } exCap)
            {
                exCap.Reset("CapitalReturn");
                exCap.Add("CapitalReturn", "工業化の伸びしろ（信用でゲート）", headroom * (c.CapitalCreditFloor + (1.0 - c.CapitalCreditFloor) * cr));
                exCap.Add("CapitalReturn", "政策金利（高金利通貨への資本移動）", c.PolicyRateCapitalDraw * w[i].InterestRate * cr);
            }
        }
        double worldRate = 0, worldCred = 0; for (int i = 0; i < n; i++) { worldRate += ret[i]; worldCred += cred[i]; } worldRate /= n; worldCred /= n;
        double riskAppetite = PoliticalQuantities.Clamp01(1.0 - c.CapitalContagion * (1.0 - worldCred));

        double flightTotal = 0;
        var taken = new double[n];
        ProbeSnap();
        for (int i = 0; i < n; i++)
        {
            if (c.CapitalDomesticFlightRatePerQuarter.Value > 0 && ngdp[i] > 1e-9)
            {
                double distrust = PoliticalQuantities.Clamp01(w[i].CumulativeDevaluation / ngdp[i] - c.CapitalFlightDistrustThreshold);
                double credSum = 0;
                if (distrust > 0)
                    for (int h = 0; h < n; h++) if (h != i) credSum += cred[h] * System.Math.Max(0, ngdp[h]) * e[h];
                if (credSum > 1e-9)
                {
                    double want = c.CapitalDomesticFlightRatePerQuarter.Value * distrust * InvestorSavings(w[i]);
                    double got = TakeGold(w[i], want);
                    w[i].CapitalAbroad += new MoneyStock(got);
                    fx -= got;
                    for (int h = 0; h < n; h++)
                    {
                        if (h == i) continue;
                        double k = got * e[i] * (cred[h] * System.Math.Max(0, ngdp[h]) * e[h] / credSum) / e[h];
                        GiveGold(w[h], k);
                        w[h].ForeignCapitalIn += new MoneyStock(k);
                        fx += k;
                    }
                }
            }
            if (Instruments.On) Instruments.CpwdMark(nations, i, 0, flightTotal);
            double flight = System.Math.Min(w[i].ForeignCapitalIn.Value, (w[i].ForeignCapitalIn * c.CapitalFlightRatePerQuarter.Value * (1.0 - cred[i])).Value);
            double fciPreFlight = w[i].ForeignCapitalIn.Value, ftPreFlight = flightTotal;
            if (flight > 1e-9) { taken[i] += TakeGold(w[i], flight); w[i].ForeignCapitalIn -= new MoneyStock(flight); flightTotal += flight * e[i]; }
            if (c.CapitalClaimCancelFromLedger > 0 && flight > 1e-9) flightTotal = ftPreFlight + (fciPreFlight - w[i].ForeignCapitalIn.Value) * e[i];
            if (Instruments.On) Instruments.CpwdMark(nations, i, 1, flightTotal);
            double divFci0 = w[i].ForeignCapitalIn.Value, divSav0 = Instruments.On ? InvestorSavings(w[i]) : 0; double div = c.CapitalDividendFromHostRent > 0 ? HostRentDividend(w[i]) : (w[i].ForeignCapitalIn * c.CapitalDividendRatePerQuarter.Value).Value;
            double divPaid0 = 0; if (div > 1e-9) { divPaid0 = TakeGold(w[i], div); taken[i] += divPaid0; } if (Instruments.On) Instruments.DivhostRecord(nations, i, e[i], divFci0, divSav0, div, divPaid0);
            if (c.CapitalCouponUnpaidWriteDown > 0) { double divWo = System.Math.Min(div - divPaid0, w[i].ForeignCapitalIn.Value); if (divWo > 1e-9) { double fciPreWo = w[i].ForeignCapitalIn.Value; w[i].ForeignCapitalIn -= new MoneyStock(divWo); flightTotal += (c.CapitalClaimCancelFromLedger > 0 ? fciPreWo - w[i].ForeignCapitalIn.Value : divWo) * e[i]; } }
            if (Instruments.On) Instruments.CpwdRow(nations, i, divPaid0, flightTotal);
        }
        ProbeDiff("逃避配当");
        if (Instruments.On) Instruments.CpwdOwner(nations, 0);
        double abroadSum = 0, backTotal = 0, takenSum = 0; for (int i = 0; i < n; i++) { abroadSum += (w[i].CapitalAbroad * e[i]).Value; backTotal += taken[i] * e[i]; takenSum += taken[i]; }
        if (abroadSum > 1e-9 && backTotal + flightTotal > 1e-9)
        {
            fx -= takenSum;
            for (int i = 0; i < n; i++)
            {
                double share = (w[i].CapitalAbroad * e[i]).Value / abroadSum;
                w[i].CapitalAbroad = new MoneyStock(System.Math.Max(0, w[i].CapitalAbroad.Value - flightTotal * share / e[i]));
                double k = backTotal * share / e[i];
                GiveGold(w[i], k);
                fx += k;
            }
        }
        else if (takenSum > 1e-9)
            for (int i = 0; i < n; i++)
                if (taken[i] > 0) GiveGold(w[i], taken[i]);
        if (Instruments.On) Instruments.CpwdOwner(nations, 1);
        ProbeDiff("還流");

        var demand = new double[n]; var supply = new double[n];
        double totalDemand = 0, totalSupply = 0;
        for (int i = 0; i < n; i++)
        {
            if (ret[i] > worldRate)
            {
                demand[i] = (ret[i] - worldRate) * System.Math.Max(0, ngdp[i]) * e[i] * c.CapitalDemandScale;
                totalDemand += demand[i];
            }
            else
            {
                double exportable = InvestorSavings(w[i]) * c.CapitalExportFrac.Value;
                supply[i] = (worldRate - ret[i]) * exportable * e[i];
                totalSupply += supply[i];
            }
        }
        double traded = System.Math.Min(totalDemand, totalSupply) * riskAppetite;
        if (traded > 1e-9)
        {
            double sFill = traded / totalSupply, sent = 0;
            for (int i = 0; i < n; i++)
                if (!(demand[i] > 0) && supply[i] > 0)
                {
                    double k = TakeGold(w[i], supply[i] * sFill / e[i]);
                    w[i].CapitalAbroad += new MoneyStock(k); sent += k * e[i]; fx -= k;
                }
            int lastD = -1; for (int i = 0; i < n; i++) if (demand[i] > 0) lastD = i;
            double given = 0;
            for (int i = 0; i < n; i++)
                if (demand[i] > 0)
                {
                    double kg = i == lastD ? sent - given : sent * (demand[i] / totalDemand);
                    given += kg;
                    double k = kg / e[i];
                    w[i].ForeignCapitalIn += new MoneyStock(k); GiveGold(w[i], k); fx += k;
                }
        }
        ProbeDiff("新規フロー");
        if (Instruments.On) Instruments.CpwdEnd(nations);

        double intPool = 0, intPaid = 0;
        for (int i = 0; i < n; i++) { intPool += w[i].ForeignIntPaidThisQuarter * e[i]; intPaid += w[i].ForeignIntPaidThisQuarter; w[i].ForeignIntPaidThisQuarter = 0; }
        for (int i = 0; i < n; i++)
        {
            double delta = (w[i].ForeignDebt - w[i].PrevForeignDebt).Value;
            w[i].PrevForeignDebt = w[i].ForeignDebt;
            if (System.Math.Abs(delta) < 1e-9) continue;
            AdjustClaims(nations, i, delta);
        }
        if (intPool > 1e-9)
        {
            double claims = 0; for (int i = 0; i < n; i++) claims += (w[i].ForeignBondClaims * e[i]).Value;
            double basis = claims;
            if (basis <= 1e-9) { for (int i = 0; i < n; i++) basis += InvestorSavings(w[i]) * e[i]; }
            if (basis > 1e-9)
            {
                fx -= intPaid;
                for (int i = 0; i < n; i++)
                {
                    double share = claims > 1e-9 ? (w[i].ForeignBondClaims * e[i]).Value / claims : InvestorSavings(w[i]) * e[i] / basis;
                    double k = intPool * share / e[i];
                    if (k > 0) { GiveGold(w[i], k); fx += k; }
                }
            }
        }
        if (w[0].Diplomacy is { } dip) dip.CumulativeFxResidual += fx;
        ProbeDiff("外債利払い");
    }


    internal static bool IsInvestor(Pop p)
        => !p.IsSoldier && p.IsModernElite;

    internal static List<Pop> Investors(World w)
    {
        var pops = new List<Pop>();
        foreach (var (_, p) in w.AllPopsWithRegion()) if (IsInvestor(p)) pops.Add(p);
        return pops;
    }

    internal static double InvestorSavings(World w)
    {
        double s = 0;
        foreach (var (_, p) in w.AllPopsWithRegion()) if (IsInvestor(p)) s += System.Math.Max(0, p.Savings.Value);
        return s;
    }

    private static double TakeGold(World w, double amount, [System.Runtime.CompilerServices.CallerFilePath] string routeFile = "", [System.Runtime.CompilerServices.CallerLineNumber] int routeLine = 0) => World.Transfer((w, Investors(w)), (null, null, null), amount, routeFile: routeFile, routeLine: routeLine);

    internal static double GiveGold(World w, double amount, (World? W, List<Pop>? Pops) from = default, TreasuryCat cat = TreasuryCat.OtherIn, [System.Runtime.CompilerServices.CallerFilePath] string routeFile = "", [System.Runtime.CompilerServices.CallerLineNumber] int routeLine = 0)
    {
        var pops = Investors(w);
        var held = new List<double>(pops.Count); double heldSum = 0;
        foreach (var p in pops) { double h = System.Math.Max(0, p.Savings.Value); held.Add(h); heldSum += h; }
        double moved = World.Transfer(from, (w, pops, heldSum > 1e-9 ? held : null), amount, cat, routeFile: routeFile, routeLine: routeLine);
        if (moved < amount) moved += World.Transfer(from, (w, null, null), amount - moved, cat, toCat: TreasuryCat.OtherIn, routeFile: routeFile, routeLine: routeLine);
        return moved;
    }

    internal static void AdjustClaims(IReadOnlyList<Simulation> nations, int i, double delta)
    {
        int n = nations.Count;
        double g = delta * nations[i].World.FxRate, basis = 0;
        for (int j = 0; j < n; j++) if (j != i) basis += (nations[j].World.ForeignBondClaims * nations[j].World.FxRate).Value;
        if (basis > 1e-9)
        {
            for (int j = 0; j < n; j++)
                if (j != i) { var wj = nations[j].World; wj.ForeignBondClaims = new MoneyStock(System.Math.Max(0, (wj.ForeignBondClaims * (1.0 + g / basis)).Value)); }
            return;
        }
        if (!(g > 0)) return;
        double inv = 0;
        for (int j = 0; j < n; j++) if (j != i) inv += InvestorSavings(nations[j].World) * nations[j].World.FxRate;
        if (inv > 1e-9)
            for (int j = 0; j < n; j++)
                if (j != i) { var wj = nations[j].World; wj.ForeignBondClaims += new MoneyStock(g * (InvestorSavings(wj) * wj.FxRate / inv) / wj.FxRate); }
    }

    internal static double HostRentDividend(World w)
    {
        double fci = w.ForeignCapitalIn.Value;
        if (!(fci > 0)) return 0;
        double rent = 0, sav = 0;
        foreach (var (_, p) in w.AllPopsWithRegion())
            if (IsInvestor(p)) { rent += p.IndustrialRentPerMonth.Value; sav += System.Math.Max(0, p.Savings.Value); }
        if (!(rent > 0)) return 0;
        double d = Calendar.PerMonthToPerQuarter(rent) * (fci / (fci + sav));
        return double.IsFinite(d) ? d : 0;
    }
}
