# G4 の赤の診断（使い捨ての木で G4 の盤面の nationgood 表を写し出す・3 通り：旗 1＋W0′／旗 1＋W0／旗 0＋W0）
import subprocess, sys
T='D:/PoB_work/c129al_work/mut'; ART='D:/PoB_work/c129al_work/g4/art'; O='D:/PoB_work/c129al_work/g4'
TI=T+'/PriceOfBread.Tests/TradeInstrumentTests.cs'; P06=T+'/PriceOfBread.SimCore/Coefficients.p06_rank_discovery.cs'; P07=T+'/PriceOfBread.SimCore/Coefficients.p07_wiring_prewarm.cs'
a=b'            var t = Read(dir, "nationgood"); var ratios = new List<double>();'
ti=open(TI,'rb').read(); p6=open(P06,'rb').read(); p7=open(P07,'rb').read()
assert ti.count(a)==1
V={'f1w1':[],'f1w0':[(P06,b'= 4283.67;',b'= 7854.08;')],'f0w0':[(P06,b'= 4283.67;',b'= 7854.08;'),(P07,b'public double FoodProcessingNoEngineer = 1;',b'public double FoodProcessingNoEngineer = 0;')]}
try:
    for k,subs in V.items():
        open(P06,'wb').write(p6); open(P07,'wb').write(p7)
        for f,x,y in subs:
            c=open(f,'rb').read(); assert c.count(x)==1,(k,x); open(f,'wb').write(c.replace(x,y))
        open(TI,'wb').write(ti.replace(a,a+('\n            System.IO.File.Copy(System.IO.Path.Combine(dir, $"nationgood_s{Seed}.tsv"), "'+O+'/ng_'+k+'.tsv", true);').encode()))
        r=subprocess.run(['dotnet','test',T+'/PriceOfBread.Tests/PriceOfBread.Tests.csproj','-c','Release','--artifacts-path',ART,'--disable-build-servers','--filter','FullyQualifiedName~TradeInstrumentTests.G4'],capture_output=True,text=True,encoding='utf-8',errors='replace')
        print(k,r.returncode,[l.strip() for l in r.stdout.splitlines() if '合格:' in l or '中央値' in l][:3],flush=True)
finally:
    open(TI,'wb').write(ti); open(P06,'wb').write(p6); open(P07,'wb').write(p7)
