namespace PriceOfBread.SimCore;

public static class Government
{
    internal const double SliderStep = 0.05;
    internal const int TaxLawStages = 3;
    internal static double InheritanceRate(InheritanceLaw l) => l switch { InheritanceLaw.Low => 0.02, InheritanceLaw.Mid => 0.06, InheritanceLaw.High => 0.20, InheritanceLaw.All => 1.0, _ => 0.0 };
    internal const double IncomeTaxTop = 0.35;
    internal const double ProgressiveTaxTop = 0.8;
    internal const double PollTaxTop = 0.5;
    internal const double LandTaxTop = 0.4;
    internal const double EducationSpendingTop = 1.0;
    internal static double TaxLawTop(PolicyAxis a) => a switch
    {
        PolicyAxis.IncomeTax => IncomeTaxTop,
        PolicyAxis.ProgressiveTax => ProgressiveTaxTop,
        PolicyAxis.PollTax => PollTaxTop,
        PolicyAxis.LandTax => LandTaxTop,
        _ => EducationSpendingTop,
    };
    internal const int MilitaryLawStages = 4;
    internal const int SocialSecurityMaxLevel = 3;

    internal static double CoercionTargetFromForce(double force, Coefficients c)
        => force / (1.0 + force / Math.Max(1e-9, c.CoercionMax));

    internal sealed record LevelLaw(PolicyAxis Axis, Func<World, int> Get, Action<World, int> Set, Func<World, NationPolicy, int> Target, int Top, int Stage = 0,
        Func<World, bool>? Open = null, Action<World, NationPolicy, int>? Enact = null, Func<World, bool>? FrontOpen = null);

