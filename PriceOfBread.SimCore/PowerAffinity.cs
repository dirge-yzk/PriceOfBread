namespace PriceOfBread.SimCore;

public static class PowerAffinity
{

    public enum Holder { Crown = 0, Parliament = 1, Army = 2, Revolution = 3 }
    public const int HolderCount = 4;
    public static readonly string[] HolderNames = ["王権", "議会", "軍部", "革命勢力"];

    public static double[] CoordOf(Holder h) => h switch
    {
        Holder.Crown => Ideologies.BasePolityCoord(Polity.Absolutist),
        Holder.Parliament => Ideologies.BasePolityCoord(Polity.Republic),
        Holder.Army => Ideologies.BasePolityCoord(Polity.MilitaryDictatorship),
        _ => Ideologies.BasePolityCoord(Polity.Absolutist),
    };

    public static double RevolutionAffinity(double best) => (Ideologies.AffMax + Ideologies.AffMin) - best;

    public static double PopWeight(Pop p, World w)
    {
        if (!(p.Size > 0)) return 0;
        double voice = w.Coef.IdeologyVoiceFloor
                     + (1.0 - w.Coef.IdeologyVoiceFloor) * InterestGroups.FranchiseVoice(p, w.Suffrage, w.Coef);
        if (!(voice >= 0)) voice = 0;
        double v = p.Size * voice;
        return !(v >= 0) ? 0 : v;
    }


    public static double[] Shares(World w) => Detail(w).Shares;

    public static Result Detail(World w)
    {
        double slope = w.Coef.IdeologyAffinitySlope;
        var s = new double[HolderCount];
        double[] cc = CoordOf(Holder.Crown), pc = CoordOf(Holder.Parliament), ac = CoordOf(Holder.Army);

        double civ = PowerShares.CivilianInstitutions(w);
        if (!(civ >= 0 && civ <= 1)) civ = 0;
        double armyDamp = 1.0 - civ;

        double soldierOrg = w.SoldierSizeK() / System.Math.Max(1.0, w.TotalPopulation) / System.Math.Max(1e-9, Military.PeaceCapShareOfPop(w.Coef));
        if (!(soldierOrg >= 0)) soldierOrg = 0; else if (soldierOrg > 1) soldierOrg = 1;

        double pctl = PowerShares.ParliamentControl(w);
        if (!(pctl >= 0 && pctl <= 1)) pctl = 0;

        double totalW = 0, loyalW = 0, restiveW = 0;
        double sumM = 0, sumInstitutional = 0;

        foreach (var p in w.AllPops())
        {
            double wt = PopWeight(p, w);
            if (!(wt > 0)) continue;
            double m = p.Militancy;
            if (!(m >= 0 && m <= 1)) m = 0;

            double aC = Ideologies.SupportAt(p.IdeologyShares, cc, slope);
            double aP = Ideologies.SupportAt(p.IdeologyShares, pc, slope);
            double aA = Ideologies.SupportAt(p.IdeologyShares, ac, slope);
            if (!(aC >= Ideologies.AffMin && aC <= Ideologies.AffMax)) aC = Ideologies.AffMin;
            if (!(aP >= Ideologies.AffMin && aP <= Ideologies.AffMax)) aP = Ideologies.AffMin;
            if (!(aA >= Ideologies.AffMin && aA <= Ideologies.AffMax)) aA = Ideologies.AffMin;

            double best = System.Math.Max(aC, System.Math.Max(aP, aA));
            double aR = RevolutionAffinity(best);

            double vote = InterestGroups.FranchiseVoice(p, w.Suffrage, w.Coef);
            if (!(vote >= 0 && vote <= 1)) vote = 0;
            double franchise = PoliticalQuantities.ProbOr(vote, pctl);
            if (!(franchise >= 0 && franchise <= 1)) franchise = 0;
            double crownTrust = (aC - Ideologies.AffMin) / (Ideologies.AffMax - Ideologies.AffMin);
            if (!(crownTrust >= 0 && crownTrust <= 1)) crownTrust = 0;
            double reach = PoliticalQuantities.ProbOr(franchise, crownTrust);
            double outside = m * (1.0 - reach);
            if (!(outside >= 0 && outside <= 1)) outside = 0;

            {
                double loyal = wt * (1.0 - outside);
                double sold = p.IsSoldier ? armyDamp : 0.0;
                s[(int)Holder.Army] += loyal * sold;
                double civilian = loyal * (1.0 - sold);
                s[(int)Holder.Crown] += civilian * (1.0 - franchise);
                s[(int)Holder.Parliament] += civilian * franchise;
                loyalW += loyal;
            }

            double eA = System.Math.Exp(aA) * armyDamp * soldierOrg, eR = System.Math.Exp(aR);
            double d2 = eA + eR;
            if (d2 > 1e-12)
            {
                double restive = wt * outside;
                s[(int)Holder.Army] += restive * eA / d2;
                s[(int)Holder.Revolution] += restive * eR / d2;
                restiveW += restive;
            }

            totalW += wt;
            sumM += wt * outside;
            sumInstitutional += wt * reach;
        }

        double tot = s[0] + s[1] + s[2] + s[3];
        if (!(tot > 1e-12))
        {
            var z = new double[HolderCount];
            z[(int)Holder.Crown] = 1.0;
            return new Result(z, 0, 0, 0, false);
        }
        double done = 0;
        for (int h = 0; h < HolderCount - 1; h++) { s[h] /= tot; done += s[h]; }
        s[HolderCount - 1] = 1.0 - done;
        if (!(s[HolderCount - 1] >= 0)) s[HolderCount - 1] = 0;

        double meanM = totalW > 1e-12 ? sumM / totalW : 0;
        double meanInst = totalW > 1e-12 ? sumInstitutional / totalW : 0;
        return new Result(s, meanM, meanInst, totalW, true);
    }

