namespace PriceOfBread.SimCore;

public static class NationalScore
{
    private const double FinancePerCapitaDivisor = 150.0;
    private const double FinanceCap = 6.0;

    public static double Of(World w) => ShareOf(w);

    public static double Standing(World w) => double.IsNaN(w.ScoreStanding) ? Of(w) : w.ScoreStanding;

    public static void StepStanding(World w) => StepStanding(w, Of(w));

    internal static void StepStanding(World w, double now)
    {
        if (double.IsNaN(w.ScoreStanding)) { w.ScoreStanding = now; return; }
        double r = PoliticalQuantities.Clamp01(w.Coef.ScoreInertiaRate);
        w.ScoreStanding = PoliticalQuantities.Toward(w.ScoreStanding, now, r);
    }
    internal readonly record struct ScoreTerms(double Soldiers, double MilitarySpend, double Iron, double Energy, double UrbanPop, double TotalPop)
    {
        internal const int Count = 6;
        internal const int IronIndex = 2;
        internal double this[int i] => i switch { 0 => Soldiers, 1 => MilitarySpend, 2 => Iron, 3 => Energy, 4 => UrbanPop, _ => TotalPop };
    }

    internal static ScoreTerms TermsOf(World w)
    {
        double soldiers = 0, urban = 0, total = 0, iron = 0, energy = 0;
        double oilToCoal = w.Coef.ColonyFuelOilEnergyRatio;
        foreach (var r in w.Regions)
        {
            if (r.Tier == RegionTier.Colonial) continue;
            foreach (var p in r.Pops)
            {
                double s = PoliticalQuantities.Pos(p.Size);
                total += s;
                if (p.IsUrban) urban += s;
                if (p.IsSoldier) soldiers += s;
            }
            var mk = r.Markets;
            if (Catalog.IronId < mk.Length) iron += PoliticalQuantities.Pos(mk[Catalog.IronId].LastProduced);
            if (Catalog.CoalId < mk.Length) energy += PoliticalQuantities.Pos(mk[Catalog.CoalId].LastSold);
            if (Catalog.CrudeOilId < mk.Length) energy += PoliticalQuantities.Pos(mk[Catalog.CrudeOilId].LastSold) * oilToCoal;
        }
        double pl = w.PriceLevel;
        double mil = !(pl > 0) ? 0 : PoliticalQuantities.Pos(PoliticalQuantities.Pos(w.LastMilitaryClaimPerMonth.Value) + PoliticalQuantities.Pos(w.LastSoldierPayPerMonth.Value)) / pl;
        return new ScoreTerms(soldiers, mil, iron, energy, urban, total);
    }

    internal static double[] ShareScores(IReadOnlyList<World> worlds, double[][]? shares = null)
    {
        var terms = new ScoreTerms[worlds.Count];
        for (int i = 0; i < terms.Length; i++) terms[i] = TermsOf(worlds[i]);
        return SharesFromTerms(terms, shares);
    }

    internal static double[] SharesFromTerms(ScoreTerms[] terms, double[][]? shares = null)
    {
        int n = terms.Length;
        var tot = new double[ScoreTerms.Count];
        for (int i = 0; i < n; i++)
            for (int t = 0; t < ScoreTerms.Count; t++) tot[t] += terms[i][t];
        var res = new double[n];
        for (int i = 0; i < n; i++)
        {
            double sum = 0; int used = 0;
            for (int t = 0; t < ScoreTerms.Count; t++)
            {
                double sh = tot[t] > 0 ? terms[i][t] / tot[t] : 0;
                if (shares is not null) shares[i][t] = sh;
                if (tot[t] > 0) { sum += sh; used++; }
            }
            res[i] = used > 0 ? 100.0 * (sum / used) : 0;
        }
        return res;
    }

    public static double[] Scores(IReadOnlyList<World> worlds) => ShareScores(worlds);

    internal static void StepStandingAll(IReadOnlyList<World> worlds)
    {
        var sc = ShareScores(worlds);
        for (int i = 0; i < worlds.Count; i++) StepStanding(worlds[i], sc[i]);
    }

    public readonly record struct ScoreLine(string Label, double Points);

    private static readonly string[] ShareLabels = ["兵の数", "軍事費", "鉄の産出", "動力の消費", "都市の人口", "総人口"];

    public static ScoreLine[][] Breakdowns(IReadOnlyList<World> worlds)
    {
        int n = worlds.Count;
        var res = new ScoreLine[n][];
        var shares = new double[n][];
        for (int i = 0; i < n; i++) shares[i] = new double[ScoreTerms.Count];
        ShareScores(worlds, shares);
        int used = 0;
        for (int t = 0; t < ScoreTerms.Count; t++)
        {
            double col = 0;
            for (int i = 0; i < n; i++) col += shares[i][t];
            if (col > 0) used++;
        }
        for (int i = 0; i < n; i++)
        {
            var l = new ScoreLine[ScoreTerms.Count];
            for (int t = 0; t < l.Length; t++) l[t] = new ScoreLine(ShareLabels[t], used > 0 ? 100.0 * shares[i][t] / used : 0);
            res[i] = l;
        }
        return res;
    }

    public static ScoreLine[] BreakdownOf(World w)
    {
        var l = NationWorldsOf(w);
        return Breakdowns(l)[l.IndexOf(w)];
    }

    public static Dictionary<int, double> ScoresByPower(Diplomacy d)
    {
        var ps = new List<Power>(); var ws = new List<World>();
        foreach (var p in d.Powers) if (p.Nation is { } nw) { ps.Add(p); ws.Add(nw); }
        var sc = Scores(ws);
        var res = new Dictionary<int, double>(ps.Count);
        for (int i = 0; i < ps.Count; i++) res[ps[i].Id] = sc[i];
        return res;
    }

    public static Dictionary<int, ScoreLine[]> BreakdownsByPower(Diplomacy d)
    {
        var ps = new List<Power>(); var ws = new List<World>();
        foreach (var p in d.Powers) if (p.Nation is { } nw) { ps.Add(p); ws.Add(nw); }
        var bd = Breakdowns(ws);
        var res = new Dictionary<int, ScoreLine[]>(ps.Count);
        for (int i = 0; i < ps.Count; i++) res[ps[i].Id] = bd[i];
        return res;
    }

    internal static List<World> NationWorldsOf(World w)
    {
        var l = new List<World>();
        var d = w.Diplomacy;
        if (d is not null) foreach (var p in d.Powers) if (p.Nation is { } nw) l.Add(nw);
        if (!l.Contains(w)) { l.Clear(); l.Add(w); }
        return l;
    }

    private static double ShareOf(World w)
    {
        var l = NationWorldsOf(w);
        return ShareScores(l)[l.IndexOf(w)];
    }

    public static double Finance(World w) =>
        w.Bank is { } b && w.TotalPopulation > 1 ? Math.Clamp(b.Loans.Value / (w.TotalPopulation * FinancePerCapitaDivisor), 0, FinanceCap) : 0;

    public static double MeanSatisfaction(World w)
    {
        double s = 0, n = 0;
        foreach (var p in w.AllPops()) if (Politics.IsMassPop(p)) { s += p.Satisfaction * p.Size; n += p.Size; }
        return n > 0 ? s / n : 0;
    }
}

public enum Doctrine { Balanced, Militarist, Industrialist, Modernizer }

public sealed class UtilityAgent : INationAgent
{
    private const double MilitaristBaseUtility = 2.5;
    private const double MilitaristEscapeThreshold = 0.5;
    private const double IndustrializationRoomTarget = 4.0;
    private const double UnrestSatisfactionThreshold = 0.62;
    private const double UModUnrestWeight = 0.3;
    private const double UIndRoomWeight = 1.2;
    private const double UModIndRoomWeight = 0.5;
    private const double UWelUnrestWeight = 2.5;
    private const double UWelFamineWeight = 2.0;
    private const double UnrestSSPressure = 1.0;
    private const double UFisWeight = 1.2;
    private const double ColonyDefenseWeight = 0.3;
    private const double FacilityBuildMinMargin = 0.05;
    private const double FacilityBuildIdleWeight = 2.0;
    private const double FacilityBuildOutlayShare = 0.10;
    private const double AiChainStarvedFill = 0.9;
    private const double AiTariffSurgeRatio = 1.5;
    private const double AiTariffImportFloorShare = 0.001;
    private const double TensionNormalizer = 100.0;
    private const double TensionUtilityWeight = 2.0;
    private const double SurvivalRatioCap = 3.0;
    private const double IndustrialistIndBias = 1.6;
    private const double IndustrialistModBias = 1.2;
    private const double ModernizerModBias = 1.5;
    private const double ModernizerWelBias = 1.2;
    internal const double GoldLiteracyLine = 0.75;
    internal const double AntitrustUrbanLine = 1.0 / 3.0;
    internal const double TrustbustUrbanLine = 0.5;
    internal const double MinimumWageUnempLowerLine = 0.5;
    internal const double MinimumWageBindLowerMin = 0.5;
    internal const double MinimumWageUnempRaiseLine = 0.25;
    internal const double MinimumWageBindRaiseMax = 1.0 / 3.0;
    private const double CompulsoryEduBufferMult = 3.0;
    private const double IndustrializeInvestFrac = 0.15;
    private const double ModernizeInvestFrac = 0.08;
    public const double OpeningModernizationFrac = ModernizeInvestFrac;
    private const double CornUrbanRepeal = 0.35;
    private const double CornAgrarianKeep = 0.15;
    private const double EmancipationRentRoom = 0.05;

    private static void StepLawToward(World w, NationPolicy p, PolicyAxis axis, int cur, int want, int max, System.Action<int> set)
    {
        want = System.Math.Clamp(want, 0, max);
        if (want == cur) return;
        int next = System.Math.Clamp(cur + (want > cur ? 1 : -1), 0, max);
        if (next == cur) return;
        var (ok, _, _) = Government.GateInfo(w, axis, want > cur, next);
        if (!ok) return;
        set(next);
    }

    private static void GateLaw(ref PolicyPreferences.Profile? shared, World w, PolicyAxis axis, int cur, int want, int max, System.Action<int> set)
    {
        want = System.Math.Clamp(want, 0, max);
        if (want == cur) { set(cur); return; }
        int next = System.Math.Clamp(cur + (want > cur ? 1 : -1), 0, max);
        var (ok, _, _) = Government.GateInfo(w, axis, want > cur, next, ref shared);
        if (ok) { set(next); return; }
        bool barred = !Government.LawPrereq(w, axis, next, w.Coef).Ok
                   || Government.BlockedByIrreversibility(w, axis, Government.Regressive(axis, want > cur));
        set(barred ? cur : next);
    }

