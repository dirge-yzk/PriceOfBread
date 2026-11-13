namespace PriceOfBread.SimCore;

public static class MarketFlow
{
    private const double Eps = 1e-9;

    private const double InnerT = 0.5;

    private const double PinnedTol = 1e-8;

    private const double PinnedClosedMaxDev = 0.1;

    private const double PinnedClosedMaxDeficit = 1e-3;

    private const double StepPerTau = 0.1;

    internal static long CntGoodSteps, CntClosedGoods, CntClosedService, CntClosedPinned, CntClosedLabor,
        CntDailyStartBand, CntDailyStartHigh, CntDailyStartService, CntDailyPredBand, CntDailyLabor, CntDailyHardBand, CntDailyNaN, CntDailyStep1,
        CntDailyDaysRun, CntDailyDaysSkipped;

    private static void Prefetch(Region region, Good[] live)
    {
        double acc = 0;
        foreach (var g in live)
        {
            var m = region.Markets[g.Id];
            acc += m.Price + m.LastDemand + m.LastProduced + m.LastImport + m.Stock + m.ReservationWage + (m.FlowAsleep ? 1 : 0) + g.RefPrice + (int)g.Tier;
        }
        PrefetchSink += acc;
    }
    [System.ThreadStatic] private static double PrefetchSink;

