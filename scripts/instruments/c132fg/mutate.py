# (1283) c132fg 突然変異：使い捨ての木に 1 つずつ当てる（バイトで読み書き・旧がちょうど 1 回のとき〔mu0 は 4 回〕だけ）。使い方: mutate.py <木> <μ>
import sys
t,mu=sys.argv[1],sys.argv[2]
P='PriceOfBread.SimCore/PopDecision.cs'; U='PriceOfBread.SimCore/Instruments/InstrumentsUrx.cs'; T='PriceOfBread.Tests/FamineGateReturnsRuralTests.cs'
GATE='        if (flow > 0 && urban.FoodFill < coef.FamineMigrationFillGate && urban.FoodFill < rural.FoodFill)\n        {\n            flow = 0;\n'
R={
 'mu0':[(T,'c.FamineGateReturnsRural = 1;','c.FamineGateReturnsRural = 0;',3),(T,'c1.FamineGateReturnsRural = 1;','c1.FamineGateReturnsRural = 0;',1)],
 'musign':[(P,'if (g < 0) flow = coef.Mobility * g * urban.Size;','if (g > 0) flow = coef.Mobility * g * urban.Size;',1)],
 'mugate':[(P,GATE,'        if (flow > 0 && coef.FamineGateReturnsRural > 0 && MigrationValues(region, coef, rural, urban).Gsym < 0) flow = coef.Mobility * MigrationValues(region, coef, rural, urban).Gsym * urban.Size;\n'+GATE,1)],
 'musize':[(P,'flow = coef.Mobility * g * urban.Size;   // (1283)','flow = coef.Mobility * g * rural.Size;   // (1283)',1)],
 'muoff':[(P,'            if (coef.FamineGateReturnsRural > 0)\n            {','            if (true)\n            {',1)],
 'muinst':[(U,' if (coef.FamineGateReturnsRural > 0 && mv.Gsym < 0) flow = coef.Mobility * mv.Gsym * urban.Size; }',' }',1)],
}
for f,o,n,k in R[mu]:
    p=t+'/'+f; b=open(p,'rb').read(); ob=o.encode('utf-8'); nb=n.encode('utf-8')
    assert b.count(ob)==k,(mu,f,b.count(ob))
    open(p,'wb').write(b.replace(ob,nb))
print('applied',mu)
