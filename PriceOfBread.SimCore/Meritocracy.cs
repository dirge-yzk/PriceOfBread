namespace PriceOfBread.SimCore;

public static class Meritocracy
{
    private const double Eps = 1e-9;

    public static void Step(World w, Coefficients c)
    {
        Span<int> targets = stackalloc int[5];
        bool fallReform = c.FallAnchorReform > 0;
        double seatsNat = Military.EstablishmentOf(w, c, out double seatNatMan);
        bool atWar = w.AtWarNow;
        double dischargeQ = Calendar.OverSteps(c.SoldierMigrationRatePerMonth.Value, Calendar.MonthsPerQuarter);
        double volunteers = 0;
        bool ins = Instruments.On;
        if (c.SoldierVacancyFromZero > 0) { _seatWorld = w; _seatPayRate = SeatPayRate(w, c); }   // (1166) 社会移動の頭の国の兵の受け取りの比（州の移動の前）
        if (ins) Instruments.SoldierVacBegin(w);   // (1166) soldiervac_s
        foreach (var r in w.Regions)
        {

            double total = 0, modern = 0, totalSav = 0, totalAsset = 0;
            foreach (var p in r.Pops)
            {
                total += p.Size; totalSav += Math.Max(0, p.Savings.Value); if (p.IsModernElite) modern += p.Size;
                if (fallReform) totalAsset += Math.Max(0, p.Savings.Value);
            }
            if (total <= Eps) continue;
            double avgPerCap = totalSav / total;
            double fallAvg = fallReform ? totalAsset / total : avgPerCap;

            double modernity = PolityModernity(w.Polity);
            double m = PoliticalQuantities.Clamp01(modernity + (modern / total) * c.MeritGroupWeight);

            if (r.MeritFromIndex < 0 || r.MeritToIndex < 0) continue;
            var from = r.Pops[r.MeritFromIndex];

            int tn = 0;
            targets[tn++] = r.MeritToIndex;
            if (r.TeacherPopIndex >= 0) targets[tn++] = r.TeacherPopIndex;
            if (r.BureaucratPopIndex >= 0) targets[tn++] = r.BureaucratPopIndex;
            if (r.DoctorPopIndex >= 0) targets[tn++] = r.DoctorPopIndex;
            if (r.ResearcherPopIndex >= 0) targets[tn++] = r.ResearcherPopIndex;

            double fromIncome = from.Size > Eps ? Math.Max(0, from.EarnedPerMonth.Value) / from.Size : 0;
            double secular = w.EducationLaw switch
            {
                EducationLaw.Public or EducationLaw.Compulsory => 1.0,
                EducationLaw.Religious => c.ReligiousProfessionShare.Value,
                _ => 0.0,
            };
            double he = 0, hw = 0;
            foreach (var p in r.Pops) if (p.IsModernElite) { he += p.EducationFill * p.Size; hw += p.Size; }
            double higherEdu = hw > Eps ? he / hw : 1.0;
            double gateEduPro = higherEdu;
            if (c.PromotionGateOwnSchooling > 0)
                gateEduPro = Math.Max(higherEdu, c.PromotionGateOwnSchooling * from.EducationFill);

            Pop? soldierPop = null;
            foreach (var p in r.Pops) if (p.IsSoldier) { soldierPop = p; break; }
            double soldierVac = 0;
            if (soldierPop is not null && seatNatMan > Eps)
            {
                double seatsR = seatsNat * (Military.RegionManpower(r, c) / seatNatMan);
                if (!atWar && soldierPop.Size > seatsR + Eps && c.JobSearchRatePerQuarter.Value > 0)
                {
                    double noSeat = 0;
                    RouteViaPool(r, w, m, avgPerCap, soldierPop, 0, dischargeQ * (soldierPop.Size - seatsR), secular, higherEdu, c, ref noSeat, forced: true);
                }
                soldierVac = Math.Max(0, seatsR - soldierPop.Size);
            }
            double soldierVac0 = soldierVac;
            if (ins && soldierPop is not null) Instruments.SoldierVacEnter(w, r, soldierPop, soldierVac0);   // (1166) soldiervac_s

            for (int i = 0; i < tn; i++)
            {
                var t = r.Pops[targets[i]];
                if (t.Size <= Eps) continue;
                double gate = targets[i] == r.BureaucratPopIndex ? 1.0 : secular;
                if (gate <= Eps) continue;
                double attract = Math.Max(0, t.EarnedPerMonth.Value) / Math.Max(Eps, t.Size) / Math.Max(Eps, fromIncome * c.PromotionPremium);
                if (attract <= 1.0) continue;
                double effPen = c.MinorityPromotionPenalty * (1.0 - c.MinorityPenaltyModernBlend * m);
                double minorityF = w.EthnicEnabled ? Math.Max(0, 1.0 - effPen * (1.0 - from.CoreEthnicShare.Value)) : 1.0;
                double gateEdu = targets[i] == r.BureaucratPopIndex ? higherEdu : gateEduPro;
                if (c.PromotionGateHigherEdu > 0 && (targets[i] == r.MeritToIndex || targets[i] == r.ResearcherPopIndex))
                    gateEdu *= 1.0 - c.PromotionGateHigherEdu * (1.0 - from.HigherEduFill);
                double flow = c.PromotionSpeedPerQuarter.Value * m * from.Literacy * gateEdu * gate * from.Size * Math.Min(attract - 1.0, 1.0) * minorityF;
                Move(from, t, Math.Min(flow, from.Size), c, selectLiteracy: true, genderAccess: GenderAccess(w, r, targets[i], c));
            }

            if (c.PettyEntryRatePerQuarter.Value > 0 && avgPerCap > Eps && from.Size > Eps
                && from.Savings.Value / from.Size >= avgPerCap)
            {
                for (int pi = 0; pi < 2; pi++)
                {
                    int idx = pi == 0 ? r.ArtisanPopIndex : r.MerchantPopIndex;
                    if (idx < 0) continue;
                    var t = r.Pops[idx];
                    if (t.Size <= Eps && !(c.ColonyPettyEntryEmptyTarget > 0 && idx == r.MerchantPopIndex)) continue;
                    double attract = Math.Max(0, t.EarnedPerMonth.Value) / Math.Max(Eps, t.Size) / Math.Max(Eps, fromIncome * c.PromotionPremium);
                    if (attract > 1.0)
                    {
                        double petty = Math.Min(c.PettyEntryRatePerQuarter.Value * from.Size * Math.Min(attract - 1.0, 1.0), from.Size);
                        double szP0 = ins ? t.Size : 0;
                        Move(from, t, petty, c);
                        if (ins) Instruments.MerchRoute(r, pi == 1 ? 0 : 1, t.Size - szP0);
                    }
                }
            }

            if (c.ArtisanDeclineRatePerQuarter.Value > 0 && r.ArtisanPopIndex >= 0)
            {
                var artisan = r.Pops[r.ArtisanPopIndex];
                if (artisan.Size > Eps && from.Size > Eps)
                {
                    double artIncome = Math.Max(0, artisan.EarnedPerMonth.Value) / artisan.Size;
                    double wrkIncome = Math.Max(0, from.EarnedPerMonth.Value) / from.Size;
                    double attract = wrkIncome / Math.Max(Eps, artIncome * c.PromotionPremium);
                    if (attract > 1.0)
                    {
                        double f = Math.Min(c.ArtisanDeclineRatePerQuarter.Value * artisan.Size * Math.Min(attract - 1.0, 1.0), artisan.Size);
                        if (c.JobSearchRatePerQuarter.Value > 0) RouteViaPool(r, w, m, avgPerCap, artisan, 1, f, secular, higherEdu, c, ref soldierVac, forced: true);
                        else Move(artisan, from, f, c);
                    }
                }
            }

            if (c.JobSearchRatePerQuarter.Value > 0)
                for (int i = 0; i < r.Pops.Count; i++)
                {
                    var p = r.Pops[i];
                    if (p.Closed || p.Size <= Eps) continue;
                    double u = c.JobSearchUsesEma > 0 && p.UnemploymentEma.Value >= 0 ? p.UnemploymentEma.Value : p.UnemploymentRate.Value;
                    if (u <= Eps) continue;
                    int cls = ClassOf(r, i);
                    if (cls < 0) continue;
                    double eff = c.JobSearchRatePerQuarter.Value;   // (1276) K15：職探しの率は識字に依らない（旗と重みは焼き込んだ）
                    double poolFlow = eff * u * p.Size;
                    RouteViaPool(r, w, m, avgPerCap, p, cls, poolFlow, secular, higherEdu, c, ref soldierVac, forced: p.IsSoldier);
                }

            if (w.EducationLaw == EducationLaw.Religious && r.ClergyPopIndex >= 0 && avgPerCap > Eps)
            {
                var clergy = r.Pops[r.ClergyPopIndex];
                double clergyPerCap = clergy.Size > Eps ? clergy.Savings.Value / clergy.Size : 0;
                double attract = clergyPerCap / avgPerCap;
                if (attract > c.ClergyAttractThreshold)
                    Move(from, clergy, c.ClergyJoinRatePerQuarter.Value * from.Literacy * from.Size * Math.Min(1.0, attract / c.ClergyAttractThreshold - 1.0), c, selectLiteracy: true);
            }

            if (c.ClergyExitRatePerQuarter.Value > 0 && w.EducationLaw != EducationLaw.Religious && r.ClergyPopIndex >= 0 && avgPerCap > Eps)
            {
                var clergyOut = r.Pops[r.ClergyPopIndex];
                if (clergyOut.Size > Eps)
                {
                    double poverty = 1.0 - (clergyOut.Savings.Value / clergyOut.Size) / avgPerCap;
                    if (poverty > 0)
                    {
                        Pop? dest = r.TeacherPopIndex >= 0 ? r.Pops[r.TeacherPopIndex] : null;
                        if (dest is null)
                            foreach (var pp in r.Pops) if (pp.IsCommoner && pp.ClassId == PopClass.Worker) { dest = pp; break; }
                        if (dest is not null) Move(clergyOut, dest, c.ClergyExitRatePerQuarter.Value * poverty * clergyOut.Size, c);
                    }
                }
            }

            if (r.InvestorPopIndex >= 0 && avgPerCap > Eps)
            {
                var capitalist = r.Pops[r.InvestorPopIndex];
                for (int i = 0; i < tn; i++)
                {
                    var t = r.Pops[targets[i]];
                    if (t.Size <= Eps) continue;
                    if (AssetPerCap(t) > c.CapitalRiseThreshold * FallAnchor(t, fallAvg, c))
                        Move(t, capitalist, c.CapitalRiseRatePerQuarter.Value * t.Size, c);
                }
                {
                    for (int pi = 0; pi < 2; pi++)
                    {
                        int idx = pi == 0 ? r.MerchantPopIndex : r.ArtisanPopIndex;
                        if (idx < 0) continue;
                        var t = r.Pops[idx];
                        if (t.Size <= Eps) continue;
                        if (AssetPerCap(t) > c.CapitalRiseThreshold * FallAnchor(t, fallAvg, c))
                        {
                            double szC0 = ins ? t.Size : 0;
                            Move(t, capitalist, c.CapitalRiseRatePerQuarter.Value * t.Size, c);
                            if (ins && pi == 0) Instruments.MerchRoute(r, 2, szC0 - t.Size);
                        }
                    }
                }
            }

            if (avgPerCap > Eps && tn > 0)
            {
                int best = targets[0];
                double bestPc = -1;
                for (int i = 0; i < tn; i++)
                {
                    var t = r.Pops[targets[i]];
                    double pc = t.Size > Eps ? t.Savings.Value / t.Size : 0;
                    if (pc > bestPc) { bestPc = pc; best = targets[i]; }
                }
                foreach (var p in r.Pops)
                {
                    bool isCapitalist = r.InvestorPopIndex >= 0 && p == r.Pops[r.InvestorPopIndex];
                    bool upper = p.Closed || isCapitalist;
                    if (!upper || p.IsSoldier || p.Size <= Eps) continue;
                    if (r.ClergyPopIndex >= 0 && p == r.Pops[r.ClergyPopIndex] && p.Size <= c.ClergyFloorShare.Value * total) continue;
                    double pc = AssetPerCap(p);
                    double anchor = FallAnchor(p, fallAvg, c);
                    bool debtRuined = c.PopDebtDriven > 0 && c.DebtRuinRatio > 0
                                   && p.Debt.Value > c.DebtRuinRatio * Math.Max(Eps, Calendar.PerMonthToPerYear(p.EarnedPerMonth).Value);
                    if (!debtRuined && pc >= c.UpperFallThreshold * anchor) continue;
                    double depth = FallDepth(pc, c.UpperFallThreshold * anchor, debtRuined, c);
                    if (c.JobSearchRatePerQuarter.Value > 0)
                    {
                        double rate = isCapitalist && (debtRuined || pc < c.InsolventThreshold * anchor) ? c.BankruptcyFallRatePerQuarter.Value : c.UpperFallRatePerQuarter.Value;
                        RouteViaPool(r, w, m, avgPerCap, p, 2, rate * depth * p.Size, secular, higherEdu, c, ref soldierVac, forced: true);
                    }
                    else if (isCapitalist && r.ArtisanPopIndex >= 0)
                    {
                        double rate = debtRuined || pc < c.InsolventThreshold * anchor ? c.BankruptcyFallRatePerQuarter.Value : c.UpperFallRatePerQuarter.Value;
                        Move(p, r.Pops[r.ArtisanPopIndex], rate * depth * p.Size, c);
                    }
                    else
                        Move(p, r.Pops[best], c.UpperFallRatePerQuarter.Value * depth * p.Size, c, genderAccess: GenderAccess(w, r, best, c));
                }
            }

            if (avgPerCap > Eps)
                for (int i = 0; i < tn; i++)
                {
                    var t = r.Pops[targets[i]];
                    if (t.Size <= Eps) continue;
                    double pc5 = AssetPerCap(t);
                    double thr5 = c.MiddleFallThreshold * FallAnchor(t, fallAvg, c);
                    if (pc5 < thr5)
                    {
                        double depth5 = FallDepth(pc5, thr5, false, c);
                        if (c.JobSearchRatePerQuarter.Value > 0) RouteViaPool(r, w, m, avgPerCap, t, 1, c.MiddleFallRatePerQuarter.Value * depth5 * t.Size, secular, higherEdu, c, ref soldierVac, forced: true);
                        else Move(t, from, c.MiddleFallRatePerQuarter.Value * depth5 * t.Size, c);
                    }
                }

            if (r.MerchantPopIndex >= 0 && avgPerCap > Eps)
            {
                var mt = r.Pops[r.MerchantPopIndex];
                double pcM = mt.Size > Eps ? AssetPerCap(mt) : 0;
                double thrM = c.MiddleFallThreshold * FallAnchor(mt, fallAvg, c);
                if (ins) Instruments.MerchGate(r, mt, c);
                if (mt.Size > Eps && pcM < thrM)
                {
                    double depthM = FallDepth(pcM, thrM, false, c);
                    double szF0 = ins ? mt.Size : 0;
                    if (c.JobSearchRatePerQuarter.Value > 0) RouteViaPool(r, w, m, avgPerCap, mt, 1, c.MiddleFallRatePerQuarter.Value * depthM * mt.Size, secular, higherEdu, c, ref soldierVac, forced: true);
                    else Move(mt, from, c.MiddleFallRatePerQuarter.Value * depthM * mt.Size, c);
                    if (ins) Instruments.MerchRoute(r, 3, szF0 - mt.Size);
                }
            }

            if (r.ArtisanPopIndex >= 0 && avgPerCap > Eps)
            {
                var artisan = r.Pops[r.ArtisanPopIndex];
                double pc = artisan.Size > Eps ? AssetPerCap(artisan) : 0;
                double anc6 = FallAnchor(artisan, fallAvg, c);
                if (artisan.Size > Eps && pc < c.MiddleFallThreshold * anc6)
                {
                    double rate = pc < c.InsolventThreshold * anc6 ? c.BankruptcyFallRatePerQuarter.Value : c.MiddleFallRatePerQuarter.Value;
                    double depth6 = FallDepth(pc, c.MiddleFallThreshold * anc6, false, c);
                    double szA0 = ins ? artisan.Size : 0;
                    if (c.JobSearchRatePerQuarter.Value > 0) RouteViaPool(r, w, m, avgPerCap, artisan, 1, rate * depth6 * artisan.Size, secular, higherEdu, c, ref soldierVac, forced: true);
                    else Move(artisan, from, rate * depth6 * artisan.Size, c);
                    if (ins) Instruments.MerchRoute(r, 4, szA0 - artisan.Size);
                }
            }

            volunteers += Math.Max(0, soldierVac0 - soldierVac);
            if (ins && soldierPop is not null) Instruments.SoldierVacExit(w, r, soldierPop);   // (1166) soldiervac_s
        }
        if (volunteers > Eps) { double sEnd = w.SoldierSizeK(); Military.DiluteSkill(w, sEnd - volunteers, sEnd); }
        _seatWorld = null;
    }