    public static void StepDay(Region region, Coefficients coef, int days = 1, long[]? cost = null, bool monthStart = true, MinimumWageLaw minWage = MinimumWageLaw.None)
    {
        int h = days;
        double div = Calendar.DaysPerMonth;
        double k = coef.SoftBandSharpness;
        double lnCeilGoods = System.Math.Log(coef.PriceCeilFactor);
        double lnFloorGoods = -System.Math.Log(coef.PriceFloorFactor);
        double effWageMax = EffWageMax(region, coef);
        double lnCeilWage = System.Math.Log(effWageMax);
        double lnFloorWage = -System.Math.Log(coef.WageClampMin);
        bool closedGoods = h > 1 && lnFloorGoods > Eps && lnCeilGoods > Eps;
        bool closedWage = h > 1 && lnFloorWage > Eps && lnCeilWage > Eps;
        long nSteps = 0, nClosedL = 0, nClosedG = 0, nClosedS = 0, nClosedP = 0, nStart = 0, nStartHi = 0, nStartSvc = 0, nPred = 0, nLabor = 0, nHard = 0, nNaN = 0, nStep1 = 0, nRun = 0, nSkip = 0;
        long kLabor = 0, kSvcD = 0, kGoodsD = 0, kRunL = 0, kRunS = 0, kRunG = 0;
        bool insOn = Instruments.On;
        if (monthStart && cost is not null)
        {
            long deadM = 0;
            foreach (var mk in region.Markets) if (mk.LastDemand == 0.0 && mk.LastProduced == 0.0 && mk.Stock == 0.0) deadM++;
            cost[CostLedger.DeadMarketMonth] += deadM;
        }
        var live = region.EnsureLiveGoods();
        Prefetch(region, live);
        long nAsleep = 0;
        foreach (var good in live)
        {
            int gi = good.Id;
            var m = region.Markets[gi];
            if (!monthStart && m.FlowAsleep) { nAsleep++; continue; }
            m.FlowAsleep = false;
            double totalDemand = m.LastDemand;
            if (totalDemand < Eps && m.LastProduced < Eps) continue;
            nSteps++;

            m.TargetStock = System.Math.Max(coef.TargetStockFloor, coef.TargetStockBuffer * totalDemand) * region.StockpileFactor(gi, coef);
            double gap = (m.TargetStock - m.Stock) / (m.TargetStock + m.Stock + Eps);
            double flowIn = m.LastProduced + m.LastImport;
            double supply = System.Math.Max(flowIn, System.Math.Min(flowIn + m.Stock, totalDemand));

            double pl = region.PriceLevel;
            if (good.Tier == GoodTier.Service)
            {
                double flow = (totalDemand - supply) / (totalDemand + supply + Eps);
                double adj = coef.FlowAdjustSpeed * flow / div;
                double wrp = good.RefPrice * pl;
                if (Catalog.IsLaborGood(gi))
                {
                    double mwFloor = 0;
                    if (minWage != MinimumWageLaw.None && MinimumWageFloor.Applies(minWage, gi))
                    {
                        mwFloor = MinimumWageFloor.AbsFloor(region, gi, wrp * effWageMax, minWage);
                        if (cost is not null) cost[CostLedger.MinimumWageBasketScans] += region.Goods.Count + region.Producers.Count;
                    }
                    bool bandFloorOff = coef.LaborBandFloorOffGate > 0;
                    bool laborClosed = h > 1 && bandFloorOff;
                    double P = m.Price, RW = m.ReservationWage;
                    int run = 0;
                    while (run < h)
                    {
                        run++;
                        double target = P * (1 + adj);
                        double rw = RW;
                        if (!(rw > Eps)) rw = P;
                        double rwSpeed = target >= rw ? coef.ReservationWageRisePerDay : coef.ReservationWageFallPerDay;
                        rw = PoliticalQuantities.Toward(rw, target, rwSpeed);
                        double want = System.Math.Max(target, rw);
                        double p1 = bandFloorOff && want < wrp
                            ? System.Math.Max(want, Eps)
                            : SoftBandLn(want, wrp, lnFloorWage, lnCeilWage, k);
                        bool free1 = laborClosed && run == 1 && want < wrp && !(mwFloor > 0 && p1 < mwFloor);
                        if (mwFloor > 0 && p1 < mwFloor) p1 = mwFloor;
                        bool still = Same(p1, P) && Same(rw, RW);
                        P = p1; RW = rw;
                        if (still) break;
                        if (free1 && LaborClosed(P, RW, adj, h - 1, wrp, mwFloor, coef.ReservationWageRisePerDay, coef.ReservationWageFallPerDay, out double pL))
                        {
                            P = pL; RW = pL;
                            nClosedL++;
                            break;
                        }
                    }
                    m.ReservationWage = RW;
                    m.Price = P;
                    if (insOn) Instruments.LwStep(region, gi, P, mwFloor);
                    if (h == 1) nStep1++; else nLabor++;
                    nRun += run; nSkip += h - run; kLabor++; kRunL += run;
                }
                else
                {
                    double P0 = m.Price;
                    if (closedWage)
                    {
                        double t0 = BandT(P0, wrp, lnFloorWage, lnCeilWage, out _, out _);
                        if (System.Math.Abs(t0) < InnerT)
                        {
                            double g1 = 1 + adj;
                            double tp = BandT(P0 * System.Math.Pow(g1, h), wrp, lnFloorWage, lnCeilWage, out double lp, out _);
                            if (g1 > 0 && System.Math.Abs(tp) < InnerT)
                            {
                                m.Price = wrp * System.Math.Exp(lp * SoftT(tp, k, h));
                                nClosedS++;
                                continue;
                            }
                            nPred++;
                        }
                        else if (MultiNationSim.TimeOn && double.IsNaN(t0)) nNaN++;
                        else { nStart++; if (MultiNationSim.TimeOn) { nStartSvc++; if (t0 > 0) nStartHi++; } }
                    }
                    else if (h == 1) nStep1++;
                    else nHard++;
                    if (closedWage && ServiceClosed(P0, wrp, adj, lnFloorWage, lnCeilWage, k, h, out double psc, coef.ServiceClosedMaxCurv))
                    {
                        m.Price = psc;
                        nClosedS++;
                        continue;
                    }
                    double P = P0;
                    int run = 0;
                    while (run < h)
                    {
                        run++;
                        double p1 = SoftBandLn(P * (1 + adj), wrp, lnFloorWage, lnCeilWage, k);
                        bool still = Same(p1, P);
                        P = p1;
                        if (still) break;
                    }
                    m.Price = P;
                    nRun += run; nSkip += h - run; kSvcD++; kRunS += run;
                }
            }
            else
            {
                double rp = System.Math.Max(good.RefPrice * pl, Eps);
                double demandRaw = coef.PriceLawSeesExports > 0 ? totalDemand + m.LastExport : totalDemand;
                double P0 = m.Price;
                bool pinned = !(supply > Eps * demandRaw) && P0 > rp;
                if (closedGoods)
                {
                    double t0 = BandT(P0, rp, lnFloorGoods, lnCeilGoods, out _, out double u0);
                    if (System.Math.Abs(t0) < InnerT)
                    {
                        int hs = SubStep(h, u0, coef, div, out double amp0);
                        double Ps = P0, us = u0; bool inner = true;
                        for (int dn = 0; dn < h; dn += hs)
                        {
                            if (dn > 0 && !(System.Math.Abs(BandT(Ps, rp, lnFloorGoods, lnCeilGoods, out _, out us)) < InnerT)) { inner = false; break; }
                            double g1 = 1 + (dn == 0 && Ps >= Eps ? GoodsYAmp(Ps, rp, demandRaw, supply, gap, coef, div, amp0) : GoodsY(Ps, rp, demandRaw, supply, gap, coef, div, Ps >= Eps ? us : System.Math.Log(Ps / rp)));
                            double tp = BandT(Ps * System.Math.Pow(g1, hs), rp, lnFloorGoods, lnCeilGoods, out double lp, out _);
                            if (!(g1 > 0 && System.Math.Abs(tp) < InnerT)) { inner = false; break; }
                            Ps = rp * System.Math.Exp(lp * SoftT(tp, k, hs));
                        }
                        if (inner)
                        {
                            m.Price = Ps;
                            nClosedG++;
                            continue;
                        }
                        nPred++;
                    }
                    else if (MultiNationSim.TimeOn && double.IsNaN(t0)) nNaN++;
                    else if (pinned && PinnedClosed(P0, rp, u0, demandRaw, supply, gap, coef, div, lnCeilGoods, k, h, out double pc, out bool atRest))
                    {
                        m.Price = pc;
                        if (atRest) m.FlowAsleep = true;
                        nClosedP++;
                        continue;
                    }
                    else { nStart++; if (MultiNationSim.TimeOn && t0 > 0) nStartHi++; }
                }
                else if (h == 1) nStep1++;
                else nHard++;
                double P = P0;
                int run = 0;
                while (run < h)
                {
                    run++;
                    double p1 = P * (1 + GoodsY(P, rp, demandRaw, supply, gap, coef, div, System.Math.Log(P / rp)));
                    p1 = SoftBandLn(p1, rp, lnFloorGoods, lnCeilGoods, k);
                    bool still = Same(p1, P) || (pinned && System.Math.Abs(p1 - P) <= PinnedTol * P);
                    P = p1;
                    if (still) { if (pinned) m.FlowAsleep = true; break; }
                }
                m.Price = P;
                nRun += run; nSkip += h - run; kGoodsD++; kRunG += run;
            }
        }
        if (insOn) Instruments.PxStep(region, live.Length, nSteps, nAsleep, kLabor, nClosedS, kSvcD, nClosedG, nClosedP, kGoodsD, kRunL, kRunS, kRunG);
        if (cost is not null) cost[CostLedger.PriceGoods] += live.Length - nAsleep;
        if (cost is not null) cost[CostLedger.PriceDailyRuns] += nRun;
        if (MultiNationSim.TimeOn)
        {
            System.Threading.Interlocked.Add(ref CntGoodSteps, nSteps);
            System.Threading.Interlocked.Add(ref CntClosedGoods, nClosedG);
            System.Threading.Interlocked.Add(ref CntClosedService, nClosedS);
            System.Threading.Interlocked.Add(ref CntClosedPinned, nClosedP);
            System.Threading.Interlocked.Add(ref CntDailyStartBand, nStart);
            System.Threading.Interlocked.Add(ref CntDailyStartHigh, nStartHi);
            System.Threading.Interlocked.Add(ref CntDailyStartService, nStartSvc);
            System.Threading.Interlocked.Add(ref CntDailyPredBand, nPred);
            System.Threading.Interlocked.Add(ref CntDailyLabor, nLabor);
            System.Threading.Interlocked.Add(ref CntClosedLabor, nClosedL);
            System.Threading.Interlocked.Add(ref CntDailyHardBand, nHard);
            System.Threading.Interlocked.Add(ref CntDailyNaN, nNaN);
            System.Threading.Interlocked.Add(ref CntDailyStep1, nStep1);
            System.Threading.Interlocked.Add(ref CntDailyDaysRun, nRun);
            System.Threading.Interlocked.Add(ref CntDailyDaysSkipped, nSkip);
        }
    }

