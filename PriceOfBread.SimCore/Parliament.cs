namespace PriceOfBread.SimCore;

public sealed class PoliticalParty
{
    public string Name = "";
    public int Family;
    public int Ideo = -1;
    public double VoteShare;
    public bool Permanent;
    public int AbsentQuarters;
}

public static class Parliament
{
    public enum Party { Conservative, Liberal, Socialist }

    private const int PartyAbsenceDissolveQuarters = 40;

    public static void Step(World w, Coefficients c)
    {
        bool ins = Instruments.On;   // (1118) 計器の門は 1 回だけ読む（OnDoubleReadScanTests）
        if (ins) Instruments.ParlHead(w);
        if (!w.ParliamentEnabled || !w.PoliticsEnabled || w.InCivilWar) return;

        if (w.Parties.Count == 0)
        {
            w.Parties.Add(new PoliticalParty { Name = "保守党", Family = 0, Permanent = true });
            w.Parties.Add(new PoliticalParty { Name = "自由党", Family = 1, Permanent = true });
            w.Parties.Add(new PoliticalParty { Name = "社会党", Family = 2, Permanent = true });
        }
        SyncDynamicParties(w, c);
        StepManifesto(w, c);

        // (1118) 旗が正：席が空の選挙政体で思想がまだ無い（開幕の四半期 0）なら、議会の前に思想を 1 回出す（得票が保守 100% へ退避しない）
        bool gapless = c.ParliamentSeatsWithoutGap > 0;
        if (gapless && PolicyPreferences.HasLegislature(w.Polity)
            && w.PartySeats[0] + w.PartySeats[1] + w.PartySeats[2] < 1e-9 && !HasIdeology(w))
            Ideologies.UpdateShares(w);
        if (ins) Instruments.ParlPreVotes(w);
        ComputeVotes(w, c);
        bool seatsEmpty = w.PartySeats[0] + w.PartySeats[1] + w.PartySeats[2] < 1e-9;
        if (seatsEmpty && (gapless ? HasIdeology(w) : w.QuartersSinceElection >= 1)
            && PolicyPreferences.HasLegislature(w.Polity))
        {
            SnapshotSeats(w);
            w.GoverningParty = (int)Winner(w);
        }
        w.QuartersSinceElection++;

        bool hasElections = PolicyPreferences.HasLegislature(w.Polity);
        if (hasElections)
        {
            w.QuartersWithElections++;
            bool noConfidence = w.QuartersSinceElection >= (int)c.EarlyElectionMinQuarters
                && w.GoverningParty is >= 0 and < 3 && w.VoteShares[w.GoverningParty] < c.NoConfidenceThreshold;
            if (w.PendingElection is null
                && !noConfidence && w.QuartersSinceElection == c.ElectionIntervalQuarters - 1)
            {
                w.PendingElection = BuildElectionEvent(w, c);
                w.ElectionCalls++;
                if (w.SelfPower?.IsPlayer != true) UtilityAgent.DecideElection(w);
            }
            if (w.QuartersSinceElection >= c.ElectionIntervalQuarters || noConfidence)
            {
                if (noConfidence && w.QuartersSinceElection < c.ElectionIntervalQuarters)
                    w.LogAdd($"{w.Year} {w.SelfPower?.Name}で内閣不信任＝繰り上げ総選挙");
                w.GoverningParty = (int)Winner(w);
                w.QuartersSinceElection = 0;
                w.ElectionsHeld++;
                SnapshotSeats(w);
                w.ElectionPushIdeology = -1; w.ElectionPushStrength = 0; w.PendingElection = null;
            }
        }
        else
        {
            w.GoverningParty = (int)RulingParty(w.Polity);
            w.QuartersSinceElection = 0;
            w.ElectionPushIdeology = -1; w.ElectionPushStrength = 0; w.PendingElection = null;
            if (c.NonElectiveSeatsEmpty > 0) { w.PartySeats[0] = 0; w.PartySeats[1] = 0; w.PartySeats[2] = 0; }
            else SnapshotSeats(w);
        }

        w.GoverningPartyName = GoverningPartyOf(w)?.Name ?? "";
    }

