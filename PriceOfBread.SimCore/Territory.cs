namespace PriceOfBread.SimCore;

public static class Territory
{
    private const double MaxMigrationFraction = 0.2;
    internal const double OpportunityFactorFloor = 0.1;

    public static void Step(World w, Coefficients c)
    {
        var active = new List<Region>();
        foreach (var r in w.Regions) active.Add(r);
        int k = active.Count;
        if (k < 2) return;

        var opp = new double[k]; var stay = new double[k]; var lat = new double[k]; var lon = new double[k]; var classes = new int[k];
        for (int i = 0; i < k; i++)
        {
            var r = active[i];
            opp[i] = OppChecked(r, c);
            stay[i] = opp[i] * Holding(r, c);
            (lat[i], lon[i]) = Places.Coord(r.Name);
            foreach (var p in r.Pops) if (!p.IsSoldier) classes[i] |= 1 << p.ClassId;
        }
        var plan = new List<(int S, int D, double F)>();
        var inflow = new double[k];
        var gap = new double[k];
        double tech = w.WorldTechLevel;
        var size0 = new double[k][];
        long seenPairs = 0;
        for (int s = 0; s < k; s++)
        {
            if (!double.IsFinite(stay[s])) continue;
            seenPairs += k;
            double gMax = 0, wSum = 0;
            for (int d = 0; d < k; d++)
            {
                gap[d] = 0;
                if (d == s || !double.IsFinite(opp[d])) continue;
                double g = opp[d] - stay[s] - c.TransportFriction;
                if (!(g > 0)) continue;
                double km = Geo.DistanceKm(lat[s], lon[s], lat[d], lon[d]);
                g -= EntryFrictionTerm(km, tech, c);
                if (!(g > 0)) continue;
                gap[d] = g; wSum += g;
                if (g > gMax) gMax = g;
            }
            if (!(wSum > 0)) continue;
            double fraction = PoliticalQuantities.ClampOr(c.RegionMobility * gMax, 0, MaxMigrationFraction, 0.0);
            size0[s] = new double[active[s].Pops.Count];
            for (int i = 0; i < size0[s].Length; i++) size0[s][i] = active[s].Pops[i].Size;
            for (int d = 0; d < k; d++)
            {
                if (!(gap[d] > 0)) continue;
                double f = fraction * (gap[d] / wSum);
                plan.Add((s, d, f));
                inflow[d] += f * Movable(active[s], classes[d]);
            }
        }
        w.Cost[CostLedger.MigrationPairs] += seenPairs;
        var scale = new double[k];
        for (int d = 0; d < k; d++) if (inflow[d] > 0) scale[d] = ReceiveScale(active[d], inflow[d], c);
        foreach (var (s, d, f) in plan)
        {
            double g = f * scale[d];
            if (g > 0) MoveRegion(active[s], active[d], g, size0[s]!, c);
        }
    }

    internal static double MigrationDecay(double km, double worldTech, Coefficients c)
        => c.MigrationDistanceScale > 0 ? 1.0 / (1.0 + km / (c.MigrationDistanceScale * c.EraFactor(worldTech, c.EraMigrationScale))) : 1.0;

    internal static double EntryFrictionTerm(double km, double worldTech, Coefficients c)
        => c.MigrationDistanceScale > 0 ? c.TransportFriction * km / (c.MigrationDistanceScale * c.EraFactor(worldTech, c.EraMigrationScale)) : 0.0;

    private static double Movable(Region src, int dstClasses)
    {
        double m = 0;
        foreach (var sp in src.Pops)
            if (sp.IsCommoner && (dstClasses >> sp.ClassId & 1) != 0) m += sp.Size;
        return m;
    }

    private static double ReceiveScale(Region dst, double planned, Coefficients c)
    {
        double cap = (Calendar.PerQuarterToPerMonth(c.ImmigrationReceiveCapPerQuarter) * dst.SumPopulation() * AbsorbCap(dst, c)).Value;
        return planned <= cap ? 1.0 : cap > 0 ? cap / planned : 0.0;
    }

    private static double OppChecked(Region r, Coefficients c)
    {
        double o = Opportunity(r, c);
        if (Simulation.ConservationProbe && !double.IsFinite(o))
            Console.Error.WriteLine($"[非有限診断] Territory.Opportunity {r.Name}={o}　pop={r.SumPopulation()} 賃金P={r.WagePremium} 工業={r.TotalIndustrialization} 住居={r.HousingFill} 犯罪={r.CrimeRate} 耕地={r.ArableLand} 生産性={r.ProductivityBonus}");
        return o;
    }