    private static double GoodsY(double price, double rp, double demandRaw, double supply, double gap, Coefficients coef, double div, double lnDev)
    {
        return GoodsYAmp(price, rp, demandRaw, supply, gap, coef, div, AmpOf(lnDev, coef));
    }

    private static double AmpOf(double lnDev, Coefficients coef)
    {
        double lnRef = coef.PriceAmpLnScale;
        double ndist = System.Math.Min(1.0, System.Math.Abs(lnDev) / lnRef);
        return coef.PriceAmpCenter + (coef.PriceAmpEdge - coef.PriceAmpCenter) * (ndist == 1.0 ? 1.0 : System.Math.Pow(ndist, coef.PriceAmpPower));
    }

    private static double GoodsYAmp(double price, double rp, double demandRaw, double supply, double gap, Coefficients coef, double div, double amp)
    {
        double dev = price / rp;
        double e = coef.PriceElasticity;
        double eD = demandRaw * System.Math.Pow(dev, -e);
        double eS = supply == 0 && dev > 0 && dev <= double.MaxValue && e >= 0 && e <= 1 ? supply : supply * System.Math.Pow(dev, e);
        double flow = (eD - eS) / (eD + eS + Eps);
        return (coef.PriceAdjustSpeed * gap + coef.FlowAdjustSpeed * flow) / div * amp;
    }