    public static void Dissolve(World w)
    {
        if (PolicyPreferences.HasLegislature(w.Polity) && w.ParliamentEnabled)
            w.QuartersSinceElection = w.Coef.ElectionIntervalQuarters;
    }

    /// <summary>(1118) 思想が有る＝IdeologyShares の和が 0 でない POP が 1 つでも居る（ComputeVotes が保守へ退避する量と同じ・Politics.Step の UpdateShares を 1 回でも通った国は真）。</summary>
    private static bool HasIdeology(World w)
    {
        foreach (var p in w.AllPops())
        {
            double s = 0;
            foreach (var x in p.IdeologyShares) s += x;
            if (!(s < 1e-9)) return true;
        }
        return false;
    }

    private static void SnapshotSeats(World w)
    {
        w.PartySeats[0] = w.VoteShares[0];
        w.PartySeats[1] = w.VoteShares[1];
        w.PartySeats[2] = w.VoteShares[2];
    }

    public static List<(string Class, double Voters, double Cons, double Lib, double Soc)> SupportBase(World w)
    {
        var by = new Dictionary<string, (double v, double c, double l, double s)>();
        var eff = w.Suffrage;
        foreach (var (rg, p) in w.AllPopsWithRegion())
        {
            double voters = p.Size * InterestGroups.SeatVoice(w, rg, p, eff);
            if (voters <= 1e-9) continue;
            double pc = 0, pl = 0, ps = 0;
            for (int i = 0; i < Ideologies.Count; i++)
            {
                int fam = PoliticalQuantities.FamilyOf(i);
                if (fam == 0) pc += p.IdeologyShares[i]; else if (fam == 1) pl += p.IdeologyShares[i]; else ps += p.IdeologyShares[i];
            }
            double sum = pc + pl + ps; if (sum < 1e-9) { pc = 1; sum = 1; }
            by.TryGetValue(p.ClassName, out var acc);
            by[p.ClassName] = (acc.v + voters, acc.c + voters * pc / sum, acc.l + voters * pl / sum, acc.s + voters * ps / sum);
        }
        var list = new List<(string, double, double, double, double)>();
        foreach (var kv in by) list.Add((kv.Key, kv.Value.v, kv.Value.c, kv.Value.l, kv.Value.s));
        list.Sort((a, b) => b.Item2 != a.Item2 ? b.Item2.CompareTo(a.Item2) : string.CompareOrdinal(a.Item1, b.Item1));
        return list;
    }