    internal static double Opportunity(Region r, Coefficients c)
    {
        double pop = r.SumPopulation();
        double land = LandFactor(r, r.SumRuralPopulation(), c);
        double wagePull = Math.Max(OpportunityFactorFloor, 1.0 + c.OpportunityWageWeight * (r.WagePremium - 1.0));
        if (!(pop >= 1.0)) return r.ProductivityBonus * wagePull * land;
        double unemp = 0; foreach (var p in r.Pops) if (!p.IsSoldier) unemp += p.Size * p.UnemploymentRate.Value;
        double unrest = Politics.AvgRegionMilitancy(r);
        double pushPenalty = 1.0 - c.PushUnrestWeight * unrest - c.CrimePushWeight * r.CrimeRate;
        double housingFriction = 1.0 - c.HousingMigrationFriction * (1.0 - r.HousingFill);
        return r.ProductivityBonus * wagePull * land * (1.0 - unemp / pop) * Math.Max(OpportunityFactorFloor, pushPenalty) * Math.Max(OpportunityFactorFloor, housingFriction);
    }

    internal static double LandFactor(Region r, double ruralLabor, Coefficients c)
    {
        if (r.ArableLand >= 1e8) return 1.0;
        double ratio = r.ArableLand / Math.Max(1.0, ruralLabor);
        return Math.Min(1.0, Math.Pow(ratio, c.LandDiminishExponent));
    }

    internal static double Holding(Region r, Coefficients c)
    {
        if (r.ArableLand >= 1e8) return 1.0;
        double ratio = r.ArableLand / Math.Max(1.0, r.SumRuralPopulation());
        return Math.Pow(Math.Max(1.0, ratio), c.LandDiminishExponent);
    }

    internal static double AbsorbCap(Region? r, Coefficients c)
    {
        if (r is null || !(c.ImmigrationAbsorbExponent > 0)) return 1.0;
        double slack = PoliticalQuantities.Clamp01(r.FoodFill) * PoliticalQuantities.Clamp01(r.HousingFill) * LandFactor(r, r.SumRuralPopulation(), c);
        if (!(slack > 0)) return 0;
        return Math.Pow(slack, c.ImmigrationAbsorbExponent);
    }

    private static void MoveRegion(Region src, Region dst, double fraction, double[] size0, Coefficients c)
    {
        for (int i = 0; i < src.Pops.Count; i++)
        {
            var sp = src.Pops[i];
            if (sp.Closed || sp.IsSoldier || sp.IsCriminal) continue;
            double moveSize = size0[i] * fraction;
            if (!(moveSize > 1e-9)) continue;
            if (dst.PopOf(sp.ClassId) is not { } dp) continue;
            MoveBetweenRegions(sp, dp, moveSize, c);
            if (Instruments.On) Instruments.Migrated(src, dst, moveSize);
        }
    }
    internal static void MoveBetweenRegions(Pop sp, Pop dp, double moveSize, Coefficients c) => Pop.Blend(sp, dp, moveSize);

    internal enum Rehoming
    {
        Ceded,
        Seceded,
        Returned,
        Sovereign,
    }

    internal static void Rehome(Region r, Rehoming how, bool sameCore = false)
    {
        if (how != Rehoming.Sovereign)
        {
            foreach (var m in r.Markets) { m.PrevStock = m.Stock; m.LastProduced = m.LastSold = m.LastImport = m.LastExport = m.LastShortage = 0; m.PopBought = null; }
            r.ColonialResources = [];
        }
        switch (how)
        {
            case Rehoming.Ceded:
                if (r.Tier != RegionTier.Colonial) r.Tier = RegionTier.Integrated;
                if (!sameCore) foreach (var p in r.Pops) p.CoreEthnicShare = new Share(0.0);
                r.TierInParent = null;
                break;
            case Rehoming.Seceded:
                foreach (var p in r.Pops) p.CoreEthnicShare = new Share(1.0 - p.CoreEthnicShare.Value);
                r.TierInParent = r.Tier;
                r.AnnexBaggage = 0;
                break;
            case Rehoming.Returned:
                foreach (var p in r.Pops) p.CoreEthnicShare = new Share(1.0 - p.CoreEthnicShare.Value);
                if (r.TierInParent is { } tp) r.Tier = tp;
                r.TierInParent = null;
                break;
            case Rehoming.Sovereign:
                r.Tier = RegionTier.Core;
                r.Dominance = 1.0;
                break;
        }
    }

    internal static int DroppedStateTarget(int target, int units) => Math.Min(target, units);

