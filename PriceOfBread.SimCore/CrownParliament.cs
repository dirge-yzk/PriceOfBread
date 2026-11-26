namespace PriceOfBread.SimCore;

public static class CrownParliament
{
    private const int RegimeFamilyCount = 4;
    internal const double CrownRollbackLegitimacyMin = 0.7;
    internal const double CrownRollbackArmyAllegianceMin = 0.3;

    public static void Step(World w, Coefficients c)
    {
        double target = ParliamentControlTarget(w.Polity);
        if (w.ParliamentControl < 0)
            w.ParliamentControl = target;
        if (c.SummonParliamentRate > 0)
        {
            if (c.SummonByInterestShare > 0)
            {
                double rev = w.LastRevenuePerMonth.Value;
                double x = !(rev > 0) ? 0 : w.LastInterestPerMonth.Value / rev;
                double s = PoliticalQuantities.Clamp01(x / c.MaxInterestShareOfRevenue.Value);
                double t = PowerShares.DebtGrip(w);
                w.ParliamentControl = w.ParliamentControl + c.SummonParliamentRate * s * System.Math.Max(0, t - w.ParliamentControl);
            }
            else if (w.AusterityIntensity > 0.5)
                w.ParliamentControl = PoliticalQuantities.Clamp01(w.ParliamentControl + c.SummonParliamentRate * (w.AusterityIntensity - 0.5) * 2.0);
        }
        if (c.SummonParliamentRate > 0 && w.Crown != CrownForm.None && w.CoopParliamentCrown < -0.5 && w.Legitimacy > CrownRollbackLegitimacyMin && w.ArmyAllegiance > CrownRollbackArmyAllegianceMin)
            w.ParliamentControl = System.Math.Max(0, w.ParliamentControl - c.SummonParliamentRate);

        bool hasLegislature = PolicyPreferences.HasLegislature(w.Polity);
        if (hasLegislature && w.ParliamentEnabled)
        {
            double classAffinity = w.Crown != CrownForm.None ? w.PartySeats[0] - w.PartySeats[2] : 0.0;
            double policyDist = PolicyDistance(w);
            double memory = System.Math.Min(1.0, w.MandateSpent);
            w.CoopParliamentCrown = System.Math.Clamp(
                c.CoopClassAffinityWeight * classAffinity - c.CoopPolicyGapWeight * policyDist - c.CoopConflictMemoryWeight * memory, -1, 1);
        }
        else w.CoopParliamentCrown = 0;

        double lean = SoldierCrownLean(w);
        double milAff = c.ArmyMilitarismAffinity * PoliticalQuantities.OfficerAppointmentCrownward(w);

        w.CoopPublicRegime = System.Math.Clamp((w.MeanSatisfactionPol - c.CoopPublicSatPivot) / (1.0 - c.CoopPublicSatPivot) - Politics.AvgMassMilitancy(w), -1, 1);

        if (w.Seizure == SeizureForm.None)
        {
            var agg = Ideologies.National(w);
            double S(Polity p) => Ideologies.SupportAt(agg, Ideologies.BasePolityCoord(p), c.IdeologyAffinitySlope);
            double[] curCoord = PoliticalQuantities.DriftedPolityCoord(w.Polity, w.Suffrage, c);
            double cur = Ideologies.SupportAt(agg, curCoord, c.IdeologyAffinitySlope);
            double rep = System.Math.Max(S(Polity.ConstitutionalMonarchy), S(Polity.Republic));
            double inertia = c.PolityInertia * w.Legitimacy;
            if (c.ParliamentPressureRate > 0 && rep - cur > inertia
                && w.ReformistPower + w.RadicalPower > PoliticalQuantities.EffectiveCoerce(w, c))
                w.ParliamentControl = PoliticalQuantities.Clamp01(w.ParliamentControl + c.ParliamentPressureRate * (rep - cur));
            if (c.ParliamentPressureRate > 0 && w.Crown != CrownForm.None)
            {
                double crown = System.Math.Max(S(Polity.Absolutist), System.Math.Max(S(Polity.Despotic), S(Polity.Shogunate)));
                if (crown > rep)
                    w.ParliamentControl = System.Math.Max(0, w.ParliamentControl - c.ParliamentPressureRate * (crown - rep) * w.Legitimacy);
            }
            if (c.ArmyPcPressure > 0 && lean > 0)
                w.ParliamentControl = System.Math.Max(0, w.ParliamentControl - c.ArmyPcPressure * lean * PolicyPreferences.ArmyPower(w));
            if (w.Crown != CrownForm.None && w.Seizure == SeizureForm.None
                && w.ParliamentControl >= c.ConstitutionalControlFloor
                && S(Polity.Republic) - S(Polity.ConstitutionalMonarchy) > c.PolityInertia * w.Legitimacy)
            {
                w.Crown = CrownForm.None;
                Instruments.CrownSourceIfOn(w, 4);
                w.LogAdd($"{w.Year} {w.SelfPower?.Name}の議会が王冠を廃した");
            }
            if (w.Seizure == SeizureForm.None)
            {
                if (w.Crown == CrownForm.Shogunate)
                {
                    double tension = w.Diplomacy is { } sdip && w.SelfPower is { } ssp ? sdip.MaxTension(ssp) : 0;
                    double mil = Politics.AvgMassMilitancy(w);
                    bool external = tension > c.ShogunateEndTension;
                    bool fromBelow = mil > c.ShogunateEndMilitancy;
                    if (external || fromBelow)
                    {
                        w.Crown = CrownForm.Absolutist;
                        Instruments.CrownSourceIfOn(w, 5);
                        if (fromBelow && mil >= tension / Diplomacy.TensionMax)
                        {
                            w.ParliamentControl = System.Math.Max(w.ParliamentControl, c.ConstitutionalControlFloor);
                            w.LogAdd($"{w.Year} {w.SelfPower?.Name}の幕府が倒れ議会が開かれた");
                        }
                        else w.LogAdd($"{w.Year} {w.SelfPower?.Name}の幕府が倒れ親政に復した");
                    }
                }
            }
        }

        {
            var derived = LabelFromAxes(w, c);
            if (derived != w.Polity)
            {
                w.RegimeTenureQuarters = 0;
                w.Polity = derived;
                Politics.RaiseFoundingInstitutions(w, allowRevoke: true, c);
                w.LogAdd($"{w.Year} {w.SelfPower?.Name}の統治が変わった（{PlayText.PolityRegimeJa(derived)}）");
            }
        }

        w.ArmyAllegiance = System.Math.Clamp(lean + milAff, -1, 1);

        w.RegimeTenureQuarters++;
        for (int i = 0; i < RegimeFamilyCount; i++) w.RegimeTrackRecord[i] *= 1.0 - c.RegimeRecordDecay;
        w.RecentRegimeChanges *= 1.0 - w.ReplacementRate(Calendar.MonthsPerQuarter);
        double record = w.CoopPublicRegime;
        if (record > 0)
            record *= PoliticalQuantities.Clamp01(w.RegimeTenureQuarters / (Calendar.QuartersPerYear * System.Math.Max(1.0, c.RegimeRecordSuccessTenureGate)));
        int fam = RegimeFamily(w.Polity);
        w.RegimeTrackRecord[fam] = PoliticalQuantities.ClampOr(w.RegimeTrackRecord[fam] + c.RegimeRecordAccumRate * record, -1, 1, 0);
    }