    private static void ComputeVotes(World w, Coefficients c)
    {
        var eff = w.Suffrage;
        int push = w.ElectionPushIdeology;
        double pushMul = 1.0 + w.ElectionPushStrength * c.ElectionPushScale;
        double cons = 0, lib = 0, soc = 0;
        bool dynamic = w.Parties.Count > 3;
        var ideoRaw = new double[Ideologies.Count];
        foreach (var (rg, p) in w.AllPopsWithRegion())
        {
            double voters = p.Size * InterestGroups.SeatVoice(w, rg, p, eff);
            if (voters <= 1e-9) continue;
            var s = p.IdeologyShares;
            double pc = 0, pl = 0, ps = 0;
            for (int i = 0; i < Ideologies.Count; i++)
            {
                double sv = i == push ? s[i] * pushMul : s[i];
                int fam = PoliticalQuantities.FamilyOf(i);
                if (fam == 0) pc += sv; else if (fam == 1) pl += sv; else ps += sv;
            }
            double sum = pc + pl + ps;
            if (sum < 1e-9) { pc = 1; sum = 1; }
            cons += voters * pc / sum; lib += voters * pl / sum; soc += voters * ps / sum;
            if (dynamic)
            {
                double sumI = s[0] + s[1] + s[4] + s[8] + s[2] + s[3] + s[5] + s[6] + s[7] + s[9] + s[10];
                if (push >= 0) sumI += s[push] * (pushMul - 1.0);
                if (!(sumI < 1e-9))
                    for (int i = 0; i < Ideologies.Count; i++) ideoRaw[i] += voters * (i == push ? s[i] * pushMul : s[i]) / sumI;
            }
        }
        double t = cons + lib + soc; if (t < 1e-9) t = 1;
        if (Instruments.On) w.DiagSfVoteWeightUsed = cons + lib + soc;
        double v0 = cons / t, v1 = lib / t, v2 = soc / t;
        if (c.ElectoralDistortion > 0)
        {
            double e = 1.0 + c.ElectoralDistortion;
            double d0 = System.Math.Pow(v0, e), d1 = System.Math.Pow(v1, e), d2 = System.Math.Pow(v2, e);
            double dt = d0 + d1 + d2; if (dt < 1e-9) dt = 1;
            v0 = d0 / dt; v1 = d1 / dt; v2 = d2 / dt;
        }
        w.VoteShares[0] = v0; w.VoteShares[1] = v1; w.VoteShares[2] = v2;

        if (dynamic)
        {
            double[] famRaw = [cons, lib, soc]; double[] famFinal = [v0, v1, v2];
            double[] dynSum = new double[3];
            foreach (var pt in w.Parties)
            {
                if (pt.Permanent) continue;
                pt.VoteShare = famRaw[pt.Family] > 1e-9 ? ideoRaw[pt.Ideo] / famRaw[pt.Family] * famFinal[pt.Family] : 0;
                dynSum[pt.Family] += pt.VoteShare;
            }
            foreach (var pt in w.Parties)
                if (pt.Permanent) pt.VoteShare = System.Math.Max(0, famFinal[pt.Family] - dynSum[pt.Family]);
        }
        else foreach (var pt in w.Parties) if (pt.Permanent) pt.VoteShare = pt.Family == 0 ? v0 : pt.Family == 1 ? v1 : v2;
    }

    private static void SyncDynamicParties(World w, Coefficients c)
    {
        for (int i = w.Parties.Count - 1; i >= 0; i--)
        {
            var pt = w.Parties[i];
            if (pt.Permanent) continue;
            if (AnyQualifying(w, c, pt.Ideo)) pt.AbsentQuarters = 0;
            else if (++pt.AbsentQuarters >= PartyAbsenceDissolveQuarters) w.Parties.RemoveAt(i);
        }
        if (w.ParliamentControl <= 0) return;
        foreach (var f in w.Factions)
        {
            if (!Qualifies(w, c, f)) continue;
            bool exists = false;
            foreach (var pt in w.Parties)
                if (!pt.Permanent && pt.Ideo == f.Ideo) { exists = true; break; }
            if (exists) continue;
            var np = new PoliticalParty
            {
                Name = $"{FactionSnapshot.IdeologyJa((Ideology)f.Ideo)}党",
                Family = PoliticalQuantities.FamilyOf(f.Ideo), Ideo = f.Ideo,
            };
            w.Parties.Add(np);
            w.LogAdd($"{w.Year} {w.SelfPower?.Name}で{np.Name}が結党した");
        }
    }

    private static bool Qualifies(World w, Coefficients c, FactionState f)
    {
        if (f.Cohesion < c.PartyFormCohesion) return false;
        double voice = f.Bloc == 0 ? 1.0 : InterestGroups.SuffrageGate(w.Suffrage, f.Bloc == 1, c);
        return voice > 0;
    }

    private static bool AnyQualifying(World w, Coefficients c, int ideo)
    {
        foreach (var f in w.Factions) if (f.Ideo == ideo && Qualifies(w, c, f)) return true;
        return false;
    }

    public static PoliticalParty? GoverningPartyOf(World w)
    {
        PoliticalParty? best = null;
        foreach (var pt in w.Parties)
            if (pt.Family == w.GoverningParty && (best is null || pt.VoteShare > best.VoteShare)) best = pt;
        return best;
    }

