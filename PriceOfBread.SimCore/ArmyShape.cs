namespace PriceOfBread.SimCore;

public static class ArmyShape
{
    public static void Compute(Diplomacy dip, Coefficients c)
    {
        var ps = new List<Power>();
        foreach (var p in dip.Powers) if (p.Nation is not null) ps.Add(p);
        int n = ps.Count;
        if (n == 0) return;
        bool ins = Instruments.On;

        var hrLand = new double[n * n];
        var hrCross = new double[n * n];
        var hrSea = new double[n * n];
        double[]? fmHome = ins ? new double[n * n] : null;
        double[]? fmH = ins ? new double[n * n] : null;
        var border = Borders(ps, c, fmHome);
        var contact = new double[n];
        double contactSum = 0;
        for (int i = 0; i < n; i++)
        {
            double x = ps[i].Nation!.TradeValueSmoothed;
            contact[i] = x > 0 ? x : 0;
            if (c.FleetContactPerGdp == 1)
            {
                double gdpYear = Calendar.PerMonthToPerYear(ps[i].Nation!.NominalGdpPerMonth).Value;
                contact[i] = gdpYear > 1e-9 ? contact[i] * c.ClearingsPerYear / gdpYear : 0;
                if (!(contact[i] >= 0)) contact[i] = 0;
            }
            contactSum += contact[i];
        }
        double contactMean = contactSum / n;
        for (int j = 0; j < n; j++)
            for (int i = 0; i < n; i++)
            {
                if (i == j) continue;
                double h = Hostility(dip, ps[i], ps[j], c);
                if (fmH is not null) fmH[j * n + i] = h;
                double proj = dip.Projection(ps[j], ps[i], c);
                bool cross = dip.IsSeaCrossing(ps[j], ps[i]);
                hrLand[j * n + i] = h * (border[j * n + i] + (cross ? 0 : proj));
                hrCross[j * n + i] = cross ? h * proj : 0;
                double sea = 0;
                for (int b = 0; b < Basins.Count; b++) sea += ps[i].BasinPresence[b] * ps[j].BasinPresence[b];
                hrSea[j * n + i] = h * sea * (contactMean > 0 ? contact[i] / contactMean : 1.0);
            }

        for (int i = 0; i < n; i++) { ps[i].NeedLand = 0; ps[i].NeedSea = 0; }
        double maxColSum = 0;
        var unrest = new double[n];
        var landPot = new double[n];
        for (int j = 0; j < n; j++) landPot[j] = LandPotential(ps[j]);
        for (int j = 0; j < n; j++)
        {
            double u = unrest[j] = Unrest(ps[j]);
            double sumL = u, sumS = u;
            for (int k = 0; k < n; k++) { sumL += hrLand[j * n + k] + hrCross[j * n + k]; sumS += hrSea[j * n + k]; }
            double colL = 0;
            for (int i = 0; i < n; i++)
            {
                if (sumL > 1e-12)
                {
                    double a = hrLand[j * n + i] / sumL, ax = hrCross[j * n + i] / sumL;
                    ps[i].NeedLand += a * landPot[j];
                    ps[i].NeedSea += ax * landPot[j];
                    colL += a + ax;
                }
                if (sumS > 1e-12) ps[i].NeedSea += hrSea[j * n + i] / sumS * ps[j].NavalPower;
            }
            if (colL > maxColSum) maxColSum = colL;
        }

        for (int i = 0; i < n; i++)
        {
            var p = ps[i]; var w = p.Nation!;
            double garrisonMen = c.InternalGarrisonPerPop * w.TotalPopulation * unrest[i];
            var (qTech, qCohesion, qMilitarism) = Quality(w, c);
            double q = qTech * qCohesion * qMilitarism;
            p.NeedInternal = garrisonMen / System.Math.Max(1e-9, c.SoldiersPerMilitary) * q;
            p.NeedMen = c.SoldiersPerMilitary * ((p.NeedLand + p.NeedInternal) + c.NavalCrewPerGauge * p.NeedSea) / System.Math.Max(0.05, q);
            p.AttentionColSumMax = maxColSum;
        }
        if (ins) Instruments.ArmyNeed(ps, c, border, fmHome!, fmH!, hrLand, hrCross, hrSea, landPot, unrest);
    }