    public static Polity PolityFromShares(World w)
    {
        var co = Coalition(w);
        var eff = co.Effective;
        bool crowned = w.Crown != CrownForm.None;
        if (co.SoleRuler is { } r)
            return r switch
            {
                Holder.Army => Polity.MilitaryDictatorship,
                Holder.Revolution => Polity.ProletarianDictatorship,
                Holder.Crown => crowned ? Polity.Absolutist : Polity.BourgeoisDictatorship,
                _ => crowned ? Polity.ConstitutionalMonarchy : Polity.Republic,
            };
        double cw = eff[(int)Holder.Crown], pw = eff[(int)Holder.Parliament];
        if (!(cw + pw > PartnerViableShare * 0.5)) return Polity.Anarchy;
        return pw > cw ? (crowned ? Polity.ConstitutionalMonarchy : Polity.Republic)
                       : (crowned ? Polity.Absolutist : Polity.BourgeoisDictatorship);
    }


    public static (double Crown, double Parliament) Legitimacy2(World w)
    {
        var s = Shares(w);
        double c = s[(int)Holder.Crown], p = s[(int)Holder.Parliament], sum = c + p;
        if (!(sum > 1e-12)) return (1.0, 0.0);
        return (c / sum, p / sum);
    }

    public static (double Crown, double Parliament) BudgetWeights(BudgetLaw law) => law switch
    {
        BudgetLaw.Decree => (1.0, 0.0),
        BudgetLaw.Partial => (0.5, 0.5),
        _ => (0.0, 1.0),
    };

    public static (double Crown, double Parliament) CommandWeights(CommandLaw law) => law switch
    {
        CommandLaw.Crown => (1.0, 0.0),
        CommandLaw.Split => (0.5, 0.5),
        _ => (0.0, 1.0),
    };

    public static bool ArmyHoldsCommand(World w)
    {
        double civ = PowerShares.CivilianInstitutions(w);
        if (!(civ >= 0 && civ <= 1)) civ = 0;
        var s = Shares(w);
        double civilian = s[(int)Holder.Crown] + s[(int)Holder.Parliament];
        if (!(civilian >= 0)) return false;
        if (!(w.RegimeTrackRecord[1] < 0)) return false;
        return civilian < 1.0 - civ;
    }

    public static (double Crown, double Parliament, double Army) BudgetShares(World w)
    {
        if (ArmyHoldsCommand(w)) return (0, 0, 1.0);
        var (lc, lp) = Legitimacy2(w);
        var (wc, wp) = BudgetWeights(w.BudgetLaw);
        double a = lc * wc, b = lp * wp, sum = a + b;
        if (!(sum > 1e-12)) return (1.0, 0, 0);
        return (a / sum, b / sum, 0);
    }

    public static (double Crown, double Parliament, double Army) CommandShares(World w)
    {
        if (ArmyHoldsCommand(w)) return (0, 0, 1.0);
        var (lc, lp) = Legitimacy2(w);
        var (wc, wp) = CommandWeights(w.CommandLaw);
        double a = lc * wc, b = lp * wp, sum = a + b;
        if (!(sum > 1e-12)) return (1.0, 0, 0);
        return (a / sum, b / sum, 0);
    }

