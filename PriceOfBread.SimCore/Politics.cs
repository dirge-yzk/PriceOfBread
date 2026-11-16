namespace PriceOfBread.SimCore;

public static class Politics
{
    public static double FreedomFromLaws(Suffrage suffrage, CensorshipLaw press)
    {
        double political = suffrage switch
        {
            Suffrage.Universal => 0.65,
            Suffrage.AdultMale => 0.55,
            Suffrage.Census => 0.50,
            _ => 0.20,
        };
        return PoliticalQuantities.Clamp01(political + 0.25 * PressFreedom(press));
    }

    public static double PressFreedom(CensorshipLaw press) => press switch
    {
        CensorshipLaw.FreePress => 1.0,
        CensorshipLaw.Censorship => 0.6,
        _ => 0.0,
    };

    public static CensorshipLaw FoundingPress(Polity p) => p switch
    {
        Polity.Republic => CensorshipLaw.FreePress,
        Polity.ConstitutionalMonarchy => CensorshipLaw.Censorship,
        _ => CensorshipLaw.StatePressOnly,
    };

    public static Suffrage PolitySuffrage(Polity p) => p switch
    {
        Polity.Republic => Suffrage.AdultMale,
        Polity.ProletarianDictatorship => Suffrage.AdultMale,
        Polity.ConstitutionalMonarchy => Suffrage.Census,
        Polity.BourgeoisDictatorship => Suffrage.Census,
        _ => Suffrage.None,
    };

    private static double ConsciousnessConversion(EducationLaw law) => law switch
    {
        EducationLaw.Public => 1.0,
        EducationLaw.Compulsory => 1.0,
        EducationLaw.Religious => 0.15,
        _ => 0.1,
    };

    private static double SchoolingConversion(Pop p, double convertNow, Coefficients c)
    {
        if (c.ConsciousnessFromSchoolingStock <= 0) return convertNow;
        double stock = p.SchoolingConvStock;
        if (stock < 0) stock = convertNow;
        stock = p.GenerationalMix(stock, convertNow, Calendar.MonthsPerQuarter);
        p.SchoolingConvStock = stock;
        double g = c.ConsciousnessFromSchoolingStock;
        return (1.0 - g) * convertNow + g * stock;
    }


    internal static double LivingWant(Pop p, Coefficients c)
    {
        double refv = p.LivingRef < 0 ? p.Satisfaction : p.LivingRef;
        return refv - p.Satisfaction;
    }

    internal static double FamineDrive(Pop p, Coefficients c)
    {
        if (!(c.FamineDriveGain > 0) || !(p.FoodNeed > 1e-9)) return 0;
        double fill = p.FoodMet / p.FoodNeed;
        if (fill > 1) fill = 1;
        double refv = p.FoodRef < 0 ? fill : p.FoodRef;
        double drive = c.FamineDriveGain * (refv - fill - c.FamineDropOnset);
        return PoliticalQuantities.Clamp01(drive);
    }

    internal static double EffectiveVoice(World w, Coefficients c, Pop p)
    {
        double effVoice = InterestGroups.FranchiseVoice(p, w.Suffrage, c);
        {
            double reality = w.Polity is Polity.ConstitutionalMonarchy or Polity.Republic
                ? (w.SecretBallot == SecretBallotLaw.Secret ? 1.0 : c.OpenBallotReality)
                : c.PaperFranchiseReality;
            effVoice *= reality;
        }
        return effVoice;
    }

    internal static double Discontent(World w, Coefficients c, Pop p, double[] regimeCoord, double lawGap)
    {
        double distance = c.DiscontentLawGap > 0 ? lawGap : AxisDistance(p, regimeCoord);
        double d = DiscontentRaw(w, c, p, distance);
        return PoliticalQuantities.Clamp01(d);
    }

    private static (double Living, double Voice) DiscontentTerms(World w, Coefficients c, Pop p, double distance)
        => (c.WantWeight * LivingWant(p, c), PoliticalTerm(w, c, p, distance));

    private static double PoliticalTerm(World w, Coefficients c, Pop p, double distance)
        => c.PoliticalDistanceWeight * LawDistance(w, c, p, distance);

    internal static double DiscontentRaw(World w, Coefficients c, Pop p, double distance)
    {
        var (living, voice) = DiscontentTerms(w, c, p, distance);
        return living + voice;
    }

    private static double AxisDistance(Pop p, double[] regimeCoord)
    {
        double[] ideal = Ideologies.IdealPoint(p.IdeologyShares);
        double pd2 = 0; for (int i = 0; i < 4; i++) { double x = ideal[i] - regimeCoord[i]; pd2 += x * x; }
        return Math.Sqrt(pd2) * 0.5;
    }

    private static double LawDistance(World w, Coefficients c, Pop p, double distance)
        => Math.Min(p.Consciousness, p.Literacy) * (1.0 - EffectiveVoice(w, c, p)) * distance;