    internal static void TargetDemand(World w, PolicyAxis a, bool inc)
    {
        var p = w.Policy;
        int n = inc ? 1 : -1;
        int StepTo(int cur, int max)
        {
            int next = System.Math.Clamp(cur + n, 0, max);
            return next != cur && !Government.GateInfo(w, a, inc, next).Ok ? cur : next;
        }
        bool FlipTo(bool cur) => cur != inc && !Government.GateInfo(w, a, inc, inc ? 1 : 0).Ok ? cur : inc;
        double Slide(double cur, bool staged = false)
            => staged
                ? Government.TaxValue(a, StepTo(Government.TaxStage(a, cur), Government.TaxLawStages))
                : PoliticalQuantities.Clamp01(cur + n * w.Coef.FactionConcessionSlider);
        switch (a)
        {
            case PolicyAxis.Suffrage: p.Suffrage = (Suffrage)StepTo((int)w.Suffrage, 3); break;
            case PolicyAxis.EducationLaw: p.EducationLaw = (EducationLaw)StepTo((int)w.EducationLaw, 3); break;
            case PolicyAxis.Censorship: p.Censorship = (CensorshipLaw)StepTo((int)w.Censorship, 2); break;
            case PolicyAxis.LaborUnion: p.LaborUnion = (LaborUnionLaw)StepTo((int)w.LaborUnion, 2); break;
            case PolicyAxis.ShorterHours: p.WorkHours = (WorkHours)StepTo((int)w.WorkHours, 3); break;
            case PolicyAxis.CornLaws: p.CornLaws = (ReformLevel)StepTo((int)w.CornLaws, 2); break;
            case PolicyAxis.LandReform: p.LandReform = (ReformLevel)StepTo((int)w.LandReform, 2); break;
            case PolicyAxis.SerfEmancipation: p.SerfEmancipation = (ReformLevel)StepTo((int)w.SerfEmancipation, 2); break;
            case PolicyAxis.WomenRights: p.WomenRights = (WomenRightsLaw)StepTo((int)w.WomenRights, 3); break;
            case PolicyAxis.IncomeTax: p.IncomeTaxTarget = Slide(w.TaxRate.Value, staged: true); break;
            case PolicyAxis.SocialSecurity: p.SocialSecurityTarget = StepTo((int)w.SocialSecurityLaw, Government.SocialSecurityMaxLevel) / (double)Government.SocialSecurityMaxLevel; break;
            case PolicyAxis.Tariff: p.TariffTarget = Slide(w.Tariff); break;
            case PolicyAxis.PollTax: p.PollTaxTarget = Slide(w.PollTax, staged: true); break;
            case PolicyAxis.ProgressiveTax: p.ProgressiveTaxTarget = Slide(w.ProgressiveTax, staged: true); break;
            case PolicyAxis.EducationSpending: p.EducationSpendingTarget = Slide(w.EducationSpending, staged: true); break;
            case PolicyAxis.LandTax: p.LandTaxTarget = Slide(w.LandTax, staged: true); break;
            case PolicyAxis.Coercion:
                p.MilitarySpendingTarget = System.Math.Max(1, StepTo(Government.MilitaryStage(w.MilitarySpending.Value), Government.MilitaryLawStages))
                                         / (double)Government.MilitaryLawStages;
                break;
            case PolicyAxis.ConscriptionLaw: p.ConscriptionLaw = (ConscriptionLaw)StepTo((int)w.ConscriptionLaw, 2); break;
            case PolicyAxis.ImmigrationRestriction: p.ImmigrationRestriction = (ImmigrationLaw)StepTo((int)w.ImmigrationRestriction, 2); break;
            case PolicyAxis.SpecialistExitBan: p.SpecialistExitLaw = (SpecialistExitLaw)StepTo((int)w.SpecialistExitLaw, 2); break;
            case PolicyAxis.MonetaryStandard: p.MonetaryTarget = StepTo((int)w.MonetaryLaw, 2); break;
            case PolicyAxis.CapitalMobility: p.CapitalControlTarget = FlipTo(w.CapitalControlLaw); break;
            case PolicyAxis.ColonialSuffrage: p.ColonialSuffrage = (ColonialSuffrageLaw)StepTo((int)w.ColonialSuffrage, 2); break;
            case PolicyAxis.MigrantNativeSuffrage: p.MigrantNativeSuffrage = (MigrantNativeSuffrageLaw)StepTo((int)w.MigrantNativeSuffrage, 1); break;
            case PolicyAxis.NavyUpkeep: p.NavyUpkeepTarget = Slide(w.NavyUpkeep.Value); break;
            case PolicyAxis.SecretBallot: p.SecretBallot = (SecretBallotLaw)StepTo((int)w.SecretBallot, 1); break;
            case PolicyAxis.Bureaucracy: p.Bureaucracy = (BureaucracyLaw)StepTo((int)w.Bureaucracy, 2); break;
            case PolicyAxis.ChildLabor: p.ChildLaborAllowed = FlipTo(w.ChildLaborAllowed); break;
            case PolicyAxis.StateReligion: p.StateReligion = (StateReligionLaw)System.Math.Min(StepTo((int)w.StateReligion, 2), 2); break;
            case PolicyAxis.Antitrust: p.Antitrust = (AntitrustLaw)StepTo((int)w.Antitrust, 2); break;
            case PolicyAxis.Guild: if (w.Coef.GuildLawGate > 0) p.Guild = (GuildLaw)StepTo((int)w.Guild, 2); break;
            case PolicyAxis.Inheritance: p.Inheritance = (InheritanceLaw)StepTo((int)w.Inheritance, 4); break;
            case PolicyAxis.MinimumWage: p.MinimumWage = (MinimumWageLaw)StepTo((int)w.MinimumWage, 3); break;
            case PolicyAxis.WartimeRationing: p.WartimeRationing = FlipTo(w.WartimeRationing); break;
            case PolicyAxis.MilitaryExport: p.MilitaryExportBan = FlipTo(w.MilitaryExportBan); break;
            case PolicyAxis.CivilianArms: if (w.Coef.RebelArmsMarketEnabled > 0) p.CivilianArmsBan = FlipTo(w.CivilianArmsBan); break;
            case PolicyAxis.OpiumPossession: p.OpiumPossession = (ReformLevel)StepTo((int)w.OpiumPossession, 2); break;
            case PolicyAxis.OpiumExport: p.OpiumExportBan = FlipTo(w.OpiumExportBan); break;
            case PolicyAxis.Nationalization:
                if (!Government.GateInfo(w, a, inc, inc ? 1 : 0).Ok) break;
                if (inc) { if (!p.NationalizeRailways) p.NationalizeRailways = true; else if (!p.NationalizeMines) p.NationalizeMines = true; else if (!p.NationalizeMilitary) p.NationalizeMilitary = true; else if (!p.NationalizeOil) p.NationalizeOil = true; else if (!p.NationalizeElectricity) p.NationalizeElectricity = true; else if (!p.NationalizeAll && Government.LawPrereq(w, PolicyAxis.NationalizationAll, 1, w.Coef).Ok) p.NationalizeAll = true; }
                else { if (p.NationalizeAll) p.NationalizeAll = false; else if (p.NationalizeElectricity) p.NationalizeElectricity = false; else if (p.NationalizeOil) p.NationalizeOil = false; else if (p.NationalizeMilitary) p.NationalizeMilitary = false; else if (p.NationalizeMines) p.NationalizeMines = false; else p.NationalizeRailways = false; }
                break;
        }
    }