    public static double Legitimacy(World w) { var s = Shares(w); return s[(int)Holder.Crown] + s[(int)Holder.Parliament]; }


    public const double PartnerViableShare = PowerShares.PartnerViableShare;

    public static CoalitionResult Coalition(World w)
    {
        var raw = Shares(w);
        MechanismProbe.Eval(MechanismProbe.Site.CoalitionComputed);
        if (raw[(int)Holder.Revolution] > 0) MechanismProbe.Eval(MechanismProbe.Site.CoalitionRevolutionAlly);
        bool probeAnyAlly = false, probeRevAlly = false;
        double slope = w.Coef.IdeologyAffinitySlope;
        var eff = (double[])raw.Clone();
        var ally = new Holder[HolderCount];
        for (int h = 0; h < HolderCount; h++) ally[h] = (Holder)h;

        for (int h = 0; h < HolderCount; h++)
        {
            double mine = raw[h];
            if (!(mine > 0)) continue;
            int best = -1; double bestPull = 0;
            for (int k = 0; k < HolderCount; k++)
            {
                if (k == h) continue;
                double theirs = raw[k];
                if (!(theirs > mine)) continue;
                double pull = theirs * Closeness((Holder)h, (Holder)k, slope, w.Coef.CoalitionClosenessFloor);
                if (pull > bestPull) { bestPull = pull; best = k; }
            }
            if (best >= 0)
            {
                probeAnyAlly = true;
                if ((Holder)h == Holder.Revolution || (Holder)best == Holder.Revolution) probeRevAlly = true;
                double bond = Closeness((Holder)h, (Holder)best, slope, w.Coef.CoalitionClosenessFloor);
                if (!(bond >= 0 && bond <= 1)) bond = 0;
                double moved = mine * bond;
                ally[h] = (Holder)best; eff[best] += moved; eff[h] -= moved;
            }
        }

        if (probeAnyAlly) MechanismProbe.Hit(MechanismProbe.Site.CoalitionComputed);
        if (probeRevAlly) MechanismProbe.Hit(MechanismProbe.Site.CoalitionRevolutionAlly);
        int rulerIdx = -1;
        for (int h = 0; h < HolderCount; h++)
            if (eff[h] >= PartnerViableShare && ally[h] == (Holder)h && (rulerIdx < 0 || eff[h] > eff[rulerIdx])) rulerIdx = h;

        return new CoalitionResult(raw, eff, ally, rulerIdx >= 0 ? (Holder?)rulerIdx : null);
    }

    public static double Closeness(Holder a, Holder b, double slope, double floor = 0)
    {
        if (a == Holder.Revolution || b == Holder.Revolution) return floor;
        double aff = Ideologies.AffinityOf(CoordOf(a), CoordOf(b), slope);
        double v = (aff - Ideologies.AffMin) / (Ideologies.AffMax - Ideologies.AffMin);
        if (!(v >= 0 && v <= 1)) return floor;
        return floor > 0 ? floor + (1.0 - floor) * v : v;
    }

    public readonly record struct CoalitionResult(double[] Raw, double[] Effective, Holder[] Ally, Holder? SoleRuler);

    public readonly record struct Result(double[] Shares, double MeanMilitancy, double MeanInstitutional, double TotalWeight, bool Any);


    public static double[] IdeologyDestination(int ideologyIndex, double slope)
    {
        var one = new double[Ideologies.Count];
        if (ideologyIndex >= 0 && ideologyIndex < Ideologies.Count) one[ideologyIndex] = 1.0;
        double aC = Ideologies.SupportAt(one, CoordOf(Holder.Crown), slope);
        double aP = Ideologies.SupportAt(one, CoordOf(Holder.Parliament), slope);
        double aA = Ideologies.SupportAt(one, CoordOf(Holder.Army), slope);
        double aR = RevolutionAffinity(System.Math.Max(aC, System.Math.Max(aP, aA)));
        double eC = System.Math.Exp(aC), eP = System.Math.Exp(aP), eA = System.Math.Exp(aA), eR = System.Math.Exp(aR);
        double d1 = eC + eP, d2 = eA + eR;
        return [
            d1 > 1e-12 ? eC / d1 : 0.5,
            d1 > 1e-12 ? eP / d1 : 0.5,
            d2 > 1e-12 ? eA / d2 : 0.5,
            d2 > 1e-12 ? eR / d2 : 0.5,
        ];
    }
}