    internal static void GateAllLawTargets(World w, NationPolicy p)
    {
        PolicyPreferences.Profile? shared = null;
        {
            long scanT0 = Instruments.On ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
            w.Cost[CostLedger.AiBureaucracyScans]++;
            BureaucracyFollowsWants(w, p, ref shared);
            if (Instruments.On) Instruments.BurTime(w, System.Diagnostics.Stopwatch.GetTimestamp() - scanT0);
        }
        GateLaw(ref shared, w, PolicyAxis.EducationLaw, (int)w.EducationLaw, (int)p.EducationLaw, (int)EducationLaw.Compulsory, v => p.EducationLaw = (EducationLaw)v);
        GateLaw(ref shared, w, PolicyAxis.Suffrage, (int)w.Suffrage, (int)p.Suffrage, (int)Suffrage.Universal, v => p.Suffrage = (Suffrage)v);
        GateLaw(ref shared, w, PolicyAxis.WomenRights, (int)w.WomenRights, (int)p.WomenRights, (int)WomenRightsLaw.Suffrage, v => p.WomenRights = (WomenRightsLaw)v);
        GateLaw(ref shared, w, PolicyAxis.Censorship, (int)w.Censorship, (int)p.Censorship, (int)CensorshipLaw.FreePress, v => p.Censorship = (CensorshipLaw)v);
        GateLaw(ref shared, w, PolicyAxis.LaborUnion, (int)w.LaborUnion, (int)p.LaborUnion, (int)LaborUnionLaw.Tolerated, v => p.LaborUnion = (LaborUnionLaw)v);
        GateLaw(ref shared, w, PolicyAxis.ShorterHours, (int)w.WorkHours, (int)p.WorkHours, (int)WorkHours.Unlimited, v => p.WorkHours = (WorkHours)v);
        GateLaw(ref shared, w, PolicyAxis.ChildLabor, w.ChildLaborAllowed ? 1 : 0, p.ChildLaborAllowed ? 1 : 0, 1, v => p.ChildLaborAllowed = v != 0);
        GateLaw(ref shared, w, PolicyAxis.ConscriptionLaw, (int)w.ConscriptionLaw, (int)p.ConscriptionLaw, (int)ConscriptionLaw.Universal, v => p.ConscriptionLaw = (ConscriptionLaw)v);
        GateLaw(ref shared, w, PolicyAxis.SecretBallot, (int)w.SecretBallot, (int)p.SecretBallot, (int)SecretBallotLaw.Secret, v => p.SecretBallot = (SecretBallotLaw)v);
        GateLaw(ref shared, w, PolicyAxis.Bureaucracy, (int)w.Bureaucracy, (int)p.Bureaucracy, (int)BureaucracyLaw.Professional, v => p.Bureaucracy = (BureaucracyLaw)v);
        GateLaw(ref shared, w, PolicyAxis.Antitrust, (int)w.Antitrust, (int)p.Antitrust, (int)AntitrustLaw.Trustbust, v => p.Antitrust = (AntitrustLaw)v);
        if (w.Coef.GuildLawGate > 0) GateLaw(ref shared, w, PolicyAxis.Guild, (int)w.Guild, (int)p.Guild, (int)GuildLaw.None, v => p.Guild = (GuildLaw)v);
        GateLaw(ref shared, w, PolicyAxis.Inheritance, (int)w.Inheritance, (int)p.Inheritance, (int)InheritanceLaw.All, v => p.Inheritance = (InheritanceLaw)v);
        GateLaw(ref shared, w, PolicyAxis.MinimumWage, (int)w.MinimumWage, (int)p.MinimumWage, (int)MinimumWageLaw.Sufficiency, v => p.MinimumWage = (MinimumWageLaw)v);
        GateLaw(ref shared, w, PolicyAxis.WartimeRationing, w.WartimeRationing ? 1 : 0, p.WartimeRationing ? 1 : 0, 1, v => p.WartimeRationing = v != 0);
        GateLaw(ref shared, w, PolicyAxis.MilitaryExport, w.MilitaryExportBan ? 1 : 0, p.MilitaryExportBan ? 1 : 0, 1, v => p.MilitaryExportBan = v != 0);
        if (w.Coef.RebelArmsMarketEnabled > 0) GateLaw(ref shared, w, PolicyAxis.CivilianArms, w.CivilianArmsBan ? 1 : 0, p.CivilianArmsBan ? 1 : 0, 1, v => p.CivilianArmsBan = v != 0);
        GateLaw(ref shared, w, PolicyAxis.Nationalization, w.NationalizeRailways ? 1 : 0, p.NationalizeRailways ? 1 : 0, 1, v => p.NationalizeRailways = v != 0);
        GateLaw(ref shared, w, PolicyAxis.Nationalization, w.NationalizeMines ? 1 : 0, p.NationalizeMines ? 1 : 0, 1, v => p.NationalizeMines = v != 0);
        GateLaw(ref shared, w, PolicyAxis.Nationalization, w.NationalizeMilitary ? 1 : 0, p.NationalizeMilitary ? 1 : 0, 1, v => p.NationalizeMilitary = v != 0);
        GateLaw(ref shared, w, PolicyAxis.Nationalization, w.NationalizeOil ? 1 : 0, p.NationalizeOil ? 1 : 0, 1, v => p.NationalizeOil = v != 0);
        GateLaw(ref shared, w, PolicyAxis.Nationalization, w.NationalizeElectricity ? 1 : 0, p.NationalizeElectricity ? 1 : 0, 1, v => p.NationalizeElectricity = v != 0);
        GateLaw(ref shared, w, PolicyAxis.NationalizationAll, w.NationalizeAll ? 1 : 0, p.NationalizeAll ? 1 : 0, 1, v => p.NationalizeAll = v != 0);
        GateLaw(ref shared, w, PolicyAxis.OpiumPossession, (int)w.OpiumPossession, (int)p.OpiumPossession, (int)ReformLevel.Full, v => p.OpiumPossession = (ReformLevel)v);
        GateLaw(ref shared, w, PolicyAxis.OpiumExport, w.OpiumExportBan ? 1 : 0, p.OpiumExportBan ? 1 : 0, 1, v => p.OpiumExportBan = v != 0);
        GateLaw(ref shared, w, PolicyAxis.CapitalMobility, w.CapitalControlLaw ? 1 : 0, p.CapitalControlTarget ? 1 : 0, 1, v => p.CapitalControlTarget = v != 0);
        void GateTax(PolicyAxis a, double cur, double tgt, Action<double> set)
            => GateLaw(ref shared, w, a, Government.TaxStage(a, cur), Government.TaxStage(a, tgt), Government.TaxLawStages, v => set(Government.TaxValue(a, v)));
        GateTax(PolicyAxis.IncomeTax, w.TaxRate.Value, p.IncomeTaxTarget, v => p.IncomeTaxTarget = v);
        GateTax(PolicyAxis.ProgressiveTax, w.ProgressiveTax, p.ProgressiveTaxTarget, v => p.ProgressiveTaxTarget = v);
        GateTax(PolicyAxis.PollTax, w.PollTax, p.PollTaxTarget, v => p.PollTaxTarget = v);
        GateTax(PolicyAxis.LandTax, w.LandTax, p.LandTaxTarget, v => p.LandTaxTarget = v);
        GateTax(PolicyAxis.EducationSpending, w.EducationSpending, p.EducationSpendingTarget, v => p.EducationSpendingTarget = v);
        GateLaw(ref shared, w, PolicyAxis.Coercion, Government.MilitaryStage(w.MilitarySpending.Value), Government.MilitaryStage(p.MilitarySpendingTarget),
                Government.MilitaryLawStages, v => p.MilitarySpendingTarget = Math.Max(1, v) / (double)Government.MilitaryLawStages);
    }

    internal static void BureaucracyFollowsWants(World w, NationPolicy p, ref PolicyPreferences.Profile? shared)
    {
        int wantMask = 0, blockMask = 0, feasMask = 0; var need = w.Bureaucracy; var applyNeed = w.Bureaucracy; var burIn = p.Bureaucracy; var coef = w.Coef;
        PolicyPreferences.Profile? sh = shared;
        void Scan(PolicyAxis axis, int cur, int tgt)
        {
            if (!(tgt > cur)) return;
            int next = cur + 1;
            bool okNow = Government.LawPrereq(w, axis, next, coef).Ok;
            if (okNow && w.Bureaucracy == BureaucracyLaw.Spoils) return;
            if (Government.LawPrereq(w, axis, next, coef, BureaucracyLaw.Spoils).Ok || !Government.LawPrereq(w, axis, next, coef, BureaucracyLaw.Professional).Ok) return;
            int bit = axis switch { PolicyAxis.SocialSecurity => 1, PolicyAxis.Antitrust => 2, PolicyAxis.Nationalization => 4, PolicyAxis.NationalizationAll => 8, PolicyAxis.ProgressiveTax => 16, _ => 32 };
            wantMask |= bit;
            if (okNow) return;
            blockMask |= bit;
            var lawNeed = w.Bureaucracy;
            for (var level = w.Bureaucracy + 1; level <= BureaucracyLaw.Professional; level++)
                if (Government.LawPrereq(w, axis, next, coef, level).Ok) { lawNeed = level; break; }
            if (lawNeed > need) need = lawNeed;
            bool feasible = Government.GateInfo(w, axis, true, next, ref sh, lawNeed).Ok;
            if (feasible) feasMask |= bit;
            if (feasible && lawNeed > applyNeed) applyNeed = lawNeed;
        }
        foreach (var law in Government.LevelLaws) Scan(law.Axis, law.Get(w), law.Target(w, p));
        foreach (var law in Government.RateLaws) Scan(law.Axis, Government.TaxStage(law.Axis, law.Get(w)), Government.TaxStage(law.Axis, law.Target(p)));
        shared = sh;
        if (applyNeed > w.Bureaucracy && p.Bureaucracy < applyNeed) p.Bureaucracy = applyNeed;
        if (Instruments.On && (wantMask != 0 || p.Bureaucracy != w.Bureaucracy)) Instruments.BurObserve(w, burIn, p.Bureaucracy, wantMask, blockMask, feasMask, blockMask != 0 ? (int)need : -1);
    }


    public static double AutoModernizationFrac(World w, double fiscalStress)
    {
        double easeInd = 1 - fiscalStress;
        double indGap = w.ModernizeReturnShare();
        return System.Math.Max(0, indGap * easeInd * ModernizeInvestFrac);
    }

    private const double DumpingExportRatio = 1.3;
    private const double DumpingTariff = -0.10;
    internal const double RecessionDrawdownThreshold = 0.08;
    private const double PreTighteningInterestRate = 0.32;
    private const double EasingInterestRate = 0.05;
    private const double OverheatHeatThreshold = 0.7;
    private const double TighteningInterestRate = 0.45;
    private const double NeutralInterestRate = 0.22;
    private const double EducationEaseFloor = 0.3;
    private const double EducationEaseSpan = 0.7;
    private const double FiscalTaxStressMult = 0.4;
    private const double OpiumBanHysteresis = 0.3;
    private const double OpiumFullBanRatio = 2.0;
    private const double FireSaleFiscalStress = 0.7;
    private const double MobilizeOutmatchRatio = 1.2;
    private const double MobilizeLosingScore = 0.5;
    private const double AiElectionIncumbentAffinity = 0.5;
    private const double AiElectionMinPush = 0.05;

    private const double MilitaristDoctrineDegree = 0.25;

    public static Doctrine DoctrineFrom(World w)
    {
        var (trad, modern, mass) = InterestGroups.Blocs(w);
        if (trad >= modern && trad >= mass) return PoliticalQuantities.MilitaryPoliticalRatio(w) >= MilitaristDoctrineDegree ? Doctrine.Militarist : Doctrine.Balanced;
        if (modern >= mass) return Doctrine.Industrialist;
        return Doctrine.Modernizer;
    }

    internal enum Stance { Modernize, Industrialize, Welfare, Fiscal, Militarize }

    internal readonly record struct StanceBreakdown(
        double UMod, double UInd, double UWel, double UFis, double UMil,
        double MilitaristBase, double TensionTerm, double ColonyDefense, double Survival, double Tension,
        string ThreatFrom, double ThreatRatio);


    public static double FiscalBufferOf(World w)
        => Math.Max(1.0, (w.Coef.FiscalStressBufferMonths * (w.LastInterestPerMonth + w.LastAdminClaimPerMonth + w.LastMilitaryClaimPerMonth)).Value);

    public static double OpulenceLineOf(World w)
        => (FiscalBufferOf(w) + (w.Coef.FiscalBufferSoldierPayWeight * w.Coef.FiscalStressBufferMonths * w.LastSoldierPayPerMonth).Value) * OpulenceBufferQuarters;

    public static double FiscalStressOf(World w)
    {
        double s = 1.0 - w.FiscalFundingRatio.Value;
        return s >= 0 ? (s <= 1 ? s : 1.0) : (s < 0 ? 0 : 1.0);
    }

    internal static double InterestBurdenStressOf(World w)
    {
        double interest = w.LastInterestPerMonth.Value;
        double excess = interest - w.Coef.MaxInterestShareOfRevenue.Value * w.LastRevenuePerMonth.Value;
        return excess > 0 ? excess / interest : 0.0;
    }

    internal static double SecurityShare(World w, bool army = false)
    {
        double t = army ? w.SecurityThreatArmy : w.SecurityThreat, own = w.SecurityThreatOwnMass;
        double x = t / (t + own);
        return x >= 0 && x <= 1 ? x : 0;
    }

    internal static double ArmyShareOrNaN(World w)
    {
        double x = w.SecurityThreatArmy / (w.SecurityThreatArmy + w.SecurityThreatOwnMass);
        return x >= 0 && x <= 1 ? x : double.NaN;
    }

    internal const double ConscriptionRaiseShare = 0.5;
    internal const double ConscriptionLowerMargin = 1.0 / 3.0;

    internal static ConscriptionLaw ConscriptionLowerTarget(ConscriptionLaw cur, double share, ConscriptionLaw hold)
    {
        double keep = share / ConscriptionRaiseShare + ConscriptionLowerMargin;
        if (!(PoliticalQuantities.ConscriptionDepth(cur) > keep)) return hold < cur ? cur : hold;
        var k = cur;
        while (k > ConscriptionLaw.Volunteer && PoliticalQuantities.ConscriptionDepth(k) > keep) k--;
        return k;
    }

    internal static double SecurityStageTarget(World w)
    {
        double lo = 1.0 / Government.MilitaryLawStages, hi = (Government.MilitaryLawStages - 1.0) / Government.MilitaryLawStages;
        return lo + (hi - lo) * SecurityShare(w);
    }

    private static Stance ChooseStance(World w) => ChooseStance(w, out _);

    /// <summary>(1385) c147wb AI の不穏。旗 SatReadersToDiscontent 1＝大衆の層の不満度の平均 D̄（Politics.AvgMassDiscontent・0.62 との差をやめる）／旗 0＝max(0, 0.62−満足度)（今の式）。
    /// ChooseStance と社会保障の床の 2 か所が同じ関数を呼ぶ。</summary>
    internal static double UnrestOf(World w, double sat)
        => w.Coef.SatReadersToDiscontent > 0 ? Politics.AvgMassDiscontent(w) : Math.Max(0, UnrestSatisfactionThreshold - sat);