    public static void Step(World w, Coefficients c)
    {
        if (!w.PoliticsEnabled) return;

        if (w.RuptureWindowQuarters > 0) w.RuptureWindowQuarters--;
        if (w.LostRegionsCumulative > 1e-9)
            w.LostRegionsCumulative *= 1.0 - w.ReplacementRate(Calendar.MonthsPerQuarter);

        if (w.PendingRevoltDemand != 0)
        {
            double now = FactionSnapshot.AxisPosition(w, (PolicyAxis)(Math.Abs(w.PendingRevoltDemand) - 1)), was = w.PendingRevoltDemandPos;
            if (w.PendingRevoltDemand > 0 ? !(now <= was + 1e-9) : !(now >= was - 1e-9))
            {
                w.PendingRevoltDemand = 0;
                foreach (var r in w.Regions) if (r.Id == w.PendingRevoltRegion)
                    foreach (var p in r.Pops) if (p.IsCommoner) p.Militancy *= c.ReformMilitancyRelief;
            }
        }

        double convertNow = ConsciousnessConversion(w.EducationLaw);
        double momentum = PoliticalQuantities.Clamp01((w.ReformistPower + w.RadicalPower) / Math.Max(1, PoliticalQuantities.EffectiveCoerce(w, c)) * c.BandwagonSensitivity);

        bool inRevolt = w.InCivilWar || w.RevoltingFaction is not null || w.PendingRebelRegions is not null;
        foreach (var p in w.AllPops())
        {
            if (p.MilitiaShare.Value > 0 && !inRevolt) p.MilitiaShare = default;
            double urban = p.IsUrban ? c.UrbanConsciousnessBoost : 1.0;
            double convert = SchoolingConversion(p, convertNow, c);
            double bandwagon = c.BandwagonWeight * momentum * (1.0 - p.Literacy);
            double newsReach = c.NewsConsciousnessFloor + (1.0 - c.NewsConsciousnessFloor) * p.NewsFill;
            double press = 0.75 + 0.5 * PressFreedom(w.Censorship) * newsReach;
            // [PORT:P2] 意識targetへの寄与＝識字×教育変換×都市化＋便乗、×報道（新聞/検閲）。時定数17年の遅い変数＝速い興奮は闘争度(P1)の仕事
            double target = PoliticalQuantities.Clamp01((p.Literacy * convert * urban + bandwagon) * press);
            p.Consciousness = PoliticalQuantities.Toward(p.Consciousness, target, c.ConsciousnessSpeed);
        }

        double warWeariness = w.SelfPower?.WarExhaustion ?? 0;
        double[] regimeCoord = PoliticalQuantities.DriftedPolityCoord(w.Polity, w.Suffrage, c);
        foreach (var milReg in w.Regions)
        {
        foreach (var p in milReg.Pops)
        {
            if (p.LivingRef < 0) p.LivingRef = p.Satisfaction;
            double foodFill = p.FoodNeed > 1e-9 ? Math.Min(1.0, p.FoodMet / p.FoodNeed) : double.NaN;
            if (p.FoodRef < 0 && foodFill >= 0) p.FoodRef = foodFill;
            double voice = PoliticalTerm(w, c, p, AxisDistance(p, regimeCoord));
            double minorityMil = w.EthnicEnabled ? c.MinorityMilitancy * (1.0 - p.CoreEthnicShare.Value) : 0;
            if (w.EthnicEnabled)
                minorityMil *= 1.0 - PoliticalQuantities.Clamp01(RegionAvgSuffrageInclusion(w, c, milReg));
            // [PORT:P1] ★段2＝飢餓の駆動項（設計メモ §7-3・作者裁定2026-09-09）。⚠旧 FamineAbsoluteMilitancy=0.04 は2四半期で闘争度を 0.0024 しか動かせず
            double famineMil = FamineDrive(p, c);
            // [PORT:P1] 闘争度targetへの寄与（加算項）＝法（政治）の隔たり／民族／厭戦／飢餓の**4本**（生活の失望は不満度だけが持つ＝闘争度の目標に入らない・★(431)(551)）。新規機構はこのポートに独立加算で接続する。
            {
                p.MilVoice = voice;
                p.MilWar = c.WarWearinessMilitancy * warWeariness;
                p.MilNation = minorityMil;
                p.MilFamine = famineMil;
            }
            double sum = voice + minorityMil + c.WarWearinessMilitancy * warWeariness + famineMil;
            double target = PoliticalQuantities.Clamp01(sum);
            p.Militancy = PoliticalQuantities.Toward(p.Militancy, target, c.MilitancySpeed);
            if (c.LivingRefSpeed > 0)
                p.LivingRef = PoliticalQuantities.Toward(p.LivingRef, p.Satisfaction, c.LivingRefSpeed);
            if (c.LivingRefSpeed > 0 && foodFill >= 0)
                p.FoodRef = PoliticalQuantities.Toward(p.FoodRef, foodFill, c.LivingRefSpeed);
        }
        }

        Ideologies.UpdateShares(w);

        var (hope, conscSum, sizeSum) = MeasureMobilization(w, c);
        double reformist = w.ReformistPower, radical = w.RadicalPower;
        w.MeanSatisfactionPol = sizeSum > 1e-9 ? NationalScore.MeanSatisfaction(w) : 1.0;


        double dynErodeMul = 1.0, dynRegenMul = 1.0;
        {
            if (w.DynasticMandate < 0) w.DynasticMandate = 0.5;
            double dmPrev = w.DynasticMandate;
            var (uneduShare, uneduSatAvg) = PoliticalQuantities.MandateBase(w);
            double dmSat = c.DynMandateSatWeight * ((uneduSatAvg >= 0 ? uneduSatAvg : w.MeanSatisfactionPol) - c.DynMandateSatPivot) / (1.0 - c.DynMandateSatPivot);
            double dmFood = c.DynMandateFoodWeight * Math.Min(0, w.FoodFill - c.DynMandateFoodAnchor);
            double dmWar = -c.DynMandateWarWeight * (w.SelfPower?.WarExhaustion ?? 0);
            double dynPerf = dmSat + dmFood + dmWar;
            double dmTarget = PoliticalQuantities.Clamp01(0.5 + dynPerf);
            w.DynasticMandate = PoliticalQuantities.Toward(w.DynasticMandate, dmTarget, c.DynMandateEmaRate);
            if (w.Explain != null)
            {
                double r = c.DynMandateEmaRate;
                w.Explain.Reset("DynasticMandate");
                w.Explain.Add("DynasticMandate", "前期からの天命(履歴の累積)", dmPrev);
                w.Explain.Add("DynasticMandate", "中立への回帰", r * (0.5 - dmPrev));
                w.Explain.Add("DynasticMandate", "未教育層の満足(天命の母数)", r * dmSat);
                w.Explain.Add("DynasticMandate", "飢えの評価", r * dmFood);
                w.Explain.Add("DynasticMandate", "戦争疲弊の評価", r * dmWar);
                double dmClamp = r * ((dmTarget - 0.5) - dynPerf);
                if (dmClamp != 0) w.Explain.Add("DynasticMandate", "評価の飽和", dmClamp);
            }
            double dynE = (w.DynasticMandate - 0.5) * 2.0 * uneduShare;
            if (c.MandateReligiousCircuit > 0)
            {
                double lawOpen = w.StateReligion switch { StateReligionLaw.InnerFreedom => 0.85, StateReligionLaw.ReligiousFreedom => 0.7, StateReligionLaw.StateAtheism => 0.0, _ => 1.0 };
                bool clergyModeled = false; double clergySize = 0;
                foreach (var p in w.AllPops()) if (p.ClassId == PopClass.Clergy) { clergyModeled = true; clergySize += p.Size; }
                double illit = uneduShare * w.TotalPopulation;
                double capacity = clergyModeled && illit > 1e-9 ? Math.Min(1.0, clergySize / (illit * c.ClergyMediationPerCapita)) : 1.0;
                dynE *= 1.0 - c.MandateReligiousCircuit * (1.0 - lawOpen * capacity);
            }
            dynErodeMul = 1.0 - c.DynMandateAmplify * dynE;   // [PORT:P3] 天命→正統性の連続項レバー（侵食/回復の乗数）。名君＝侵食が鈍る／暗君＝加速
            dynRegenMul = 1.0 + c.DynMandateAmplify * dynE;
        }
        double avgConsc = sizeSum > 1e-9 ? conscSum / sizeSum : 0;
        double institutionalGap = avgConsc * Math.Max(0, c.RepresentationThreshold - w.Freedom);
        double erosion = (c.LegitimacyErosionRate * institutionalGap) * dynErodeMul + c.DefeatShockLegitWeight * w.DefeatShock;
        if (w.Explain != null)
        {
            w.Explain.Reset("Legitimacy");
            w.Explain.Add("Legitimacy", "前期からの正統性(履歴の累積)", w.Legitimacy);
            w.Explain.Add("Legitimacy", "自然回復(安定期の信任回復)", c.LegitimacyRegenRate * Math.Max(0, 1 - w.Legitimacy) * dynRegenMul);
            w.Explain.Add("Legitimacy", "制度ギャップ侵食(意識と参政権・報道の乖離)", -c.LegitimacyErosionRate * institutionalGap * dynErodeMul);
            w.Explain.Add("Legitimacy", "敗戦ショック", -c.DefeatShockLegitWeight * w.DefeatShock);
        }
        if (w.ParliamentEnabled)
        {
            double maxSeat = Math.Max(w.PartySeats[0], Math.Max(w.PartySeats[1], w.PartySeats[2]));
            if (w.Polity == Polity.ConstitutionalMonarchy)
            {
                double constErode = c.ConstMonarchyErosion * (w.PartySeats[1] + w.PartySeats[2]) * dynErodeMul;
                erosion += constErode;
                w.Explain?.Add("Legitimacy", "議会の反王党派議席(立憲君主の形骸化)", -constErode);
            }
            if (w.Polity is Polity.ConstitutionalMonarchy or Polity.Republic && maxSeat > c.PartyDominanceThreshold.Value && w.MeanSatisfactionPol < c.DominanceInstabilitySatisfaction)
            {
                double domErode = c.DominanceLegitimacyErosion * (maxSeat - c.PartyDominanceThreshold.Value) / (1 - c.PartyDominanceThreshold.Value) * dynErodeMul;
                erosion += domErode;
                w.Explain?.Add("Legitimacy", "一党支配の私物化(多元的正統性の毀損)", -domErode);
            }
        }
        double legStart = w.Legitimacy;
        double leg = w.Legitimacy - erosion + c.LegitimacyRegenRate * Math.Max(0, 1 - w.Legitimacy) * dynRegenMul;
        double mandateClaim = w.MandateSpentNew;
        leg -= mandateClaim;
        w.Explain?.Add("Legitimacy", "マンデート行使(政治資本で反対を押し切る)", -mandateClaim);
        w.MandateSpentNew = 0;
        double refill = w.PoliticalCapitalMax / Math.Max(1e-9, c.MandateRefillQuarters) * PoliticalQuantities.PoliticalCapitalRate(w);
        if (!(refill > 0)) refill = 0;
        w.MandateSpent = Math.Max(0, w.MandateSpent - refill);
        double legClamped = PoliticalQuantities.Clamp01(leg);
        if (w.Explain != null && legClamped != leg)
            w.Explain.Add("Legitimacy", legClamped > leg ? "下限クランプ(0未満を切上げ)" : "上限クランプ(1超を切下げ)", legClamped - leg);
        w.Legitimacy = legClamped;
        // ①[PORT:P3] P10①信任帝政の正統性の下限（2026-07-18 点火＝旗 RedefinedMandateEnabled は 2026-09-26 に焼き込み）：立憲君主×王冠保持×高NC×高天命の時のみ Legitimacy≥floor×Mandate の床。
        double mandateFloor = w.DynasticMandate >= 0
            && w.Polity == Polity.ConstitutionalMonarchy && w.Crown != CrownForm.None
            && w.NationalConsciousness >= c.RedefinedMandateNcMin && w.DynasticMandate >= c.RedefinedMandateMandateMin
            ? c.RedefinedMandateFloor * w.DynasticMandate : 0;
        double orderFloor = 1.0 - hope;
        double legFloor = Math.Max(mandateFloor, orderFloor) * (1.0 - PoliticalQuantities.Clamp01(w.DefeatShock));
        if (w.Legitimacy < legFloor)
        {
            w.Explain?.Add("Legitimacy", mandateFloor >= orderFloor ? "信任帝政の下限(民の信任を得た皇帝)" : "政変続きへの失望(秩序の受け入れ)", legFloor - w.Legitimacy);
            w.Legitimacy = legFloor;
        }
        bool eroding = w.Legitimacy < legStart;
        w.DefeatShock *= c.DefeatShockKeepPerQuarter;
        w.VictoryShock *= c.DefeatShockKeepPerQuarter;
        w.FamineMemory = w.FamineMemory * c.FamineMemoryKeepPerQuarter + Math.Max(0, c.DynMandateFoodAnchor - w.FoodFill);

        double warExh = w.SelfPower?.WarExhaustion ?? 0;
        double warDrain = 1.0
            + (Government.AtWar(w) ? c.WarRallyCoercion * Math.Max(0, 1.0 - warExh / Math.Max(1e-9, c.WarRallyDecayPoint)) : 0)
            - c.WarCoercionDrain * warExh;   // [PORT:P5] 戦時ドレイン→保持力の乗数（軍は内外を兼ねる一つの武力）
        w.Coercion = w.BaseCoercion * w.Legitimacy * w.FiscalFundingRatio.Value * warDrain;
        double coerce = PoliticalQuantities.EffectiveCoerce(w, c);

        Factions.Step(w, c);
        bool capitalHeld = w.SecedingRegion is not null;
        if (!capitalHeld) Transition(w, c, reformist, radical, coerce, eroding);
        bool capitalRose = w.PendingRebelRegions is not null;
        CheckSecession(w, c, coerce);
        if (!capitalRose && !capitalHeld) CheckFactionRevolt(w, c, coerce);
        if (!capitalRose && !capitalHeld) CheckRegionalRevolt(w, c, coerce);

        w.Freedom = FreedomFromLaws(w.Suffrage, w.Censorship);

        CrownParliament.Step(w, c);
    }

