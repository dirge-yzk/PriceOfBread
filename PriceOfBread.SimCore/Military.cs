namespace PriceOfBread.SimCore;

public static class Military
{
    public static void StepSkill(World w, Coefficients c, bool atWar)
    {
        bool fighting = atWar || w.InCivilWar;
        double train = w.MilitaryTrainingFill;
        w.ArmySkill = NextSkill(w.ArmySkill, train, fighting, c);
    }

    internal static void DiluteSkill(World w, double before, double after)
    {
        if (!(after > before) || !(before >= 0)) return;
        double keep = before / after;
        w.ArmySkill *= keep;
    }
    private static double NextSkill(double skill, double upkeep, bool fighting, Coefficients c)
    {
        double v = skill;
        double over = upkeep - c.SkillUpkeepThreshold;
        if (over > 0) v = PoliticalQuantities.Toward(v, 1.0, c.SkillGainRate * over);
        else v -= c.SkillDecayPerQuarter * v * Math.Min(1.0, -over / Math.Max(1e-9, c.SkillUpkeepThreshold));
        if (fighting) v = PoliticalQuantities.Toward(v, 1.0, c.SkillWarGain);
        return PoliticalQuantities.Clamp01(v);
    }

    public static double SkillCombatMultiplier(double skill, Coefficients c) => 1.0 + c.SkillCombatMax * PoliticalQuantities.Clamp01(skill);

    public static double EnvelopeOf(World w, Coefficients c) => EnvelopeAt(w, c, w.MilitarySpending.Value);

    public static double EnvelopeAt(World w, Coefficients c, double level)
    {
        double e = level * c.MilitaryEnvelopeShare.Value * Math.Max(0, w.SpendableRevenue());
        double war = WarStageShare(w, level);
        if (war > 0 && Fiscal.BorrowGateOpen(w, c)) e += war * Fiscal.BorrowTicket(w.NominalGdpPerMonth.Value, c);
        return e > 0 ? e : 0;
    }

    internal static double WarStageShare(World w, double level)
    {
        if (!Government.WarStageOpen(w)) return 0;
        double target = Government.MilitaryTotalTarget(w, w.Policy);
        if (!(level <= target)) level = target;
        double s = (level - (Government.MilitaryLawStages - 1) / (double)Government.MilitaryLawStages) * Government.MilitaryLawStages;
        return s > 0 ? (s < 1 ? s : 1.0) : 0;
    }

    public static double EstablishmentOf(World w, Coefficients c, out double natManpower)
    {
        natManpower = 0;
        double poolEarn = 0, poolSize = 0;
        bool keep = w.ConscriptionLaw >= ConscriptionLaw.Selective;
        foreach (var r in w.Regions)
        {
            double m = RegionManpower(r, c);
            if (!(m > 0)) continue;
            natManpower += m;
            foreach (var p in r.Pops)
                if (!p.IsSoldier && (p.ClassId == c.SoldierRecruitClassA || p.ClassId == c.SoldierRecruitClassB))
                { poolEarn += Math.Max(0, p.LastEarnedPerMonth.Value); poolSize += keep ? p.Size : WorkingAdults(p); }
        }
        double manLimit = c.MaxSoldierFraction.Value * natManpower;
        double wage = poolSize > 1e-9 ? poolEarn / poolSize : 0;
        double equip = 0;
        for (int b = 0; b < c.MilitaryGoods.Length; b++)
            if (c.MilitaryGoods[b] < Catalog.RefPrices.Length) equip += c.MilitaryUpkeepScale * c.MilitaryRates[b] * Catalog.RefPrices[c.MilitaryGoods[b]];
        double perSoldier = wage * c.SoldierPayMult + equip * w.CostPriceLevel;
        double econLimit = perSoldier > 1e-9 ? EnvelopeOf(w, c) / perSoldier : 0;
        double seats = Math.Min(econLimit, manLimit);
        return seats > 0 ? seats : 0;
    }