    internal static int RegimeFamily(Polity p) => p switch
    {
        Polity.Absolutist or Polity.Despotic or Polity.Shogunate => 0,
        Polity.ConstitutionalMonarchy or Polity.Republic => 1,
        Polity.MilitaryDictatorship or Polity.BourgeoisDictatorship => 2,
        _ => 3,
    };

    public static void ApplyRegime(World w, Coefficients c, Polity target)
    {
        switch (target)
        {
            case Polity.Republic: w.Crown = CrownForm.None; w.Seizure = SeizureForm.None; break;
            case Polity.ProletarianDictatorship: w.Crown = CrownForm.None; w.Seizure = SeizureForm.Proletarian; break;
            case Polity.ConstitutionalMonarchy: if (w.Crown == CrownForm.None) w.Crown = CrownForm.Absolutist; w.Seizure = SeizureForm.None; break;
            case Polity.Absolutist: w.Crown = CrownForm.Absolutist; w.Seizure = SeizureForm.None; break;
            case Polity.Despotic: w.Crown = CrownForm.Despotic; w.Seizure = SeizureForm.None; break;
            case Polity.Shogunate: w.Crown = CrownForm.Shogunate; w.Seizure = SeizureForm.None; break;
            case Polity.BourgeoisDictatorship: w.Seizure = SeizureForm.Bourgeois; break;
            case Polity.MilitaryDictatorship: w.Seizure = SeizureForm.Military; break;
            case Polity.Anarchy: w.Seizure = SeizureForm.Anarchy; break;
        }
        if (w.Polity != target)
        {
            w.RegimeTenureQuarters = 0;
            w.RecentRegimeChanges += 1;
        }
        w.Polity = target;
        w.ParliamentControl = ParliamentControlTarget(target);
    }