    internal static void TargetCrisisDemands(World w)
    {
        if (w.PendingRevoltDemand != 0)
            TargetDemand(w, (PolicyAxis)(System.Math.Abs(w.PendingRevoltDemand) - 1), w.PendingRevoltDemand > 0);
        if (w.PendingFactionCrisis is not { } f) return;
        foreach (var d in FactionSnapshot.ComputeDemands(w, f.Bloc, lawsOnly: true)) TargetDemand(w, d.Axis, d.Increase);
    }


    private static Party Winner(World w)
    {
        int best = 0; double bv = w.VoteShares[0];
        if (w.VoteShares[1] > bv) { best = 1; bv = w.VoteShares[1]; }
        if (w.VoteShares[2] > bv) { best = 2; }
        return (Party)best;
    }

    private static Party RulingParty(Polity p) => p switch
    {
        Polity.BourgeoisDictatorship => Party.Liberal,
        Polity.ProletarianDictatorship => Party.Socialist,
        _ => Party.Conservative,
    };

    private static void StepManifesto(World w, Coefficients c)
    {
        if (c.ManifestoCreditDecay.Value > 0 && w.ManifestoCredit > 0) w.ManifestoCredit = c.ManifestoCreditDecay.Remaining(w.ManifestoCredit);
        if (w.ManifestoIdeology < 0 || w.ManifestoDone) return;
        double moved = FactionSnapshot.AxisPosition(w, w.ManifestoAxis) - w.ManifestoBaseline;
        if (!w.ManifestoIncrease) moved = -moved;
        if (!(moved >= c.ManifestoFulfillMove.Value)) return;
        w.ManifestoDone = true;
        w.ManifestoCredit += c.ManifestoFulfillCredit;
        w.LogAdd($"{w.Year} {w.SelfPower?.Name}の与党が公約を果たした（{FactionSnapshot.DemandJa(w.ManifestoAxis, w.ManifestoIncrease)}）");
    }

