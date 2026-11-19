# (1242) c126al 突然変異 μ0（変えない）〜μ5：使い捨ての木で 1 つずつ当て、試験を回し、元へ戻す。
import subprocess, sys, re, pathlib
T='D:/PoB_work/c126al_work/mut'; ART='D:/PoB_work/c126al_work/mut_art'
SIM=T+'/PriceOfBread.SimCore/Simulation.cs'; CAT=T+'/PriceOfBread.SimCore/Catalog.cs'
ENUM=b'id is FlourId or BreadId or MeatId or PreservedMeatId or DriedFishId or BeerId or SpiritsId or CheeseId or ChocolateId;'
M={
 'mu0':[],
 'mu1':[(SIM,b'coef.FoodProcessingNoEngineer > 0 && Catalog.IsFoodProcessingGood(d.Id)',b'Catalog.IsFoodProcessingGood(d.Id)')],
 'mu2':[(CAT,b'        _ => 0.02,',b'        _ => 0.0,')],
 'mu3':[(CAT,ENUM,ENUM.replace(b'FlourId or ',b''))],
 'mu4':[(CAT,ENUM,ENUM.replace(b'ChocolateId;',b'ChocolateId or SugarId;'))],
 'mu5':[(CAT,b'public static bool IsFoodProcessingGood(int id) => '+ENUM,b'public static bool IsFoodProcessingGood(int id) => id >= 0;')],
}
out=open('D:/PoB_work/c126al_work/mutsrc/mut_result.txt','w',encoding='utf-8')
for k,subs in M.items():
    orig={}
    for f,a,b in subs:
        d=orig.setdefault(f,open(f,'rb').read()); cur=open(f,'rb').read()
        assert cur.count(a)==1,(k,a); open(f,'wb').write(cur.replace(a,b))
    r=subprocess.run(['dotnet','test',T+'/PriceOfBread.Tests/PriceOfBread.Tests.csproj','-c','Release','--artifacts-path',ART,
       '--filter','FullyQualifiedName~FoodProcessingNoEngineerTests|FullyQualifiedName~ServiceEconomyTests.複数レシピ'],capture_output=True,text=True,encoding='utf-8',errors='replace')
    fails=sorted(set(re.findall(r'^\s+失敗 (\S+)',r.stdout,re.M)))
    summ=[l.strip() for l in r.stdout.splitlines() if '合格:' in l]
    out.write(f'== {k} rc={r.returncode} {summ}\n'); [out.write(f'  赤 {x}\n') for x in fails]
    if r.returncode!=0 and not fails: out.write(r.stdout[-1500:]+'\n')
    out.flush()
    for f,d in orig.items(): open(f,'wb').write(d)
out.close()