    internal static readonly LevelLaw[] LevelLaws =
    [
        new(PolicyAxis.EducationLaw, w => (int)w.EducationLaw, (w, v) => w.EducationLaw = (EducationLaw)v, (w, p) => (int)p.EducationLaw, 3, Stage: 2),
        new(PolicyAxis.Suffrage, w => (int)w.Suffrage, (w, v) => w.Suffrage = (Suffrage)v, (w, p) => (int)p.Suffrage, 3, Stage: 2),
        new(PolicyAxis.WomenRights, w => (int)w.WomenRights, (w, v) => w.WomenRights = (WomenRightsLaw)v, (w, p) => (int)p.WomenRights, 3, Stage: 2),
        new(PolicyAxis.MonetaryStandard, w => (int)w.MonetaryLaw, (w, v) => w.MonetaryLaw = (MonetaryStandardLaw)v,
            (w, p) => Math.Clamp(p.MonetaryTarget, 0, 2), 2,
            Enact: (w, p, t) => w.MonetaryLaw = (MonetaryStandardLaw)StepLevel((int)w.MonetaryLaw, t, w, PolicyAxis.MonetaryStandard)),
        new(PolicyAxis.CapitalMobility, w => w.CapitalControlLaw ? 1 : 0, (w, v) => w.CapitalControlLaw = v == 1, (w, p) => p.CapitalControlTarget ? 1 : 0, 1),
        new(PolicyAxis.Censorship, w => (int)w.Censorship, (w, v) => w.Censorship = (CensorshipLaw)v, (w, p) => (int)p.Censorship, 2, Stage: 2),
        new(PolicyAxis.LaborUnion, w => (int)w.LaborUnion, (w, v) => w.LaborUnion = (LaborUnionLaw)v, (w, p) => (int)p.LaborUnion, 2, Stage: 2),
        new(PolicyAxis.ShorterHours, w => (int)w.WorkHours, (w, v) => w.WorkHours = (WorkHours)v, (w, p) => (int)p.WorkHours, 3, Stage: 2),
        new(PolicyAxis.ChildLabor, w => w.ChildLaborAllowed ? 1 : 0, (w, v) => w.ChildLaborAllowed = v == 1, (w, p) => p.ChildLaborAllowed ? 1 : 0, 1),
        new(PolicyAxis.CornLaws, w => (int)w.CornLaws, (w, v) => w.CornLaws = (ReformLevel)v, (w, p) => (int)p.CornLaws, 2, Stage: 2),
        new(PolicyAxis.LandReform, w => (int)w.LandReform, (w, v) => w.LandReform = (ReformLevel)v, (w, p) => (int)p.LandReform, 2, Stage: 2),
        new(PolicyAxis.SerfEmancipation, w => (int)w.SerfEmancipation, (w, v) => w.SerfEmancipation = (ReformLevel)v, (w, p) => (int)p.SerfEmancipation, 2, Stage: 2),
        new(PolicyAxis.SocialSecurity, w => (int)w.SocialSecurityLaw, (w, v) => w.SocialSecurityLaw = (SocialSecurityLaw)v,
            (w, p) => Math.Clamp((int)Math.Round(Q5(p.SocialSecurityTarget) * SocialSecurityMaxLevel), 0, SocialSecurityMaxLevel), SocialSecurityMaxLevel, Stage: 2,
            Enact: (w, p, t) =>
            {
                p.SocialSecurityLaw = (SocialSecurityLaw)t;
                w.SocialSecurityLaw = (SocialSecurityLaw)StepLevel((int)w.SocialSecurityLaw, t, w, PolicyAxis.SocialSecurity);
                w.SocialSecurity = (int)w.SocialSecurityLaw / (double)SocialSecurityMaxLevel;
                w.Pension = w.SocialSecurityLaw == SocialSecurityLaw.Pension ? ReformLevel.Full : ReformLevel.None;
            }),
        new(PolicyAxis.ConscriptionLaw, w => (int)w.ConscriptionLaw, (w, v) => w.ConscriptionLaw = (ConscriptionLaw)v, (w, p) => (int)p.ConscriptionLaw, 2, Stage: 2),
        new(PolicyAxis.SecretBallot, w => (int)w.SecretBallot, (w, v) => w.SecretBallot = (SecretBallotLaw)v, (w, p) => (int)p.SecretBallot, 1),
        new(PolicyAxis.Bureaucracy, w => (int)w.Bureaucracy, (w, v) => w.Bureaucracy = (BureaucracyLaw)v, (w, p) => (int)p.Bureaucracy, 2, Stage: 2),
        new(PolicyAxis.ImmigrationRestriction, w => (int)w.ImmigrationRestriction, (w, v) => w.ImmigrationRestriction = (ImmigrationLaw)v, (w, p) => (int)p.ImmigrationRestriction, 2, Stage: 2),
        new(PolicyAxis.SpecialistExitBan, w => (int)w.SpecialistExitLaw, (w, v) => w.SpecialistExitLaw = (SpecialistExitLaw)v, (w, p) => (int)p.SpecialistExitLaw, 2, Stage: 2),
        new(PolicyAxis.StateReligion, w => (int)w.StateReligion, (w, v) => w.StateReligion = (StateReligionLaw)v, (w, p) => (int)p.StateReligion, 2, Stage: 2,
            Enact: (w, p, t) => w.StateReligion = w.Polity == Polity.ProletarianDictatorship
                ? StateReligionLaw.StateAtheism
                : (StateReligionLaw)StepLevel((int)w.StateReligion, t, w, PolicyAxis.StateReligion),
            FrontOpen: w => w.Polity != Polity.ProletarianDictatorship),
        new(PolicyAxis.Antitrust, w => (int)w.Antitrust, (w, v) => w.Antitrust = (AntitrustLaw)v, (w, p) => (int)p.Antitrust, 2, Stage: 2),
        new(PolicyAxis.Guild, w => (int)w.Guild, (w, v) => w.Guild = (GuildLaw)v, (w, p) => (int)p.Guild, 2, Stage: 2, Open: w => w.Coef.GuildLawGate > 0),
        new(PolicyAxis.MinimumWage, w => (int)w.MinimumWage, (w, v) => w.MinimumWage = (MinimumWageLaw)v, (w, p) => (int)p.MinimumWage, 3, Stage: 2),
        new(PolicyAxis.WartimeRationing, w => w.WartimeRationing ? 1 : 0, (w, v) => w.WartimeRationing = v == 1, (w, p) => p.WartimeRationing ? 1 : 0, 1),
        new(PolicyAxis.MilitaryExport, w => w.MilitaryExportBan ? 1 : 0, (w, v) => w.MilitaryExportBan = v == 1, (w, p) => p.MilitaryExportBan ? 1 : 0, 1),
        new(PolicyAxis.CivilianArms, w => w.CivilianArmsBan ? 1 : 0, (w, v) => w.CivilianArmsBan = v == 1, (w, p) => p.CivilianArmsBan ? 1 : 0, 1, Open: w => w.Coef.RebelArmsMarketEnabled > 0,
            Enact: (w, p, t) =>
            {
                int before = w.CivilianArmsBan ? 1 : 0;
                if (before != t) MechanismProbe.Eval(MechanismProbe.Site.CivilianArmsLaw);
                int after = StepLevel(before, t, w, PolicyAxis.CivilianArms);
                w.CivilianArmsBan = after == 1;
                if (after != before) MechanismProbe.Hit(MechanismProbe.Site.CivilianArmsLaw);
            }),
        new(PolicyAxis.OpiumPossession, w => (int)w.OpiumPossession, (w, v) => w.OpiumPossession = (ReformLevel)v, (w, p) => (int)p.OpiumPossession, 2, Stage: 2),
        new(PolicyAxis.OpiumExport, w => w.OpiumExportBan ? 1 : 0, (w, v) => w.OpiumExportBan = v == 1, (w, p) => p.OpiumExportBan ? 1 : 0, 1),
        new(PolicyAxis.Nationalization, w => w.NationalizeRailways ? 1 : 0, (w, v) => w.NationalizeRailways = v == 1, (w, p) => p.NationalizeRailways ? 1 : 0, 1),
        new(PolicyAxis.Nationalization, w => w.NationalizeMines ? 1 : 0, (w, v) => w.NationalizeMines = v == 1, (w, p) => p.NationalizeMines ? 1 : 0, 1),
        new(PolicyAxis.Nationalization, w => w.NationalizeMilitary ? 1 : 0, (w, v) => w.NationalizeMilitary = v == 1, (w, p) => p.NationalizeMilitary ? 1 : 0, 1),
        new(PolicyAxis.Nationalization, w => w.NationalizeOil ? 1 : 0, (w, v) => w.NationalizeOil = v == 1, (w, p) => p.NationalizeOil ? 1 : 0, 1),
        new(PolicyAxis.Nationalization, w => w.NationalizeElectricity ? 1 : 0, (w, v) => w.NationalizeElectricity = v == 1, (w, p) => p.NationalizeElectricity ? 1 : 0, 1),
        new(PolicyAxis.NationalizationAll, w => w.NationalizeAll ? 1 : 0, (w, v) => w.NationalizeAll = v == 1,
            (w, p) => p.NationalizeAll && (w.Polity == Polity.ProletarianDictatorship || w.NationalizeAll) ? 1 : 0, 1),
        new(PolicyAxis.ColonialSuffrage, w => (int)w.ColonialSuffrage, (w, v) => w.ColonialSuffrage = (ColonialSuffrageLaw)v, (w, p) => (int)p.ColonialSuffrage, 2, Stage: 2),
        new(PolicyAxis.MigrantNativeSuffrage, w => (int)w.MigrantNativeSuffrage, (w, v) => w.MigrantNativeSuffrage = (MigrantNativeSuffrageLaw)v, (w, p) => (int)p.MigrantNativeSuffrage, 1, Stage: 1),
        new(PolicyAxis.Inheritance, w => (int)w.Inheritance, (w, v) => w.Inheritance = (InheritanceLaw)v, (w, p) => (int)p.Inheritance, 4, Stage: 2),
    ];