    internal static double UnionLegality(LaborUnionLaw law) => law switch { LaborUnionLaw.Tolerated => 1.0, LaborUnionLaw.Controlled => 0.5, _ => 0.0 };

    internal static double UnionRepression(LaborUnionLaw law) => 1.0 - UnionLegality(law);

    internal static (double Hope, double ConscSum, double SizeSum) MeasureMobilization(World w, Coefficients c)
    {
        double unionMob = c.UnionMobilization * UnionLegality(w.LaborUnion);
        double reformist = 0, radical = 0, conscSum = 0, sizeSum = 0;
        foreach (var p in w.AllPops())
        {
            if (p.IsSoldier) { conscSum += p.Consciousness * p.Size; sizeSum += p.Size; continue; }
            reformist += p.Size * p.Consciousness * (1 - p.Militancy);
            radical += p.Size * p.Consciousness * p.Militancy;
            if (unionMob > 0 && IsMassPop(p))
                reformist += p.Size * p.Consciousness * unionMob;
            double whoActs = 1.0 - c.DesperationConsciousnessGate * p.Consciousness;
            radical += p.Size * Math.Max(0, p.Militancy - c.RiotMilitancyThreshold) * c.DesperationChallengeWeight * whoActs;
            conscSum += p.Consciousness * p.Size;
            sizeSum += p.Size;
        }
        reformist *= 1 + c.ReformPressureBoost * w.ExternalPressure;
        double hope = 1.0 / Math.Max(1.0, w.RecentRegimeChanges);
        reformist *= hope;
        radical *= hope;
        w.ReformistPower = reformist;
        w.RadicalPower = radical;
        return (hope, conscSum, sizeSum);
    }

    internal static void Transition(World w, Coefficients c, double reformist, double radical, double coerce, bool eroding)
    {
        double challenge = reformist + radical;
        var agg = Ideologies.National(w);
        double[] curCoord = PoliticalQuantities.DriftedPolityCoord(w.Polity, w.Suffrage, c);
        double bourgeoisCoup = 0;
        if (c.BourgeoisCoupWeight > 0 && w.ParliamentEnabled && w.Polity is Polity.ConstitutionalMonarchy or Polity.Republic
            && w.MeanSatisfactionPol < c.DominanceInstabilitySatisfaction)
        {
            var (_, modernPower, _) = InterestGroups.Blocs(w);
            bourgeoisCoup = c.BourgeoisCoupWeight * modernPower * AvgMassMilitancy(w);
        }
        Polity attractor = w.Polity; double best = double.NegativeInfinity, cur = 0;
        foreach (var pol in Ideologies.Polities)
        {
            double s = Ideologies.SupportAt(agg, pol == w.Polity ? curCoord : Ideologies.BasePolityCoord(pol), c.IdeologyAffinitySlope);
            if (pol == Polity.BourgeoisDictatorship) s += bourgeoisCoup;
            if (c.RegimeTrackRecordWeight > 0)   // [PORT:P4] 実績ストック→遷移支持の乗数（Max(0.05,·)＝符号保存）
                s *= Math.Max(0.05, 1.0 + c.RegimeTrackRecordWeight * w.RegimeTrackRecord[CrownParliament.RegimeFamily(pol)]);
            if (pol == w.Polity) cur = s;
            if (s > best) { best = s; attractor = pol; }
        }
        if (attractor == w.Polity || best - cur <= c.PolityInertia * w.Legitimacy) return;
        {
            double holdA = SoldierHoldShare(w, c, curCoord, attractor, out double sCurS, out double sAttS, out double soldierW);
            if (Instruments.On) Instruments.CoupGate(w, attractor, sCurS, sAttS, holdA, soldierW, coerce, challenge);
            challenge += coerce * (1.0 - holdA); coerce *= holdA;
        }
        double anticipation = c.ReformAnticipationWeight > 0
            ? c.ReformAnticipationWeight * PoliticalQuantities.ChallengeRatio(challenge, coerce)
            : 0;
        bool reformFromAbove = !IsSeizure(attractor)
                             && w.Legitimacy < c.ReformFromAboveLegitThreshold + anticipation && eroding;
        if (challenge <= coerce && !reformFromAbove) { if (Instruments.On) Instruments.CoupRoute(w, 1); return; }
        if (!IsSeizure(attractor) && radical <= reformist && !(reformFromAbove && c.ReformGrantPcWidth > 0)) { if (Instruments.On) Instruments.CoupRoute(w, 2); return; }
        bool violent = (IsSeizure(attractor) || radical > reformist) && !reformFromAbove;
        if (violent && challenge < coerce * c.CivilWarLandslideRatio)
        {
            if (w.InCivilWar || w.PendingRebelRegions is not null) { if (Instruments.On) Instruments.CoupRoute(w, 7); return; }
            if (RaiseRevolt(w, w.Regions[0], attractor))
            {
                if (Instruments.On) Instruments.CoupRoute(w, 3);
                w.LogAdd($"{w.Year} {w.SelfPower?.Name}の首都{w.Regions[0].Name}で蜂起＝政府は首都を出て戦う（内戦）");
                return;
            }
        }
        if (reformFromAbove && c.GrantedControlOnReform > 0)
        {
            double grant = c.GrantedControlOnReform
                + c.ReformGrantPcWidth * PoliticalQuantities.ChallengeRatio(challenge, coerce);
            w.ParliamentControl = PoliticalQuantities.Clamp01(Math.Max(w.ParliamentControl, grant));
            w.ReformGrantCount++;
        }
        if (reformFromAbove) { if (Instruments.On) Instruments.CoupRoute(w, 4); return; }
        if (Instruments.On) Instruments.CoupRoute(w, 5, violent);
        ExecuteRegimeChange(w, c, attractor, violent);
    }

    private static bool IsSeizure(Polity p) => p is Polity.BourgeoisDictatorship or Polity.ProletarianDictatorship
                                                 or Polity.MilitaryDictatorship or Polity.Anarchy;

    internal static void ExecuteRegimeChange(World w, Coefficients c, Polity target, bool violent)
    {
        if (violent)
        {
            RevolutionAwakensNation(w, c);
            if (IsSeizure(target)) w.RuptureWindowQuarters = Math.Max(w.RuptureWindowQuarters, (int)c.RuptureWindowQuartersOnCrush);
        }
        CrownParliament.ApplyRegime(w, c, target);
        if (Instruments.On) Instruments.CrownSource(w, 1);
        RaiseFoundingInstitutions(w, allowRevoke: true, c);
        if (target is Polity.ProletarianDictatorship or Polity.Anarchy) Fiscal.RepudiateDebt(w, c);
        var (dt, dm, dmass) = Dispossessed(target);
        ResolveRevolt(w, c, dt, dm, dmass);
        if (dt)
        {
            var floor = target == Polity.ProletarianDictatorship ? ReformLevel.Full
                      : target == Polity.Republic ? ReformLevel.Limited
                      : (ReformLevel?)null;
            if (floor is { } fl && (int)w.LandReform < (int)fl)
            {
                w.LandReform = fl;
                w.Policy.LandReform = (ReformLevel)Math.Max((int)w.Policy.LandReform, (int)fl);
            }
        }
    }

    public static void RaiseFoundingInstitutions(World w, bool allowRevoke = false, Coefficients? c = null)
    {
        var sufTarget = PolitySuffrage(w.Polity);
        var pressTarget = FoundingPress(w.Polity);
        if (allowRevoke && c is not null)
        {
            int su = (int)w.Suffrage, pr = (int)w.Censorship;
            sufTarget = (Suffrage)Math.Clamp((int)sufTarget, su - 1, su + 1);
            pressTarget = (CensorshipLaw)Math.Clamp((int)pressTarget, pr - 1, pr + 1);
        }
        if (allowRevoke)
        {
            bool ratchet = c is not null && !IsReactionaryPolity(w.Polity);
            if (ratchet)
            {
                var sufFloorL = (Suffrage)Math.Max((int)w.Suffrage, (int)sufTarget);
                w.Suffrage = sufFloorL;
                w.Policy.Suffrage = (Suffrage)Math.Max((int)w.Policy.Suffrage, (int)sufFloorL);
                var pressFloorL = (CensorshipLaw)Math.Max((int)w.Censorship, (int)pressTarget);
                w.Censorship = pressFloorL;
                w.Policy.Censorship = (CensorshipLaw)Math.Max((int)w.Policy.Censorship, (int)pressFloorL);
            }
            else
            {
                w.Suffrage = sufTarget;
                w.Policy.Suffrage = sufTarget;
                w.Censorship = pressTarget;
                w.Policy.Censorship = pressTarget;
            }
        }
        else
        {
            var sufFloor = (Suffrage)Math.Max((int)w.Suffrage, (int)sufTarget);
            w.Suffrage = sufFloor;
            w.Policy.Suffrage = (Suffrage)Math.Max((int)w.Policy.Suffrage, (int)sufFloor);
            var pressFloor = (CensorshipLaw)Math.Max((int)w.Censorship, (int)pressTarget);
            w.Censorship = pressFloor;
            w.Policy.Censorship = (CensorshipLaw)Math.Max((int)w.Policy.Censorship, (int)pressFloor);
        }
    }

    private static bool IsReactionaryPolity(Polity p) => p is not (Polity.ConstitutionalMonarchy or Polity.Republic);

    private static (bool trad, bool modern, bool mass) Dispossessed(Polity target) => target switch
    {
        Polity.Absolutist or Polity.Despotic or Polity.Shogunate => (false, true, true),
        Polity.ConstitutionalMonarchy or Polity.Republic         => (true, false, false),
        Polity.BourgeoisDictatorship                             => (false, false, true),
        Polity.ProletarianDictatorship                           => (true, true, false),
        Polity.MilitaryDictatorship                              => (true, false, true),
        Polity.Anarchy                                           => (true, true, true),
        _                                                        => (false, false, false),
    };