    internal static Stance ChooseStance(World w, out StanceBreakdown bd)
    {
        var doctrine = DoctrineFrom(w);
        double sat = NationalScore.MeanSatisfaction(w);
        double indRoom = IndustrializationRoomTarget * w.ModernizeReturnShare();
        double unrest = UnrestOf(w, sat);
        double fiscalNeed = FiscalStressOf(w);

        double famine = Math.Max(0, 1.0 - w.FoodFill);
        double uMod = UModIndRoomWeight * indRoom + UModUnrestWeight * unrest;
        double uInd = UIndRoomWeight * indRoom;
        double uWel = UWelUnrestWeight * unrest + UWelFamineWeight * famine;
        double uFis = UFisWeight * fiscalNeed;
        double tension = w.Diplomacy is { } dip && w.SelfPower is { } sp ? dip.MaxTension(sp) : 0;
        double colonyUnits = w.SelfPower?.ColonyMassUnits() ?? 0;
        double colonyDefense = ColonyDefenseWeight * w.Coef.ColoniesForFullColonial
                             * Math.Min(1.0, colonyUnits / Math.Max(1.0, w.Coef.ColoniesForFullColonial));
        double survival = 0;
        string threatFrom = "-"; double threatRatio = 0;
        if (w.Diplomacy is { } dipS && w.SelfPower is { } spS)
        {
            double maxReach = 0;
            foreach (var q in dipS.Powers)
                if (q != spS && q.Nation is not null)
                {
                    double nextMax = Math.Max(maxReach, dipS.FrontPush(q, spS, w.Coef));
                    if (nextMax != maxReach) threatFrom = q.Name;
                    maxReach = nextMax;
                }
            double ratio = maxReach / Math.Max(1e-9, spS.LandPower);
            threatRatio = ratio;
            if (ratio > w.Coef.SurvivalThreatRatio)
            {
                survival = w.Coef.SurvivalMilitaryWeight * PoliticalQuantities.ClampOr(ratio - w.Coef.SurvivalThreatRatio, 0, SurvivalRatioCap, 0);
                survival *= 1.0 - (1.0 - PoliticalQuantities.Clamp01(w.Coef.SurvivalThreatRatio / ratio));
            }
        }
        double militaristBase = (doctrine == Doctrine.Militarist ? MilitaristBaseUtility : 0.0) * (1.0 - PoliticalQuantities.Clamp01((fiscalNeed - MilitaristEscapeThreshold) / MilitaristEscapeThreshold));
        double tensionTerm = TensionUtilityWeight * PoliticalQuantities.Clamp01(tension / TensionNormalizer);
        double uMil = militaristBase + tensionTerm + colonyDefense + survival;

        double revolt = Politics.AvgMassMilitancy(w) * (1.0 - w.Legitimacy);
        uWel += w.Coef.StanceRevoltWelfare * revolt;
        uMil += w.Coef.StanceRevoltCoercion * revolt;
        uFis += w.Coef.StanceDrawdownFiscal * w.GdpDrawdown.Value;

        switch (doctrine)
        {
            case Doctrine.Industrialist: uInd *= IndustrialistIndBias; uMod *= IndustrialistModBias; break;
            case Doctrine.Modernizer: uMod *= ModernizerModBias; uWel *= ModernizerWelBias; break;
        }

        Stance best = Stance.Modernize; double bestU = uMod;
        if (uInd > bestU) { best = Stance.Industrialize; bestU = uInd; }
        if (uWel > bestU) { best = Stance.Welfare; bestU = uWel; }
        if (uFis > bestU) { best = Stance.Fiscal; bestU = uFis; }
        if (uMil > bestU) { best = Stance.Militarize; bestU = uMil; }
        bd = new StanceBreakdown(uMod, uInd, uWel, uFis, uMil, militaristBase, tensionTerm, colonyDefense, survival, tension, threatFrom, threatRatio);
        return best;
    }

    private static void RaiseWomenRights(NationPolicy p, WomenRightsLaw to)
    { if (to > p.WomenRights) p.WomenRights = to; }

    internal static void DecideModernLaws(World w, NationPolicy p, Stance s)
    {
        bool atWar = w.Diplomacy is { } dip && w.SelfPower is { } sp && dip.Wars.Any(war => war.A == sp || war.B == sp);
        p.WartimeRationing = atWar;
        p.MilitaryExportBan = atWar || s == Stance.Militarize;
        if (w.Coef.RebelArmsMarketEnabled > 0) p.CivilianArmsBan = w.Suffrage < Suffrage.AdultMale;
        var csTop = w.ConscriptionLaw == ConscriptionLaw.Universal || Government.UniversalConscriptionOpen(w) ? ConscriptionLaw.Universal : ConscriptionLaw.Selective;
        double armyShare = ArmyShareOrNaN(w);
        if (!atWar && w.ConscriptionPreWar >= 0) p.ConscriptionLaw = w.ConscriptionLaw;
        else if (armyShare >= ConscriptionRaiseShare || (atWar && w.Mobilized)) p.ConscriptionLaw = csTop;
        else if (!atWar && armyShare >= 0) p.ConscriptionLaw = ConscriptionLowerTarget(w.ConscriptionLaw, armyShare, p.ConscriptionLaw);
        else if (atWar && p.ConscriptionLaw < w.ConscriptionLaw) p.ConscriptionLaw = w.ConscriptionLaw;
        if (w.Suffrage >= Suffrage.AdultMale) p.SecretBallot = SecretBallotLaw.Secret;
        if (AntitrustTargetFromBoard(w) is { } antiTarget)
            p.Antitrust = antiTarget;
        if (GuildTargetFromBoard(w) is { } guildTarget)
            p.Guild = guildTarget;
        {
            var mwChosen = MinimumWageTargetFromFloor(w);
            if (mwChosen is { } mwTarget)
                p.MinimumWage = mwTarget;
        }
        if (w.Polity == Polity.ProletarianDictatorship)
        {
            p.NationalizeRailways = p.NationalizeMines = p.NationalizeMilitary = p.NationalizeOil
                = p.NationalizeElectricity = p.NationalizeAll = true;
        }
        else if (w.GoverningParty == (int)Parliament.Party.Socialist)
            p.NationalizeRailways = p.NationalizeElectricity = true;

        {
            double inflowRate = w.TotalPopulation > 1e-9 ? System.Math.Max(0, w.LastImmigration) / w.TotalPopulation : 0;
            double unemployment = PolicyPreferences.AvgUnemployment(w);
            double pressure = w.Coef.ImmigrationRestrictionPrefScale * inflowRate * (1.0 + unemployment);
            if (!(pressure >= 0)) pressure = 0;
            int immWant = (int)p.ImmigrationRestriction;
            if (w.ImmigrationRestriction != ImmigrationLaw.Closed
                && pressure > w.Coef.ImmigrationRestrictionPrefScale * w.Coef.ImmigrationReceiveCapPerQuarter.Value
                              * (w.ImmigrationRestriction == ImmigrationLaw.Open ? 1.0 : w.Coef.ImmigrationSelectiveFrac.Value))
                immWant = System.Math.Max(immWant, (int)w.ImmigrationRestriction + 1);
            if (w.UnequalTreatyInForce) immWant = (int)ImmigrationLaw.Open;
            StepLawToward(w, p, PolicyAxis.ImmigrationRestriction, (int)p.ImmigrationRestriction, immWant, (int)ImmigrationLaw.Closed,
                v => p.ImmigrationRestriction = (ImmigrationLaw)v);

            if (w.Polity != Polity.ProletarianDictatorship)
            {
                int relWant = DoctrineFrom(w) switch
                {
                    Doctrine.Modernizer => (int)StateReligionLaw.ReligiousFreedom,
                    Doctrine.Industrialist => (int)StateReligionLaw.InnerFreedom,
                    _ => (int)p.StateReligion,
                };
                relWant = System.Math.Max(relWant, System.Math.Min((int)p.StateReligion, (int)StateReligionLaw.ReligiousFreedom));
                StepLawToward(w, p, PolicyAxis.StateReligion, (int)p.StateReligion, relWant, (int)StateReligionLaw.ReligiousFreedom,
                    v => p.StateReligion = (StateReligionLaw)v);
            }
        }
    }

    public static void DecideElection(World w)
    {
        var c = w.Coef;
        if (w.PendingElection is not { } ev) return;
        double margin = w.ElectionMargin;
        if (!(margin > 0)) margin = 0;
        double danger = 0;
        if (c.NoConfidenceThreshold > 1e-9 && w.GoverningParty is >= 0 and < 3)
        {
            double share = w.VoteShares[w.GoverningParty];
            double d = 1.0 - (share - c.NoConfidenceThreshold) / c.NoConfidenceThreshold;
            if (d > 1.0) d = 1.0;
            if (!(d > 0)) d = 0;
            danger = d;
        }
        double stress = FiscalStressOf(w);
        if (!(stress >= 0)) stress = 1.0;
        if (stress > 1.0) stress = 1.0;
        double spent = w.MandateSpent;
        if (!(spent > 0)) spent = 0;
        double lever = danger * (1.0 - stress) / (1.0 + spent);
        if (lever > 1.0) lever = 1.0;
        if (!(lever >= AiElectionMinPush)) { w.ResolveElection(-1, 0); return; }
        int bloc = DoctrineFrom(w) switch { Doctrine.Industrialist => 1, Doctrine.Modernizer => 2, _ => 0 };
        int bestK = -1; double bestU = 0;
        for (int k = 0; k < ev.AgendaIdeologies.Length; k++)
        {
            var (trad, modern, mass, net) = PolicyPreferences.Stance(w, ev.AgendaAxes[k], ev.AgendaIncrease[k]);
            double align = bloc == 0 ? trad : bloc == 1 ? modern : mass;
            if (PoliticalQuantities.FamilyOf(ev.AgendaIdeologies[k]) == w.GoverningParty) align += AiElectionIncumbentAffinity;
            if (!(align > 0)) continue;
            double price = net < 0 ? -net : 0;
            if (!(price <= margin)) continue;
            double afford = margin > 1e-9 ? 1.0 - price / margin : (price > 0 ? 0 : 1.0);
            if (!(afford > 0)) continue;
            double u = align * afford;
            if (u > bestU) { bestU = u; bestK = k; }
        }
        if (bestK < 0) { w.ResolveElection(-1, 0); return; }
        w.ResolveElection(ev.AgendaIdeologies[bestK], lever);
    }


    internal static bool GoldWishFromBoard(World w) => w.MonetaryLaw == MonetaryStandardLaw.Gold || PoliticalQuantities.AvgLiteracy(w, coreOnly: true) > GoldLiteracyLine;

    internal static double HomeUrbanShare(World w)
    {
        double total = 0, urban = 0;
        foreach (var r in w.Regions)
        {
            if (r.Tier == RegionTier.Colonial) continue;
            total += r.SumPopulation(); urban += r.SumUrbanPopulation();
        }
        return total > 1e-9 ? urban / total : 0.0;
    }

    internal static AntitrustLaw? AntitrustTargetFromBoard(World w)
    {
        double u = HomeUrbanShare(w);
        if (u > TrustbustUrbanLine) return AntitrustLaw.Trustbust;
        if (u > AntitrustUrbanLine) return w.Antitrust >= AntitrustLaw.CartelBan ? null : AntitrustLaw.CartelBan;
        return null;
    }

    internal static GuildLaw? GuildTargetFromBoard(World w)
    {
        if (!(w.Coef.GuildLawGate > 0)) return null;
        double u = HomeUrbanShare(w);
        if (u > TrustbustUrbanLine) return GuildLaw.None;
        if (u > AntitrustUrbanLine) return w.Guild >= GuildLaw.Limited ? null : GuildLaw.Limited;
        return null;
    }

    internal static MinimumWageLaw? MinimumWageTargetFromFloor(World w)
    {
        var cost = w.Cost;
        var h = MinimumWageFloor.ReadHomeLabor(w, cost);
        double u = h.Unemployment;
        var cur = w.MinimumWage;
        double bCur = double.NaN, bNext = double.NaN;
        if (u >= MinimumWageUnempLowerLine && cur > MinimumWageLaw.None) bCur = MinimumWageFloor.BindShare(h, cur, cost);
        if (u < MinimumWageUnempRaiseLine && cur < MinimumWageLaw.Sufficiency) bNext = MinimumWageFloor.BindShare(h, cur + 1, cost);
        return MinimumWageFloorWant(cur, u, bCur, bNext);
    }

    internal static MinimumWageLaw? MinimumWageFloorWant(MinimumWageLaw cur, double unemployment, double bindCur, double bindNext)
    {
        if (double.IsNaN(unemployment)) return null;
        if (cur > MinimumWageLaw.None && unemployment >= MinimumWageUnempLowerLine && bindCur >= MinimumWageBindLowerMin) return cur - 1;
        if (cur < MinimumWageLaw.Sufficiency && unemployment < MinimumWageUnempRaiseLine && bindNext >= 0 && bindNext <= MinimumWageBindRaiseMax) return cur + 1;
        return cur;
    }