    internal static double Hostility(Diplomacy dip, Power a, Power b, Coefficients c)
    {
        if (dip.HasAlliance(a, b)) return 0;
        double t = System.Math.Max(0, dip.Tension(a, b));
        double h = c.AttentionHostilityFloor + (1 - c.AttentionHostilityFloor) * t / (t + c.AttentionTensionHalf);
        if (dip.IsDependentOf(b, a) || dip.IsDependentOf(a, b)) h *= 1.0 - c.AllyAttentionDiscount;
        return h;
    }

    private static readonly int[][] PassNeighbors = BuildPassNeighbors();
    private static int[][] BuildPassNeighbors()
    {
        var nodes = PathGraph.Nodes;
        var lists = new List<int>?[nodes.Length];
        foreach (var e in PathGraph.Edges)
        {
            if (nodes[e.A].Kind == "pass") (lists[e.A] ??= []).Add(e.B);
            if (nodes[e.B].Kind == "pass") (lists[e.B] ??= []).Add(e.A);
        }
        var res = new int[nodes.Length][];
        for (int i = 0; i < nodes.Length; i++) res[i] = lists[i] is { } l ? [.. l] : [];
        return res;
    }

    internal static double[] Borders(List<Power> ps, Coefficients c, double[]? toHome = null)
    {
        int n = ps.Count, nn = PathGraph.Nodes.Length;
        var owner = new int[nn]; var terr = new double[nn];
        var home = toHome is null ? null : new bool[nn];
        System.Array.Fill(owner, -1);
        for (int i = 0; i < n; i++)
        {
            if (ps[i].Nation is not { } w) continue;
            foreach (var r in w.Regions)
            {
                var cc = Climate.CityCoord(r.Name);
                if (cc.Lat == 0 && cc.Lon == 0) continue;
                int nd = PathGraph.NearestNode(cc.Lat, cc.Lon);
                if (nd < 0) continue;
                owner[nd] = i;
                if (home is not null) home[nd] = r.Tier != RegionTier.Colonial;
                int ti = (int)r.Terrain;
                terr[nd] = ti < c.TerrainHorseCost.Length ? c.TerrainHorseCost[ti] : 1.0;
            }
        }
        var b = new double[n * n];
        foreach (var e in PathGraph.Edges) Add(e.A, e.B, 1.0);
        for (int p = 0; p < nn; p++)
        {
            var nb = PassNeighbors[p];
            for (int x = 0; x < nb.Length; x++)
                for (int y = x + 1; y < nb.Length; y++) Add(nb[x], nb[y], 1.0 / PathGraph.PassCostMult);
        }
        return b;

        void Add(int u, int v, double mult)
        {
            int ou = owner[u], ov = owner[v];
            if (ou < 0 || ov < 0 || ou == ov) return;
            double w = mult * 2.0 / (terr[u] + terr[v]);
            b[ou * n + ov] += w; b[ov * n + ou] += w;
            if (home is not null && home[u] && home[v]) { toHome![ou * n + ov] += w; toHome[ov * n + ou] += w; }
        }
    }

    internal static double NavalSpendShareOf(Power p)
    {
        double need = p.NeedSea + p.NeedLand + p.NeedInternal;
        if (!(need > 0)) return -1.0;
        double land = System.Math.Max(p.NeedLand + p.NeedInternal, ForcePotential(p));
        double s = p.NeedSea / (p.NeedSea + land);
        return s >= 0 ? s : -1.0;
    }

    internal static double ForcePotential(Power p)
    {
        if (p.Nation is not { } w) return p.Military;
        var c = w.Coef;
        var (t, co, mi) = Quality(w, c);
        return w.SoldierSizeK() / System.Math.Max(1e-9, c.SoldiersPerMilitary) * (t * co * mi);
    }

    internal static double LandPotential(Power p) => ForcePotential(p) * (1 - p.NavalShare.Value);


    internal static double Unrest(Power p)
        => p.Nation is { } w ? PoliticalQuantities.Clamp01(Politics.AvgMassMilitancy(w) * (1.0 - w.Legitimacy)) : 0;

    internal static (double Tech, double Cohesion, double Militarism) Quality(World w, Coefficients c)
    {
        double techQuality = 1.0;
        double cohesion = w.EthnicEnabled ? 1 + c.UnityMilitaryQuality * (w.EthnicUnity - 0.5) : 1.0;
        double militarism = 1.0 + (c.MilitarismStrengthBonus - 1.0) * PoliticalQuantities.ArmyProfessionalism(w);
        return (techQuality, cohesion, militarism);
    }
}