    private static bool RaiseRevolt(World w, Region seat, Polity polity)
    {
        if (w.StepsDiplomacy || w.SelfPower is null || w.LeavableRegions([seat], capital: true).Count == 0) return false;
        w.PendingRebelRegions = [seat];
        w.PendingRebelPolity = polity;
        w.HadCivilWar = true;
        return true;
    }

    internal static void SettleRevolt(World w, Coefficients c, bool rebelWon, Polity target, FactionState? movement, IEnumerable<Pop> rebels)
    {
        if (rebelWon)
        {
            if (target != w.Polity) { ExecuteRegimeChange(w, c, target, violent: true); if (Instruments.On) Instruments.CrownSource(w, 3); }
            else if (movement is not null) ConcedeToFaction(w, c, movement);
        }
        else
        {
            foreach (var p in rebels) p.Militancy *= c.ReformMilitancyRelief;
            w.RuptureWindowQuarters = Math.Max(w.RuptureWindowQuarters, (int)c.RuptureWindowQuartersOnCrush);
        }
        SpendRevolt(w, movement);
    }

    internal static void SpendRevolt(World w, FactionState? movement)
    {
        if (movement is null) return;
        movement.Cohesion = 0;
        if (ReferenceEquals(w.RevoltingFaction, movement)) w.RevoltingFaction = null;
    }

    private static void CheckFactionRevolt(World w, Coefficients c, double coerce)
    {
        if (w.FactionRng is null) return;
        if (w.InCivilWar || w.PendingRebelRegions is not null) return;
        FactionState? best = null; double bestStrength = 0, bestDespair = 0;
        foreach (var f in w.Factions)
        {
            if (f.Cohesion < c.FactionCohesionFightThreshold) continue;
            if (!w.Regions.Contains(f.Region)) continue;
            double strength = FactionStrength(w, c, f);
            if (!(c.RevoltIgnitionIntensity > 0))
            {
                if (strength > bestStrength) { bestStrength = strength; best = f; }
                continue;
            }
            double intensity = strength / Math.Max(1e-9, f.Size);
            double despair = RegionRevoltHeat(f.Region, c)
                           + c.RevoltGrievanceWeight * Math.Min(1.0, c.FactionRejectionMutation * f.Rejections);
            if (!(intensity > c.RevoltIgnitionIntensity)) continue;
            if (!(despair > c.RegionalRevoltThreshold)) continue;
            if (despair > bestDespair || (despair == bestDespair && strength > bestStrength)) { bestDespair = despair; bestStrength = strength; best = f; }
        }
        if (best is null) { ClearFactionCrisis(w); return; }
        double hold = LocalizedHold(w, c, best.Region, coerce);
        double militia = LoyalistMilitia(w, c, best);
        if (!(c.RevoltIgnitionIntensity > 0))
        {
            if (bestStrength <= hold + militia) { ClearFactionCrisis(w); return; }
            bool externalWar = false;
            if (w.Diplomacy is { } dipEx && w.SelfPower is { } spEx)
                foreach (var war in dipEx.Wars)
                    if (war.CivilWarParent is null && (war.A == spEx || war.B == spEx)) { externalWar = true; break; }
            bool weakness = w.Legitimacy < c.FactionStrikeLegitimacy
                         || w.GdpDrawdown.Value > c.FactionStrikeDrawdown.Value
                         || externalWar;
            if (!weakness && bestStrength < (hold + militia) * c.FactionOverwhelmFactor) { ClearFactionCrisis(w); return; }
        }
        CrisisThroughLawPath(w, c, best, coerce);
    }

    private static void TriggerFactionRevolt(World w, Coefficients c, FactionState best, double coerce)
    {
        w.FactionRevolts++;
        best.Rejections += 1;
        if (c.RevoltMilitiaRaise > 0)
        {
            foreach (var p in best.Region.Pops)
            {
                if (p.IsSoldier) continue;
                int pb = PolicyPreferences.BlocOf(p);
                if (pb != best.Bloc) continue;
                double share = Math.Min(1.0, c.RevoltMilitiaRaise * p.Militancy);
                if (!(share > p.MilitiaShare.Value)) continue;
                p.MilitiaShare = new Share(share);
            }
        }
        Polity target = NearestPolity(best.Ideo);
        if (Instruments.On) Instruments.PrdfRevolt(w, (int)target);
        if (RaiseRevolt(w, best.Region, target))
        {
            w.RevoltingFaction = best;
            w.LogAdd($"{w.Year} {w.SelfPower?.Name}で{Factions.DisplayName(best)}が蜂起し{best.Region.Name}に拠って立った（内戦）");
            return;
        }
        bool won = FactionStrength(w, c, best) > coerce + LoyalistMilitia(w, c, best);
        w.LogAdd(won
            ? $"{w.Year} {w.SelfPower?.Name}で{Factions.DisplayName(best)}が蜂起し政府が屈した"
            : $"{w.Year} {w.SelfPower?.Name}で{Factions.DisplayName(best)}の蜂起が鎮圧された");
        SettleRevolt(w, c, won, target, best, best.Region.Pops);
    }

    private static string DemandsJa(List<FactionSnapshot.Demand> d) =>
        d.Count > 0 ? string.Join("・", d.ConvertAll(x => FactionSnapshot.DemandJa(x.Axis, x.Increase))) : "体制への漠然とした不満";

    private static void ClearFactionCrisis(World w)
    {
        if (w.PendingFactionCrisis is { } f) { f.CrisisDemands = null; f.CrisisDemandPos = null; }
        w.PendingFactionCrisis = null; w.PendingFactionCrisisDemands = "";
    }

    private static void CrisisThroughLawPath(World w, Coefficients c, FactionState best, double coerce)
    {
        if (!ReferenceEquals(w.PendingFactionCrisis, best) || best.CrisisDemands is null)
        {
            if (w.PendingFactionCrisis is { } prev && !ReferenceEquals(prev, best)) { prev.CrisisDemands = null; prev.CrisisDemandPos = null; }
            w.PendingFactionCrisis = best;
            RecordCrisisDemands(w, best);
            w.LogAdd($"{w.Year} {w.SelfPower?.Name}で{Factions.DisplayName(best)}が要求を掲げた（{w.PendingFactionCrisisDemands}）");
            return;
        }
        MechanismProbe.Eval(MechanismProbe.Site.FactionConcede);
        MechanismProbe.Eval(MechanismProbe.Site.FactionCompensate);
        MechanismProbe.Eval(MechanismProbe.Site.FactionRevolt);
        int moved = CrisisDemandsMoved(w, best);
        if (moved == 1)
        {
            best.Rejections = 0;
            RecordCrisisDemands(w, best);
            MechanismProbe.Hit(MechanismProbe.Site.FactionConcede);
            w.LogAdd($"{w.Year} {w.SelfPower?.Name}が{Factions.DisplayName(best)}の要求を法で受け入れた");
            return;
        }
        if (moved == 2)
        {
            best.Rejections += 1;
            RecordCrisisDemands(w, best);
            MechanismProbe.Hit(MechanismProbe.Site.FactionCompensate);
            w.LogAdd($"{w.Year} {w.SelfPower?.Name}が{Factions.DisplayName(best)}へ別の軸で譲歩した（本丸の要求は通っていない）");
            return;
        }
        ClearFactionCrisis(w);
        MechanismProbe.Hit(MechanismProbe.Site.FactionRevolt);
        TriggerFactionRevolt(w, c, best, coerce);
    }

    private static void RecordCrisisDemands(World w, FactionState f)
    {
        var d = FactionSnapshot.ComputeDemands(w, f.Bloc, lawsOnly: true);
        f.CrisisDemands = new int[d.Count]; f.CrisisDemandPos = new double[d.Count];
        for (int i = 0; i < d.Count; i++)
        {
            f.CrisisDemands[i] = d[i].Increase ? (int)d[i].Axis + 1 : -((int)d[i].Axis + 1);
            f.CrisisDemandPos[i] = FactionSnapshot.AxisPosition(w, d[i].Axis);
        }
        w.PendingFactionCrisisDemands = DemandsJa(d);
    }

    private static int CrisisDemandsMoved(World w, FactionState f)
    {
        int moved = 0;
        for (int i = 0; i < f.CrisisDemands!.Length; i++)
        {
            int code = f.CrisisDemands[i]; bool inc = code > 0;
            var axis = (PolicyAxis)(Math.Abs(code) - 1);
            double now = FactionSnapshot.AxisPosition(w, axis), was = f.CrisisDemandPos![i];
            bool advanced = inc ? !(now <= was + 1e-9) : !(now >= was - 1e-9);
            if (!advanced) advanced = Government.EnactingStage(w, axis, inc);
            if (!advanced) continue;
            if (i == 0) return 1;
            moved = 2;
        }
        return moved;
    }

    public static double FactionStrength(World w, Coefficients c, FactionState f)
    {
        double voice = f.Bloc == 0 ? 1.0
                     : f.Bloc == 1 ? (w.Suffrage >= Suffrage.Census ? 1.0 : c.ModernEliteSubCensusVoice)
                     : (w.Suffrage >= Suffrage.AdultMale ? 1.0 : 0.0);
        double silenced = 1.0 - voice;
        if (!(silenced >= c.RevoltVoiceFloor)) silenced = c.RevoltVoiceFloor;
        double strength = f.Size * f.AvgMilitancy * f.Cohesion * silenced;
        if (c.RebelArmsMarketEnabled > 0) strength *= f.EquipFill;
        return strength;
    }