    public void DecideQuarter(World w)
    {
        bool goldWish = GoldWishFromBoard(w);
        w.Policy.MonetaryTarget = goldWish ? 2 : 1;
        {
            double cover = w.SpecieCoverRatio();
            w.Policy.CapitalControlTarget = cover < w.Coef.GoldCoverMin.Value;
        }
        if (w.SelfPower is { } mSelf && w.Diplomacy is { } mDip)
        {
            bool mAtWar = mDip.AtWarAny(mSelf);
            double mLosing = 0;
            double mScale = mAtWar ? mDip.WarScale(mSelf, w.Coef, out mLosing) : 0;
            if (!mAtWar) w.Mobilized = false;
            else if (mScale >= MobilizeOutmatchRatio || mLosing > MobilizeLosingScore) w.Mobilized = true;
            else if (mScale < 1.0 / MobilizeOutmatchRatio && !(mLosing > 0)) w.Mobilized = false;
            if (Instruments.On) Instruments.Mobilization(w, mAtWar, mScale, mLosing);
        }
        Apply(w, ChooseStance(w));
        if (!w.OnGoldStandard && w.CurrencyReformCooldown <= 0
            && Fiscal.CurrencyMemoryBroken(w.CumulativeDevaluation, w.NominalGdpPerMonth.Value, w.Coef.CapitalFlightDistrustThreshold)
            && w.NoDevaluationMonths >= w.Coef.CurrencyReformCalmMonths)
            w.RequestCurrencyReform(w.ExchangeRate);
        ReactProtectiveTariff(w);
        SetNavalUpkeepShare(w);
        TryTradeBias(w);
        TryBuildFacility(w);
    }

    internal static void RelieveOpulence(World w)
    {
        if (w.SelfPower is null) return;
        double buffer = OpulenceLineOf(w);
        if (!(buffer > 1e-9) || w.Treasury.Value <= buffer) return;
        double mul = Math.Max(OpulenceTaxFloor, buffer / w.Treasury.Value);
        mul = 1.0 - (1.0 - mul);
        var p = w.Policy;
        p.IncomeTaxTarget *= mul; p.PollTaxTarget *= mul;
        p.LandTaxTarget *= mul; p.ProgressiveTaxTarget *= mul; p.TariffTarget *= mul;
    }
    internal const double OpulenceBufferQuarters = 8.0;
    private const double OpulenceTaxFloor = 0.25;

    private double[]? _importEma;
    private void ReactProtectiveTariff(World w)
    {
        if (w.ImportsByGood is not { } imp) return;
        if (_importEma is not { } ema || ema.Length != imp.Length)
        {
            _importEma = (double[])imp.Clone();
            return;
        }
        double floor = (AiTariffImportFloorShare * w.NominalGdpPerMonth).Value;
        bool surge = false;
        for (int g = 0; g < imp.Length; g++)
        {
            double cur = imp[g], prev = ema[g];
            ema[g] = PoliticalQuantities.Toward(prev, cur, w.Coef.AiTariffEmaRate);
            if (surge) continue;
            if (!(cur > floor)) continue;
            if (!(prev > 1e-9)) continue;
            if (!(cur > prev * AiTariffSurgeRatio)) continue;
            if (!DomesticMarginNegative(w, g)) continue;
            surge = true;
        }
        if (!surge)
        {
            if (w.Coef.AiTariffRelaxation > 0 && w.Policy.TariffTarget > 0)
                w.Policy.TariffTarget = System.Math.Max(0, w.Policy.TariffTarget - w.Coef.TariffReactionMax * w.Coef.AiTariffRelaxation);
            return;
        }
        double want = System.Math.Min(1.0, w.Tariff + w.Coef.TariffReactionMax);
        if (want > w.Policy.TariffTarget) w.Policy.TariffTarget = want;
    }

    private static bool DomesticMarginNegative(World w, int goodId)
    {
        double num = 0, den = 0;
        foreach (var rg in w.Regions)
        {
            foreach (var pr in rg.Producers)
                if (pr.GoodId == goodId && !pr.Bankrupt)
                {
                    double sc = Math.Max(0, pr.Scale);
                    num += pr.SmoothedMargin * sc; den += sc;
                }
        }
        return den > 1e-9 && num < 0;
    }

    internal static void SetNavalUpkeepShare(World w)
    {
        if (w.SelfPower is not { } sp || w.Diplomacy is null) return;
        double share = ArmyShape.NavalSpendShareOf(sp);
        if (!(share >= 0)) return;
        var p = w.Policy;
        double total = PoliticalQuantities.Clamp01(p.ArmyUpkeepTarget + p.NavyUpkeepTarget);
        p.NavyUpkeepTarget = total * share;
        p.ArmyUpkeepTarget = total - p.NavyUpkeepTarget;
    }

    private static void TryTradeBias(World w)
    {
        if (w.SelfPower is null) return;
        if (w.Coef.TradeBiasStrength <= 0) return;
        bool broke = FiscalStressOf(w) > TradeBiasStressCeiling;
        for (int g = 0; g < Catalog.Goods.Length; g++)
        {
            if (broke) { if (w.TradeBiasOf(g) != 0) { w.SetTradeBias(g, 0); if (Instruments.On) w.DiagSfStressFoldCum++; } continue; }
            double stock = 0, target = 0;
            foreach (var r in w.Regions)
            {
                if (g >= r.Markets.Length) continue;
                stock += r.Markets[g].Stock; target += r.Markets[g].TargetStock;
            }
            if (!(target > 1e-9)) { if (w.TradeBiasOf(g) != 0) w.SetTradeBias(g, 0); continue; }
            double ratio = stock / target;
            int want = ratio < TradeBiasShortageRatio ? 1 : ratio > TradeBiasGlutRatio ? -1 : 0;
            if (want != w.TradeBiasOf(g)) w.SetTradeBias(g, want);
        }
    }
    private const double TradeBiasStressCeiling = 0.4;
    private const double TradeBiasShortageRatio = 0.5;
    private const double TradeBiasGlutRatio = 2.0;

    public readonly record struct FacilityBuildValue(double Score, double Excess, bool WorldOpened);

    public static FacilityBuildValue FacilityBuildScore(World w, Region rg, Producer pr) => FacilityBuildScore(w, rg, pr, null);

    internal static FacilityBuildValue FacilityBuildScore(World w, Region rg, Producer pr, RootSupply? rs)
    {
        if (pr.GoodId < 0 || pr.GoodId >= Catalog.RefPrices.Length || pr.GoodId >= rg.Markets.Length) return new(double.NaN, double.NaN, false);
        if (pr.RecipeName == Simulation.HouseholdRecipeName) return new(double.NaN, double.NaN, false);
        if (!(Discovery.Gate(rg, pr) > 0)) return new(double.NaN, double.NaN, false);
        double rp = Catalog.RefPrices[pr.GoodId] * Math.Max(rg.PriceLevel, 1e-9);
        if (!(rp > 1e-9)) return new(double.NaN, double.NaN, false);
        double eff = rg.Markets[pr.GoodId].Price / rp - 1.0;
        bool worldOpened = false;
        if (w.WorldGoodsPrices is { } wgp && pr.GoodId < wgp.Length)
        {
            double worldRel = wgp[pr.GoodId] / rp - 1.0 - w.Coef.TransportMargin;
            if (worldRel > eff) { eff = worldRel; worldOpened = true; }
        }
        double idle = 0;
        if (pr.LaborPopIndex >= 0 && pr.LaborPopIndex < rg.Pops.Count)
        {
            var lp = rg.Pops[pr.LaborPopIndex];
            if (lp.Size > 1e-9) idle = PoliticalQuantities.Clamp01(lp.UnemploymentRate.Value);
        }
        rs ??= new RootSupply(w);
        if (!rs.Of(pr)) return new(double.NaN, eff, worldOpened);
        return new(eff * (1.0 + FacilityBuildIdleWeight * idle), eff, worldOpened);
    }

    private static void TryBuildFacility(World w)
    {
        if (w.SelfPower is null) return;
        if (!(w.FiscalFundingRatio.Value >= 1)) { if (Instruments.On) Instruments.AiBuildReturn(w, funding: true); return; }
        double buffer = FiscalBufferOf(w);
        double monthlyBuild = 0;
        var pendingRegion = new int[w.Regions.Count];
        for (int ri0 = 0; ri0 < w.Regions.Count; ri0++)
        {
            var rg0 = w.Regions[ri0];
            foreach (var pr0 in rg0.Producers)
            {
                if (pr0.ConstructionLeft > 0)
                {
                    monthlyBuild += Calendar.PerDayToPerMonth(pr0.ConstructionPayPerDay);
                    monthlyBuild += World.ConstructionMaterialPerMonth(rg0, pr0, w.Coef);
                }
                int pw = Math.Max(0, pr0.StateTarget - World.StateUnits(pr0, w.Coef));
                if (pw > 0 && pr0.ConstructionLeft <= 0) { double e0 = NewFacilityOutlayPerMonth(w, rg0, pr0); monthlyBuild += e0 > 0 ? e0 : 0; }
                pendingRegion[ri0] += pw;
            }
        }
        double outlayCap = FacilityBuildOutlayShare * buffer;
        if (Instruments.On) Instruments.AiBuildEnter(w, outlayCap - monthlyBuild, buffer);
        if (monthlyBuild > outlayCap) { if (Instruments.On) Instruments.AiBuildReturn(w, funding: false); return; }
        var bestGr = new int[w.Regions.Count]; var bestScoreR = new double[w.Regions.Count]; for (int i = 0; i < bestGr.Length; i++) bestGr[i] = -1;
        void Consider(int cri, int gid, double sc)
        {
            if (sc > bestScoreR[cri]) { bestScoreR[cri] = sc; bestGr[cri] = gid; }
        }
        var firstOf = new Producer?[Catalog.Goods.Length];
        var rootSupply = new RootSupply(w);
        for (int ri = 0; ri < w.Regions.Count; ri++)
        {
            var rg = w.Regions[ri];
            if (pendingRegion[ri] > 0) continue;
            System.Array.Clear(firstOf);
            foreach (var pr in rg.Producers)
            {
                if (pr.GoodId >= 0 && pr.GoodId < firstOf.Length) firstOf[pr.GoodId] ??= pr;
                if (pr.ConstructionLeft > 0 || pr.Bankrupt) continue;
                if (pr.GoodId < 0 || pr.GoodId >= Catalog.Goods.Length) continue;
                if (Catalog.Goods[pr.GoodId].Tier == GoodTier.Service) continue;
                var val = FacilityBuildScore(w, rg, pr, rootSupply);
                if (double.IsNaN(val.Score))
                {
                    if (Instruments.On && val.Excess > FacilityBuildMinMargin) Instruments.AiBuildRootDropped(w, pr.GoodId, rootSupply is { } rsx ? rsx.Blocker : -1);
                    continue;
                }
                double eff = val.Excess;
                bool worldOpened = val.WorldOpened;
                if (eff > FacilityBuildMinMargin)
                {
                    var chain = ProductionDiag.ExplainCost(w, rg, firstOf[pr.GoodId]!);
                    int starved = -1; double minFill = AiChainStarvedFill;
                    foreach (var it in chain.Items)
                        if (!it.IsLabor && it.Qty > 1e-9 && it.Fill < minFill) { minFill = it.Fill; starved = it.GoodId; }
                    if (starved >= 0 && starved < Catalog.Goods.Length && starved < Catalog.RefPrices.Length
                        && Catalog.Goods[starved].Tier != GoodTier.Service
                        && World.FindFactory(rg, starved) is { ConstructionLeft: <= 0, Bankrupt: false } upstream && Discovery.Gate(rg, upstream) > 0
                        && rootSupply.Of(upstream))
                        Consider(ri, starved, eff * (1.0 + (1.0 - minFill)));
                }
                if (!(eff > FacilityBuildMinMargin)) continue;
                if (!worldOpened && !(pr.SmoothedMargin > 0)) continue;
                if (Instruments.On && Catalog.FactorySpec(pr.GoodId).Cost > buffer) Instruments.AiBuildOverCap(w, pr.GoodId);
                Consider(ri, pr.GoodId, val.Score);
            }
        }
        if (Instruments.On)
        {
            for (int ri = 0; ri < w.Regions.Count; ri++)
            {
                if (bestGr[ri] < 0) continue;
                var bq = World.FindFactory(w.Regions[ri], bestGr[ri]);
                Instruments.AiBuildBest(w, bestGr[ri], bq is not null && !rootSupply.Of(bq));
            }
        }
        for (int ri = 0; ri < w.Regions.Count; ri++)
        {
            if (bestGr[ri] < 0) continue;
            if (monthlyBuild > outlayCap) break;
            var bp = World.FindFactory(w.Regions[ri], bestGr[ri]);
            if (bp is null) continue;
            double est = NewFacilityOutlayPerMonth(w, w.Regions[ri], bp);
            if (!(monthlyBuild + est <= outlayCap)) { if (Instruments.On) Instruments.AiBuildSkipRoom(w, bestGr[ri]); continue; }
            w.SetStateTarget(ri, bestGr[ri], World.StateUnits(bp, w.Coef) + 1);
            if (Instruments.On) Instruments.AiBuildTried(w, bp.GoodId, est, bp.ConstructionLeft > 0, Catalog.FactorySpec(bp.GoodId).Cost > buffer);
            if (bp.ConstructionLeft > 0)
            {
                monthlyBuild += Calendar.PerDayToPerMonth(bp.ConstructionPayPerDay);
                monthlyBuild += World.ConstructionMaterialPerMonth(w.Regions[ri], bp, w.Coef);
            }
            else monthlyBuild += est > 0 ? est : 0;
        }
    }

