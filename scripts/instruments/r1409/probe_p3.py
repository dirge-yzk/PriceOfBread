p='D:/PoB_work/r1409_work/wt_probe/PriceOfBread.Tests/Probe1409Tests.cs'
b=open(p,'rb').read().decode('utf-8')
i=b.index('    public void P2_'); j=b.index('            {', i)
new='''    public void P2_越境の対照() => Xb(Masks, [30], "probe_xb.tsv");
    [Fact]
    public void P3_越境の対照_pm() => Xb([0, 16, 31], [15], "probe_xb_pm15.tsv");

    static void Xb(int[] masks, int[] days, string file)
    {
        var lines = new System.Collections.Generic.List<string> { "mask\tdays\tseed\tcrossBorderOutK\tmigrationOutK\tnations40" };
        foreach (int d in days)
        foreach (int m in masks)
            for (ulong seed = 1789; seed <= 1796; seed++)
'''
b=b[:i]+new+b[j:]
b=b.replace('seed, _ => new UtilityAgent());','seed, _ => new UtilityAgent()); foreach (var nw in sim.Nations) nw.Coef.PriceOnlyStepDays = d;')
b=b.replace('lines.Add($"{m}\t{seed}','lines.Add($"{m}\t{d}\t{seed}')
b=b.replace('System.IO.File.WriteAllLines(Out + "probe_xb.tsv", lines);','System.IO.File.WriteAllLines(Out + file, lines);')
open(p,'wb').write(b.encode('utf-8')); print('ok', b.count('Out + file'), b.count('PriceOnlyStepDays = d'))