    private static readonly Dictionary<PolicyAxis, LevelLaw> LevelLawByAxis = LevelLaws.GroupBy(l => l.Axis).ToDictionary(g => g.Key, g => g.First());

    internal sealed record RateLaw(PolicyAxis Axis, Func<World, double> Get, Action<World, double> Set, Func<NationPolicy, double> Target);

    internal static readonly RateLaw[] RateLaws =
    [
        new(PolicyAxis.IncomeTax, w => w.TaxRate.Value, (w, v) => w.TaxRate = new Share(v), p => p.IncomeTaxTarget),
        new(PolicyAxis.ProgressiveTax, w => w.ProgressiveTax, (w, v) => w.ProgressiveTax = v, p => p.ProgressiveTaxTarget),
        new(PolicyAxis.PollTax, w => w.PollTax, (w, v) => w.PollTax = v, p => p.PollTaxTarget),
        new(PolicyAxis.LandTax, w => w.LandTax, (w, v) => w.LandTax = v, p => p.LandTaxTarget),
        new(PolicyAxis.EducationSpending, w => w.EducationSpending, (w, v) => w.EducationSpending = v, p => p.EducationSpendingTarget),
    ];

    private static readonly Dictionary<PolicyAxis, RateLaw> RateLawByAxis = RateLaws.ToDictionary(l => l.Axis);

    internal static double LevelPosition(World w, PolicyAxis a)
        => RateLawByAxis.TryGetValue(a, out var rate) ? TaxStage(a, rate.Get(w)) / (double)TaxLawStages
         : LevelLawByAxis.TryGetValue(a, out var law) ? Math.Min(law.Get(w), law.Top) / (double)law.Top : 0.5;

