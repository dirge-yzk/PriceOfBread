namespace PriceOfBread.SimCore;

public static class Crime
{
    public static void Step(World w, Coefficients c)
    {
        if (!w.PoliticsEnabled) { w.PublicOrder = 1.0; w.CrimeRate = 0; return; }
        double pop = w.TotalPopulation;
        if (pop < 1e-9) return;

        double effCoerce = PoliticalQuantities.EffectiveCoerce(w, c);
        double order = PoliticalQuantities.Clamp01(effCoerce / Math.Max(1e-9, pop * c.CrimeOrderPerPop));
        w.PublicOrder = order;
        double worstRate = 0;

        foreach (var r in w.Regions)
        {
            double regionPop = r.SumPopulation();
            if (regionPop < 1e-9) continue;

            double criminal = 0;
            foreach (var p in r.Pops)
                criminal += p.Size * p.Militancy * (1.0 - p.Satisfaction);
            double urbanShare = r.SumUrbanPopulation() / regionPop;
            double crimeRate = c.CrimeBase * (1.0 - order) * PoliticalQuantities.Clamp01(c.CrimeMilitancyWeight * criminal / regionPop) * (1.0 + c.CrimeUrbanWeight * urbanShare);
            r.CrimeRate = crimeRate;
            worstRate = Math.Max(worstRate, crimeRate);
            if (crimeRate <= 1e-9 || criminal <= 1e-9) continue;

            double pool = 0;
            var stolen = new List<double>(r.Pops.Count);
            foreach (var p in r.Pops)
            {
                double steal = Math.Min(Math.Max(0, Calendar.OneMonthOf(p.LastEarnedPerMonth).Value) * crimeRate, Math.Max(0, p.Savings.Value));
                stolen.Add(steal);
                pool += steal;
            }
            if (pool > 1e-12)
            {
                var poor = new List<Pop>(); var need = new List<double>();
                foreach (var p in r.Pops)
                {
                    double weight = p.Size * p.Militancy * (1.0 - p.Satisfaction);
                    if (weight > 0) { poor.Add(p); need.Add(weight); }
                }
                World.Transfer((w, r.Pops), (w, poor, need), pool, fromWeights: stolen);
                if (Instruments.On) Instruments.Theft(r, pool, poor);
            }

        }
        w.CrimeRate = worstRate;
    }
}