    public static Polity LabelFromAxes(World w, Coefficients c)
    {
        switch (w.Seizure)
        {
            case SeizureForm.Anarchy: return Polity.Anarchy;
            case SeizureForm.Military: return Polity.MilitaryDictatorship;
            case SeizureForm.Proletarian: return Polity.ProletarianDictatorship;
            case SeizureForm.Bourgeois: return Polity.BourgeoisDictatorship;
        }
        double floor = PolicyPreferences.HasLegislature(w.Polity)
            ? c.ConstitutionalControlExit : c.ConstitutionalControlFloor;
        if (w.ParliamentControl >= floor)
            return w.Crown == CrownForm.None ? Polity.Republic : Polity.ConstitutionalMonarchy;
        if (w.Crown == CrownForm.None) return Polity.Republic;
        return w.Crown switch
        {
            CrownForm.Absolutist => Polity.Absolutist,
            CrownForm.Despotic => Polity.Despotic,
            CrownForm.Shogunate => Polity.Shogunate,
            _ => Polity.BourgeoisDictatorship,
        };
    }

    private static double ParliamentControlTarget(Polity p) => p switch
    {
        Polity.Republic => 1.0,
        Polity.ConstitutionalMonarchy => 0.65,
        Polity.BourgeoisDictatorship => 0.30,
        Polity.ProletarianDictatorship => 0.20,
        Polity.MilitaryDictatorship => 0.10,
        _ => 0.0,
    };

    internal static double SoldierCrownLean(World w)
    {
        double sum = 0, size = 0;
        foreach (var p in w.AllPops())
        {
            if (!p.IsSoldier) continue;
            var s = p.IdeologyShares;
            double crown = 0, anti = 0;
            for (int i = 0; i < Ideologies.Count; i++)
                if (PoliticalQuantities.FamilyOf(i) == 0) crown += s[i]; else anti += s[i];
            sum += (crown - anti) * p.Size;
            size += p.Size;
        }
        return size > 1e-9 ? sum / size : 0.0;
    }

    private static double PolicyDistance(World w)
    {
        var p = w.Policy;
        double sum = 0; int n = 0;
        void Enum2<T>(T cur, T tgt) where T : struct, System.Enum
        {
            int range = System.Enum.GetValues<T>().Length - 1;
            if (range <= 0) return;
            sum += System.Math.Abs(System.Convert.ToInt32(tgt) - System.Convert.ToInt32(cur)) / (double)range; n++;
        }
        void Slider(double cur, double tgt) { sum += PoliticalQuantities.Clamp01(System.Math.Abs(tgt - cur)); n++; }

        Enum2(w.Suffrage, p.Suffrage);
        Enum2(w.EducationLaw, p.EducationLaw);
        Enum2(w.Censorship, p.Censorship);
        Enum2(w.LaborUnion, p.LaborUnion);
        Enum2(w.WorkHours, p.WorkHours);
        Enum2(w.CornLaws, p.CornLaws);
        Enum2(w.LandReform, p.LandReform);
        Enum2(w.SerfEmancipation, p.SerfEmancipation);
        Enum2(w.WomenRights, p.WomenRights);
        Slider(w.Tariff, p.TariffTarget);
        Slider(w.SocialSecurity, p.SocialSecurityTarget);
        Slider(w.ProgressiveTax, p.ProgressiveTaxTarget);
        Slider(w.TaxRate.Value, p.IncomeTaxTarget);
        return n > 0 ? sum / n : 0.0;
    }
}
