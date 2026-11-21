# (1278) c131cr 突然変異：使い捨ての木に 1 つずつ当てる（バイトで読み書き・旧がちょうど 1 回のときだけ）。使い方: mutate.py <木> <μ>
import sys
t,mu=sys.argv[1],sys.argv[2]
S='PriceOfBread.SimCore/Simulation.cs'; D='PriceOfBread.SimCore/Diplomacy.cs'; P='PriceOfBread.SimCore/places.json'; T='PriceOfBread.Tests/ColonyLabelsFromDataTests.cs'
R={
 'mu0':[(T,'new Coefficients { ColonyLabelsFromData = flag }','new Coefficients { ColonyLabelsFromData = 0 }'),(T,'n.World.Coef.ColonyLabelsFromData = flag;','n.World.Coef.ColonyLabelsFromData = 0;')],
 'mucap':[(S,'? Places.CultureOf(col.Name) ?? "" : w.Region.Culture,','? w.Region.Culture : w.Region.Culture,'),
          (S,'? Places.ReligionOf(col.Name) ?? "" : w.Region.Religion,','? w.Region.Religion : w.Region.Religion,')],
 'mufall':[(S,'? Places.CultureOf(col.Name) ?? "" : w.Region.Culture,','? Places.CultureOf(col.Name) ?? w.Region.Culture : w.Region.Culture,'),
           (S,'? Places.ReligionOf(col.Name) ?? "" : w.Region.Religion,','? Places.ReligionOf(col.Name) ?? w.Region.Religion : w.Region.Religion,')],
 'mudata':[(P,', "culture": "モンゴ", "religion": "土着信仰"','')],
 'mufilt':[(D,'            if (homeOnly && r.Tier == RegionTier.Colonial) continue;\r\n','')],
 'mufall2':[(D,'            if (homeOnly && r.Tier == RegionTier.Colonial) continue;\r\n','            if (homeOnly && r.Tier != RegionTier.Core) continue;\r\n')],
}
for f,o,n in R[mu]:
    p=t+'/'+f; b=open(p,'rb').read(); ob=o.encode('utf-8'); nb=n.encode('utf-8')
    assert b.count(ob)==1,(mu,f,b.count(ob))
    open(p,'wb').write(b.replace(ob,nb))
print('applied',mu)
