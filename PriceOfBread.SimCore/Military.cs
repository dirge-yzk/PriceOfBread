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
            double exh = c.WarExitFromCapacity > 0 ? 0 : self.WarExhaustion;
            double frontDepth = Math.Min(1.0, Math.Max(0, fronts - 1) * c.ConscriptionFrontStep);
            double ratioDepth = self.LandPower > 1e-9 ? PoliticalQuantities.Clamp01(enemyLand / self.LandPower - 1.0) : 0;
            double depth = PoliticalQuantities.Clamp01(c.ConscriptionExhaustionWeight * exh + frontDepth + c.ConscriptionEnemyRatioWeight * ratioDepth);
            conscriptFrac = c.ConscriptionFloorFraction.Value + (c.ConscriptionFraction.Value - c.ConscriptionFloorFraction.Value) * depth;
        }
        if (atWar && w.Mobilized) conscriptFrac = Math.Max(conscriptFrac, c.MobilizationConscriptFrac.Value);
        double seatsNat = EstablishmentOf(w, c, out double natManpower);
        bool lawConscripts = w.ConscriptionLaw >= ConscriptionLaw.Selective;
        double soldiersBefore = lawConscripts ? w.SoldierSizeK() : 0;
        if (c.SoldierToVacantSeat > 0)   // (1271) 旗：徴募の前に、席を超える州の兵を空席の州へ（NaN は旧の形へ倒れる）
            MoveToVacantSeats(w, c, seatsNat, natManpower,
                atWar && lawConscripts ? conscriptFrac * (w.ConscriptionLaw == ConscriptionLaw.Universal ? c.UniversalConscriptionFactor : 1.0) : double.NaN);
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
            DrawFromPool(soldier, poolA, poolB, delta, c, wartimeConscript, DraftAgeHi(w), w, r);
        }

        double sSize = w.SoldierSizeK();
        if (lawConscripts) DiluteSkill(w, soldiersBefore, sSize);
        double moraleFill = PowerShares.SoldierWelfare(w);
        double equipFillEff = w.Region.MilitaryEquipFill;
        w.MilitaryReadiness = PoliticalQuantities.Clamp01(moraleFill * equipFillEff);

        NavalFuelStep(w, c);
    }

    /// <summary>(1271) 旗 SoldierToVacantSeat：同じ国の中で、兵が席を超える州の〈超過×SoldierMigrationRatePerMonth〉を、席の空いた州へ空席の比で移す。
    /// 席＝下の徴募のループの目標と同じ式（平時＝国の定員×州の兵役の母数の比・徴兵の法がある国の戦時＝その値と warFrac×母数の大きい方）。
    /// 移す総量＝min(率×Σ超過, Σ空席)＝兵の総数は保存（移すだけ）。お金・年齢・思想は MoveCohort（Pop.Blend）のまま・熟練は国の値（ArmySkill）で動かない。
    /// 徴兵の法に依らない（駐屯の配置は徴募の法と別）・戦時も同じ式。移し元と移し先の組は州の順の北西隅の規則（組の数は州の数−1 以下）。</summary>
    internal static void MoveToVacantSeats(World w, Coefficients c, double seatsNat, double natManpower, double warFrac)
    {
        if (!(natManpower > 1e-9)) return;
        int n = w.Regions.Count;
        var pops = new Pop?[n]; var gap = new double[n];
        double over = 0, vacant = 0;
        for (int i = 0; i < n; i++)
        {
            var r = w.Regions[i];
            Pop? s = null;
            foreach (var p in r.Pops) if (p.IsSoldier) { s = p; break; }
            if (s is null) continue;
            double manpower = RegionManpower(r, c);
            double seat = seatsNat * (manpower / natManpower);
            if (warFrac > 0) seat = Math.Max(seat, warFrac * manpower);
            double g = seat - s.Size;
            if (!double.IsFinite(g)) continue;
            pops[i] = s; gap[i] = g;
            if (g < 0) over -= g; else vacant += g;
        }
        double move = Math.Min(c.SoldierMigrationRatePerMonth.Value * over, vacant);
        if (!(move > 1e-12)) return;
        double outScale = move / over, inScale = move / vacant;
        bool insOn = Instruments.On;
        int j = -1; double need = 0;
        for (int i = 0; i < n; i++)
        {
            if (pops[i] is not { } src || !(gap[i] < 0)) continue;
            double give = -gap[i] * outScale;
            while (give > 1e-12)
            {
                if (!(need > 1e-12))
                {
                    do j++; while (j < n && !(pops[j] is not null && gap[j] > 0));
                    if (j >= n) return;
                    need = gap[j] * inScale;
                    continue;
                }
                double amt = Math.Min(give, need), s0 = src.Size;
                MoveCohort(src, pops[j], amt, c);
                if (insOn) Instruments.PopMove(Instruments.PopMoveKind.Garrison, w.Regions[i], w.Regions[j], s0 - src.Size);   // (1271) popclose_s の garrisonInK／OutK
                give -= amt; need -= amt;
            }
        }
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

    private static void DrawFromPool(Pop soldier, Pop? a, Pop? b, double amount, Coefficients c, bool conscription, double draftHi, World w, Region r)
    {
        double sa = a?.Size ?? 0, sb = b?.Size ?? 0, pool = sa + sb;
        if (pool < 1e-9) return;
        amount = Math.Min(amount, pool * 0.5);
        double da = Math.Min(amount * (sa / pool), sa), db = Math.Min(amount * (sb / pool), sb);
        MoveCohort(a, soldier, da, c, draftHi);
        MoveCohort(b, soldier, db, c, draftHi);
        bool insOn = Instruments.On;
        double insMa0 = insOn ? (a?.Militancy ?? 0) : 0, insMb0 = insOn ? (b?.Militancy ?? 0) : 0;   // (1206) 計器 soldiervac_s の強制の徴募の列（On の時だけ）
        if (conscription && c.ConscriptGrievanceRate > 0)
        {
            if (a is not null && sa > 1e-9 && da > 1e-12) a.Militancy = Math.Min(1, a.Militancy + c.ConscriptGrievanceRate * (da / sa));
            if (b is not null && sb > 1e-9 && db > 1e-12) b.Militancy = Math.Min(1, b.Militancy + c.ConscriptGrievanceRate * (db / sb));
        }
        if (insOn) Instruments.SoldierVacDraw(w, r, conscription, da + db, (a is null ? 0 : (a.Militancy - insMa0) * a.Size) + (b is null ? 0 : (b.Militancy - insMb0) * b.Size));
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