    internal static double NewFacilityOutlayPerMonth(World w, Region rg, Producer bp)
    {
        double payPerDay = World.FactoryPayPerDay(bp.GoodId, w.Coef, out _);
        return Calendar.PerDayToPerMonth(payPerDay) + World.ConstructionMaterialPerMonth(rg, bp, w.Coef);
    }

    internal static void Apply(World w, Stance s)
    {
        var p = w.Policy;
        p.CoercionTarget = 400;
        int stateMonopTier = 0;
        p.MilitarySpendingTarget = SecurityStageTarget(w);
        p.Censorship = CensorshipLaw.FreePress;
        p.LaborUnion = LaborUnionLaw.Tolerated;
        p.WorkHours = WorkHours.Twelve;
        switch (s)
        {
            case Stance.Modernize:
                p.EducationLaw = EducationLaw.Public;
                if (w.EducationLaw >= EducationLaw.Public && w.Treasury.Value > CompulsoryEduBufferMult * FiscalBufferOf(w))
                    p.EducationLaw = EducationLaw.Compulsory;
                p.EducationSpendingTarget = Government.EducationSpendingTop;
                p.AdminSpendingTarget = 0.6;
                p.IncomeTaxTarget = 0.25;
                p.LandTaxTarget = Government.LandTaxTop; p.ProgressiveTaxTarget = 0.3; p.PollTaxTarget = 0;
                stateMonopTier = 1;
                p.SocialSecurityTarget = 0.3;
                p.ChildLaborAllowed = false;
                RaiseWomenRights(p, WomenRightsLaw.Suffrage);
                if (p.Suffrage < Suffrage.Census) p.Suffrage = Suffrage.Census;
                break;
            case Stance.Industrialize:
                p.EducationLaw = w.EducationLaw > EducationLaw.Public ? w.EducationLaw : EducationLaw.Public;
                p.EducationSpendingTarget = 0.6;
                p.AdminSpendingTarget = 0.5;
                p.IncomeTaxTarget = 0.15; p.TariffTarget = 0.2;
                p.LandTaxTarget = 0.3; p.PollTaxTarget = 0;
                stateMonopTier = 2;
                break;
            case Stance.Welfare:
                p.SocialSecurityTarget = 0.6; p.EducationSpendingTarget = 0.5;
                p.AdminSpendingTarget = 0.5;
                RaiseWomenRights(p, WomenRightsLaw.Suffrage);
                p.IncomeTaxTarget = 0.2;
                p.ProgressiveTaxTarget = 0.6; p.LandTaxTarget = Government.LandTaxTop; p.PollTaxTarget = 0;
                stateMonopTier = 1;
                p.WorkHours = WorkHours.Ten;
                p.ChildLaborAllowed = false;
                break;
            case Stance.Fiscal:
                p.IncomeTaxTarget = Government.IncomeTaxTop; p.EducationSpendingTarget = 0.2;
                p.AdminSpendingTarget = 0.3;
                p.PollTaxTarget = 0.3; p.LandTaxTarget = Government.LandTaxTop; p.ProgressiveTaxTarget = 0.4;
                stateMonopTier = 3;
                p.SocialSecurityTarget = 0.1;
                break;
            case Stance.Militarize:
                p.CoercionTarget = 4000;
                p.AdminSpendingTarget = 0.5;
                p.IncomeTaxTarget = 0.25;
                p.PollTaxTarget = 0.4;
                stateMonopTier = 2;
                p.LandTaxTarget = Math.Max(p.LandTaxTarget, Government.LandTaxTop);
                p.TariffTarget = Math.Max(p.TariffTarget, 0.3);
                p.EducationLaw = w.EducationLaw > EducationLaw.Public ? w.EducationLaw : EducationLaw.Public;
                p.EducationSpendingTarget = Math.Max(p.EducationSpendingTarget, 0.4);
                p.Censorship = CensorshipLaw.StatePressOnly;
                p.LaborUnion = LaborUnionLaw.Illegal;
                if (PolicyPreferences.HasLegislature(w.Polity) && DoctrineFrom(w) == Doctrine.Militarist)
                    p.Suffrage = Suffrage.Census;
                break;
        }
        if (w.Polity == Polity.ProletarianDictatorship)
        {
            stateMonopTier = World.StateMonopolyMaxTier;
            p.ProgressiveTaxTarget = Math.Max(p.ProgressiveTaxTarget, Government.ProgressiveTaxTop);
            p.SocialSecurityTarget = Math.Max(p.SocialSecurityTarget, 1.0);
            p.SocialSecurityLaw = SocialSecurityLaw.Pension;
        }
        if (Government.WarStageOpen(w))
        {
            p.MilitarySpendingTarget = Government.MilitaryLawStages / (double)Government.MilitaryLawStages;
            if (Instruments.On) Instruments.WarStagePicked(w);
        }

        {
            double satNow = NationalScore.MeanSatisfaction(w);
            double unrestNow = UnrestOf(w, satNow);
            double ssFloor = Math.Min(1.0, UnrestSSPressure * unrestNow / UnrestSatisfactionThreshold);
            if (ssFloor > p.SocialSecurityTarget) p.SocialSecurityTarget = ssFloor;
        }

        if (Politics.AvgMassMilitancy(w) > w.Coef.RiotMilitancyThreshold && p.Suffrage < Suffrage.AdultMale)
            p.Suffrage = Suffrage.AdultMale;

        if (w.ParliamentEnabled && w.PoliticsEnabled && !w.InCivilWar
            && PolicyPreferences.HasLegislature(w.Polity))
        {
            if (w.GoverningParty == (int)Parliament.Party.Liberal)
            {
                if (p.EducationLaw < EducationLaw.Public) p.EducationLaw = EducationLaw.Public;
                if (p.Suffrage < Suffrage.Census) p.Suffrage = Suffrage.Census;
                if (p.WorkHours > WorkHours.Ten) p.WorkHours = WorkHours.Ten;
            }
            else if (w.GoverningParty == (int)Parliament.Party.Socialist)
            {
                if (p.EducationLaw < EducationLaw.Public) p.EducationLaw = EducationLaw.Public;
                if (p.Suffrage < Suffrage.AdultMale) p.Suffrage = Suffrage.AdultMale;
                p.LaborUnion = LaborUnionLaw.Tolerated;
                p.WorkHours = WorkHours.Eight;
            }
        }

        DecideModernLaws(w, p, s);

        double drawdown = w.GdpDrawdown.Value;
        double heat = w.FinancialHeat;
        if (drawdown > RecessionDrawdownThreshold) p.InterestRateTarget = EasingInterestRate;
        else if (heat > OverheatHeatThreshold) p.InterestRateTarget = TighteningInterestRate;
        else if (heat > OverheatHeatThreshold * w.Coef.PreTightenHeatFrac.Value) p.InterestRateTarget = PreTighteningInterestRate;
        else p.InterestRateTarget = NeutralInterestRate;

        double fiscalStress = FiscalStressOf(w);
        if (Instruments.On) Instruments.FiscalStress(w, fiscalStress);
        if (fiscalStress > 0)
        {
            double stressEff = drawdown > RecessionDrawdownThreshold ? fiscalStress * w.Coef.CrisisAusterityRelief : fiscalStress;
            double ease = 1 - stressEff;
            p.SocialSecurityTarget *= ease;
            p.EducationSpendingTarget = Math.Min(p.EducationSpendingTarget, EducationEaseFloor + EducationEaseSpan * ease);
            p.AdminSpendingTarget = Math.Max(p.AdminSpendingTarget, 0.5);
            p.PollTaxTarget = Math.Max(p.PollTaxTarget, FiscalTaxStressMult * stressEff);
        }
        if (!w.WarmupFrozen)
        {
            int curTier = w.StateMonopolyTier;
            if (Instruments.On) Instruments.MonopolyPush(w, InterestBurdenStressOf(w), double.NaN, double.NaN, false);   // (1413) 押しの抽選は消した＝p・u は NaN・当たりは 0 の同じ値を書く
            if (fiscalStress > 0) stateMonopTier = Math.Max(stateMonopTier, curTier + 1);
            else if (w.Treasury.Value > FiscalBufferOf(w)) stateMonopTier = Math.Max(stateMonopTier, curTier - 1);
            else stateMonopTier = Math.Max(stateMonopTier, curTier);
        }
        else if (w.OpeningStateMonopolyTier > 0) stateMonopTier = Math.Max(stateMonopTier, w.OpeningStateMonopolyTier);
        if (Instruments.On) Instruments.MonopolyDecision(w, w.StateMonopolyTier, Math.Clamp(stateMonopTier, 0, World.StateMonopolyMaxTier));
        w.SetStateMonopolyTier(stateMonopTier);



        {
            bool famine = !(w.FoodFill >= w.Coef.DynMandateFoodAnchor);
            int cornWant = famine || w.UrbanShare > CornUrbanRepeal ? 0
                         : w.UrbanShare < CornAgrarianKeep ? 2 : (int)p.CornLaws;
            StepLawToward(w, p, PolicyAxis.CornLaws, (int)p.CornLaws, cornWant, 2, v => p.CornLaws = (ReformLevel)v);

            double rentRoom = 0; int rentN = 0;
            foreach (var rg in w.Regions)
            {
                foreach (var pr in rg.Producers)
                {
                    if (!pr.IsRgo) continue;
                    rentN++;
                    if (pr.RentShare.Value > rg.FarmRentCap.Value) rentRoom += pr.RentShare.Value - rg.FarmRentCap.Value;
                }
            }
            double room = rentN > 0 ? rentRoom / rentN : 0;
            StepLawToward(w, p, PolicyAxis.SerfEmancipation, (int)p.SerfEmancipation, (int)p.SerfEmancipation + 1, 2, v => p.SerfEmancipation = (ReformLevel)v);
            if (room > EmancipationRentRoom || famine)
                StepLawToward(w, p, PolicyAxis.LandReform, (int)p.LandReform, (int)p.LandReform + 1, 2, v => p.LandReform = (ReformLevel)v);
        }

        if (w.Coef.OpiumAddictionGrowth > 0)
        {
            double obAdd = 0, obSz = 0;
            foreach (var r in w.Regions) { double rp = r.SumPopulation(); obAdd += r.OpiumAddiction * rp; obSz += rp; }
            double obAvg = obSz > 1e-9 ? obAdd / obSz : 0;
            double thr = w.Coef.OpiumBanThreshold;
            p.OpiumPossession = obAvg > thr * OpiumFullBanRatio ? ReformLevel.Full
                              : obAvg > thr ? ReformLevel.Limited
                              : obAvg < thr * OpiumBanHysteresis ? ReformLevel.None
                              : p.OpiumPossession;
            {
                double opiumPressure = 0;
                if (w.Diplomacy is { } odip && w.SelfPower is { } oself)
                    foreach (var other in odip.Powers)
                    {
                        if (ReferenceEquals(other, oself) || other.Nation is not { } ow2) continue;
                        if (ow2.OpiumPossession == ReformLevel.None) continue;
                        if (odip.Tension(oself, other) < w.Coef.OpiumExportPressureTension) continue;
                        opiumPressure += other.TradeWeight;
                    }
                p.OpiumExportBan = opiumPressure > w.Coef.OpiumExportPressureThreshold;
            }
        }

        if (w.SelfPower is { } spv && w.Diplomacy is { } dipv)
        {
            double easeInd = 1 - fiscalStress;
            double retShare = w.ModernizeReturnShare();
            double indGap = retShare;
            if (Instruments.On) Instruments.ModernizeSignal(w, s, indGap);
            w.ModernizationFrac = indGap * easeInd * s switch
            {
                Stance.Industrialize => IndustrializeInvestFrac,
                Stance.Modernize => ModernizeInvestFrac,
                Stance.Militarize => ModernizeInvestFrac,
                _ => 0,
            };
            if (s == Stance.Industrialize && fiscalStress <= 1e-9 && w.LastExportValue > w.LastImportValue * DumpingExportRatio
                && retShare > 0)
                p.TariffTarget = DumpingTariff;
            else if (p.TariffTarget < 0)
                p.TariffTarget = 0;
            bool atWar = dipv.Wars.Exists(war => war.A == spv || war.B == spv);
            for (int ri = 0; ri < w.Regions.Count; ri++)
            {
                var rg = w.Regions[ri];
                foreach (var pr in rg.Producers)
                {
                    if (Catalog.Goods[pr.GoodId].Tier == GoodTier.Service) continue;
                    if (Catalog.FactorySpec(pr.GoodId).Category == "軍需")
                    {
                        bool want = atWar && fiscalStress < 0.5;
                        if (pr.Subsidized != want)
                        {
                            if (Instruments.On) Instruments.SubsidyWrite(w, rg, pr, want);
                            w.SetSubsidy(ri, pr.GoodId, want);
                        }
                    }
                    else if (w.Coef.AiDevelopmentalSubsidy > 0 && s == Stance.Industrialize
                             && Catalog.Goods[pr.GoodId].Tier is GoodTier.Intermediate or GoodTier.Producer)
                    {
                        bool want = !atWar && fiscalStress < 0.5 && pr.SmoothedMargin < 0 && !pr.Bankrupt;
                        if (pr.Subsidized != want)
                        {
                            if (Instruments.On) Instruments.SubsidyWrite(w, rg, pr, want);
                            w.SetSubsidy(ri, pr.GoodId, want);
                        }
                    }
                }
            }
            if (Instruments.On && fiscalStress > FireSaleFiscalStress) Instruments.FireSaleRun(w);
            if (fiscalStress > FireSaleFiscalStress)
                for (int ri = 0; ri < w.Regions.Count; ri++)
                {
                    var rg = w.Regions[ri];
                    bool sold = false;
                    foreach (var pr in rg.Producers)
                    {
                        if (Instruments.On && pr.BuiltScale > 1e-9) Instruments.FireSaleSeen(w, rg, pr);
                        if (pr.BuiltScale > 1e-9 && w.CanSellFactory(ri, pr.GoodId).Ok)
                        {
                            if (Instruments.On) Instruments.FireSaleFired(w, rg, pr);
                            w.SetStateTarget(ri, pr.GoodId, Math.Max(0, World.StateUnits(pr, w.Coef) - 1)); sold = true; break;
                        }
                    }
                    if (sold) break;
                }

        }


        RelieveOpulence(w);
        Parliament.TargetCrisisDemands(w);
        GateAllLawTargets(w, p);
    }
}