    public static void Step(World w, Coefficients c)
    {
        var p = w.Policy;

        w.PoliticalPushBudget = 1.0;
        if (c.PoliticalBudgetGate > 0)
        {
            int fronts = CountOpenFronts(w, p);
            if (Instruments.On) Instruments.PushFronts(w, fronts);
            if (fronts > 1) w.PoliticalPushBudget = Math.Pow(1.0 / fronts, c.PoliticalBudgetGate);
        }

        w.PoliticalCapitalMax = PoliticalQuantities.PoliticalCapitalMax(w);

        double austGdp = w.FiscalEnabled && w.GovDebt.Value > 0 ? w.NominalGdpPerMonth.Value : 0;
        double austerity = austGdp > 1e-9
            ? PoliticalQuantities.Clamp01((Calendar.ToGdpYear(w.GovDebt, Calendar.PerMonthToPerYear(new MoneyPerMonth(austGdp))) - c.AusterityStartDebt) / c.AusterityRange) : 0;

        if (w.WarmupFrozen) { w.AusterityIntensity = austerity; return; }

        if (w.AtWarNow) { if (w.ConscriptionPreWar < 0) w.ConscriptionPreWar = (int)w.ConscriptionLaw; }
        else if (w.ConscriptionPreWar >= 0)
        {
            if ((int)w.ConscriptionLaw > w.ConscriptionPreWar) { w.ConscriptionLaw = (ConscriptionLaw)w.ConscriptionPreWar; p.ConscriptionLaw = w.ConscriptionLaw; }
            w.ConscriptionPreWar = -1;
        }

        int stepIdx = -1;
        if (Instruments.On) Instruments.StockAtLevelHead(w);
        foreach (var law in LevelLaws)
        {
            stepIdx++;
            if (Instruments.On) Instruments.LevelBegin(w, stepIdx);
            if (law.Open != null && !law.Open(w)) continue;
            int target = law.Target(w, p);
            if (law.Enact != null) law.Enact(w, p, target);
            else law.Set(w, StepLevel(law.Get(w), target, w, law.Axis));
        }

        foreach (var law in RateLaws) law.Set(w, Move(law.Get(w), Q4(law.Axis, law.Target(p)), c, w, law.Axis));
        if (c.LandTaxPhiMove > 0) MoveLandTaxPhi(w, c, w.LandTaxBasisPhi);
        if (w.CoercionFollowsForce)
            p.CoercionTarget = CoercionTargetFromForce(w.SoldierSizeK() * w.MilitaryReadiness * c.CoercionPerSoldier, c);
        w.BaseCoercion      = Move(w.BaseCoercion,      p.CoercionTarget,          c, w, PolicyAxis.Coercion, c.CoercionMax, mandate: !w.CoercionFollowsForce);
        double milArmyQ = Q5(p.ArmyUpkeepTarget), milNavyQ = Q5(p.NavyUpkeepTarget);
        double milRawTotal = milArmyQ + milNavyQ;
        double milTotalTarget = MilitaryTotalTarget(w, p);
        double milFrom = w.MilitarySpending.Value, milHigh = (MilitaryLawStages - 1) / (double)MilitaryLawStages;
        if (!WarStageOpen(w) && !(milFrom <= milHigh)) milFrom = milHigh;
        if (Instruments.On && !(milFrom == w.MilitarySpending.Value)) Instruments.MilitaryExpiry(w);
        double milTotal = Move(milFrom, milTotalTarget, c, w, PolicyAxis.Coercion, fullPush: WarStageOpen(w));
        double navRawA = p.ArmyUpkeepTarget, navRawN = p.NavyUpkeepTarget;
        double navShareTarget = navRawA + navRawN > 1e-9 ? navRawN / (navRawA + navRawN) : w.NavalSpendShare.Value;
        w.SetMilitarySpending(new Share(milTotal), new Share(navShareTarget));
        if (MilitaryStage(milRawTotal) > MilitaryStage(milTotalTarget)) p.MilitarySpendingTarget = milTotalTarget;
        w.Tariff            = Move(w.Tariff,            TariffTargetFloored(p, w, c), c, w, PolicyAxis.Tariff);
        double rateTarget = Q5(PoliticalQuantities.Clamp01(p.InterestRateTarget));
        if (w.OnGoldStandard && !w.CapitalControlLaw && w.GoldStandardLaw)
        {
            double coverage = w.BroadMoney.Value > 1e-9 ? w.BaseMoney / w.BroadMoney : 1.0;
            double coverTarget = c.GoldCoverMin.Value * c.GoldRejoinCoverMargin;
            double gap = coverTarget > 1e-9 ? (coverTarget - coverage) / coverTarget : 0;
            gap = Math.Clamp(gap, -1, 1);
            if (!(gap >= -1 && gap <= 1)) gap = 0;
            rateTarget = PoliticalQuantities.Clamp01(w.InterestRate + c.TrilemmaRateResponse * gap);
        }
        w.InterestRate = PoliticalQuantities.Toward(w.InterestRate, rateTarget, c.PolicyBaseSpeed);
        w.AdminSpending = PoliticalQuantities.Toward(w.AdminSpending, Q5(PoliticalQuantities.Clamp01(p.AdminSpendingTarget)), c.PolicyBaseSpeed);

        if (Instruments.On) Instruments.StockAtStepEnd(w);
        w.AusterityIntensity = austerity;
    }

    private static double TariffTargetFloored(NationPolicy p, World w, Coefficients co) => PoliticalQuantities.ClampOr(Q5(p.TariffTarget) + w.TariffRetaliationBias, co.TariffFloor, 1, w.Tariff);

    internal static double Q5(double x) => Math.Round(x / SliderStep, MidpointRounding.AwayFromZero) * SliderStep;

    internal static int TaxStage(PolicyAxis a, double x) => Math.Clamp((int)Math.Round(Q5(x) / TaxLawTop(a) * TaxLawStages, MidpointRounding.AwayFromZero), 0, TaxLawStages);