    internal static int ClassOf(Region r, int i)
    {
        var p = r.Pops[i];
        if (p.Closed || i == r.InvestorPopIndex) return 2;
        if (i == r.MeritToIndex || i == r.TeacherPopIndex || i == r.BureaucratPopIndex
            || i == r.DoctorPopIndex || i == r.MerchantPopIndex || i == r.ArtisanPopIndex) return 1;
        return 0;
    }

    private static void RouteViaPool(Region r, World w, double m, double avgPerCap, Pop src, int srcClass, double flow,
                                     double secular, double higherEdu, Coefficients c, ref double soldierVac, bool forced = false)
    {
        if (flow <= Eps || srcClass < 0 || src.Size <= Eps) return;
        if (c.SerfGateCoversPool > 0 && c.SerfMobilityGate > 0 && !src.IsUrban)
        {
            double serfFreePool = 1.0 - Privileges.SeigneurialDegree(w);
            flow *= (1.0 - c.SerfMobilityGate) + c.SerfMobilityGate * serfFreePool;
            if (flow <= Eps) return;
        }
        double srcInc = Math.Max(0, src.EarnedPerMonth.Value) / src.Size;
        double remaining = Math.Min(flow, src.Size);
        remaining = FillJobs(r, w, m, avgPerCap, src, srcClass, remaining, srcInc * c.PromotionPremium, secular, higherEdu, ref soldierVac, c);
        if (remaining > Eps && srcClass > 0)
            remaining = FillJobs(r, w, m, avgPerCap, src, srcClass - 1, remaining, -1.0, secular, higherEdu, ref soldierVac, c);
        if (forced && remaining > Eps)
            for (int cls = Math.Max(0, srcClass - 1); cls >= 0 && remaining > Eps; cls--)
            {
                int best = -1; double bestInc = -1;
                for (int i = 0; i < r.Pops.Count; i++)
                {
                    var t = r.Pops[i];
                    if (ReferenceEquals(t, src) || t.Closed || t.IsSoldier || t.Size <= Eps) continue;
                    if (i == r.InvestorPopIndex || i == r.MerchantPopIndex || i == r.CriminalPopIndex) continue;
                    if (ClassOf(r, i) != cls) continue;
                    double inc = JobIncome(t, src);
                    if (inc > bestInc) { bestInc = inc; best = i; }
                }
                if (best >= 0) { Move(src, r.Pops[best], remaining, c, genderAccess: GenderAccess(w, r, best, c)); remaining = 0; }
            }
    }