    internal static double EffWageMax(Region region, Coefficients coef)
        => coef.WageClampMax;

    private static bool Same(double a, double b) => System.BitConverter.DoubleToInt64Bits(a) == System.BitConverter.DoubleToInt64Bits(b);

    private static double BandT(double price, double rp, double lnFloorPos, double lnCeilPos, out double L, out double u)
    {
        u = System.Math.Log(System.Math.Max(price, Eps) / rp);
        L = u >= 0 ? lnCeilPos : lnFloorPos;
        return u / L;
    }

    internal static bool LaborClosed(double P1, double RW1, double adj, int n, double wrp, double mwFloor, double rise, double fall, out double P)
    {
        P = P1;
        double g = 1 + adj;
        if (!(n >= 1) || !(g > 0) || !(RW1 > Eps)) return false;
        if (adj >= 0)
        {
            if (!(rise == 1.0)) return false;
            double t2 = P1 * g;
            if (!(t2 >= RW1) || !(t2 > Eps)) return false;
            if (mwFloor > 0 && !(t2 >= mwFloor)) return false;
            double pe = P1 * System.Math.Pow(g, n);
            if (!(pe < wrp)) return false;
            P = pe; return true;
        }
        if (!(fall > 0 && fall < 1)) return false;
        double t = P1 * g;
        if (!(t < RW1)) return false;
        double rw2 = RW1 + (t - RW1) * fall;
        if (!(rw2 < wrp)) return false;
        double p = n == 1 ? rw2 : rw2 * System.Math.Pow(1 + fall * adj, n - 1);
        if (!(p > Eps) || (mwFloor > 0 && !(p >= mwFloor))) return false;
        P = p; return true;
    }

    internal static bool ServiceClosed(double P0, double rp, double adj, double lnFloor, double lnCeil, double k, int h, out double P, double maxCurv = 0.05)
    {
        P = P0;
        if (!(adj > -1) || adj == 0) return false;
        double u0 = System.Math.Log(System.Math.Max(P0, Eps) / rp);
        double lnc = System.Math.Log(1 + adj);
        double sg = lnc > 0 ? 1.0 : -1.0;
        if (!(u0 * sg >= 0)) return false;
        double L = sg > 0 ? lnCeil : lnFloor;
        if (!(L > Eps)) return false;
        double c = sg * lnc / L, t0 = sg * u0 / L, t = t0, xk = 0, b = 0, root = 0, x = 0;
        bool conv = false;
        for (int it = 0; it < 40; it++)
        {
            x = t + c;
            if (!(x > 0)) return false;
            xk = System.Math.Pow(x, k); b = 1 + xk; root = System.Math.Pow(b, 1.0 / k);
            double dt = (x / root - t) / (1.0 / (root * b) - 1);
            t -= dt;
            if (!(System.Math.Abs(dt) > 1e-13)) { conv = !double.IsNaN(t); break; }
        }
        if (!conv || !(t > 0)) return false;
        double e0 = t0 - t;
        if (!(System.Math.Abs(e0) * L < PinnedClosedMaxDev)) return false;
        x = t + c; xk = System.Math.Pow(x, k); b = 1 + xk; root = System.Math.Pow(b, 1.0 / k);
        double rho = 1.0 / (root * b);
        double s2 = -(k + 1) * (xk / x) / (root * b * b);
        double a = s2 / (2 * rho * (1 - rho));
        if (!(System.Math.Abs(a * e0) < maxCurv)) return false;
        double z = System.Math.Pow(rho, h) * (e0 + a * e0 * e0);
        double eh = z - a * z * z;
        double p = rp * System.Math.Exp(sg * L * (t + eh));
        if (!(p > 0) || double.IsInfinity(p)) return false;
        P = p;
        return true;
    }