    internal static double TaxValue(PolicyAxis a, double stage)
    {
        double s = !(stage >= 0.0) ? 0.0 : Math.Min(stage, (double)TaxLawStages);
        return s == TaxLawStages ? TaxLawTop(a) : s * TaxLawTop(a) / TaxLawStages;
    }

    internal static int MilitaryStage(double x) => Math.Clamp((int)Math.Round(x * MilitaryLawStages, MidpointRounding.AwayFromZero), 1, MilitaryLawStages);

    internal static double Q4(PolicyAxis a, double x) => TaxValue(a, TaxStage(a, x));

    internal static bool WarStageOpen(World w) => w.AtWarNow && w.Mobilized;

    internal static double MilitaryTotalTarget(World w, NationPolicy p)
    {
        double raw = Q5(p.ArmyUpkeepTarget) + Q5(p.NavyUpkeepTarget);
        int stage = Math.Min(MilitaryStage(raw), WarStageOpen(w) ? MilitaryLawStages : MilitaryLawStages - 1);
        return stage / (double)MilitaryLawStages;
    }

    private static int AusterityStage(World w, Coefficients c, PolicyAxis a)
        => (int)Math.Ceiling(PoliticalQuantities.Clamp01(Math.Min(1.0, w.AusterityIntensity * c.AusterityTaxHike) / TaxLawTop(a)) * TaxLawStages);

    private static bool FiscalStagedAxis(PolicyAxis a) => RateLawByAxis.ContainsKey(a) || a == PolicyAxis.Coercion;

    private static double MandateMargin(World w, PolicyAxis a, int targetLevel)
    {
        if (!w.PoliticsEnabled) return 0;
        return IsConstitutional(a, targetLevel) ? 0 : double.PositiveInfinity;
    }

    private static bool IsConstitutional(PolicyAxis a, int targetLevel) =>
        a is PolicyAxis.Suffrage or PolicyAxis.SecretBallot
        or PolicyAxis.ColonialSuffrage or PolicyAxis.MigrantNativeSuffrage
        || (a == PolicyAxis.WomenRights && targetLevel >= (int)WomenRightsLaw.Suffrage);

    internal static int NextStage(World w, PolicyAxis a, bool increasing)
    {
        int cur = CurrentStage(w, a);
        return cur == int.MaxValue ? (increasing ? 1 : 0) : cur + (increasing ? 1 : -1);
    }

    internal static bool AtTopStage(World w, PolicyAxis a)
        => RateLawByAxis.TryGetValue(a, out var rate) ? TaxStage(a, rate.Get(w)) >= TaxLawStages
         : a == PolicyAxis.Coercion ? MilitaryStage(w.MilitarySpending.Value) >= MilitaryLawStages
         : LevelLawByAxis.TryGetValue(a, out var law) && law.Get(w) >= law.Top;

    private static int CountOpenFronts(World w, NationPolicy p)
    {
        int n = 0;
        if (Instruments.On) Instruments.FrontsBegin(w);
        void Front(double cur, double target, char kind, int idx, PolicyAxis axis) { if (Math.Abs(target - cur) > 0.026) { n++; if (Instruments.On) Instruments.FrontCounted(w, kind, idx, axis, cur, target); } }
        int levelIdx = 0;
        foreach (var law in LevelLaws)
        {
            if ((law.FrontOpen ?? law.Open)?.Invoke(w) != false)
            {
                int lc = law.Get(w), lt = law.Target(w, p);
                if (lc != lt) { n++; if (Instruments.On) Instruments.FrontCounted(w, 'L', levelIdx, law.Axis, lc, lt); }
            }
            levelIdx++;
        }
        int rateIdx = 0;
        foreach (var law in RateLaws) Front(law.Get(w), Q4(law.Axis, law.Target(p)), 'R', rateIdx++, law.Axis);
        Front(w.MilitarySpending.Value, MilitaryTotalTarget(w, p), 'M', 0, PolicyAxis.Coercion);
        if (!w.CoercionFollowsForce) Front(w.BaseCoercion / Math.Max(1e-9, w.Coef.CoercionMax), Q5(p.CoercionTarget / Math.Max(1e-9, w.Coef.CoercionMax)), 'C', 0, PolicyAxis.Coercion);
        Front(w.Tariff, Q5(TariffTargetFloored(p, w, w.Coef)), 'T', 0, PolicyAxis.Tariff);
        return n;
    }

