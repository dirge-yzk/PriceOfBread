import sys,subprocess,os
R='D:/PoB_work/c146kt_work/mut/'
def rd(p): return open(R+p,'rb').read().decode('utf-8')
def wr(p,s): open(R+p,'wb').write(s.encode('utf-8'))
def sub(p,a,b,count=1):
    s=rd(p); assert s.count(a)>=1,(p,a); wr(p,s.replace(a,b,count))
BS=chr(92)
muts={
 'mu1':('PriceOfBread.SimCore/Simulation.cs','World.Coef.FemalePartBase;   // 構造的参加率','World.Coef.FemalePartBase + (World.Coef.FemalePartProperty - World.Coef.FemalePartBase) * World.WomenWageRetention;   // 構造的参加率','FemalePartLawFreeTests'),
 'mu3':('PriceOfBread.SimCore/LaborSupply.cs','c.FemaleNeedWeight * Need(supplyFill) * r.WomenWageRetention','c.FemaleNeedWeight * Need(supplyFill)','FemalePartLawFreeTests'),
 'm1b':('PriceOfBread.SimCore/Model.cs','public sealed class Pop\n{','public sealed class Pop\n{\n    void ZZ() { Militancy = 0; }','DerivedValueRatchetTests'),
 'pf':('PriceOfBread.SimCore/Politics.cs','PoliticalQuantities.Clamp01(0.5 + dynPerf)','PoliticalQuantities.Clamp01(0.6 + dynPerf)','DerivedValueRatchetTests'),
 'l3':('PriceOfBread.SimCore/Politics.cs','        Instruments.LawstepSideBegin(w);   // (1372) 計器 lawstep_s（On は関数の中で読む）\n','','LawstepInstrumentTests'),
 'l3e':('PriceOfBread.SimCore/Politics.cs','        Instruments.LawstepSideEnd(w, 4);   // (1372)\n','','LawstepInstrumentTests'),
 'l5':('PriceOfBread.Tests/LawstepInstrumentTests.cs','            // 切る：切っている間に段を動かして施行を通しても、行は溜まらない（On に戻して四半期の末を書いても出ない）\n            Instruments.Configure(null, null); Instruments.SelectTables(null);\n','','LawstepInstrumentTests'),
 'bare':('PriceOfBread.Tests/ValueScan.cs','        var d = Regex.Match(t, $@"'+BS+'b(class|struct)'+BS+'s+{type}'+BS+'b[^{{;]*'+BS+'{{");\n        if (!d.Success) return (t, new List<Match>());','        if (true) return (t, new List<Match>());\n        var d = Regex.Match(t, $@"x");','DerivedValueRatchetTests'),
 'defdrop':('PriceOfBread.Tests/ValueScan.cs','if (last != null) lines.Add("D:"','if (false) lines.Add("D:"','DerivedValueRatchetTests'),
}
k=sys.argv[1]
p,a,b,flt=muts[k]
sub(p,a,b)
r=subprocess.run(['dotnet','test',R+'PriceOfBread.Tests/PriceOfBread.Tests.csproj','-c','Release','--artifacts-path','D:/PoB_work/c146kt_art_mut','--filter','FullyQualifiedName~'+flt],capture_output=True,text=True,encoding='utf-8',errors='replace')
out=r.stdout
import re
print(k,[l for l in out.splitlines() if '失敗!' in l or '成功!' in l or 'error CS' in l][:3])
print([l.strip() for l in out.splitlines() if l.strip().startswith('失敗 ')][:6])