    private static bool PinnedClosed(double P0, double rp, double u0, double demandRaw, double supply, double gap, Coefficients coef, double div, double L, double k, int h,
                                     out double P, out bool atRest)
    {
        P = P0; atRest = false;
        if (!(u0 >= coef.PriceAmpLnScale) || !(L > Eps)) return false;
        double eD = demandRaw * System.Math.Pow(P0 / rp, -coef.PriceElasticity);
        if (!(Eps < PinnedClosedMaxDeficit * (eD + Eps))) return false;
        double y = GoodsY(P0, rp, demandRaw, supply, gap, coef, div, u0);
        if (!(y > -1)) return false;
        double c = System.Math.Log(1 + y) / L;
        double t0 = u0 / L, t = t0, xk = 0, b = 0, root = 0, x = 0;
        bool conv = false;
        for (int it = 0; it < 40; it++)
        {
            x = t + c;
            if (!(x > 0)) return false;
            xk = System.Math.Pow(x, k); b = 1 + xk; root = System.Math.Pow(b, 1.0 / k);
            double dt = (x / root - t) / (1.0 / (root * b) - 1);
            t -= dt;
            if (!(System.Math.Abs(dt) > 1e-15)) { conv = !double.IsNaN(t); break; }
        }
        if (!conv || !(t * L >= coef.PriceAmpLnScale)) return false;
        double e0 = t0 - t;
        if (!(System.Math.Abs(e0) * L < PinnedClosedMaxDev)) return false;
        x = t + c; xk = System.Math.Pow(x, k); b = 1 + xk; root = System.Math.Pow(b, 1.0 / k);
        double rho = 1.0 / (root * b);
        double s2 = -(k + 1) * (xk / x) / (root * b * b);
        double a = s2 / (2 * rho * (1 - rho));
        double z = System.Math.Pow(rho, h) * (e0 + a * e0 * e0);
        double eh = z - a * z * z;
        double p = rp * System.Math.Exp(L * (t + eh));
        if (!(p > 0) || double.IsInfinity(p)) return false;
        P = p;
        atRest = !(L * (1 - rho) * System.Math.Abs(eh) > PinnedTol);
        return true;
    }

    private static int SubStep(int h, double lnDev, Coefficients coef, double div, out double amp)
    {
        amp = AmpOf(lnDev, coef);
        double g = coef.FlowAdjustSpeed * coef.PriceElasticity / div * amp;
        int hs = h;
        while (hs > 1 && hs * g > StepPerTau) { hs--; while (hs > 1 && h % hs != 0) hs--; }
        return hs;
    }

    private static double SoftT(double t, double k, int h)
        => t / System.Math.Pow(1 + h * System.Math.Pow(System.Math.Abs(t), k), 1.0 / k);

    internal static double SoftBandLn(double price, double rp, double lnFloorPos, double lnCeilPos, double sharpness)
    {
        double u = System.Math.Log(System.Math.Max(price, Eps) / rp);
        double L = u >= 0 ? lnCeilPos : lnFloorPos;
        if (L <= Eps) return rp;
        double t = u / L;
        double k = sharpness;
        double soft = t / System.Math.Pow(1 + System.Math.Pow(System.Math.Abs(t), k), 1.0 / k);
        return rp * System.Math.Exp(L * soft);
    }
}