    private static bool MandateGate(World w, PolicyAxis a, bool increasing, int targetLevel)
    {
        double net = w.PoliticalLockEnabled ? PolicyPreferences.NetSupport(w, a, increasing) : 1.0;
        if (!(net > -MandateMargin(w, a, targetLevel))) { if (Instruments.On) Instruments.GateResult(w, 1, net); return false; }
        if (net < 0)
        {
            double cost = -net * w.Coef.MandateSpendRate;
            if (Instruments.On) Instruments.Priced(w, cost);
            if (!PoliticalCapitalAffords(w, cost)) { if (Instruments.On) { Instruments.GateResult(w, 2, net); Instruments.RfBlocked(w, true, cost); } return false; }
            if (Instruments.On) Instruments.WallPass(w, cost, true, a);
            Spend(w, cost);
        }
        if (Instruments.On) Instruments.GateResult(w, net < 0 ? 4 : 3, net);
        return true;
    }

    internal static bool PoliticalCapitalAffords(World w, double cost)
    {
        if (w.MandateSpent + cost <= w.PoliticalCapitalMax) return true;
        double power = w.Coef.MandateOverStockLegitPower;
        if (!(power > 0)) return false;
        double avail = w.Legitimacy - w.MandateSpentNew;
        return w.MandateSpent <= 0 && avail > 0 && cost <= Math.Pow(avail, power);
    }


    public static bool CanStep(World w, PolicyAxis a, bool increasing)
    {
        double net = w.PoliticalLockEnabled ? PolicyPreferences.NetSupport(w, a, increasing) : 1.0;
        if (!(net > -MandateMargin(w, a, NextStage(w, a, increasing)))) return false;
        return !(net < 0) || PoliticalCapitalAffords(w, -net * w.Coef.MandateSpendRate);
    }

    internal static bool UniversalConscriptionOpen(World w) => Privileges.SeigneurialDegree(w) <= 0;

    public static (bool Ok, string Reason) LawPrereq(World w, PolicyAxis a, int targetLevel, Coefficients c, BureaucracyLaw? bur = null)
    {
        if (StagedAxis(a) && targetLevel > CurrentStage(w, a) + 1)
            return (false, "前段階が必要");
        switch (a)
        {
            case PolicyAxis.IncomeTax:
                if (targetLevel >= 1 && targetLevel > CurrentStage(w, a) && !(w.TaxRate.Value > 0) && !(w.FiscalFundingRatio.Value < 1.0)) return (false, "払えない請求が前提");
                break;
            case PolicyAxis.EducationSpending:
                if (targetLevel >= 1 && w.EducationLaw < EducationLaw.Public && targetLevel > CurrentStage(w, a)) return (false, "教育法（公教育以上）が前提");
                break;
            case PolicyAxis.PollTax:
                if (targetLevel < AusterityStage(w, c, a)) return (false, "緊縮の下限");
                break;
            case PolicyAxis.Coercion:
                if (targetLevel >= MilitaryLawStages && !WarStageOpen(w)) return (false, "総動員が前提");
                if (targetLevel < 1) return (false, "軍事費に「なし」は無い");
                if (w.AtWarNow && c.WartimeUpkeepFloor.Value > 0 && targetLevel < MilitaryStage(w.MilitarySpending.Value * c.WartimeUpkeepFloor.Value))
                    return (false, "戦争中は軍を解散できない");
                break;
            case PolicyAxis.SocialSecurity:
                if (targetLevel > (int)Math.Floor(PoliticalQuantities.Clamp01(1.0 - w.AusterityIntensity) * SocialSecurityMaxLevel)
                    && targetLevel > CurrentStage(w, a)) return (false, "緊縮の上限");
                break;
        }
        switch (a)
        {
            case PolicyAxis.Suffrage:
                if (targetLevel >= (int)Suffrage.Universal && !w.WomenSuffrage) return (false, "女性の権利（選挙権）が前提");
                break;
            case PolicyAxis.ConscriptionLaw:
                if (targetLevel >= (int)ConscriptionLaw.Universal && targetLevel > CurrentStage(w, a) && !UniversalConscriptionOpen(w)) return (false, "農奴解放（全面）が前提");
                break;
            case PolicyAxis.SecretBallot:
                if (targetLevel >= (int)SecretBallotLaw.Secret && w.Suffrage < Suffrage.Census) return (false, "参政権（制限選挙以上）が前提");
                break;
            case PolicyAxis.ColonialSuffrage:
            case PolicyAxis.MigrantNativeSuffrage:
                if (targetLevel >= 1 && w.Suffrage < Suffrage.Census) return (false, "参政権（制限選挙以上）が前提");
                break;
            case PolicyAxis.SocialSecurity:
                if (targetLevel >= (int)SocialSecurityLaw.Unemployment && (bur ?? w.Bureaucracy) < BureaucracyLaw.MeritExam) return (false, "官僚制度（資格任用以上）が前提");
                break;
            case PolicyAxis.Antitrust:
                if (targetLevel >= (int)AntitrustLaw.CartelBan && (bur ?? w.Bureaucracy) < BureaucracyLaw.MeritExam) return (false, "官僚制度（資格任用以上）が前提");
                break;
            case PolicyAxis.Nationalization:
            case PolicyAxis.NationalizationAll:
                if (targetLevel >= 1 && (bur ?? w.Bureaucracy) < BureaucracyLaw.MeritExam) return (false, "官僚制度（資格任用以上）が前提");
                if (a == PolicyAxis.NationalizationAll && w.Polity != Polity.ProletarianDictatorship) return (false, "プロレタリア独裁のみ");
                break;
            case PolicyAxis.ProgressiveTax:
                if (targetLevel >= 1 && (bur ?? w.Bureaucracy) < BureaucracyLaw.MeritExam) return (false, "官僚制度（資格任用以上）が前提");
                break;
            case PolicyAxis.WomenRights:
                if (targetLevel >= (int)WomenRightsLaw.Suffrage && w.Suffrage < Suffrage.AdultMale) return (false, "男性普通選挙が前提");
                break;
            case PolicyAxis.StateReligion:
                if (targetLevel >= (int)StateReligionLaw.StateAtheism && w.Polity != Polity.ProletarianDictatorship) return (false, "プロレタリア独裁のみ");
                break;
        }
        return (true, "");
    }