public sealed record NationStanding(
    string Name, double Score, double Industrialization, double Satisfaction, int Colonies, double Gdp,
    double Population, double Military, Polity Polity, double WarDead, double Tariff,
    Suffrage Suffrage = Suffrage.None, EducationLaw Education = EducationLaw.None, CensorshipLaw Censorship = CensorshipLaw.StatePressOnly,
    LaborUnionLaw Union = LaborUnionLaw.Illegal, WorkHours WorkHours = WorkHours.Unlimited, ReformLevel CornLaws = ReformLevel.None,
    ReformLevel LandReform = ReformLevel.None, ReformLevel Serf = ReformLevel.None, ReformLevel Pension = ReformLevel.None,
    double Militarism = 0, ConscriptionLaw Conscription = ConscriptionLaw.Selective,
    string Nation = "", string Suzerain = "", string DisplayName = "",
    double PopGrowth = 0,
    int HomeProvinces = 0,
    double NavalPower = 0, double LandPower = 0, double FleetStock = 0,
    double TradeGdp = 0,
    double MilPush = 0, double MilValve = 0, double MilCourt = 0, double MilLegitimacy = 0,
    Polity DerivedPolity = Polity.Absolutist,
    double PopGrowthGross = 0,
    double ColonyMass = 0);

public sealed record MatchResult(
    ulong Seed, int Quarters, string Winner, double WinnerScore, int DecisionQuarter,
    IReadOnlyList<NationStanding> Standings, IReadOnlyList<string> Events,
    IReadOnlyList<string> Anomalies, bool Completed,
    int HarnessPass, int HarnessWarn, int HarnessFail, double MaxLossFinanced,
    IReadOnlyList<string> HarnessIssues,
    double MaxDrawdown, int DepressionEpisodes, int OverheatDepressions, int HarvestDepressions,
    int LeadChanges = 0,
    int GoldSuspensions = 0, int GoldCoverSuspensions = 0,
    int SpawnedNations = 0, int Formations = 0,
    int CivilWarSplits = 0,
    IReadOnlyList<NationStanding>? StandingsAt1914 = null,
    bool Deadlocked = false,
    double WorstLossRate = 0,
    string WorstLossNation = "", double WorstLossGdpQ = 0, int WorstLossQuarter = -1,
    int WarMaxRepeat = 0, int WarPairsGe3 = 0, int WarPairs = 0, string WarWorstPair = "",
    int InterveneChances = 0, int PeaceOffers = 0, int PeaceBlank = 0, int PeaceRefused = 0,
    int ElectionsHeld = 0, int ElectionQuarters = 0,
    double TradeGdpMajors = 0,
    int MajorDepressionEpisodes = 0, double MajorMaxDrawdown = 0, string MajorDdNote = "",
    IReadOnlyList<string>? Jumps = null,
    IReadOnlyList<NationStanding>? StandingsAt1820 = null, IReadOnlyList<NationStanding>? StandingsAt1870 = null,
    IReadOnlyList<NationStanding>? StandingsAt1920 = null,
    IReadOnlyList<NationStanding>? StandingsAt1950 = null,
    long[]? Cost = null);

public sealed record BatchSummary(
    int N, int Breakdowns, double AvgDecisionQuarter, IReadOnlyList<(string Nation, int Wins)> WinnerCounts,
    int Deadlocks, int LateDecisions, int TotalAnomalies, int HarnessFailMatches, IReadOnlyList<string> Flags,
    IReadOnlyList<(Polity Polity, int Count)> PolityDistribution,
    IReadOnlyList<(Polity Polity, int Plays)> PolityPlayCount,
    double AvgMaxDrawdown, int GreatDepressionMatches, double AvgDepressionEpisodes,
    double OverheatShare, double HarvestShare,
    double AvgLeadChanges = 0,
    IReadOnlyList<(string Axis, double MaxShare, string Dist)>? LawDiversity = null,
    IReadOnlyList<NationFidelity>? Fidelity = null,
    int GoldSuspensions = 0, int GoldCoverSuspensions = 0, int GoldSuspensionMatches = 0,
    int Independences = 0, int Formations = 0,
    int CivilWarSplits = 0,
    IReadOnlyList<string>? JumpLines = null,
    IReadOnlyList<string>? HarnessIssueLines = null,
    double AvgWarMaxRepeat = 0, double AvgWarPairsGe3 = 0, double AvgWarPairs = 0, string WarWorstPair = "",
    double AvgInterveneChances = 0, double AvgPeaceOffers = 0, double AvgPeaceBlank = 0, double AvgPeaceRefused = 0,
    IReadOnlyList<(string Nation, double Naval, double Land)>? NavalRanking = null,
    double AvgElections = 0, double AvgElectionIntervalQ = 0,
    double AvgTradeGdpMajors = 0,
    double AvgMajorDepressionEpisodes = 0, double MaxMajorDrawdown = 0, string MaxMajorDdNote = "",
    long[]? CostTotals = null,
    IReadOnlyList<(ulong Seed, double Exact, double Near)>? SeedFidelity = null);

public static class SelfPlayMatch
{
    private const double LossRateMinOutputShare = 0.001;
    private const double LossFinancedRateThreshold = 0.35;
    private const int DeadlockFrozenQuarters = 20;
    public const double DepressionEnterDrawdown = 0.20;
    public const double DepressionExitDrawdown = 0.08;
    public const double GreatDepressionDrawdown = 0.25;
    public static bool InDepression(bool wasIn, double drawdownPerCapita) => wasIn ? drawdownPerCapita > DepressionExitDrawdown : drawdownPerCapita > DepressionEnterDrawdown;
    public static bool IsGreatDepression(double drawdownPerCapita) => drawdownPerCapita > GreatDepressionDrawdown;
    public static string DepressionWord(double drawdownPerCapita) => IsGreatDepression(drawdownPerCapita) ? "恐慌" : "景気後退";
    private const double LateDecisionQuarterFraction = 0.9;
    private const double HeatPeakDecay = 0.97;
    private const double HarvestBadDecay = 0.9;

