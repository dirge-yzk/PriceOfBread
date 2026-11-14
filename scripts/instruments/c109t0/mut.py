import subprocess,sys,os
T='D:/PoB_work/c109t0_work/mut'
def rd(p): return open(os.path.join(T,p),'rb').read().decode('utf-8')
def wr(p,s): open(os.path.join(T,p),'wb').write(s.encode('utf-8'))
BS='PriceOfBread.SimCore/Instruments/InstrumentsBuyShadow.cs'
ES='PriceOfBread.SimCore/EconomyStep.cs'
def sub(p,a,b):
    s=rd(p); assert s.count(a)==1,(a,s.count(a)); wr(p,s.replace(a,b))
M={
'M1':(BS,'double price = r.Markets[s.GoodId].Price, pf;','double price = r.Markets[s.GoodId].Price * 1.0001, pf;','T1_'),
'M2':(ES,'''        target *= InventoryAdjust(region, s.GoodId, coef);
        double output = PoliticalQuantities.Toward(s.Output, target, coef.SupplyAdjustSpeed);''','''        double output = PoliticalQuantities.Toward(s.Output, target, coef.SupplyAdjustSpeed);
        output *= InventoryAdjust(region, s.GoodId, coef);''','T1_'),
'M3':(BS,'selfS += o * self * fx;','selfS += o * self;','T2_'),
'M4':(BS,'foreach (int g in st)\n                {\n                    if (g >= ng.Gc) continue;','foreach (int g in st.Skip(1))\n                {\n                    if (g >= ng.Gc) continue;','T3_'),
'M5':(BS,'if (p.Bankrupt || !(p.Capacity != 0.0) || EconomyStep.SleepState(r, p, c) == 1) return "";','if (p.Bankrupt || !(p.Capacity != 0.0)) return ""; if (EconomyStep.SleepState(r, p, c) == 1) return "0";','T4_'),
}
which=sys.argv[1:] or list(M)
out=open('D:/PoB_work/c109t0_work/mut_result.txt','a',encoding='utf-8')
for k in which:
    p,a,b,pre=M[k]
    sub(p,a,b)
    r=subprocess.run(['dotnet','test','PriceOfBread.Tests/PriceOfBread.Tests.csproj','-c','Release','--artifacts-path','D:/PoB_work/c109t0_mutart','--disable-build-servers','--filter','FullyQualifiedName~BuyShadowInstrumentTests'],cwd=T,capture_output=True,text=True,encoding='utf-8',errors='replace')
    lines=[l for l in r.stdout.splitlines() if ('成功!' in l or '失敗!' in l or '失敗 ' in l or 'error' in l.lower()) ]
    red=[l.strip()[:120] for l in r.stdout.splitlines() if l.strip().startswith('失敗 ') or 'Failed ' in l]
    out.write(f'{k} rc={r.returncode} '+' | '.join(lines[:3])+' RED:'+';'.join(red[:6])+'\n'); out.flush()
    subprocess.run(['git','checkout','--',p],cwd=T)