    private static bool StagedAxis(PolicyAxis a)
        => LevelLawByAxis.TryGetValue(a, out var law) && law.Stage == 2
        || FiscalStagedAxis(a);

    private static int CurrentStage(World w, PolicyAxis a) => a switch
    {
        PolicyAxis.Coercion => MilitaryStage(w.MilitarySpending.Value),
        _ => RateLawByAxis.TryGetValue(a, out var rate) ? TaxStage(a, rate.Get(w))
           : LevelLawByAxis.TryGetValue(a, out var law) && law.Stage > 0 ? law.Get(w) : int.MaxValue,
    };

    internal static bool EnactingStage(World w, PolicyAxis a, bool increasing)
    {
        var p = w.Policy;
        int t = RateLawByAxis.TryGetValue(a, out var rate) ? TaxStage(a, rate.Target(p)) : -1;
        if (t < 0) return false;
        int cur = CurrentStage(w, a);
        if (increasing ? t <= cur : t >= cur) return false;
        return GateInfo(w, a, increasing, NextStage(w, a, increasing)).Ok;
    }

    public static (bool Ok, double Cost, string Reason) GateInfo(World w, PolicyAxis a, bool increasing, int targetLevel)
    {
        PolicyPreferences.Profile? none = null;
        return GateInfo(w, a, increasing, targetLevel, ref none);
    }

    internal static (bool Ok, double Cost, string Reason) GateInfo(World w, PolicyAxis a, bool increasing, int targetLevel, ref PolicyPreferences.Profile? shared, BureaucracyLaw? bur = null)
    {
        if (increasing || FiscalStagedAxis(a))
        {
            var (pok, preq) = LawPrereq(w, a, targetLevel, w.Coef, bur);
            if (!pok) return (false, 0, preq);
        }
        if (BlockedByIrreversibility(w, a, Regressive(a, increasing)))
            return (false, 0, a is PolicyAxis.ShorterHours or PolicyAxis.ChildLabor ? "戦時のみ緩和可" : "後退不能（支持層が確立済み）");
        double net = w.PoliticalLockEnabled ? PolicyPreferences.NetSupport(w, a, increasing, shared ??= PolicyPreferences.Build(w)) : 1.0;
        double cost = net < 0 ? -net * w.Coef.MandateSpendRate : 0;
        bool ok = net > -MandateMargin(w, a, targetLevel);
        if (ok && net < 0 && !PoliticalCapitalAffords(w, cost)) return (false, cost, w.Coef.MandateOverStockLegitPower > 0 && w.MandateSpent <= 0 && cost > w.PoliticalCapitalMax ? ReasonNoLegitimacy : ReasonNoCapitalStock);
        return (ok, cost, ok ? "" : ReasonNoMandate);
    }

    public const string ReasonNoMandate = "賛成不足";
    public const string ReasonNoCapitalStock = "政治資本の残りが不足";
    public const string ReasonNoLegitimacy = "正統性が不足";

    public static bool IsPoliticalGateReason(string reason) => reason == ReasonNoMandate || reason == ReasonNoCapitalStock || reason == ReasonNoLegitimacy;

    private static void Spend(World w, double amount)
    {
        double applied = amount;
        if (!(w.MandateSpent + amount <= w.PoliticalCapitalMax))
            applied = w.PoliticalCapitalMax > w.MandateSpent ? w.PoliticalCapitalMax - w.MandateSpent : 0;
        if (Instruments.On) Instruments.SpendApplied(w, amount, applied);
        w.MandateSpent += applied;
        w.MandateSpentNew += amount;
    }

    internal static void MoveLandTaxPhi(World w, Coefficients c, double target)
    {
        if (w.WarmupFrozen) return;
        w.LandTaxBasisPhi = Move(w.LandTaxBasisPhi, target, c, w, PolicyAxis.LandTax, mandate: true);
    }