    private static double FillJobs(Region r, World w, double m, double avgPerCap, Pop src, int jobClass, double amount, double minIncome,
                                   double secular, double higherEdu, ref double soldierVac, Coefficients c)
    {
        bool emptySeat = c.SoldierVacancyFromZero > 0;   // (1166) 旗：大きさ 0 の兵の席も空きがあれば候補（Meritocracy.Step 冒頭の席の準備〔_seatWorld・SeatPayRate〕と、Simulation.PromoteColonyToRegion が作る大きさ 0 の兵士の POP〔Size0 の器〕が同じ席）
        double temperature = c.JobPoolTemperature > Eps ? c.JobPoolTemperature : Eps;   // 否定形のガード（0・負・NaN は Eps へ）
        int n = r.Pops.Count;
        Span<double> share = n <= 64 ? stackalloc double[n] : new double[n];
        share.Clear();
        double incMax = double.NegativeInfinity;
        for (int i = 0; i < n; i++)
        {
            var t = r.Pops[i];
            if (ReferenceEquals(t, src) || t.Closed || (!(t.Size > Eps) && !(emptySeat && t.IsSoldier))) continue;
            if (t.IsSoldier && !(soldierVac > Eps)) continue;
            if (i == r.InvestorPopIndex || i == r.MerchantPopIndex || i == r.CriminalPopIndex) continue;
            if (ClassOf(r, i) != jobClass) continue;
            double inc = emptySeat && t.IsSoldier && !(t.Size > Eps) ? EmptySeatIncome(w, r, t, src, c) : JobIncome(t, src);
            if (inc <= minIncome) continue;
            share[i] = 1.0; if (inc > incMax) incMax = inc;
        }
        if (incMax == double.NegativeInfinity) return amount;
        double sumW = 0;
        for (int i = 0; i < n; i++)
            if (share[i] > 0)
            {
                var t = r.Pops[i];
                double inc = emptySeat && t.IsSoldier && !(t.Size > Eps) ? EmptySeatIncome(w, r, t, src, c) : JobIncome(t, src);
                share[i] = Math.Exp(Math.Max((inc - incMax) / temperature, -40));
                sumW += share[i];
            }
        double placed = 0;
        for (int i = 0; i < n; i++)
            if (share[i] > 0)
            {
                var t = r.Pops[i];
                double gate = JobGate(r, w, m, avgPerCap, src, i, jobClass, secular, higherEdu, c);
                double vacancy = Math.Max(0, 1.0 - t.UnemploymentRate.Value);
                double f = amount * (share[i] / sumW) * gate * vacancy;
                if (t.IsSoldier) f = Math.Min(f, soldierVac);
                if (f <= Eps) continue;
                f = MoveToJob(w, r, i, src, t, f, c, ref soldierVac);
                placed += f;
            }
        return amount - placed;
    }

