namespace PriceOfBread.SimCore;

public sealed class FactionState
{
    public Region Region = null!;
    public int Bloc;
    public int Ideo;
    public double Size;
    public double AvgMilitancy;
    public double FightHeads;   // (1435) 戦う頭数 Σ_{p∈組} N_p M_p（π を掛けない・Factions.Step が書く・旗 FactionStrengthByMilitancy 1 の強さが読む）
    public double Cohesion;
    public bool Mutated;
    public string MutantName = "";
    public double Rejections;
    public double EquipFill = 1.0;
    public Dictionary<int, double>? ArmsStock;
    public int[]? CrisisDemands;
    public double[]? CrisisDemandPos;
}

public static class Factions
{

    private const double ForgetCohesionFloor = 0.05;

    public static void Step(World w, Coefficients c)
    {
        if (!w.ParliamentEnabled || w.FactionRng is null) return;
        double totalPop = w.TotalPopulation;
        if (totalPop <= 1e-9) return;

        var extras = new List<(double Fight, double FightSq, double RegionPop)>();
        var observed = FactionSnapshot.ComputeClusters(w, true, extras);
        var consciousnessByBloc = new double?[PolicyPreferences.BlocCount];
        for (int ci = 0; ci < observed.Count; ci++)
        {
            var (reg, bloc, ideo, size, milSum) = observed[ci];
            if (size < FactionSnapshot.Floor(w, extras[ci].RegionPop, totalPop)) continue;
            FactionState? st = null;
            foreach (var f in w.Factions) if (ReferenceEquals(f.Region, reg) && f.Bloc == bloc && f.Ideo == ideo) { st = f; break; }
            if (st is null) { st = new FactionState { Region = reg, Bloc = bloc, Ideo = ideo }; w.Factions.Add(st); }
            st.Size = size; st.AvgMilitancy = milSum / size;
            double fight = extras[ci].Fight; st.FightHeads = fight;
            if (c.FactionStrengthByMilitancy > 0) st.AvgMilitancy = fight > 0 ? extras[ci].FightSq / fight : 0.0;   // (1435) 旗 1＝闘争度の重みの平均
            double consc = consciousnessByBloc[bloc] ??= BlocConsciousness(w, bloc);
            double growth = c.FactionCohesionGrowth * (0.5 + consc) * (st.Mutated ? 2.0 : 1.0);
            st.Cohesion = PoliticalQuantities.Clamp01(PoliticalQuantities.Toward(st.Cohesion, 1.0, growth));
        }
        for (int i = w.Factions.Count - 1; i >= 0; i--)
        {
            var f = w.Factions[i];
            bool seen = false;
            for (int ci = 0; ci < observed.Count; ci++)
            {
                var (reg, bloc, ideo, size, _) = observed[ci];
                if (ReferenceEquals(f.Region, reg) && f.Bloc == bloc && f.Ideo == ideo && size >= FactionSnapshot.Floor(w, extras[ci].RegionPop, totalPop)) { seen = true; break; }
            }
            if (seen) continue;
            f.Cohesion *= 1.0 - c.FactionCohesionDecay;
            if (f.Cohesion < ForgetCohesionFloor) w.Factions.RemoveAt(i);
        }
        if (c.FactionMergeRangeKm > 0 && w.Factions.Count >= 2)
        {
            var merged = new System.Collections.Generic.HashSet<FactionState>();
            var ordered = new System.Collections.Generic.List<FactionState>(w.Factions.Count);
            foreach (var f0 in w.Factions) if (w.Regions.Contains(f0.Region)) ordered.Add(f0);
            ordered.Sort((a, b) => a.Region.Name != b.Region.Name ? string.CompareOrdinal(a.Region.Name, b.Region.Name) : a.Bloc != b.Bloc ? a.Bloc.CompareTo(b.Bloc) : a.Ideo.CompareTo(b.Ideo));
            for (int i2 = 0; i2 < ordered.Count; i2++)
                for (int j2 = i2 + 1; j2 < ordered.Count; j2++)
                {
                    var a = ordered[i2]; var b = ordered[j2];
                    if (merged.Contains(a) || merged.Contains(b)) continue;
                    if (a.Bloc != b.Bloc || a.Ideo != b.Ideo || a.Mutated != b.Mutated || ReferenceEquals(a.Region, b.Region)) continue;
                    var ca = Climate.CityCoord(a.Region.Name); var cb = Climate.CityCoord(b.Region.Name);
                    if ((ca.Lat == 0 && ca.Lon == 0) || (cb.Lat == 0 && cb.Lon == 0)) continue;
                    if (Geo.DistanceKm(ca.Lat, ca.Lon, cb.Lat, cb.Lon) > c.FactionMergeRangeKm) continue;
                    var big = a.Size >= b.Size ? a : b; var small = big == a ? b : a;
                    big.Cohesion = PoliticalQuantities.Clamp01(big.Cohesion + c.FactionMergeCohesionGain.Value * small.Cohesion);
                    big.Rejections = System.Math.Max(big.Rejections, small.Rejections);
                    small.Cohesion = 0;
                    merged.Add(big); merged.Add(small);
                    w.LogAdd($"{w.Year} {DisplayName(small)}が{DisplayName(big)}に合流した");
                }
        }
        if (Instruments.On) { Instruments.PrdfObserve(w, observed, totalPop); Instruments.Facfloor2Observe(w); }   // (1435) facfloor2_s
    }

    public static string DisplayName(FactionState f) =>
        f.Mutated ? f.MutantName : $"{f.Region.Name}の{FactionSnapshot.IdeologyJa((Ideology)f.Ideo)}{FactionSnapshot.BlocJa(f.Bloc)}";

    private static double BlocConsciousness(World w, int bloc)
    {
        double sum = 0, size = 0;
        foreach (var p in w.AllPops())
        {
            int b = PolicyPreferences.BlocOf(p);
            if (b != bloc || p.IsSoldier) continue;
            sum += p.Consciousness * p.Size; size += p.Size;
        }
        return size > 1e-9 ? sum / size : 0;
    }
}