    private static double Move(double cur, double target, Coefficients c, World w, PolicyAxis a, double spendScale = 1.0, bool mandate = true, bool fullPush = false)
    {
        bool milProbe = Instruments.On && a == PolicyAxis.Coercion && spendScale == 1.0;
        if (Math.Abs(target - cur) < 1e-12) { if (milProbe) Instruments.MilitaryMove(w, cur, target, double.NaN, double.NaN, double.NaN, 0, default, false); if (Instruments.On) Instruments.MoveResult(w, a, spendScale, 0, cur, target, cur, double.NaN, double.NaN); return cur; }
        bool increasing = target > cur;
        var e = PolicyPreferences.Build(w);
        double net = w.PoliticalLockEnabled ? PolicyPreferences.NetSupport(w, a, increasing, e) : 1.0;
        if (!(net > -(mandate ? MandateMargin(w, a, -1) : 0))) { if (milProbe) Instruments.MilitaryMove(w, cur, target, net, double.NaN, double.NaN, 1, e, increasing); if (Instruments.On) Instruments.MoveResult(w, a, spendScale, 1, cur, target, cur, net, double.NaN); return cur; }
        double resist = PolicyPreferences.OpposingPower(w, a, increasing, e);
        if (w.Coef.ParliamentBudgetVeto > 0 && (a == PolicyAxis.IncomeTax
            || a is PolicyAxis.PollTax or PolicyAxis.LandTax or PolicyAxis.ProgressiveTax))   // [PORT:P6] 予算闘争→法ゲートの抵抗への加算（★専売は Move を持たない＝2026-09-20 に腕を削除。門は World.SetStateMonopolyOn 側の GateInfo）
            resist += w.Coef.ParliamentBudgetVeto * Math.Max(0, w.ParliamentControl) * Math.Max(0, -w.CoopParliamentCrown);
        double speed = c.PolicyBaseSpeed / (1.0 + c.PolicyResistanceWeight * resist) * (mandate && !fullPush ? w.PoliticalPushBudget : 1.0);
        double next = cur + speed * (target - cur);
        if (mandate && net < 0)
        {
            double moved = Math.Min(1.0, Math.Abs(next - cur) / spendScale);
            double cost = -net * w.Coef.MandateSpendRate * moved;
            if (Instruments.On) Instruments.Priced(w, cost);
            if (!PoliticalCapitalAffords(w, cost)) { if (milProbe) Instruments.MilitaryMove(w, cur, target, net, resist, speed, 2, e, increasing); if (Instruments.On) { Instruments.MoveResult(w, a, spendScale, 2, cur, target, cur, net, speed); Instruments.RfBlocked(w, false, cost); } return cur; }
            if (Instruments.On) Instruments.WallPass(w, cost, false, a);
            Spend(w, cost);
        }
        if (milProbe) Instruments.MilitaryMove(w, cur, target, net, resist, speed, mandate && net < 0 ? 4 : 3, e, increasing);
        if (Instruments.On) Instruments.MoveResult(w, a, spendScale, mandate && net < 0 ? 4 : 3, cur, target, next, net, speed);
        return next;
    }

    internal static bool Regressive(PolicyAxis a, bool increasing) =>
        a is PolicyAxis.ShorterHours or PolicyAxis.ChildLabor ? increasing : !increasing;

    internal static bool BlockedByIrreversibility(World w, PolicyAxis a, bool regressive)
    {
        if (!regressive) return false;
        return a is PolicyAxis.SerfEmancipation or PolicyAxis.LandReform;
    }

    internal static bool AtWar(World w)
    {
        if (w.Diplomacy is not { } dip || w.SelfPower is not { } sp) return false;
        foreach (var war in dip.Wars) if (war.A == sp || war.B == sp) return true;
        return false;
    }

    private static int StepLevel(int ci, int ti, World w, PolicyAxis a)
    {
        if (ci == ti) { if (Instruments.On) Instruments.LevelResult(w, 0, ci, ti, ci); return ci; }
        bool increasing = ti > ci;
        bool regressive = Regressive(a, increasing);
        if (BlockedByIrreversibility(w, a, regressive)) { if (Instruments.On) Instruments.LevelResult(w, 5, ci, ti, ci); return ci; }
        if (increasing && !LawPrereq(w, a, ci + 1, w.Coef).Ok) { if (Instruments.On) Instruments.LevelResult(w, 6, ci, ti, ci); return ci; }
        if (MandateGate(w, a, increasing, ci + (increasing ? 1 : -1)))
        {
            if (Instruments.On) Instruments.LevelResult(w, -1, ci, ti, ci + (increasing ? 1 : -1));
            return ci + (increasing ? 1 : -1);
        }
        if (Instruments.On) Instruments.LevelResult(w, -1, ci, ti, ci);
        return ci;
    }
}