    private static double JobIncome(Pop t, Pop src)
    {
        double inc = Math.Max(0, t.EarnedPerMonth.Value) / t.Size;
        if (t.IsSoldier == src.IsSoldier) return inc;
        double wa = t.IsSoldier ? t.Size : Military.WorkingAdults(t);
        double perWorker = wa > Eps ? Math.Max(0, t.EarnedPerMonth.Value) / wa : 0;
        double perHead = src.Size > Eps ? Military.WorkingAdults(src) / src.Size : 0;
        return perWorker * perHead;
    }

    /// <summary>(1166) 大きさ 0 の兵の席の所得（旗 SoldierVacancyFromZero の時だけ・大きさ＞0 は <see cref="JobIncome"/>）。
    /// 兵 1 人の月の給与＝州の徴募の母数の前の月の所得の単価（Simulation.MonthSettle の給与の請求と同じ入力＝四半期の最後の月の給与は、
    /// その月の EconomyStep が所得を戻す前の値＝社会移動の時点の LastEarnedPerMonth）×SoldierPayMult×国の兵が実際に受け取る比（<see cref="SeatPayRate"/>）。
    /// 徴兵の国は頭数あたり（keep）。送り手の世帯の 1 人あたりへ直す規則は JobIncome の兵の席と同じ（×働き盛り÷頭数）。</summary>
    private static double EmptySeatIncome(World w, Region r, Pop t, Pop src, Coefficients c)
    {
        double unit = Military.RecruitWage(r, c, last: true, keep: w.ConscriptionLaw >= ConscriptionLaw.Selective) * c.SoldierPayMult;
        double perWorker = unit * (ReferenceEquals(_seatWorld, w) ? _seatPayRate : SeatPayRate(w, c));
        double perHead = src.Size > Eps ? Military.WorkingAdults(src) / src.Size : 0;
        return perWorker * perHead;
    }

