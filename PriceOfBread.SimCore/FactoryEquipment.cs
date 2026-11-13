namespace PriceOfBread.SimCore;

internal static class FactoryEquipment
{
    internal static bool Eligible(Producer s)
        => s.BuysLabor && !s.IsRgo && s.GoodId >= 0 && s.GoodId < Catalog.Goods.Length && Catalog.Goods[s.GoodId].Tier != GoodTier.Service;

    internal static bool On(Coefficients c, bool warmFrozen) => c.RecipeEquipment > 0 && !warmFrozen;

    internal static void Dismantle(Producer s, double builtBefore, World? ledger = null)
    {
        double drop = builtBefore - s.BuiltScale;
        if (drop > 0) { s.Scale -= drop; if (ledger is not null && Instruments.On) Instruments.EquipmentDismantled(ledger, drop); }
    }

    internal static double Distribute(Region r, Coefficients c, double points, double indBefore, long[]? cost, World? ledger = null)
    {
        if (!(points > 0)) return 0;
        var ps = r.Producers;
        var flags = Flags(ps.Count);
        double sumScale = 0, sumW = 0, pBefore = 0; int nCand = 0;
        for (int i = 0; i < ps.Count; i++)
        {
            var s = ps[i];
            flags[i] = 0;
            if (!Eligible(s)) continue;
            sumScale += s.Scale;
            if (!(Discovery.Gate(r, s) > 0)) continue;
            double pr = s.Scale - s.BuiltScale; if (pr > 0) pBefore += pr;
            if (s.Bankrupt) { flags[i] = 1; continue; }
            flags[i] = 2; nCand++;
            double m = s.SmoothedMargin;
            if (m > 0) sumW += m;
        }
        if (cost is not null) cost[CostLedger.RecipeEquipmentScans] += 3L * ps.Count;
        if (nCand == 0) return 0;
        double ind = indBefore > 0 ? indBefore : 0;
        double phi = points / (ind + points);
        double basis = sumScale > c.FactoryBuildScale ? sumScale : c.FactoryBuildScale;
        double add = basis * phi;
        if (!(add > 0)) return 0;
        double given = 0;
        for (int i = 0; i < ps.Count; i++)
        {
            if (flags[i] != 2) continue;
            var s = ps[i];
            double w = sumW > 1e-12 ? (s.SmoothedMargin > 0 ? s.SmoothedMargin / sumW : 0.0) : 1.0 / nCand;
            double d = add * w;
            s.Scale += d; given += d;
        }
        if (!(pBefore > 1e-12))
        {
            if (ledger is not null) Instruments.EquipmentDistributed(ledger, 0.0, false, given);
            return given;
        }
        double drift = Renormalize(ps, flags, pBefore, out double dScale, out bool shortfall);
        if (ledger is not null) Instruments.EquipmentDistributed(ledger, drift, shortfall, given + dScale);
        return given;
    }

    internal static void Retire(Region r, Producer s, long[]? cost, World? ledger = null)
    {
        var ps = r.Producers;
        var flags = Flags(ps.Count);
        double pBefore = 0;
        for (int i = 0; i < ps.Count; i++)
        {
            var q = ps[i];
            flags[i] = 0;
            if (ReferenceEquals(q, s) || !Eligible(q) || !(Discovery.Gate(r, q) > 0)) continue;
            double pr = q.Scale - q.BuiltScale; if (pr > 0) pBefore += pr;
            flags[i] = 1;
        }
        if (cost is not null) cost[CostLedger.RecipeEquipmentScans] += 2L * ps.Count;
        double removed = s.Scale, freed = s.Scale - s.BuiltScale; if (freed < 0) freed = 0;
        s.Scale = 0;
        double drift = Renormalize(ps, flags, pBefore + freed, out double dScale, out bool shortfall);
        if (ledger is not null) Instruments.EquipmentRetired(ledger, drift, shortfall, dScale - removed);
    }

    private static double Renormalize(List<Producer> ps, byte[] flags, double target, out double dScale, out bool shortfall)
    {
        double pMixed = 0, pPure = 0;
        for (int i = 0; i < ps.Count; i++)
        {
            if (flags[i] == 0) continue;
            var s = ps[i];
            if (s.BuiltScale > 1e-9) { double pr = s.Scale - s.BuiltScale; if (pr > 0) pMixed += pr; }
            else if (s.Scale > 0) pPure += s.Scale;
        }
        double need = target - pMixed;
        double k = pPure > 1e-12 && need > 0 ? need / pPure : 1.0;
        dScale = 0;
        if (pPure > 1e-12 && k != 1.0)
            for (int i = 0; i < ps.Count; i++)
            {
                if (flags[i] == 0) continue;
                var s = ps[i];
                if (!(s.BuiltScale > 1e-9) && s.Scale > 0) { double before = s.Scale; s.Scale *= k; dScale += s.Scale - before; }
            }
        double achieved = pMixed + (pPure > 1e-12 ? pPure * k : pPure);
        double scale = target > 1e-9 ? target : 1e-9;
        double drift = Math.Abs(achieved - target) / scale;
        shortfall = drift > 1e-9;
        return drift;
    }

    [ThreadStatic] private static byte[]? flagBuf;
    private static byte[] Flags(int n)
    {
        var b = flagBuf;
        if (b is null || b.Length < n) flagBuf = b = new byte[Math.Max(n, 256)];
        return b;
    }

    internal static bool InSet(Region r, Producer s) => Eligible(s) && Discovery.Gate(r, s) > 0;
}
