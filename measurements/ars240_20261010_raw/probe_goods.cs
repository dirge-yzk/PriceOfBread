if (args.Length > 0 && args[0] == "goodsprobe")
{
    int spQ = ArgInt(args, 1, 4);
    var spm = new MultiNationSim(WorldData.Nations(), 1789UL, _ => new UtilityAgent());
    foreach (var nn0 in spm.Nations) nn0.TelemetryEnabled = false;
    spm.Diplomacy.Log.Enabled = false;
    static string NmOf(World w) { var p = w.SelfPower; return p is null ? w.Region.Name : (p.BaseName.Length > 0 ? p.BaseName : p.Name); }
    void Dump(string tag)
    {
        Console.WriteLine($"## {tag}");
        foreach (var n in spm.Nations)
        {
            var w = n.World; if (w.SelfPower is null) continue;
            string nm = NmOf(w);
            if (nm != "清" && nm != "フランス" && nm != "イギリス") continue;
            Console.WriteLine($"# {nm} gdpY {w.NominalGdp * 12.0:F0}");
            foreach (var d in Catalog.Goods)
            {
                double st = 0, vl = 0, pr = 0; int k = 0;
                foreach (var r in w.Regions) { if (r.State != RegionState.Active) continue; var m = r.Markets[d.Id]; if (m is null) continue; st += m.Stock; vl += m.Stock * m.Price; pr += m.Price / Catalog.RefPrices[d.Id]; k++; }
                Console.WriteLine($"{nm}\t{d.Id}\t{d.Name}\t{st:F1}\t{vl:F0}\t{(k > 0 ? pr / k : 0):F2}");
            }
        }
    }
    spm.StartWarmup();
    Dump("afterwarm");
    for (int q = 1; q <= spQ; q++)
    {
        for (int d = 0; d < Calendar.DaysPerQuarter; d++) spm.StepDay();
        if (q == 4) Dump($"Q{q}");
    }
    return;
}