    internal static double RegionManpower(Region r, Coefficients c)
    {
        double m = 0; bool sld = false;
        foreach (var p in r.Pops)
        {
            if (p.IsSoldier) { sld = true; m += RecruitMen(p); }
            else if (p.ClassId == c.SoldierRecruitClassA || p.ClassId == c.SoldierRecruitClassB) m += RecruitMen(p);
        }
        return sld ? m * Politics.RuleDelivery(r, c) : 0;
    }

    internal static double WorkingAdults(Pop p) => p.Size * p.AgeShare(16, 60);

    internal static double RecruitWage(Region r, Coefficients c, bool last, bool keep = false)
    {
        double pe = 0, pa = 0;
        foreach (var p in r.Pops)
            if (!p.IsSoldier && (p.ClassId == c.SoldierRecruitClassA || p.ClassId == c.SoldierRecruitClassB))
            { pe += Math.Max(0, (last ? p.LastEarnedPerMonth : p.EarnedPerMonth).Value); pa += keep ? p.Size : WorkingAdults(p); }
        return pa > 1e-9 ? pe / pa : 0;
    }

    internal static double SoldierPayFull(World w, Coefficients c)
    {
        bool keep = w.ConscriptionLaw >= ConscriptionLaw.Selective;
        double pay = 0;
        foreach (var r in w.Regions)
        {
            if (r.SoldierPopIndex < 0) continue;
            pay += r.Pops[r.SoldierPopIndex].Size * RecruitWage(r, c, last: false, keep: keep) * c.SoldierPayMult;
        }
        return pay;
    }

    public static void Step(World w, Coefficients c, bool atWar)
    {
        double conscriptFrac = c.ConscriptionFraction.Value;
        if (atWar && w.Diplomacy is { } dip && w.SelfPower is { } self)
        {
            int fronts = 0; double enemyLand = 0;
            foreach (var war in dip.Wars)
            {
                Power? foe = war.A == self ? war.B : war.B == self ? war.A : null;
                if (foe is null) continue;
                fronts++; enemyLand += foe.LandPower;
            }
            double exh = self.WarExhaustion;
            double frontDepth = Math.Min(1.0, Math.Max(0, fronts - 1) * c.ConscriptionFrontStep);
            double ratioDepth = self.LandPower > 1e-9 ? PoliticalQuantities.Clamp01(enemyLand / self.LandPower - 1.0) : 0;
            double depth = PoliticalQuantities.Clamp01(c.ConscriptionExhaustionWeight * exh + frontDepth + c.ConscriptionEnemyRatioWeight * ratioDepth);
            conscriptFrac = c.ConscriptionFloorFraction.Value + (c.ConscriptionFraction.Value - c.ConscriptionFloorFraction.Value) * depth;
        }
        if (atWar && w.Mobilized) conscriptFrac = Math.Max(conscriptFrac, c.MobilizationConscriptFrac.Value);
        double seatsNat = EstablishmentOf(w, c, out double natManpower);
        bool lawConscripts = w.ConscriptionLaw >= ConscriptionLaw.Selective;
        double soldiersBefore = lawConscripts ? w.SoldierSizeK() : 0;
        foreach (var r in w.Regions)
        {

            Pop? soldier = null, poolA = null, poolB = null;
            foreach (var p in r.Pops)
            {
                if (p.IsSoldier) soldier ??= p;
                else if (p.ClassId == c.SoldierRecruitClassA) poolA ??= p;
                else if (p.ClassId == c.SoldierRecruitClassB) poolB ??= p;
            }
            if (soldier == null) continue;

            if (!lawConscripts) continue;
            double manpower = RegionManpower(r, c);

            double seats = natManpower > 1e-9 ? seatsNat * (manpower / natManpower) : 0;
            double desired = seats, rate = c.SoldierMigrationRatePerMonth.Value;
            bool wartimeConscript = atWar;
            double lawFrac = w.ConscriptionLaw == ConscriptionLaw.Universal ? c.UniversalConscriptionFactor : 1.0;
            if (wartimeConscript) { desired = Math.Max(desired, conscriptFrac * lawFrac * manpower); rate = c.ConscriptionRatePerMonth.Value; }
            double delta = rate * (desired - soldier.Size);
            if (!(delta > 0)) continue;
            DrawFromPool(soldier, poolA, poolB, delta, c, wartimeConscript, DraftAgeHi(w));
        }

        double sSize = w.SoldierSizeK();
        if (lawConscripts) DiluteSkill(w, soldiersBefore, sSize);
        double moraleFill = PowerShares.SoldierWelfare(w);
        double equipFillEff = w.Region.MilitaryEquipFill;
        w.MilitaryReadiness = PoliticalQuantities.Clamp01(moraleFill * equipFillEff);

        NavalFuelStep(w, c);
    }