    [ThreadStatic] private static World? _seatWorld;   // (1166) Step の中だけ：SeatPayRate を州ごとに組み直さない（Step の頭で組み、終わりで外す＝持ち越さない）
    [ThreadStatic] private static double _seatPayRate;

    /// <summary>(1166) 国の大きさ＞0 の兵が実際に受け取った比＝Σ兵の EarnedPerMonth ÷ Σ（兵の大きさ×州の単価×SoldierPayMult）。
    /// 兵の給与（Simulation.cs の SoldierPayBudget＝満額×充足＋幹部の給与・×payRate）を、大きさ 0 の席の読みと同じ単価で割った物＝分子と分母を同じ規則で。
    /// 大きさ＞0 の兵がいなければ 0（誰も受け取っていない）。</summary>
    private static double SeatPayRate(World w, Coefficients c)
    {
        bool keep = w.ConscriptionLaw >= ConscriptionLaw.Selective;
        double num = 0, den = 0;
        foreach (var r in w.Regions)
        {
            if (r.SoldierPopIndex < 0 || r.SoldierPopIndex >= r.Pops.Count) continue;
            var s = r.Pops[r.SoldierPopIndex];
            if (!s.IsSoldier || !(s.Size > Eps)) continue;
            num += Math.Max(0, s.EarnedPerMonth.Value);
            den += s.Size * Military.RecruitWage(r, c, last: true, keep: keep) * c.SoldierPayMult;
        }
        return den > 1e-9 ? num / den : 0;
    }

