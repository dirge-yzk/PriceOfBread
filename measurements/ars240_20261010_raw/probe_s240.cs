if (args.Length > 0 && args[0] == "s240probe")
{
    int spQ = ArgInt(args, 1, 100);
    var spm = new MultiNationSim(WorldData.Nations(), 1789UL, _ => new UtilityAgent());
    foreach (var nn0 in spm.Nations) nn0.TelemetryEnabled = false;
    spm.Diplomacy.Log.Enabled = false;
    static string NmOf(World w) { var p = w.SelfPower; return p is null ? w.Region.Name : (p.BaseName.Length > 0 ? p.BaseName : p.Name); }
    var coef = new Coefficients();
    int[] gs = [Catalog.RifleId, Catalog.AmmunitionId, Catalog.ArtilleryId, Catalog.WarshipId];
    var foodIds = new HashSet<int>(); foreach (var d in Catalog.Goods) if (d.Nutrition > 0) foodIds.Add(d.Id);
    void Dump(string tag)
    {
        Console.WriteLine($"## {tag}");
        Console.WriteLine("nation\tpopK\tgdpY\tstockVal\tstockPerGdp\tfoodVal\tfoodPerGdp\tmilVal\tready\tequipfill\tmorale\tstkRifle\tstkAmmo\tstkArt\tstkShip\tshipPrice\tsoldiers\tregions");
        foreach (var n in spm.Nations)
        {
            var w = n.World; if (w.SelfPower is null) continue;
            var cap = w.Regions[0];
            double pop = w.TotalPopulation, sol = w.SoldierSize();
            double gdpY = System.Math.Max(1e-9, w.NominalGdp * 12.0);
            double sv = 0, fv = 0, mv = 0;
            foreach (var r in w.Regions)
            {
                if (r.State != RegionState.Active) continue;
                for (int g = 0; g < r.Markets.Length; g++)
                {
                    var m = r.Markets[g]; if (m is null) continue;
                    double v = System.Math.Max(0, m.Stock) * System.Math.Max(0, m.Price);
                    sv += v;
                    if (foodIds.Contains(g)) fv += v;
                    if (g == Catalog.RifleId || g == Catalog.AmmunitionId || g == Catalog.ArtilleryId || g == Catalog.WarshipId) mv += v;
                }
            }
            double shipRatio = cap.Markets[Catalog.WarshipId].Price / Catalog.RefPrices[Catalog.WarshipId];
            double mor = PowerShares.SoldierWelfare(w);
            var sb = new System.Text.StringBuilder();
            sb.Append($"{NmOf(w)}\t{pop:F1}\t{gdpY:F0}\t{sv:F0}\t{(w.NominalGdp > 1e-9 ? sv / gdpY : double.NaN):F3}\t{fv:F0}\t{(w.NominalGdp > 1e-9 ? fv / gdpY : double.NaN):F3}\t{mv:F0}\t{w.MilitaryReadiness:F3}\t{cap.MilitaryEquipFill:F3}\t{mor:F3}");
            foreach (int g in gs) sb.Append($"\t{cap.Markets[g].Stock:F1}");
            sb.Append($"\t{shipRatio:F2}\t{sol:F2}\t{w.Regions.Count}");
            Console.WriteLine(sb.ToString());
        }
    }
    Dump("construct");
    spm.StartWarmup();
    Dump("afterwarm");
    for (int q = 1; q <= spQ; q++)
    {
        for (int d = 0; d < Calendar.DaysPerQuarter; d++) spm.StepDay();
        if (q <= 4 || q == 20 || q == 40 || q == spQ) Dump($"Q{q}");
    }
    return;
}