    public static void NavalFuelStep(World w, Coefficients c)
    {
        if (!(c.NavalFuelPerGauge > 0)) return;
        var r = w.Region;
        double gauge = w.SelfPower?.FleetGauge ?? 0;
        double steamGate = Discovery.GoodGate(r, c.ShipTier2GoodId);
        double coalWant = c.NavalFuelPerGauge * gauge * PoliticalQuantities.Clamp01(w.NavyUpkeep.Value) * steamGate * c.EraFactor(w.WorldTechLevel, c.EraNavalFuel);
        double fill = 1.0;
        if (coalWant > 1e-9 && Catalog.CoalId < r.Markets.Length)
        {
            var (fuelG, want) = Colonial.ChooseFuelGood(r, c, coalWant);
            double got = Colonial.BuyUpkeepGood(w, r, fuelG, want, TreasuryCat.Navy);
            fill = want > 1e-9 ? got / want : 1.0;
            if (!(fill >= 0 && fill <= 1)) fill = 1.0;
        }
        double a = PoliticalQuantities.Clamp01(c.NavalFuelFillSmoothing);
        w.NavalFuelFillEma = (1.0 - a) * w.NavalFuelFillEma + a * fill;
        if (!(w.NavalFuelFillEma >= 0 && w.NavalFuelFillEma <= 1)) w.NavalFuelFillEma = 1.0;
    }

    private static void DrawFromPool(Pop soldier, Pop? a, Pop? b, double amount, Coefficients c, bool conscription, double draftHi)
    {
        double sa = a?.Size ?? 0, sb = b?.Size ?? 0, pool = sa + sb;
        if (pool < 1e-9) return;
        amount = Math.Min(amount, pool * 0.5);
        double da = Math.Min(amount * (sa / pool), sa), db = Math.Min(amount * (sb / pool), sb);
        MoveCohort(a, soldier, da, c, draftHi);
        MoveCohort(b, soldier, db, c, draftHi);
        if (conscription && c.ConscriptGrievanceRate > 0)
        {
            if (a is not null && sa > 1e-9 && da > 1e-12) a.Militancy = Math.Min(1, a.Militancy + c.ConscriptGrievanceRate * (da / sa));
            if (b is not null && sb > 1e-9 && db > 1e-12) b.Militancy = Math.Min(1, b.Militancy + c.ConscriptGrievanceRate * (db / sb));
        }
    }



    public static void MoveCohort(Pop? from, Pop? to, double amount, Coefficients c, double draftHi = 40)
    {
        if (from == null || to == null || amount < 1e-12 || from.Size < 1e-12) return;
        amount = Math.Min(amount, from.Size);
        bool draft = to.IsSoldier && !from.IsSoldier;
        if (draft)
        {
            amount = Math.Min(amount, from.Size * from.AgeShare(20, draftHi, 0));
            if (amount < 1e-12) return;
        }
        Pop.Blend(from, to, amount, draft: draft, draftLo: 20, draftHi: draftHi);
    }

    internal static double DraftAgeHi(World w) => w.ConscriptionLaw == ConscriptionLaw.Universal ? 50 : 40;

    internal static double RecruitMen(Pop? p) => p is null ? 0 : p.Size * p.AgeShare(16, 60, 0);

    internal static readonly double WorkingMenStdShare = Pop.StdShare(16, 60, 0);

    internal static double PeaceCapShareOfPop(Coefficients c) => c.MaxSoldierFraction.Value * WorkingMenStdShare;
}
