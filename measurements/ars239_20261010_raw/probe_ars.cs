if (args.Length > 0 && args[0] == "arsprobe")
{
    int spQ = ArgInt(args, 1, 4);
    var spm = new MultiNationSim(WorldData.Nations(), 1789UL, _ => new UtilityAgent());
    foreach (var nn0 in spm.Nations) nn0.TelemetryEnabled = false;
    spm.Diplomacy.Log.Enabled = false;
    static string NmOf(World w) { var p = w.SelfPower; return p is null ? w.Region.Name : (p.BaseName.Length > 0 ? p.BaseName : p.Name); }
    var coef = new Coefficients();
    int[] gs = [Catalog.RifleId, Catalog.AmmunitionId, Catalog.ArtilleryId, Catalog.WarshipId];
    void Dump(string tag)
    {
        Console.WriteLine($"## {tag}");
        Console.WriteLine("nation\tpopCap\testab\tsoldiers\tready\tequipfill\tmilscale\tstkRifle\tstkAmmo\tstkArt\tstkShip\tmoRifle\tmoAmmo\tmoArt\tmoShip\tmorale\theldRifle\theldArt\tfleet");
        foreach (var n in spm.Nations)
        {
            var w = n.World; if (w.SelfPower is null) continue;
            var cap = w.Regions[0];
            double sol = w.SoldierSize();
            double nm0 = 0; foreach (var rr in w.Regions) if (rr.State == RegionState.Active) nm0 += Military.RegionManpower(rr, coef);
            double est = Military.EstablishmentOf(w, coef, out double nm1);
            var sb = new System.Text.StringBuilder();
            sb.Append($"{NmOf(w)}\t{coef.MaxSoldierFraction * nm0:F2}\t{est:F2}\t{sol:F2}\t{w.MilitaryReadiness:F3}\t{cap.MilitaryEquipFill:F3}\t{cap.MilitaryScale:F2}");
            foreach (int g in gs) sb.Append($"\t{cap.Markets[g].Stock:F1}");
            for (int b = 0; b < 4; b++) sb.Append($"\t{coef.MilitaryUpkeepScale * sol * coef.MilitaryRates[b]:F2}");
            double mor = PowerShares.SoldierWelfare(w);
            double hr = double.NaN, ha = double.NaN;
            if (cap.ArmsStock is { } st)
            {
                double tr = cap.MilitaryScalePeace * coef.MilitaryRates[0] / coef.MilitaryWearRates[0];
                double ta = cap.MilitaryScalePeace * coef.MilitaryRates[2] / coef.MilitaryWearRates[2];
                if (st.TryGetValue(Catalog.RifleId, out double vr) && tr > 1e-9) hr = vr / tr;
                if (st.TryGetValue(Catalog.ArtilleryId, out double va) && ta > 1e-9) ha = va / ta;
            }
            double fl = 0; if (cap.FleetUnits is { } fu) foreach (var u in fu) fl += u;
            sb.Append($"\t{mor:F3}\t{hr:F3}\t{ha:F3}\t{fl:F2}");
            Console.WriteLine(sb.ToString());
        }
    }
    Dump("construct");
    spm.StartWarmup();
    Dump("afterwarm");
    for (int q = 1; q <= spQ; q++)
    {
        for (int d = 0; d < Calendar.DaysPerQuarter; d++) spm.StepDay();
        if (q == 1 || q == 4 || q % 20 == 0 || q == spQ) Dump($"Q{q}");
    }
    return;
}