    public static MatchResult Run(ulong seed, int quarters = Coefficients.GameQuarters, double? interestRate = null, bool lean = false)
    {
        NationConfig[] configs = WorldData.Nations();
        if (interestRate is { } ir)
            configs = [.. configs.Select(cfg => { var c2 = cfg.ShallowClone(); c2.InitialInterestRate = ir; return c2; })];
        var sim = new MultiNationSim(configs, seed, _ => new UtilityAgent());
        foreach (var nsim in sim.Nations) nsim.TelemetryEnabled = false;
        if (lean) sim.Diplomacy.Log.Enabled = false;
        if (Instruments.On) Instruments.Phase(sim, seed, "open");
        sim.StartWarmup();
        if (Instruments.On) Instruments.Phase(sim, seed, "warm");
        var anomalies = new BoundedList(AnomalySampleCap);
        var prevScores = new double[configs.Length];
        string lastLeader = "";
        int decisionQ = 0, frozen = 0, leadChanges = 0;
        var jumps = new BoundedList(AnomalySampleCap);
        var jumpRankPrev = new int[configs.Length];
        var jumpPopPrev = new double[configs.Length];
        var jumpPolityPrev = new Polity[configs.Length];
        var jumpFlipQ = new List<int>[configs.Length];
        for (int i0 = 0; i0 < configs.Length; i0++) { jumpRankPrev[i0] = -1; jumpFlipQ[i0] = new List<int>(); jumpPolityPrev[i0] = sim.Nations[i0].World.Polity; }
        List<NationStanding>? standings1914 = null;
        List<NationStanding>? standings1820 = null, standings1870 = null, standings1920 = null, standings1950 = null;
        var initPop = new Dictionary<string, double>();
        foreach (var n0 in sim.Nations)
        {
            string key0 = n0.World.SelfPower?.BaseName ?? n0.World.Region.Name;
            if (!initPop.ContainsKey(key0)) initPop[key0] = n0.World.TotalPopulation;
        }
        static int HomeProvinceCount(World w)
        {
            int n = 0;
            foreach (var r in w.Regions) if (r.Tier != RegionTier.Colonial) n++;
            return n;
        }
        List<NationStanding> BuildStandings() => sim.Nations
            .Select(n =>
            {
                var w = n.World;
                string bn = w.SelfPower?.BaseName ?? w.Region.Name;
                double baggage = 0; foreach (var rb in w.Regions) baggage += rb.AnnexBaggage;
                double growthGross = initPop.TryGetValue(bn, out var p0) && p0 > 1e-9 ? w.TotalPopulation / p0 : 0;
                double growth = growthGross > 0 ? System.Math.Max(0, w.TotalPopulation - baggage) / p0 : 0;
                return new NationStanding(w.Region.Name, NationalScore.Standing(w),
                    w.MeanIndustrialization, NationalScore.MeanSatisfaction(w), w.SelfPower?.Colonies ?? 0, w.GdpPerMonth.Value,
                    w.TotalPopulation, w.SelfPower?.Military ?? 0, w.Polity, w.WarDeadCumulative, w.Tariff,
                    w.Suffrage, w.EducationLaw, w.Censorship, w.LaborUnion, w.WorkHours, w.CornLaws, w.LandReform, w.SerfEmancipation, w.Pension,
                    PoliticalQuantities.MilitaryPoliticalRatio(w), w.ConscriptionLaw,
                    w.SelfPower?.BaseName ?? w.Region.Name,
                    (w.SelfPower is { } sp2 ? sim.Diplomacy.Suzerain(sp2)?.BaseName : null) ?? "",
                    w.SelfPower?.Name ?? "",
                    growth,
                    HomeProvinceCount(w),
                    w.SelfPower?.NavalPower ?? 0, w.SelfPower?.LandPower ?? 0, EconomyStep.FleetPowerValue(w),
                    ((w.ExportsByGood?.Sum() ?? 0) + (w.ImportsByGood?.Sum() ?? 0)) * w.Coef.ClearingsPerYear / System.Math.Max(1.0, Calendar.PerMonthToPerYear(w.NominalGdpPerMonth).Value),
                    PoliticalQuantities.MilitaryPush(w), PoliticalQuantities.CivilianValve(w),
                    PoliticalQuantities.AppointmentAtCourt(w), w.Legitimacy,
                    PowerAffinity.PolityFromShares(w),
                    growthGross,
                    (w.SelfPower?.ColonyMassUnits() ?? 0));
            })
            .OrderByDescending(s => s.Score).ToList();
        var gdpPeak = new double[configs.Length];
        var gdpTrend = new double[configs.Length];
        var inDepression = new bool[configs.Length];
        var heatPeak = new double[configs.Length];
        var harvestBad = new double[configs.Length];
        var prevLoss = new double[configs.Length];
        var lossRate = new double[configs.Length];
        var satWindow = new List<double[]>(Validation.SatRecentQuarters);
        double worstLossRate = 0;
        string worstLossNation = ""; double worstLossGdpQ = 0; int worstLossQuarter = -1;
        double worldOutPrev = 0, worldOutThis = 0;
        int lossRateSkipped = 0; string lossRateSkipWorst = "";
        double worstDrawdown = 0; int depressionEpisodes = 0, overheatDepressions = 0, harvestDepressions = 0;
        double majorMaxDrawdown = 0; int majorDepressionEpisodes = 0; string majorDdNote = "";
        var gdpPcTrend = new double[sim.Nations.Count]; var gdpPcPeak = new double[sim.Nations.Count]; var inMajorDep = new bool[sim.Nations.Count];
        double worldTrendPrev = 0, worldTrendThis = 0;
        var majorSet = new System.Collections.Generic.HashSet<string>();
        foreach (var he in HistoricalReference.Table) if (he.GdpRank1913 is not null) majorSet.Add(he.Nation);
        var isMajor = new bool[sim.Nations.Count];
        for (int i = 0; i < sim.Nations.Count; i++)
            isMajor[i] = sim.Nations[i].World.SelfPower is { } mp && majorSet.Contains(mp.BaseName.Length > 0 ? mp.BaseName : mp.Name);
        const double depEnter = DepressionEnterDrawdown, depExit = DepressionExitDrawdown, warmupQ = 12, overheatMark = 0.55, harvestMark = 0.12;
        const double lossRateSmooth = 0.18, monthsPerQuarter = 3.0;

        try
        {
            for (int q = 0; q < quarters; q++)
            {
                sim.StepQuarter();
                if (Instruments.On) Instruments.Quarter(sim, seed, q);
                if (sim.Nations.Count > gdpPeak.Length)
                {
                    int nn = sim.Nations.Count;
                    System.Array.Resize(ref prevScores, nn); System.Array.Resize(ref gdpPeak, nn); System.Array.Resize(ref gdpTrend, nn);
                    System.Array.Resize(ref inDepression, nn); System.Array.Resize(ref heatPeak, nn); System.Array.Resize(ref harvestBad, nn);
                    System.Array.Resize(ref prevLoss, nn); System.Array.Resize(ref lossRate, nn);
                    System.Array.Resize(ref isMajor, nn);
                    System.Array.Resize(ref gdpPcTrend, nn); System.Array.Resize(ref gdpPcPeak, nn); System.Array.Resize(ref inMajorDep, nn);
                }
                var scores = new double[sim.Nations.Count];
                var scoresNow = NationalScore.Scores(sim.Nations.ConvertAll(sn => sn.World));
                worldOutPrev = worldOutThis; worldOutThis = 0;
                worldTrendPrev = worldTrendThis; worldTrendThis = 0;
                for (int i = 0; i < sim.Nations.Count; i++)
                {
                    var w = sim.Nations[i].World;
                    double sc = scoresNow[i];
                    scores[i] = NationalScore.Standing(w);
                    if (double.IsNaN(sc) || double.IsInfinity(sc) || double.IsNaN(scores[i]) || double.IsInfinity(scores[i]))
                        anomalies.Add($"Q{q + 1} {w.Region.Name}: スコアが NaN/∞");
                    if (!(w.Region.TotalIndustrialization is >= 0 and <= 10))
                        anomalies.Add($"Q{q + 1} {w.Region.Name}: 工業化度が異常（{w.Region.TotalIndustrialization:F2}）");
                    if (Validation.LawOutOfRange(w) is { } bad) anomalies.Add($"Q{q + 1} {w.Region.Name}: 法の値が定義域外（{bad}）");
                    double dLoss = System.Math.Max(0, w.LossFinanced - prevLoss[i]);
                    prevLoss[i] = w.LossFinanced;
                    double outQ = (w.NominalGdpPerMonth * monthsPerQuarter).Value;
                    worldOutThis += outQ;
                    if (q >= warmupQ)
                    {
                        double floorOut = worldOutPrev > 0 ? worldOutPrev / System.Math.Max(1, sim.Nations.Count) * LossRateMinOutputShare : 1.0;
                        if (outQ <= floorOut) { if (dLoss > 0) { lossRateSkipped++; if (lossRateSkipWorst.Length == 0) lossRateSkipWorst = $"{w.SelfPower?.Name}(Q{q + 1}・産出{outQ:F0})"; } }
                        else
                        {
                            lossRate[i] = PoliticalQuantities.Toward(lossRate[i], dLoss / outQ, lossRateSmooth);
                            if (lossRate[i] > worstLossRate)
                            {
                                worstLossRate = lossRate[i];
                                worstLossNation = w.SelfPower?.Name ?? w.Region.Name;
                                worstLossGdpQ = outQ; worstLossQuarter = q + 1;
                            }
                        }
                    }
                    if (q >= warmupQ)
                    {
                        gdpTrend[i] = PoliticalQuantities.Toward(gdpTrend[i], w.GdpPerMonth.Value, w.Coef.GdpTrendSmoothing);
                        if (gdpTrend[i] > gdpPeak[i]) gdpPeak[i] = gdpTrend[i];
                        worldTrendThis += gdpTrend[i];
                        double ddFloor = worldTrendPrev > 0 ? worldTrendPrev / System.Math.Max(1, sim.Nations.Count) * LossRateMinOutputShare : 1.0;
                        double dd = gdpPeak[i] > ddFloor ? 1.0 - gdpTrend[i] / gdpPeak[i] : 0;
                        if (dd > worstDrawdown) worstDrawdown = dd;
                        if (isMajor[i])
                        {
                            double pc = (w.GdpPerMonth / System.Math.Max(1.0, w.TotalPopulation)).Value;
                            gdpPcTrend[i] = PoliticalQuantities.Toward(gdpPcTrend[i], pc, w.Coef.GdpTrendSmoothing);
                            if (gdpPcTrend[i] > gdpPcPeak[i]) gdpPcPeak[i] = gdpPcTrend[i];
                            double ddPc = gdpPcPeak[i] > 0 && gdpPeak[i] > ddFloor ? 1.0 - gdpPcTrend[i] / gdpPcPeak[i] : 0;
                            if (ddPc > majorMaxDrawdown)
                            {
                                majorMaxDrawdown = ddPc;
                                majorDdNote = $"{w.SelfPower?.Name ?? w.Region.Name}・Q{q + 1}{(w.AtWarNow ? "・戦時" : "")}";
                            }
                            if (!inMajorDep[i] && ddPc > depEnter) { inMajorDep[i] = true; majorDepressionEpisodes++; }
                            else if (inMajorDep[i] && ddPc < depExit) inMajorDep[i] = false;
                        }
                        heatPeak[i] = Math.Max(w.FinancialHeat, heatPeak[i] * HeatPeakDecay);
                        harvestBad[i] = Math.Max(1.0 - w.HarvestFactor, harvestBad[i] * HarvestBadDecay);
                        if (!inDepression[i] && dd > depEnter)
                        {
                            inDepression[i] = true; depressionEpisodes++;
                            if (heatPeak[i] > overheatMark) overheatDepressions++;
                            if (harvestBad[i] > harvestMark) harvestDepressions++;
                        }
                        else if (inDepression[i] && dd < depExit) inDepression[i] = false;
                    }
                    else { gdpTrend[i] = w.GdpPerMonth.Value; gdpPeak[i] = w.GdpPerMonth.Value; double pc0 = (w.GdpPerMonth / System.Math.Max(1.0, w.TotalPopulation)).Value; gdpPcTrend[i] = pc0; gdpPcPeak[i] = pc0; }
                }
                if (q >= quarters - Validation.SatRecentQuarters)
                    satWindow.Add([.. sim.Nations.Select(n => NationalScore.MeanSatisfaction(n.World))]);
                if (q + 1 == Coefficients.FidelityQuarters && quarters > Coefficients.FidelityQuarters)
                    standings1914 = BuildStandings();
                if (q + 1 == Coefficients.FidelityQuarters1820 && quarters > Coefficients.FidelityQuarters1820) standings1820 = BuildStandings();
                if (q + 1 == Coefficients.FidelityQuarters1870 && quarters > Coefficients.FidelityQuarters1870) standings1870 = BuildStandings();
                if (q + 1 == Coefficients.FidelityQuarters1920 && quarters > Coefficients.FidelityQuarters1920) standings1920 = BuildStandings();
                if (q + 1 == Coefficients.FidelityQuarters1950) standings1950 = BuildStandings();
                int lead = 0;
                for (int i = 1; i < scores.Length; i++) if (scores[i] > scores[lead]) lead = i;
                string leader = sim.Nations[lead].World.SelfPower?.BaseName ?? sim.Nations[lead].World.Region.Name;
                if (leader != lastLeader) { decisionQ = q + 1; if (lastLeader.Length > 0) leadChanges++; lastLeader = leader; }
                for (int ji = 0; ji < configs.Length && ji < sim.Nations.Count; ji++)
                {
                    var jw = sim.Nations[ji].World;
                    if (jw.Polity != jumpPolityPrev[ji])
                    {
                        jumpPolityPrev[ji] = jw.Polity;
                        var fl = jumpFlipQ[ji]; fl.Add(q);
                        int within = 0; foreach (var fq in fl) if (q - fq <= Calendar.QuartersPerYear * 10) within++;
                        if (within == 3)
                            jumps.Add($"Q{q + 1} {jw.SelfPower?.BaseName ?? jw.Region.Name}：10年内に政変3回");
                    }
                }
                if ((q + 1) % Calendar.QuartersPerYear == 0)
                {
                    for (int ji = 0; ji < configs.Length && ji < sim.Nations.Count; ji++)
                    {
                        int rank = 1;
                        for (int jj = 0; jj < scores.Length; jj++) if (scores[jj] > scores[ji]) rank++;
                        var jw = sim.Nations[ji].World;
                        string jn = jw.SelfPower?.BaseName ?? jw.Region.Name;
                        if (jumpRankPrev[ji] > 0 && System.Math.Abs(rank - jumpRankPrev[ji]) >= JumpRankSteps)
                            jumps.Add($"Q{q + 1} {jn}：順位{jumpRankPrev[ji]}位→{rank}位（1年で{System.Math.Abs(rank - jumpRankPrev[ji])}ランク）");
                        jumpRankPrev[ji] = rank;
                        double popNow = jw.TotalPopulation;
                        if (jumpPopPrev[ji] > 1e-9 && popNow > jumpPopPrev[ji] * (1.0 + JumpPopGrowthMax))
                            jumps.Add($"Q{q + 1} {jn}：人口が1年で{(popNow / jumpPopPrev[ji] - 1.0):P0}増（併合を含む・物理上限{JumpPopGrowthMax:P0}超）");
                        jumpPopPrev[ji] = popNow;
                    }
                }
                if (q > 0)
                {
                    bool same = true;
                    for (int i = 0; i < scores.Length; i++) if (Math.Abs(scores[i] - prevScores[i]) > 1e-6) { same = false; break; }
                    frozen = same ? frozen + 1 : 0;
                }
                scores.CopyTo(prevScores, 0);
            }
        }
        catch (OutOfMemoryException) { throw; }
        catch (Exception ex)
        {
            anomalies.Add($"例外/保存則違反: {ex.GetType().Name}: {ex.Message}");
            return new MatchResult(seed, quarters, "(中断)", 0, -1, [], [.. sim.Diplomacy.Log], anomalies, false, 0, 0, 0, 0, [], worstDrawdown, depressionEpisodes, overheatDepressions, harvestDepressions, Cost: sim.CostTotals());
        }
        finally { if (Instruments.On) Instruments.Close(sim); }

        bool deadlocked = frozen > DeadlockFrozenQuarters;
        if (deadlocked) anomalies.Add($"デッドロックの疑い：スコアが{frozen}四半期不変");

        var standings = BuildStandings();


        var hm = Metrics.Compute(sim);
        hm = hm with { Nations = [.. hm.Nations.Select((nm, i) =>
        {
            var v = satWindow.Where(a => i < a.Length).Select(a => a[i]).ToList();
            return v.Count > 0 ? nm with { SatRecentMean = v.Average(), SatRecentSwing = v.Max() - v.Min() } : nm;
        })] };
        var hc = Validation.Evaluate(hm);
        int hp = Validation.Count(hc, Verdict.Pass), hw = Validation.Count(hc, Verdict.Warn), hf = Validation.Count(hc, Verdict.Fail);
        var harnessIssues = hc.Where(ch => ch.Verdict != Verdict.Pass)
            .Select(ch => $"{(ch.Verdict == Verdict.Fail ? "×" : "△")} {ch.Name}：実測{ch.Value}（目標{ch.Target}）").ToList();

        double maxLoss = sim.Nations.Max(n => n.World.LossFinanced);
        if (worstLossRate > LossFinancedRateThreshold)
            anomalies.Add($"損失financing率が高い：ピーク{worstLossRate:P1}（{worstLossNation}・Q{worstLossQuarter}・その四半期の名目産出{worstLossGdpQ:F0}）");
        if (lossRateSkipped > 0)
            harnessIssues.Add($"（参考）損失financingの計上外：{lossRateSkipped}件が産出の床未満＝残骸国家（例 {lossRateSkipWorst}）");

        var wrs = sim.Diplomacy.WarRepeatStats();
        return new MatchResult(seed, quarters, standings[0].Nation, standings[0].Score, decisionQ,
            standings, [.. sim.Diplomacy.Log], anomalies, true, hp, hw, hf, maxLoss, harnessIssues, worstDrawdown, depressionEpisodes, overheatDepressions, harvestDepressions, leadChanges,
            sim.Nations.Sum(n => n.World.GoldSuspensions), sim.Nations.Sum(n => n.World.GoldCoverSuspensions),
            sim.Nations.Count - configs.Length, sim.Diplomacy.Powers.Count(p => p.FormationTier > 0),
            sim.Nations.Sum(n => n.World.CivilWarSplitCount),
            standings1914,
            deadlocked,
            worstLossRate,
            worstLossNation, worstLossGdpQ, worstLossQuarter,
            wrs.MaxRepeat, wrs.PairsGe3, wrs.Pairs, wrs.Worst,
            sim.Diplomacy.InterventionChances, sim.Diplomacy.PeaceOffersMade,
            sim.Diplomacy.PeaceOffersBlank, sim.Diplomacy.PeaceOffersRefused,
            sim.Nations.Sum(n => n.World.ElectionsHeld), sim.Nations.Sum(n => n.World.QuartersWithElections),
            MajorsTradeGdp(sim),
            MajorDepressionEpisodes: majorDepressionEpisodes, MajorMaxDrawdown: majorMaxDrawdown, MajorDdNote: majorDdNote,
            Jumps: [.. jumps], StandingsAt1820: standings1820, StandingsAt1870: standings1870, StandingsAt1920: standings1920, StandingsAt1950: standings1950,
            Cost: sim.CostTotals());
    }

