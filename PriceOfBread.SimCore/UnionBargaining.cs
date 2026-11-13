namespace PriceOfBread.SimCore;

internal static class UnionBargaining
{
    private const double Eps = 1e-9;

    internal struct Ctx
    {
        public double Kappa, AvgVaPerHead;
        public bool Strike;
        public int LastLp, LastOp;
        public double LastBeta, LastBetaNoStrike, LastS, LastVw, LastVo, LastU, LastStrike;
        public double MassN, UrbanMass, FacScan;
        public bool Any;
    }

    internal readonly record struct Outcome(bool Eligible, double Transfer, double Beta, double BetaNoStrike, double S, double Vw, double Vo, double U, double Excess, bool Guarded, double Strike = 0);

    internal static double Kappa(World w, Region r, bool ins = false) => Kappa(w, r, ins, out _, out _, out _);

    internal static double Kappa(World w, Region r, bool ins, out double massN, out double urbanMass, out double facScan)
    {
        massN = urbanMass = 0; facScan = ins ? w.Factions.Count : 0;
        double num = 0;
        foreach (var f in w.Factions)
            if (ReferenceEquals(f.Region, r) && f.Bloc == 2) num += f.Size * f.Cohesion;
        if (!(num > 0) && !ins) return 0;
        double den = 0;
        foreach (var p in r.Pops)
            if (PolicyPreferences.BlocOf(p) == 2) { den += p.Size; if (ins) { massN += p.Size; if (p.IsUrban) urbanMass += p.Size; } }
        if (!(num > 0)) return 0;
        return den > Eps ? num / den : 0;
    }

    internal static bool IsEligible(Producer s, byte sleep, double va)
        => sleep != 1 && s.BuysLabor && !s.IsRgo && va > 0 && s.HeadsLastMonth > Eps;

    internal static Ctx Prepare(World w, Region r, int np, byte[] sleep, double[] produced, int[] producerCount, double[] revenue, double[] inputCost, bool ins = false)
    {
        var c = new Ctx { LastLp = -1, LastOp = -1, Strike = w.LaborUnion == LaborUnionLaw.Tolerated };
        c.Kappa = Kappa(w, r, ins, out c.MassN, out c.UrbanMass, out c.FacScan);
        double sumVa = 0, sumH = 0;
        for (int pi = 0; pi < np; pi++)
        {
            if (sleep[pi] == 1) continue;
            var s = r.Producers[pi];
            if (!s.BuysLabor || s.IsRgo) continue;
            int gi = s.GoodId;
            double share = produced[gi] > Eps ? s.Output / produced[gi] : 1.0 / producerCount[gi];
            double va = revenue[gi] * share - inputCost[pi];
            if (!IsEligible(s, sleep[pi], va)) continue;
            sumVa += va; sumH += s.HeadsLastMonth;
        }
        if (sumH > Eps) { c.AvgVaPerHead = sumVa / sumH; c.Any = true; }
        return c;
    }

    internal static Outcome Transfer(ref Ctx c, World w, Region r, Producer s, byte sleep, double va, double rent)
    {
        if (!c.Any || !IsEligible(s, sleep, va)) return default;
        int lp = s.LaborPopIndex, op = s.OwnerPopIndex;
        if ((uint)lp >= (uint)r.Pops.Count || (uint)op >= (uint)r.Pops.Count) return default;
        if (c.LastLp != lp || c.LastOp != op)
        {
            var lpop = r.Pops[lp]; var opop = r.Pops[op];
            double vw = InterestGroups.FranchiseVoice(lpop, w.Suffrage, w.Coef);
            double vo = InterestGroups.FranchiseVoice(opop, w.Suffrage, w.Coef);
            double mil = lpop.Militancy;
            double m = c.Strike && mil > 0 ? (mil < 1.0 ? mil : 1.0) : 0.0;
            double u = lpop.UnemploymentRate.Value;
            double sVote = vw + vo > 0 ? vw / (vw + vo) : 0.0;
            c.LastU = u; c.LastVw = vw; c.LastVo = vo; c.LastStrike = m;
            c.LastS = PoliticalQuantities.ProbOr(sVote, m);
            c.LastBeta = BetaOf(c.Kappa, u, c.LastS);
            c.LastBetaNoStrike = BetaOf(c.Kappa, u, sVote);
            c.LastLp = lp; c.LastOp = op;
        }
        double heads = s.HeadsLastMonth;
        double excess = va / heads - c.AvgVaPerHead;
        if (!(excess > 0)) return new Outcome(true, 0, c.LastBeta, c.LastBetaNoStrike, c.LastS, c.LastVw, c.LastVo, c.LastU, 0, false, c.LastStrike);
        double raw = c.LastBeta * excess * heads;
        double t = raw;
        bool guarded = false;
        if (!(t <= rent)) { t = rent; guarded = true; }
        if (!(t > 0)) t = 0;
        return new Outcome(true, t, c.LastBeta, c.LastBetaNoStrike, c.LastS, c.LastVw, c.LastVo, c.LastU, excess * heads, guarded, c.LastStrike);
    }

    internal static double BetaOf(double kappa, double unemployment, double s)
    {
        double b = kappa * (1.0 - unemployment) * s;
        return b > 0 ? b : 0.0;
    }
}
