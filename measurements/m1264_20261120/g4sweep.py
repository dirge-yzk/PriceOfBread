# (1264) G4 の盤面の種の選び直し（追記 1 の規則）：1790 から順に、旗 0＋W0 と旗 1＋W0′ の両方で n≧80・中央値 0.3〜3・10 倍以内 7 割以上の最初の種
import subprocess, re, sys
T='D:/PoB_work/c129al_work/mut'; ART='D:/PoB_work/c129al_work/g4/art'; OUT=sys.argv[1]
TI=T+'/PriceOfBread.Tests/TradeInstrumentTests.cs'; P06=T+'/PriceOfBread.SimCore/Coefficients.p06_rank_discovery.cs'; P07=T+'/PriceOfBread.SimCore/Coefficients.p07_wiring_prewarm.cs'
ti=open(TI,'rb').read(); p6=open(P06,'rb').read(); p7=open(P07,'rb').read()
def rep(s,a,b):
    assert s.count(a)==1,a; return s.replace(a,b)
t2=rep(ti,b'private static MultiNationSim Make(bool still = false) => new([NationConfig.France(), NationConfig.Prussia(), NationConfig.Britain(), NationConfig.Austria()], 1789UL,',
          b'private static MultiNationSim Make(bool still = false, ulong seed = 1789UL) => new([NationConfig.France(), NationConfig.Prussia(), NationConfig.Britain(), NationConfig.Austria()], seed,')
t2=rep(t2,b'bool instrumentsOn = true, bool still = false, int every = 1)\n',b'bool instrumentsOn = true, bool still = false, int every = 1, ulong seed = 1789UL)\n')
t2=rep(t2,b'        var sim = Make(still);\n        foreach (var n in sim.Nations) n.World.Coef.CivilWarLandslideRatio = 0;',b'        var sim = Make(still, seed);\n        foreach (var n in sim.Nations) n.World.Coef.CivilWarLandslideRatio = 0;')
G=b'    public void G4_\xe8\xb6\x8a\xe5\xa2\x83'
i=t2.index(G); j=t2.index(b'var (dir, sim, restore) = Run(24);',i)
t2=t2[:j]+b'var (dir, sim, restore) = Run(24, seed: SWEEPSEEDUL);'+t2[j+len(b'var (dir, sim, restore) = Run(24);'):]
A=b'Assert.True(ratios[n / 2] > 0.3 && ratios[n / 2] < 3, "'
t2=rep(t2,A,b'System.Console.WriteLine("G4MED " + ratios[n / 2] + " n " + n + " in10 " + (double)ratios.Count(x => x > 0.1 && x < 10) / n); '+A)
V={'f0w0':[(P06,b'= 4283.67;',b'= 7854.08;'),(P07,b'public double FoodProcessingNoEngineer = 1;',b'public double FoodProcessingNoEngineer = 0;')],'f1w1':[]}
out=open(OUT,'w',encoding='utf-8',newline='\n')
out.write('# 種\t盤面\t中央値\tn\t10倍以内の割合\t試験の終了コード\n')
try:
    for sd in range(1790,1810):
        ok=True
        for k,subs in V.items():
            open(P06,'wb').write(p6); open(P07,'wb').write(p7)
            for f,x,y in subs:
                c=open(f,'rb').read(); open(f,'wb').write(rep(c,x,y))
            open(TI,'wb').write(t2.replace(b'SWEEPSEED',str(sd).encode()))
            r=subprocess.run(['dotnet','test',T+'/PriceOfBread.Tests/PriceOfBread.Tests.csproj','-c','Release','--artifacts-path',ART,'--disable-build-servers','--filter','FullyQualifiedName~TradeInstrumentTests.G4','--logger','console;verbosity=detailed'],capture_output=True,text=True,encoding='utf-8',errors='replace')
            m=re.findall(r'G4MED (\S+) n (\d+) in10 (\S+)',r.stdout)
            if m:
                med,n,f=float(m[0][0]),int(m[0][1]),float(m[0][2]); good=n>=80 and 0.3<med<3 and f>=0.7
                out.write(f'{sd}\t{k}\t{med:.4f}\t{n}\t{f:.3f}\t{r.returncode}\n')
            else:
                good=False; out.write(f'{sd}\t{k}\t読めない\t\t\t{r.returncode}\n')
            out.flush(); ok=ok and good
            if not ok: break
        if ok:
            out.write(f'採る種 {sd}\n'); break
finally:
    open(TI,'wb').write(ti); open(P06,'wb').write(p6); open(P07,'wb').write(p7); out.close()
