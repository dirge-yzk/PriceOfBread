if (args.Length > 0 && args[0] == "gdpprobe")
{
    var spm = new MultiNationSim(WorldData.Nations(), 1789UL, _ => new UtilityAgent());
    foreach (var nn0 in spm.Nations) nn0.TelemetryEnabled = false;
    spm.Diplomacy.Log.Enabled = false;
    spm.StartWarmup();
    int i = 0;
    foreach (var n in spm.Nations) { var w = n.World; if (w.SelfPower is null) continue; Console.WriteLine($"{i++}\t{w.NominalGdp * 12.0:F0}"); }
    return;
}