    private static double MoveToJob(World w, Region r, int idx, Pop src, Pop dst, double f, Coefficients c, ref double soldierVac)
    {
        if (!dst.IsSoldier) { Move(src, dst, f, c, genderAccess: GenderAccess(w, r, idx, c)); return f; }
        double before = dst.Size;
        Military.MoveCohort(src, dst, f, c, Military.DraftAgeHi(w));
        double moved = Math.Max(0, dst.Size - before);
        soldierVac = Math.Max(0, soldierVac - moved);
        return moved;
    }

    private static double JobGate(Region r, World w, double m, double avgPerCap, Pop src, int target, int jobClass,
                                  double secular, double higherEdu, Coefficients c)
    {
        if (jobClass != 1) return 1.0;
        if (target == r.ArtisanPopIndex)
            return c.PettyEntryRatePerQuarter.Value > 0 && avgPerCap > Eps && src.Size > Eps && src.Savings.Value / src.Size < avgPerCap ? 0.0 : 1.0;
        double minorityF = w.EthnicEnabled
            ? Math.Max(0, 1.0 - c.MinorityPromotionPenalty * (1.0 - c.MinorityPenaltyModernBlend * m) * (1.0 - src.CoreEthnicShare.Value))
            : 1.0;
        if (target == r.BureaucratPopIndex) return PoliticalQuantities.Clamp01(src.Literacy * higherEdu * minorityF);
        double gateEdu = higherEdu;
        if (c.PromotionGateOwnSchooling > 0)
            gateEdu = Math.Max(higherEdu, c.PromotionGateOwnSchooling * src.EducationFill);
        return PoliticalQuantities.Clamp01(src.Literacy * gateEdu * secular * minorityF);
    }


