import subprocess,sys,io
R='D:/PoB_work/c122cr_work/mut/'
T=chr(9)
def edit(p,o,n,crlf=False):
    b=open(R+p,'rb').read().decode('utf-8')
    assert b.count(o)==1,(p,o,b.count(o))
    b=b.replace(o,n); open(R+p,'wb').write(b.encode('utf-8'))
def run(cmd):
    return subprocess.run(cmd,shell=True,cwd=R,capture_output=True,text=True,encoding='utf-8',errors='replace')
muts=[
 ('M1','PriceOfBread.SimCore/Politics.cs','(1 + c.CultureSecessionWeight * cultDist)','(1 + 0.0 * cultDist)','CaInstruments1206Tests|FullyQualifiedName~SecessionTests'),
 ('M2','PriceOfBread.SimCore/Instruments/InstrumentsSatNeeds.cs','internal static int SatNeedsCat(GoodTier t) => t == GoodTier.Daily ? 0 : t == GoodTier.Luxury ? 1 : 2;','internal static int SatNeedsCat(GoodTier t) => t == GoodTier.Daily ? 1 : t == GoodTier.Luxury ? 0 : 2;','CaInstruments1206Tests'),
 ('M3','PriceOfBread.SimCore/Validation.cs','if (worst is null) foreach (var n in Nations) if (worst is null || JudgedSat(n) < JudgedSat(worst)) worst = n;','','HarnessSatRecentTests'),
 ('M4','PriceOfBread.SimCore/Instruments/InstrumentsRulePower.cs','double fR = secessionThreshold > 0 ? P(n) * P(pressure) / secessionThreshold : 0.0;','double fR = secessionThreshold > 0 ? P(n) * P(pressure) / secessionThreshold + 0.5 : 0.5;','RulePowerTests'),
 ('M5','PriceOfBread.SimCore/Instruments/InstrumentsWar.cs','war.CivilWarPolity.ToString() + "'+T+'" + (war.CivilWarParent.Nation?.Polity.ToString() ?? "-")','(war.CivilWarParent.Nation?.Polity.ToString() ?? "-") + "'+T+'" + war.CivilWarPolity.ToString()','CaInstruments1206Tests'),
]
out=open('D:/PoB_work/wt_c122cr/measurements/m1214_20261118_mut/mut_results.txt','w',encoding='utf-8')
for name,p,o,n,flt in muts:
    if o is None: 
        out.write(f'{name} 未実施（手で）\n'); continue
    edit(p,o,n)
    b=run('dotnet build PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path D:/PoB_work/c122cr_mutart -v q')
    t=run(f'dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path D:/PoB_work/c122cr_mutart --no-build --filter "FullyQualifiedName~{flt}"')
    lines=[l for l in t.stdout.splitlines() if ('失敗!' in l or '成功!' in l or l.strip().startswith('失敗 ') or 'Failed ' in l)]
    out.write(f'{name} {p}\n  build-err={"error" in b.stdout.lower()}\n  '+'\n  '.join(lines[:8])+'\n'); out.flush()
    run('git checkout -- .')
out.close()
