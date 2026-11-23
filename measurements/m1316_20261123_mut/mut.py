import sys,subprocess,os
R='D:/PoB_work/wt_c137sm/'
muts={
 'M1':('PriceOfBread.Tests/SaveRoundTripTests.cs','for (int d = 0; d < 45; d++) sim1.StepDay();','for (int d = 0; d < 0; d++) sim1.StepDay();','SaveRoundTripTests'),
 'M2':('PriceOfBread.SimCore/Discovery.cs','            if (!spread && rp is null) continue;\n','','DiscoveryEngineerTests'),
 'M3':('PriceOfBread.SimCore/Instruments/InstrumentsUrx.cs','double rho = rhoW > 0 ? rhoSum / rhoW : 0.0;','double rho = 1.0;','EmptyUrbanValueTests'),
 'M4':('PriceOfBread.SimCore/World.cs','            if (Instruments.On) Instruments.PopKilled(p, d, violent);   // (1316) popclose_s の死の列（読むだけ）\n','','PopCloseInstrumentTests'),
 'M5':('PriceOfBread.SimCore/SaveGame.cs','else if (!order) { res.OrderOnly++;','else if (!order) { res.Values++; res.OrderOnly++;','SaveRoundTripTests'),
}
k=sys.argv[1]; f,old,new,flt=muts[k]
p=R+f; b=open(p,'rb').read().decode('utf-8'); assert b.count(old)==1,k
open(p,'wb').write(b.replace(old,new).encode('utf-8'))
try:
    r=subprocess.run(['dotnet','test',R+'PriceOfBread.Tests/PriceOfBread.Tests.csproj','-c','Release','--artifacts-path','D:/PoB_work/c137sm_art','--filter','FullyQualifiedName~'+flt],capture_output=True,text=True,encoding='utf-8')
    out=r.stdout
    fails=[l.strip() for l in out.splitlines() if l.strip().startswith('失敗 ')]
    tail=[l for l in out.splitlines() if '合格' in l][-1:]
    open('D:/PoB_work/c137sm_work/mut/%s.txt'%k,'w',encoding='utf-8').write('\n'.join(fails+tail)+'\n')
finally:
    open(p,'wb').write(b.encode('utf-8'))