    internal static (double Rail, double Ship) TransferRegion(World from, World? to, Region r, Rehoming how, bool sameCore = false)
    {
        bool wasCapital = r.Id == 0;
        MoneyStock S = r.SumSavings(), D = default; double P = r.SumPopulation();
        foreach (var p in r.Pops) D += p.Debt;
        MoneyStock B = S - D;
        double sh = Transport.RegionShare(Transport.ShareBasis(from, from.Coef), r);
        if (!(sh > 0)) sh = 0;
        double rail = Transport.TakeRailForMove(from, r, sh);
        double ship = from.ShipCapacity * sh;
        from.ShipCapacity -= ship;
        from.Regions.Remove(r);
        if (how == Rehoming.Ceded && r.FleetUnits is { } cededFleet)
        {
            var homeReg = wasCapital ? NextCapital(from) : from.Regions.Find(q => q.Id == 0);
            if (homeReg is not null)
            {
                var dstFleet = homeReg.FleetUnits ??= new double[cededFleet.Length];
                double returned = 0;
                for (int k = 0; k < cededFleet.Length && k < dstFleet.Length; k++) { dstFleet[k] += cededFleet[k]; returned += cededFleet[k]; }
                r.FleetUnits = null;
                if (Instruments.On) Instruments.FleetReturned(from, returned);
            }
        }
        from.CumulativeTradeFlow -= B; from.PopulationDelta -= P;
        if (Instruments.On) Instruments.InheritIntentBefore(from, r, how);
        foreach (var pr in r.Producers) { pr.StateTarget = DroppedStateTarget(pr.StateTarget, World.StateUnits(pr, from.Coef)); pr.FrozenLandRentPerMonth = null; }
        if (Instruments.On) Instruments.InheritIntentAfter(from, r);
        MoneyStock Din = D, Bin = B;
        if (to is not null && to.FxRate != from.FxRate)
        {
            MoneyStock Sin = default; Din = default;
            foreach (var p in r.Pops) { p.Rebase(from, to); Sin += p.Savings; Din += p.Debt; }
            Bin = Sin - Din;
        }
        if (Instruments.On) Instruments.RegionMoved(from, to, r, how);
        from.Bank!.Carry(to?.Bank, D, Din);
        if (to is not null)
        {
            Adopt(to, r);
            r.AnnexBaggage = to.SelfPower is { } tp && tp.Id == r.OriginPowerId ? 0.0 : P;
            foreach (var p in r.Pops) Fiscal.BookForeignDebt(p, p.BondHoldings, from, to);
            to.CumulativeTradeFlow += Bin; to.PopulationDelta += P;
            Transport.Receive(to, rail, ship, from.ShipSteamShare.Value);
        }
        if (wasCapital && NextCapital(from) is { } cap) from.SetCapital(cap);
        bool backToColony = how == Rehoming.Returned && r.TierInParent == RegionTier.Colonial && r.Tier != RegionTier.Colonial;
        RegionTier tierLeft = r.Tier;
        Rehome(r, how, sameCore);
        if (how == Rehoming.Ceded && to is not null && from.Coef.CedeRuleToLocal > 0)
        {
            if (r.Rule.Length == 4) { r.Rule[1] += r.Rule[0]; r.Rule[0] = 0; }
            if (to.SelfPower is { } tsp)
            {
                string key = tsp.BaseName.Length > 0 ? tsp.BaseName : tsp.Name;
                for (int k = 0; k < r.ForeignShares.Length && k < r.ForeignRulers.Length; k++)
                    if (r.ForeignRulers[k] == key) r.ForeignShares[k] = 0;
            }
        }
        r.NavalBudget = 0; r.NavalOrder = null;
        if (backToColony && to?.Diplomacy is { } dg)
        {
            if (tierLeft == RegionTier.Core) dg.SovereignColonialRegions--; else dg.PromotedColonies--;
        }
        return (rail, ship);
    }

    internal static Region? NextCapital(World from, IReadOnlyList<Region>? leaving = null)
    {
        Region? cap = null; double capBest = double.MinValue;
        foreach (var q in from.Regions)
        {
            if ((leaving is not null && leaving.Contains(q))) continue;
            double g = 0; foreach (var s in q.Producers) { var good = q.GoodOf(s.GoodId); if (good is null || good.Tier == GoodTier.Service) continue; g += s.Output * good.RefPrice; }
            if (g > capBest || (g == capBest && (cap is null || q.Id < cap.Id))) { capBest = g; cap = q; }
        }
        return cap;
    }

    internal static void Adopt(World to, Region r)
    {
        int id = 1; foreach (var q in to.Regions) if (q.Id >= id) id = q.Id + 1;
        r.Id = id;
        to.Regions.Add(r);
    }
}