    private static double AssetPerCap(Pop p)
        => p.Savings.Value / Math.Max(Eps, p.Size);

    private static double FallAnchor(Pop p, double avgAsset, Coefficients c)
    {
        double f = c.FallAnchorReform;
        if (f <= 0) return avgAsset;
        double annualPerCap = Calendar.PerMonthToPerYear(Math.Max(0, Math.Max(p.EarnedPerMonth.Value, p.LastEarnedPerMonth.Value))) / Math.Max(Eps, p.Size);
        return (1.0 - f) * avgAsset + f * annualPerCap;
    }

    private static double FallDepth(double pc, double threshold, bool shock, Coefficients c)
    {
        double f = c.FallAnchorReform;
        if (f <= 0 || shock) return 1.0;
        double depth = threshold > Eps ? PoliticalQuantities.Clamp01((threshold - pc) / threshold) : 1.0;
        return (1.0 - f) + f * depth;
    }

    private static double GenderAccess(World w, Region r, int targetIdx, Coefficients c)
    {
        if (targetIdx != r.MeritToIndex && targetIdx != r.BureaucratPopIndex && targetIdx != r.DoctorPopIndex) return 1.0;
        double access = (int)w.WomenRights / (double)(int)WomenRightsLaw.Suffrage * w.WomenWageRetention;
        return 1.0 - (1.0 - access);
    }

    private static void Move(Pop fromPop, Pop toPop, double flow, Coefficients c, bool selectLiteracy = false, double genderAccess = 1.0)
    {
        if (flow <= Eps || fromPop.Size <= Eps || ReferenceEquals(fromPop, toPop)) return;
        flow = Math.Min(flow, fromPop.Size);
        bool select = fromPop.Size - flow > Eps;
        double moveLit = selectLiteracy && c.PromotionLiteracySelection > 0 && select
            ? PoliticalQuantities.Clamp01(fromPop.Literacy + c.PromotionLiteracySelection * (1.0 - fromPop.Literacy)) : double.NaN;
        double femAccess = genderAccess < 1.0 && select ? genderAccess : double.NaN;
        Pop.Blend(fromPop, toPop, flow, femaleAccess: femAccess, literacy: moveLit);
    }

    private static double PolityModernity(Polity p) => p switch
    {
        Polity.ConstitutionalMonarchy => 0.5,
        Polity.Republic => 0.6,
        Polity.ProletarianDictatorship => 0.7,
        Polity.BourgeoisDictatorship => 0.5,
        Polity.Despotic => 0.3,
        Polity.MilitaryDictatorship => 0.35,
        _ => 0.1,
    };
}