    internal static ElectionEvent BuildElectionEvent(World w, Coefficients c)
    {
        var eff = w.Suffrage;
        var ideoRaw = new double[Ideologies.Count];
        foreach (var (rg, p) in w.AllPopsWithRegion())
        {
            double voters = p.Size * InterestGroups.SeatVoice(w, rg, p, eff);
            if (voters <= 1e-9) continue;
            var s = p.IdeologyShares;
            double sum = 0; for (int i = 0; i < Ideologies.Count; i++) sum += s[i];
            if (sum < 1e-9) continue;
            for (int i = 0; i < Ideologies.Count; i++) ideoRaw[i] += voters * s[i] / sum;
        }
        Span<int> top = [-1, -1, -1];
        for (int i = 0; i < Ideologies.Count; i++)
        {
            if (ideoRaw[i] <= 1e-9) continue;
            for (int k = 0; k < 3; k++)
                if (top[k] < 0 || ideoRaw[i] > ideoRaw[top[k]])
                { for (int m = 2; m > k; m--) top[m] = top[m - 1]; top[k] = i; break; }
        }
        int nTop = top[2] >= 0 ? 3 : top[1] >= 0 ? 2 : top[0] >= 0 ? 1 : 0;
        var ideos = new int[nTop]; var axes = new PolicyAxis[nTop]; var inc = new bool[nTop]; var labels = new string[nTop];
        List<FactionSnapshot.Demand>?[] demandByFam = new List<FactionSnapshot.Demand>?[3];
        for (int k = 0; k < nTop; k++)
        {
            ideos[k] = top[k];
            int fam = PoliticalQuantities.FamilyOf(top[k]);
            var ds = demandByFam[fam] ??= FactionSnapshot.ComputeDemands(w, fam);
            if (ds.Count > 0)
            {
                axes[k] = ds[0].Axis; inc[k] = ds[0].Increase;
                labels[k] = $"{FactionSnapshot.IdeologyJa((Ideology)top[k])}：{FactionSnapshot.DemandJa(ds[0].Axis, ds[0].Increase)}";
            }
            else { axes[k] = default; inc[k] = true; labels[k] = FactionSnapshot.IdeologyJa((Ideology)top[k]); }
        }
        return new ElectionEvent { AgendaIdeologies = ideos, AgendaAxes = axes, AgendaIncrease = inc, AgendaLabels = labels };
    }
}

public sealed class ElectionEvent
{
    public int[] AgendaIdeologies = [];
    public PolicyAxis[] AgendaAxes = [];
    public bool[] AgendaIncrease = [];
    public string[] AgendaLabels = [];
}