    internal static Polity NearestPolity(int ideo)
    {
        var oneHot = new double[Ideologies.Count]; oneHot[ideo] = 1.0;
        var coord = Ideologies.IdealPoint(oneHot);
        Polity best = Ideologies.Polities[0]; double bd = double.MaxValue;
        foreach (var pol in Ideologies.Polities)
        {
            var pc = Ideologies.BasePolityCoord(pol);
            double d = 0; for (int i = 0; i < 4; i++) { double x = coord[i] - pc[i]; d += x * x; }
            if (d < bd) { bd = d; best = pol; }
        }
        return best;
    }

    internal static double LoyalistMilitia(World w, Coefficients c, FactionState f)
    {
        if (c.LoyalistMilitiaWeight <= 0) return 0;
        var oneHot = new double[Ideologies.Count]; oneHot[f.Ideo] = 1.0;
        var fCoord = Ideologies.IdealPoint(oneHot);
        double militia = 0;
        var size = new double[2];
        var shares = new double[2][] { new double[Ideologies.Count], new double[Ideologies.Count] };
        foreach (var p in w.AllPops())
        {
            int pb = PolicyPreferences.BlocOf(p);
            if (pb >= 2 || pb == f.Bloc || p.IsSoldier) continue;
            size[pb] += p.Size;
            for (int i = 0; i < Ideologies.Count; i++) shares[pb][i] += p.IdeologyShares[i] * p.Size;
        }
        for (int b = 0; b < 2; b++)
        {
            if (b == f.Bloc || size[b] <= 1e-9) continue;
            for (int i = 0; i < Ideologies.Count; i++) shares[b][i] /= size[b];
            var ideal = Ideologies.IdealPoint(shares[b]);
            double d = 0; for (int i = 0; i < 4; i++) { double x = ideal[i] - fCoord[i]; d += x * x; }
            double dist = Math.Sqrt(d);
            militia += size[b] * Math.Max(0, dist - c.LoyalistDistanceFloor) * (f.Mutated ? 1.0 + c.LoyalistHeresyBoost : 1.0);
        }
        return c.LoyalistMilitiaWeight * militia;
    }

    internal static double LocalizedHold(World w, Coefficients c, Region region, double coerce)
    {
        if (!(c.FactionRevoltLocalCoerce > 0)) return coerce;
        double blend = PoliticalQuantities.Clamp01(c.FactionRevoltLocalCoerce);
        double share = region.SumPopulation() / Math.Max(1.0, w.TotalPopulation);
        if (!(share >= 0 && share <= 1)) share = 1.0;
        return coerce * ((1.0 - blend) + blend * share);
    }

    private static void ConcedeToFaction(World w, Coefficients c, FactionState f)
    {
        var demands = FactionSnapshot.ComputeDemands(w, f.Bloc);
        int steps = 0;
        for (int i = 0; i < demands.Count; i++)
            steps += ApplyDemand(w, c, demands[i].Axis, demands[i].Increase, i == 0 ? c.FactionConcessionSteps : 1);
        double spend = c.RevoltConcessionSpend * Math.Max(1, steps);
        if (Instruments.On) Instruments.ConcedeSpent(w, spend);
        w.MandateSpent += spend;
        w.MandateSpentNew += spend;
        foreach (var p in f.Region.Pops) if (p.IsCommoner) p.Militancy *= c.ReformMilitancyRelief;
        w.LogAdd($"{w.Year} {w.SelfPower?.Name}が{Factions.DisplayName(f)}の要求をのんだ");
    }

    internal static int ApplyDemand(World w, Coefficients c, PolicyAxis a, bool inc, int n)
    {
        var p = w.Policy;
        int StepEnumAxis(int cur, int max, Action<int> set)
        {
            int v = Math.Clamp(cur + (inc ? n : -n), 0, max);
            if (inc) while (v > cur && !Government.LawPrereq(w, a, v, c).Ok) v--;
            set(v); return Math.Abs(v - cur);
        }
        int Slide(double cur, Action<double> set, bool staged = false)
        {
            if (staged)
            {
                int s0 = Government.TaxStage(a, cur), s = Math.Clamp(s0 + (inc ? n : -n), 0, Government.TaxLawStages);
                if (inc) while (s > s0 && !Government.LawPrereq(w, a, s, c).Ok) s--;
                else while (s < s0 && !Government.LawPrereq(w, a, s, c).Ok) s++;
                if (s == s0) return 0;
                set(Government.TaxValue(a, s)); return 1;
            }
            double v = PoliticalQuantities.Clamp01(cur + (inc ? 1 : -1) * c.FactionConcessionSlider * n);
            set(v); return Math.Abs(v - cur) > 1e-9 ? 1 : 0;
        }
        int Flip(bool cur, Action<bool> set) { if (cur == inc) return 0; set(inc); return 1; }
        return a switch
        {
            PolicyAxis.Suffrage => StepEnumAxis((int)w.Suffrage, 3, v => { w.Suffrage = (Suffrage)v; p.Suffrage = w.Suffrage; }),
            PolicyAxis.EducationLaw => StepEnumAxis((int)w.EducationLaw, 3, v => { w.EducationLaw = (EducationLaw)v; p.EducationLaw = w.EducationLaw; }),
            PolicyAxis.Censorship => StepEnumAxis((int)w.Censorship, 2, v => { w.Censorship = (CensorshipLaw)v; p.Censorship = w.Censorship; }),
            PolicyAxis.LaborUnion => StepEnumAxis((int)w.LaborUnion, 2, v => { w.LaborUnion = (LaborUnionLaw)v; p.LaborUnion = w.LaborUnion; }),
            PolicyAxis.ShorterHours => StepEnumAxis((int)w.WorkHours, 3, v => { w.WorkHours = (WorkHours)v; p.WorkHours = w.WorkHours; }),
            PolicyAxis.CornLaws => StepEnumAxis((int)w.CornLaws, 2, v => { w.CornLaws = (ReformLevel)v; p.CornLaws = w.CornLaws; }),
            PolicyAxis.LandReform => StepEnumAxis((int)w.LandReform, 2, v => { w.LandReform = (ReformLevel)v; p.LandReform = w.LandReform; }),
            PolicyAxis.SerfEmancipation => StepEnumAxis((int)w.SerfEmancipation, 2, v => { w.SerfEmancipation = (ReformLevel)v; p.SerfEmancipation = w.SerfEmancipation; }),
            PolicyAxis.WomenRights => StepEnumAxis((int)w.WomenRights, 3, v => { w.WomenRights = (WomenRightsLaw)v; p.WomenRights = w.WomenRights; }),
            PolicyAxis.IncomeTax => Slide(w.TaxRate.Value, v => { w.TaxRate = new Share(v); p.IncomeTaxTarget = v; }, staged: true),
            PolicyAxis.SocialSecurity => Slide(w.SocialSecurity, v => { w.SocialSecurity = v; p.SocialSecurityTarget = v; }),
            PolicyAxis.Tariff => Slide(w.Tariff, v => { w.Tariff = v; p.TariffTarget = v; }),
            PolicyAxis.PollTax => Slide(w.PollTax, v => { w.PollTax = v; p.PollTaxTarget = v; }, staged: true),
            PolicyAxis.ProgressiveTax => Slide(w.ProgressiveTax, v => { w.ProgressiveTax = v; p.ProgressiveTaxTarget = v; }, staged: true),
            PolicyAxis.Coercion => Slide(PoliticalQuantities.Clamp01(w.BaseCoercion / Math.Max(1e-9, c.CoercionMax)), v => { w.BaseCoercion = v * c.CoercionMax; p.CoercionTarget = v * c.CoercionMax; }),
            PolicyAxis.EducationSpending => Slide(w.EducationSpending, v => { w.EducationSpending = v; p.EducationSpendingTarget = v; }, staged: true),
            PolicyAxis.LandTax => Slide(w.LandTax, v => { w.LandTax = v; p.LandTaxTarget = v; }, staged: true),
            PolicyAxis.ConscriptionLaw => StepEnumAxis((int)w.ConscriptionLaw, 2, v => { w.ConscriptionLaw = (ConscriptionLaw)v; p.ConscriptionLaw = w.ConscriptionLaw; }),
            PolicyAxis.SecretBallot => StepEnumAxis((int)w.SecretBallot, 1, v => { w.SecretBallot = (SecretBallotLaw)v; p.SecretBallot = w.SecretBallot; }),
            PolicyAxis.Bureaucracy => StepEnumAxis((int)w.Bureaucracy, 2, v => { w.Bureaucracy = (BureaucracyLaw)v; p.Bureaucracy = w.Bureaucracy; }),
            PolicyAxis.ChildLabor => Flip(w.ChildLaborAllowed, v => { w.ChildLaborAllowed = v; p.ChildLaborAllowed = v; }),
            PolicyAxis.StateReligion => StepEnumAxis((int)w.StateReligion, 2, v => { w.StateReligion = (StateReligionLaw)v; p.StateReligion = w.StateReligion; }),
            PolicyAxis.Antitrust => StepEnumAxis((int)w.Antitrust, 2, v => { w.Antitrust = (AntitrustLaw)v; p.Antitrust = w.Antitrust; }),
            PolicyAxis.Guild => w.Coef.GuildLawGate > 0 ? StepEnumAxis((int)w.Guild, 2, v => { w.Guild = (GuildLaw)v; p.Guild = w.Guild; }) : 0,
            PolicyAxis.MinimumWage => StepEnumAxis((int)w.MinimumWage, 3, v => { w.MinimumWage = (MinimumWageLaw)v; p.MinimumWage = w.MinimumWage; }),
            PolicyAxis.Inheritance => StepEnumAxis((int)w.Inheritance, 4, v => { w.Inheritance = (InheritanceLaw)v; p.Inheritance = w.Inheritance; }),
            PolicyAxis.WartimeRationing => Flip(w.WartimeRationing, v => { w.WartimeRationing = v; p.WartimeRationing = v; }),
            PolicyAxis.MilitaryExport => Flip(w.MilitaryExportBan, v => { w.MilitaryExportBan = v; p.MilitaryExportBan = v; }),
            PolicyAxis.CivilianArms => c.RebelArmsMarketEnabled > 0 ? Flip(w.CivilianArmsBan, v => { w.CivilianArmsBan = v; p.CivilianArmsBan = v; }) : 0,
            PolicyAxis.OpiumPossession => StepEnumAxis((int)w.OpiumPossession, 2, v => { w.OpiumPossession = (ReformLevel)v; p.OpiumPossession = w.OpiumPossession; }),
            PolicyAxis.OpiumExport => Flip(w.OpiumExportBan, v => { w.OpiumExportBan = v; p.OpiumExportBan = v; }),
            PolicyAxis.Nationalization => StepNationalization(w, p, inc),
            PolicyAxis.ImmigrationRestriction => StepEnumAxis((int)w.ImmigrationRestriction, 2, v => { w.ImmigrationRestriction = (ImmigrationLaw)v; p.ImmigrationRestriction = w.ImmigrationRestriction; }),
            PolicyAxis.SpecialistExitBan => StepEnumAxis((int)w.SpecialistExitLaw, 2, v => { w.SpecialistExitLaw = (SpecialistExitLaw)v; p.SpecialistExitLaw = w.SpecialistExitLaw; }),
            PolicyAxis.MonetaryStandard => StepEnumAxis((int)w.MonetaryLaw, 2, v => { w.MonetaryLaw = (MonetaryStandardLaw)v; p.MonetaryTarget = v; }),
            PolicyAxis.CapitalMobility => Flip(w.CapitalControlLaw, v => { w.CapitalControlLaw = v; p.CapitalControlTarget = v; }),
            PolicyAxis.ColonialSuffrage => StepEnumAxis((int)w.ColonialSuffrage, 2, v => { w.ColonialSuffrage = (ColonialSuffrageLaw)v; p.ColonialSuffrage = w.ColonialSuffrage; }),
            PolicyAxis.MigrantNativeSuffrage => StepEnumAxis((int)w.MigrantNativeSuffrage, 1, v => { w.MigrantNativeSuffrage = (MigrantNativeSuffrageLaw)v; p.MigrantNativeSuffrage = w.MigrantNativeSuffrage; }),
            PolicyAxis.NavyUpkeep => ConcedeNavyUpkeep(w, p, inc, c.FactionConcessionSlider * n),
            _ => 0,
        };
    }