    private static readonly string[] TradeGdpMajorNames = ["イギリス", "フランス", "ロシア", "プロイセン", "清", "アメリカ"];
    internal static double MajorsTradeGdpRef1913()
    {
        double s = 0; int n = 0;
        foreach (var name in TradeGdpMajorNames)
            foreach (var (tn, op) in HistoricalReference.TradeOpenness1913)
                if (tn == name) { s += op; n++; break; }
        return n > 0 ? s / n : 0;
    }
    private static double MajorsTradeGdp(MultiNationSim sim)
    {
        double sum = 0; int n = 0;
        foreach (var nation in sim.Nations)
        {
            var w = nation.World; var p = w.SelfPower; if (p is null) continue;
            if (System.Array.IndexOf(TradeGdpMajorNames, p.BaseName.Length > 0 ? p.BaseName : p.Name) < 0) continue;
            double ex = 0, im = 0;
            if (w.ExportsByGood is { } eg) foreach (var v in eg) ex += v;
            if (w.ImportsByGood is { } ig) foreach (var v in ig) im += v;
            sum += (ex + im) * w.Coef.ClearingsPerYear / System.Math.Max(1.0, Calendar.PerMonthToPerYear(w.NominalGdpPerMonth).Value); n++;
        }
        return n > 0 ? sum / n : 0;
    }

    private const int JumpRankSteps = 10;
    private const double JumpPopGrowthMax = 0.10;
    private const int AnomalySampleCap = 64;

    private sealed class BoundedList(int cap) : IReadOnlyList<string>
    {
        private readonly List<string> _kept = [];
        private int _total;
        public void Add(string line) { _total++; if (_kept.Count < cap) _kept.Add(line); }
        public int Count => _total;
        public string this[int i] => _kept.Count == 0 ? "" : _kept[System.Math.Clamp(i, 0, _kept.Count - 1)];
        public IEnumerator<string> GetEnumerator() => _kept.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _kept.GetEnumerator();
    }

    public static BatchSummary RunBatch(int n, int quarters = Coefficients.GameQuarters, double? interestRate = null, int? parallel = null, ulong seedBase = 1)
        => Summarize(RunMatches(n, quarters, interestRate, parallel, seedBase), quarters);

    public static MatchResult[] RunMatches(int n, int quarters = Coefficients.GameQuarters, double? interestRate = null, int? parallel = null, ulong seedBase = 1)
    {
        var arr = new MatchResult[n];
        _ = WorldData.Nations();
        var opts = new System.Threading.Tasks.ParallelOptions { MaxDegreeOfParallelism = parallel ?? Math.Clamp(Environment.ProcessorCount, 2, Math.Max(2, n)) };
        System.Threading.Tasks.Parallel.For(0, n, opts, i => arr[i] = Run(seedBase + (ulong)i, quarters, interestRate, lean: true));
        return arr;
    }

    public static BatchSummary Summarize(IReadOnlyList<MatchResult> arr, int quarters = Coefficients.GameQuarters)
    {
        int n = arr.Count;
        var results = new List<MatchResult>(arr);

        int breakdowns = results.Count(r => !r.Completed);
        var completed = results.Where(r => r.Completed).ToList();
        double avgDec = completed.Count > 0 ? completed.Average(r => r.DecisionQuarter) : 0;
        var winCounts = completed.GroupBy(r => r.Winner).Select(g => (g.Key, g.Count()))
            .OrderByDescending(x => x.Item2).ToList();
        int deadlocks = results.Count(r => r.Deadlocked);
        int late = completed.Count(r => r.DecisionQuarter > quarters * LateDecisionQuarterFraction);
        int totalAnom = results.Sum(r => r.Anomalies.Count);
        int harnessFail = results.Count(r => !r.Completed || r.HarnessFail > 0);
        int lossFin = results.Count(r => r.WorstLossRate > LossFinancedRateThreshold);

        var flags = new List<string>();
        if (breakdowns > 0) flags.Add($"破綻：{breakdowns}/{n} が未完走（実装バグの疑い）");
        foreach (var r in results.Where(x => !x.Completed))
            flags.Add($"  └ seed={r.Seed}: {(r.Anomalies.Count > 0 ? r.Anomalies[^1] : "不明")}");
        foreach (var (nation, wins) in winCounts)
            if (wins > n * 0.5) flags.Add($"常勝の疑い：{nation} が {wins}/{n} 勝");
        foreach (var g in completed.GroupBy(r => r.Standings[^1].Nation))
            if (g.Count() > n * 0.5) flags.Add($"即死/低迷の疑い：{g.Key} が {g.Count()}/{n} で最下位");
        if (lossFin > 0)
        {
            var wl = results.OrderByDescending(r => r.WorstLossRate).First();
            flags.Add($"損失financing率が高い：{lossFin}/{n} 試合（ピーク率の最大 {wl.WorstLossRate:P1}＝seed{wl.Seed} {wl.WorstLossNation} Q{wl.WorstLossQuarter}・その四半期の名目産出 {wl.WorstLossGdpQ:F0}）");
        }
        var harnessLines = results.SelectMany(r => r.HarnessIssues.Select(i => (r.Seed, Issue: i)))
            .GroupBy(x => x.Issue.Split('：')[0])
            .OrderByDescending(x => x.Count())
            .Select(g => $"{g.Key}：{g.Count()}/{n}　" + string.Join("　", g.Select(x => $"seed{x.Seed}={x.Issue[(x.Issue.IndexOf('：') + 1)..]}")))
            .ToList();

        var jumpLines = results.SelectMany(r => (r.Jumps ?? []).Select(j => (r.Seed, Jump: j)))
            .GroupBy(x => x.Jump.Contains("順位") ? "順位の跳躍" : x.Jump.Contains("政変") ? "10年内に政変3回" : "人口の物理上限超")
            .OrderByDescending(g => g.Count())
            .Select(g => $"{g.Key}：{g.Count()}件　" + string.Join("　", g.Take(6).Select(x => $"seed{x.Seed} {x.Jump}")))
            .ToList();

        double avgDrawdown = completed.Count > 0 ? completed.Average(r => r.MaxDrawdown) : 0;
        int greatDep = completed.Count(r => r.MaxDrawdown > GreatDepressionDrawdown);
        double avgDepEp = completed.Count > 0 ? completed.Average(r => r.DepressionEpisodes) : 0;
        int totalDep = completed.Sum(r => r.DepressionEpisodes);
        int totalOverheat = completed.Sum(r => r.OverheatDepressions);
        int totalHarvest = completed.Sum(r => r.HarvestDepressions);
        double overheatShare = totalDep > 0 ? (double)totalOverheat / totalDep : 0;
        double harvestShare = totalDep > 0 ? (double)totalHarvest / totalDep : 0;

        var polityDist = completed.SelectMany(r => r.Standings).GroupBy(s => s.Polity)
            .Select(g => (g.Key, g.Count())).OrderByDescending(x => x.Item2).ToList();
        var polityPlays = completed.SelectMany(r => r.Standings.Select(s => s.Polity).Distinct())
            .GroupBy(p => p).Select(g => (g.Key, g.Count())).OrderByDescending(x => x.Item2).ToList();

        double avgLeadChanges = completed.Count > 0 ? completed.Average(r => r.LeadChanges) : 0;
        var lawDiv = new List<(string Axis, double MaxShare, string Dist)>();
        var lawSamples = completed.SelectMany(r => r.Standings).ToList();
        if (lawSamples.Count > 0)
        {
            void Axis<T>(string name, Func<NationStanding, T> pick) where T : notnull
            {
                var groups = lawSamples.GroupBy(pick).Select(g => (Val: g.Key.ToString() ?? "", Cnt: g.Count()))
                    .OrderByDescending(x => x.Cnt).ToList();
                double maxShare = (double)groups[0].Cnt / lawSamples.Count;
                string dist = string.Join(" ", groups.Select(g => $"{g.Val}{(double)g.Cnt / lawSamples.Count:P0}"));
                lawDiv.Add((name, maxShare, dist));
            }
            Axis("参政権", s => s.Suffrage);
            Axis("教育法", s => s.Education);
            Axis("報道", s => s.Censorship);
            Axis("労組", s => s.Union);
            Axis("労働時間", s => s.WorkHours);
            Axis("穀物法", s => s.CornLaws);
            Axis("土地改革", s => s.LandReform);
            Axis("農奴解放", s => s.Serf);
            Axis("年金", s => s.Pension);
            Axis("徴兵法", s => s.Conscription);
        }

        var fidelity = HistoricalReference.Evaluate(completed);
        var seedFid = completed.Select(r =>
        {
            var f1 = HistoricalReference.Evaluate([r]);
            return (r.Seed, f1.Select(x => x.Exact).Where(v => !double.IsNaN(v)).DefaultIfEmpty(double.NaN).Average(),
                    f1.Select(x => x.Near).Where(v => !double.IsNaN(v)).DefaultIfEmpty(double.NaN).Average());
        }).ToList();

        int goldSusp = completed.Sum(r => r.GoldSuspensions);
        int goldCoverSusp = completed.Sum(r => r.GoldCoverSuspensions);
        int goldSuspMatches = completed.Count(r => r.GoldSuspensions > 0);

        int independences = completed.Sum(r => r.SpawnedNations);
        int formations = completed.Sum(r => r.Formations);
        int civilSplits = completed.Sum(r => r.CivilWarSplits);

        return new BatchSummary(n, breakdowns, avgDec, winCounts, deadlocks, late, totalAnom, harnessFail, flags, polityDist, polityPlays,
            avgDrawdown, greatDep, avgDepEp, overheatShare, harvestShare, avgLeadChanges, lawDiv, fidelity, goldSusp, goldCoverSusp, goldSuspMatches,
            independences, formations, civilSplits, JumpLines: jumpLines, HarnessIssueLines: harnessLines,
            completed.Count > 0 ? completed.Average(r => r.WarMaxRepeat) : 0,
            completed.Count > 0 ? completed.Average(r => r.WarPairsGe3) : 0,
            completed.Count > 0 ? completed.Average(r => r.WarPairs) : 0,
            completed.OrderByDescending(r => r.WarMaxRepeat).FirstOrDefault() is { } wr ? $"{wr.WarWorstPair}（{wr.WarMaxRepeat}回・seed{wr.Seed}）" : "",
            completed.Count > 0 ? completed.Average(r => r.InterveneChances) : 0,
            completed.Count > 0 ? completed.Average(r => r.PeaceOffers) : 0,
            completed.Count > 0 ? completed.Average(r => r.PeaceBlank) : 0,
            completed.Count > 0 ? completed.Average(r => r.PeaceRefused) : 0,
            completed.SelectMany(r => r.StandingsAt1914 ?? r.Standings)
                .GroupBy(s2 => string.IsNullOrEmpty(s2.Nation) ? s2.Name : s2.Nation)
                .Select(g => (Nation: g.Key, Naval: g.Average(x => x.NavalPower), Land: g.Average(x => x.FleetStock)))
                .OrderByDescending(x => x.Naval).ToList(),
            completed.Count > 0 ? completed.Average(r => r.ElectionsHeld) : 0,
            completed.Sum(r => (long)r.ElectionsHeld) > 0 ? completed.Sum(r => (double)r.ElectionQuarters) / completed.Sum(r => (double)r.ElectionsHeld) : 0,
            completed.Count > 0 ? completed.Average(r => r.TradeGdpMajors) : 0,
            completed.Count > 0 ? completed.Average(r => r.MajorDepressionEpisodes) : 0,
            completed.Count > 0 ? completed.Max(r => r.MajorMaxDrawdown) : 0,
            completed.OrderByDescending(r => r.MajorMaxDrawdown).FirstOrDefault() is { } mdd ? $"{mdd.MajorDdNote}・seed{mdd.Seed}" : "",
            CostTotals: SumCost(results),
            SeedFidelity: seedFid);
    }

    private static long[] SumCost(IEnumerable<MatchResult> results)
    {
        var t = new long[CostLedger.Count];
        foreach (var r in results) CostLedger.AddInto(t, r.Cost);
        return t;
    }
}