    private static int ConcedeNavyUpkeep(World w, NationPolicy p, bool inc, double step)
    {
        double navy = PoliticalQuantities.Clamp01(w.NavyUpkeep.Value + (inc ? step : -step));
        if (Math.Abs(navy - w.NavyUpkeep.Value) <= 1e-9) return 0;
        w.SetMilitaryUpkeep(w.ArmyUpkeep, new Share(navy));
        p.NavyUpkeepTarget = navy;
        p.ArmyUpkeepTarget = Math.Min(p.ArmyUpkeepTarget, 1.0 - navy);
        return 1;
    }

    private static int StepNationalization(World w, NationPolicy p, bool inc)
    {
        var flags = new (bool Cur, System.Action<bool> Set)[]
        {
            (w.NationalizeRailways, v => { w.NationalizeRailways = v; p.NationalizeRailways = v; }),
            (w.NationalizeMines, v => { w.NationalizeMines = v; p.NationalizeMines = v; }),
            (w.NationalizeMilitary, v => { w.NationalizeMilitary = v; p.NationalizeMilitary = v; }),
            (w.NationalizeOil, v => { w.NationalizeOil = v; p.NationalizeOil = v; }),
            (w.NationalizeElectricity, v => { w.NationalizeElectricity = v; p.NationalizeElectricity = v; }),
        };
        if (inc)
        {
            foreach (var f in flags) if (!f.Cur) { f.Set(true); return 1; }
            if (!w.NationalizeAll && Government.LawPrereq(w, PolicyAxis.NationalizationAll, 1, w.Coef).Ok)
            { w.NationalizeAll = true; p.NationalizeAll = true; return 1; }
            return 0;
        }
        if (w.NationalizeAll) { w.NationalizeAll = false; p.NationalizeAll = false; return 1; }
        for (int i = flags.Length - 1; i >= 0; i--) if (flags[i].Cur) { flags[i].Set(false); return 1; }
        return 0;
    }

    private static void CheckRegionalRevolt(World w, Coefficients c, double coerce)
    {
        if (c.RegionalRevoltThreshold <= 0) return;
        var (worst, worstHeat, crowd) = SelectRevoltRegion(w, c);
        if (worst is null || worstHeat <= c.RegionalRevoltThreshold) return;
        double strength = worstHeat * crowd * c.RegionalRevoltStrength;
        coerce = LocalizedHold(w, c, worst, coerce);
        if (strength <= coerce)
        {
            double dead = Math.Min(crowd * c.RevoltSuppressDeadFrac.Value, crowd);
            if (dead > 1e-9 && crowd > 1e-9)
                dead = w.KillPops(worst.Pops.FindAll(p => p.IsCommoner), null, dead, violent: true);
            double legBeforeRevolt = w.Legitimacy;
            w.Legitimacy = Math.Max(0, w.Legitimacy - c.RevoltSuppressLegitCost);
            w.Explain?.Add("Legitimacy", "一揆鎮圧の流血(ストック正統性への傷)", w.Legitimacy - legBeforeRevolt);
            w.LogAdd($"{w.Year} {w.SelfPower?.Name}が{worst.Name}の一揆を鎮圧（死者{dead:F0}）");
        }
        else
        {
            ConcedeToRevolt(w, c, worst);
            w.LogAdd($"{w.Year} {w.SelfPower?.Name}が{worst.Name}の一揆に譲歩");
        }
    }

    private static void ConcedeToRevolt(World w, Coefficients c, Region worst)
    {
        PolicyAxis axis; bool inc;
        if (w.CornLaws > ReformLevel.None) { axis = PolicyAxis.CornLaws; inc = false; }
        else if (w.WorkHours > WorkHours.Eight) { axis = PolicyAxis.ShorterHours; inc = false; }
        else if (w.SocialSecurityLaw < SocialSecurityLaw.Pension) { axis = PolicyAxis.SocialSecurity; inc = true; }
        else if (w.SerfEmancipation < ReformLevel.Full) { axis = PolicyAxis.SerfEmancipation; inc = true; }
        else if (w.Suffrage < Suffrage.AdultMale) { axis = PolicyAxis.Suffrage; inc = true; }
        else return;
        w.PendingRevoltDemand = inc ? (int)axis + 1 : -((int)axis + 1);
        w.PendingRevoltDemandPos = FactionSnapshot.AxisPosition(w, axis);
        w.PendingRevoltRegion = worst.Id;
        Parliament.TargetDemand(w, axis, inc);
    }

    internal static void CheckSecession(World w, Coefficients c, double coerce)
    {
        if (w.Regions.Count < 2) return;
        double stance = w.Tariff * 2 - 1;
        double hold = w.Legitimacy / (1 + w.TotalPopulation * c.SecessionHoldPerPop);
        hold += c.SecessionHoldCoercion * c.SecessionThreshold * (coerce / (coerce + c.PoliticalReferencePop));
        var cap = w.Regions[0];
        Region? worst = null; double worstPressure = 0;
        double worstMargin = double.NegativeInfinity, worstEffHold = hold;
        int restive = 0;
        var margins = new List<(Region R, double Hold, double Pressure)>();
        foreach (var r in w.Regions)
        {
            if (r.Id == 0) continue;
            r.TariffInterest = RegionTariffInterest(r);
            double cultDist = ((r.Culture.Length > 0 && cap.Culture.Length > 0 && r.Culture != cap.Culture) ? 0.5 : 0)
                            + ((r.Religion.Length > 0 && cap.Religion.Length > 0 && r.Religion != cap.Religion) ? 0.5 : 0);
            if (c.CultureDistanceFromEthnic > 0)
                cultDist = PoliticalQuantities.Toward(cultDist, 1.0 - AvgRegionCoreShare(r), c.CultureDistanceFromEthnic);
            double mil = AvgRegionMilitancy(r);
            double pressure = Math.Abs(stance - r.TariffInterest) * mil * c.SecessionPressureScale * (1 + c.CultureSecessionWeight * cultDist);
            if (c.SecessionCultureAdditive > 0 && w.EthnicEnabled)
                pressure += c.SecessionCultureAdditive * c.MinorityMilitancy * cultDist * (1.0 - AvgRegionCoreShare(r)) * mil;
            if (c.SecessionConsciousnessWeight > 0)
            {
                double nc = AvgRegionConsciousness(r);
                pressure *= Math.Max(0, 1.0 - c.SecessionConsciousnessWeight * nc);
            }
            // (1173) 旗 SecessionInclusionOnce＞0：包摂は闘争度の目標（上の少数派の項）の 1 回だけ＝圧には闘争度を通して入る（設計思想⑨）。0＝旧の 2 回目の割引
            if (!(c.SecessionInclusionOnce > 0)) pressure *= 1.0 - PoliticalQuantities.Clamp01(RegionAvgSuffrageInclusion(w, c, r));
            double crowdSec = RegionCrowd(r);
            pressure *= crowdSec > 0 ? crowdSec / (crowdSec + c.ColonyPromotionMinPop) : 0.0;
            if (c.SecessionHoldProjection > 0)
            {
                double proj = 1.0;
                var rc = Places.Coord(r.Name);
                if (!(rc.Lat == 0 && rc.Lon == 0))
                {
                    double km = Geo.DistanceKm(w.Latitude, w.Longitude, rc.Lat, rc.Lon);
                    if (km > 0 && double.IsFinite(km)) proj = 1.0 / (1.0 + c.DistanceWarDecay * km / Diplomacy.DistanceNormKm);
                }
                double eff = hold * (1.0 + c.SecessionHoldProjection * (proj - 1.0));
                double margin = pressure - eff;
                if (worst is null || margin > worstMargin) { worstMargin = margin; worstPressure = pressure; worstEffHold = eff; worst = r; }
                margins.Add((r, eff, pressure));
            }
            else
            {
                if (pressure > worstPressure) { worstPressure = pressure; worst = r; }
                margins.Add((r, hold, pressure));
            }
            if (pressure > c.SecessionThreshold) restive++;
        }
        if (c.SecessionHoldSharedArmy > 0)
        {
            int civilFronts = 0;
            if (w.Diplomacy is { } dipCw && w.SelfPower is { } spCw)
                foreach (var war in dipCw.Wars) if (war.CivilWarParent == spCw) civilFronts++;
            if (civilFronts == 0 && w.RevoltingFaction is { } rfCw && w.Regions.Contains(rfCw.Region)) civilFronts = 1;
            restive += civilFronts;
            double sharedDiv = 1.0 + c.SecessionHoldSharedArmy * System.Math.Max(0, restive - 1);
            hold /= sharedDiv; worstEffHold /= sharedDiv;
        }
        double sharedDivDom = c.SecessionHoldSharedArmy > 0 ? 1.0 + c.SecessionHoldSharedArmy * System.Math.Max(0, restive - 1) : 1.0;
        foreach (var (r2, h2, p2) in margins)
        {
            double d = r2.Dominance + (h2 / sharedDivDom - p2) * c.DominanceSpeed;
            if (!(d > 0)) d = 0; else if (d > 1) d = 1;
            r2.Dominance = d;
        }
        if (Instruments.On) Instruments.SecObserve(w, c, coerce, stance, hold, sharedDivDom, restive, margins);
        {
            double order = PoliticalQuantities.Clamp01(w.PublicOrder);
            bool byRegion = c.RuleOrderByRegion > 0;
            double rawOrder = 0, mbar = 0;
            if (byRegion)
            {
                rawOrder = PoliticalQuantities.EffectiveCoerce(w, c) / Math.Max(1e-9, w.TotalPopulation * c.CrimeOrderPerPop);
                double milSum = 0, sizeSum = 0;
                foreach (var rm in w.Regions) { var (mm, ss) = RegionMilitancy(rm); milSum += mm; sizeSum += ss; }
                mbar = sizeSum > 1e-9 ? milSum / sizeSum : 0;
            }
            double intermediary = Privileges.IntermediaryDegree(w);
            foreach (var r4 in w.Regions)
            {
                if (r4.Rule.Length != 4) continue;
                double orderR = order;
                if (byRegion)
                {
                    double mr = AvgRegionMilitancy(r4);
                    orderR = !(mr > 0) ? 1.0 : PoliticalQuantities.Clamp01(rawOrder * mbar / mr);
                }
                double tUngov = (1.0 - orderR) * (1.0 - RegionReach(w, c, r4));
                double tLocal = System.Math.Min(1.0 - tUngov, (r4.RuleInit.Length == 4 ? r4.RuleInit[1] : 0) * intermediary);
                double tForeign = ForeignShareSum(r4);
                double tState = System.Math.Max(0, 1.0 - tLocal - tForeign - tUngov);
                double[] t = [tState, tLocal, tForeign, tUngov];
                double sum = 0;
                for (int k = 0; k < 4; k++) { double v = r4.Rule[k] + (t[k] - r4.Rule[k]) * c.DominanceSpeed; if (!(v > 0)) v = 0; r4.Rule[k] = v; sum += v; }
                if (sum > 1e-9) for (int k = 0; k < 4; k++) r4.Rule[k] /= sum; else r4.Rule[0] = 1.0;
                if (r4.RuleStateLastYear != w.Year && r4.RuleStateYears.Length > 0)
                {
                    r4.RuleStateYears[r4.RuleStateHead % r4.RuleStateYears.Length] = r4.Rule[0];
                    r4.RuleStateHead = (r4.RuleStateHead + 1) % r4.RuleStateYears.Length;
                    r4.RuleStateLastYear = w.Year;
                }
            }
        }
        w.SecessionHold = c.SecessionHoldProjection > 0 ? worstEffHold : hold;
        if (w.StepsDiplomacy || w.SelfPower is null) return;
        List<Region>? go = null;
        foreach (var (r3, _, _) in margins) if (!(r3.Dominance > 0)) (go ??= []).Add(r3);
        if (go is not null)
        {
            (w.PendingSecessionWars ??= []).AddRange(go);
            w.HadSecession = true;
            foreach (var r3 in go) w.LogAdd($"{w.Year} {r3.Name}が{w.SelfPower?.Name}からの独立を求めて立った（分離独立戦争）");
        }
    }

    private static double RegionTariffInterest(Region r)
    {
        double pull = 0, weight = 0;
        foreach (var good in r.Goods)
        {
            if (good.Id < 0 || good.Id >= r.Markets.Length || good.RefPrice <= 1e-9) continue;
            var m = r.Markets[good.Id];
            if (m.LastProduced <= 1e-9) continue;
            double competitiveness = PoliticalQuantities.ClampOr((m.Price - good.RefPrice) / good.RefPrice, -1.0, 1.0, 0);
            double wgt = m.LastProduced * good.RefPrice;
            pull += wgt * competitiveness; weight += wgt;
        }
        return weight > 1e-9 ? PoliticalQuantities.ClampOr(pull / weight, -1.0, 1.0, 0.0) : 0.0;
    }

    internal static double AvgRegionMilitancy(Region r)
    {
        var (mil, size) = RegionMilitancy(r);
        return size > 1e-9 ? mil / size : 0;
    }

    internal static (double Mil, double Size) RegionMilitancy(Region r)
    {
        double mil = 0, size = 0;
        foreach (var p in r.Pops) if (p.IsCommoner) { mil += p.Militancy * p.Size; size += p.Size; }
        return (mil, size);
    }

    internal static double RegionCrowd(Region r)
    {
        double crowd = 0; foreach (var p in r.Pops) if (p.IsCommoner) crowd += p.Size;
        return crowd;
    }

    internal static (Region? Region, double Heat, double Crowd) SelectRevoltRegion(World w, Coefficients c)
    {
        Region? worst = null; double worstHeat = 0, worstKey = 0, worstCrowd = 0;
        foreach (var r in w.Regions)
        {
            if (w.PendingRebelRegions is { } pend && pend.Contains(r)) continue;
            double heat = RegionRevoltHeat(r, c);
            double crowd = RegionCrowd(r);
            double key = heat * crowd;
            if (key > worstKey) { worstKey = key; worstHeat = heat; worstCrowd = crowd; worst = r; }
        }
        return (worst, worstHeat, worstCrowd);
    }

    internal static double RegionRevoltHeat(Region r, Coefficients c) => AvgRegionMilitancy(r);

    internal static double AvgRegionConsciousness(Region r, double none = 0)
    {
        double nc = 0, size = 0;
        foreach (var p in r.Pops) if (!p.IsSoldier && p.NationalConsciousness >= 0) { nc += p.NationalConsciousness * p.Size; size += p.Size; }
        return size > 1e-9 ? PoliticalQuantities.Clamp01(nc / size) : none;
    }

    /// <summary>州の包摂（政治的国民に算入される割合）。旗 SecessionInclusionOnce＞0 は人の版 <see cref="InterestGroups.SuffrageInclusion"/> の
    /// 非兵士の大きさの重みの平均（植民地格は植民地参政権を読む・大きさ 0 なら 1）。0＝旧の州の版（移民原住民参政権が付与なら 1・他は中核の割合）。</summary>
    internal static double RegionAvgSuffrageInclusion(World w, Coefficients c, Region r)
    {
        if (!(c.SecessionInclusionOnce > 0))
            return w.MigrantNativeSuffrage == MigrantNativeSuffrageLaw.Granted ? 1.0 : AvgRegionCoreShare(r);
        double incl = 0, size = 0;
        foreach (var p in r.Pops) if (!p.IsSoldier) { incl += InterestGroups.SuffrageInclusion(w, r, p) * p.Size; size += p.Size; }
        return size > 1e-9 ? incl / size : 1.0;
    }

    internal static double RegionReach(World w, Coefficients c, Region r)
    {
        var rc = Places.Coord(r.Name);
        if (rc.Lat == 0 && rc.Lon == 0) return 1.0;
        double km = Geo.DistanceKm(w.Latitude, w.Longitude, rc.Lat, rc.Lon);
        if (!(km > 0) || !double.IsFinite(km)) return 1.0;
        double v = 1.0 / (1.0 + c.DistanceWarDecay * km / Diplomacy.DistanceNormKm);
        return !(v > 0 && v <= 1.0) ? 1.0 : v;
    }

    internal static double StateReach(World w, Coefficients c)
    {
        double num = 0, den = 0;
        foreach (var r in w.Regions)
        {
            double pop = r.SumPopulation();
            if (!(pop > 0)) continue;
            num += pop * (RuleReach(r) * PoliticalQuantities.Clamp01(r.AdminFill)); den += pop;
        }
        return den > 0 ? num / den : 1.0;
    }

    internal static double ForeignPortShare(World w)
    {
        double num = 0, den = 0;
        foreach (var r in w.Regions)
        {
            if (!r.IsPort) continue;
            if (w.Coef.TreatyPortHomeOnly > 0 && r.Tier == RegionTier.Colonial) continue;   // (1138) 旗 1＝植民地格の港を本国の関税の母集合に入れない（旗 0 は短絡＝旧の行）
            double pop = r.SumPopulation();
            if (!(pop > 0)) continue;
            num += pop * ForeignShareSum(r); den += pop;
        }
        return den > 0 ? num / den : 0.0;
    }

    internal static bool TreatyPortInForce(World w, Coefficients c)
    {
        double line = 0.5 * c.ForeignRuleFromTreaty;
        if (!(line > 0)) return false;
        foreach (var r in w.Regions)
            if (r.IsPort && !(c.TreatyPortHomeOnly > 0 && r.Tier == RegionTier.Colonial) && ForeignShareSum(r) >= line) return true;   // (1138) 旗 1＝植民地格の港では立たない
        return false;
    }

    internal static double RuleReach(Region r)
    {
        if (r.Rule.Length != 4) return 1.0;
        double v = PoliticalQuantities.Clamp01(r.Rule[0]);
        return !(v >= 0 && v <= 1) ? 1.0 : v;
    }

    internal static double ForeignShareSum(Region r)
    {
        double v = 0; foreach (var x in r.ForeignShares) if (x > 0) v += x;
        return !(v >= 0) ? 0 : System.Math.Min(1.0, v);
    }
    internal static void AddForeignShare(Region r, string baseName, double amount)
    {
        if (!(amount > 0) || string.IsNullOrEmpty(baseName)) return;
        int k = System.Array.IndexOf(r.ForeignRulers, baseName);
        if (k < 0)
        {
            var names = new string[r.ForeignRulers.Length + 1]; var shares = new double[r.ForeignShares.Length + 1];
            System.Array.Copy(r.ForeignRulers, names, r.ForeignRulers.Length); System.Array.Copy(r.ForeignShares, shares, r.ForeignShares.Length);
            names[^1] = baseName; shares[^1] = 0; r.ForeignRulers = names; r.ForeignShares = shares; k = names.Length - 1;
        }
        r.ForeignShares[k] = System.Math.Min(1.0, r.ForeignShares[k] + amount);
    }

    internal static double RuleDelivery(Region r, Coefficients c)
    {
        if (r.Rule.Length != 4) return 1.0;
        double deliver = 1.0 - PoliticalQuantities.Clamp01(c.PrivilegeTaxDiscount.Value);
        double v = PoliticalQuantities.Clamp01(r.Rule[0]) * (deliver + (1.0 - deliver) * PoliticalQuantities.Clamp01(r.AdminFill)) + PoliticalQuantities.Clamp01(r.Rule[1]) * deliver;
        return !(v >= 0 && v <= 1) ? 1.0 : v;
    }

    internal static double AvgRegionCoreShare(Region r)
    {
        double core = 0, size = 0;
        foreach (var p in r.Pops) if (!p.IsSoldier) { core += p.CoreEthnicShare.Value * p.Size; size += p.Size; }
        return size > 1e-9 ? core / size : 1.0;
    }

    internal static double SoldierHoldShare(World w, Coefficients c, double[] curCoord, Polity attractor, out double sCur, out double sAtt, out double totalWeight)
    {
        var aggS = Ideologies.Soldiers(w, out totalWeight);
        var attCoord = Ideologies.BasePolityCoord(attractor);
        sCur = Ideologies.SupportAt(aggS, curCoord, c.IdeologyAffinitySlope);
        sAtt = Ideologies.SupportAt(aggS, attCoord, c.IdeologyAffinitySlope);
        return 1.0 - Ideologies.DefectionShare(aggS, curCoord, attCoord, c.IdeologyAffinitySlope);
    }

    private static void RevolutionAwakensNation(World w, Coefficients c)
    {
        if (c.RevolutionConsciousness <= 0) return;
        if (c.NationConsciousnessPerPop > 0)
        {
            double seed = w.InitialNationalConsciousness >= 0 ? w.InitialNationalConsciousness : c.NationBuildInitialDefault;
            foreach (var p in w.AllPops())
            {
                double v = p.NationalConsciousness >= 0 ? p.NationalConsciousness : seed;
                p.NationalConsciousness = PoliticalQuantities.Clamp01(v + c.RevolutionConsciousness * (1.0 - v));
            }
            return;
        }
        if (w.NationalConsciousness < 0)
            w.NationalConsciousness = w.InitialNationalConsciousness >= 0 ? w.InitialNationalConsciousness : c.NationBuildInitialDefault;
        w.NationalConsciousness = PoliticalQuantities.Clamp01(w.NationalConsciousness + c.RevolutionConsciousness * (1.0 - w.NationalConsciousness));
    }

    public static double RevolutionPressure(World w)
        => PoliticalQuantities.ChallengeRatio(w.ReformistPower + w.RadicalPower, PoliticalQuantities.EffectiveCoerce(w, w.Coef));

    public static double GiniIndex(World w)
    {
        var wpcList = new List<(double pc, double size)>();
        foreach (var gp in w.AllPops()) { if (gp.Size <= 1e-9) continue; wpcList.Add((Math.Max(0, gp.Savings.Value) / gp.Size, gp.Size)); }
        double gini = 0;
        if (wpcList.Count > 1)
        {
            wpcList.Sort((a, b) => a.pc.CompareTo(b.pc));
            double totSize = 0, totWealth = 0; foreach (var e in wpcList) { totSize += e.size; totWealth += e.pc * e.size; }
            if (totSize > 1e-9 && totWealth > 1e-9)
            {
                double cumX = 0, cumY = 0, area = 0;
                foreach (var e in wpcList)
                {
                    double prevX = cumX, prevY = cumY;
                    cumX += e.size / totSize; cumY += e.pc * e.size / totWealth;
                    area += (cumX - prevX) * (cumY + prevY);
                }
                gini = 1.0 - area;
            }
        }
        return PoliticalQuantities.Clamp01(gini);
    }

    internal static bool IsMassPop(Pop p) => p.IsCommoner && !p.IsModernElite;

    public static double AvgMassMilitancy(World w)
    {
        double mil = 0, size = 0;
        foreach (var p in w.AllPops())
            if (IsMassPop(p)) { mil += p.Militancy * p.Size; size += p.Size; }
        return !(size > 1e-9) ? 0 : mil / size;
    }

    public static double AvgMassDiscontent(World w)
    {
        var c = w.Coef;
        double[] coord = PoliticalQuantities.DriftedPolityCoord(w.Polity, w.Suffrage, c);
        double[] gap = c.DiscontentLawGap > 0 ? PolicyPreferences.LawGap(w) : [];
        double dis = 0, size = 0;
        foreach (var p in w.AllPops())
            if (IsMassPop(p)) { dis += Discontent(w, c, p, coord, gap.Length > 0 ? gap[PolicyPreferences.BlocOf(p)] : 0) * p.Size; size += p.Size; }
        return !(size > 1e-9) ? 0 : dis / size;
    }

    private static void ResolveRevolt(World w, Coefficients c, bool depTrad, bool depModern, bool depMass)
    {
        bool terror = c.RevolutionTerror > 0
                   && w.Polity is Polity.Republic or Polity.ProletarianDictatorship or Polity.Anarchy;
        double confiscated = 0; MoneyStock confiscatedBonds = default;
        List<Pop>? seizedPops = null; List<double>? seizedAmt = null;
        List<Pop>? purgedPops = null; double purgeTotal = 0;
        foreach (var p in w.AllPops())
        {
            bool deposed = (depTrad && p.Closed)
                        || (depModern && p.IsModernElite)
                        || (depMass && IsMassPop(p));
            if (deposed)
            {
                if (terror && !p.IsSoldier && p.Size > 0)
                {
                    (purgedPops ??= []).Add(p); purgeTotal += p.Size * c.RevolutionTerror;
                    double seized = Math.Max(0, p.Savings.Value) * c.RevolutionTerror;
                    if (seized > 0) { (seizedPops ??= []).Add(p); (seizedAmt ??= []).Add(seized); confiscated += seized; }
                    MoneyStock seizedBonds = p.BondHoldings * c.RevolutionTerror;
                    p.BondHoldings -= seizedBonds; confiscatedBonds += seizedBonds;
                }
            }
        }
        if (purgedPops is not null) w.TerrorPurgeCumulative += w.KillPops(purgedPops, null, purgeTotal, violent: true);
        if (confiscated > 0 || confiscatedBonds.Value > 0)
        {
            var masses = new List<Pop>(); double ms = 0;
            foreach (var p in w.AllPops()) if (IsMassPop(p)) { masses.Add(p); ms += p.Size; }
            if (!(ms > 1e-9)) { masses = new List<Pop>(w.AllPops()); ms = 0; foreach (var p in masses) ms += p.Size; }
            if (seizedPops is not null) World.Transfer((w, seizedPops), (w, masses, null), confiscated, TreasuryCat.OtherIn, fromWeights: seizedAmt);
            if (ms > 1e-9) foreach (var p in masses) p.BondHoldings += confiscatedBonds * (p.Size / ms);
        }
    }
}
