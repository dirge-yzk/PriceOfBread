# (1270) c130rc 突然変異：使い捨ての木に 1 つずつ当てる（バイトで読み書き・旧がちょうど 1 回のときだけ）。使い方: mutate.py <木> <μ>
import sys
t,mu=sys.argv[1],sys.argv[2]
M='PriceOfBread.SimCore/MultiNationSim.cs'; T='PriceOfBread.Tests/RecruitVacancyTests.cs'
R={
 'mu0':[(T,'coef: new Coefficients { RecruitDestByVacancy = flag }','coef: new Coefficients { RecruitDestByVacancy = 0 }')],
 'muopp':[(M,'(vac ??= new()).Add((r, v)); vacSum += v; ow += v * o;','(vac ??= new()).Add((r, o)); vacSum += o; ow += v * o;')],
 'musign':[(M,'double v = r.Markets[Catalog.AdvLaborId].LastShortage;','double v = -r.Markets[Catalog.AdvLaborId].LastShortage;')],
 'mumax':[(M,'foreach (var (r, v) in vac) MoveSpecialists(Nations[bestS].World, dst, bestSrcR, r, bestHeads * (v / vacSum), bestCost);',
            '{ var mx = vac[0]; foreach (var e in vac) if (e.V > mx.V) mx = e; MoveSpecialists(Nations[bestS].World, dst, bestSrcR, mx.R, bestHeads, bestCost); }')],
 'muskip':[(M,'if (vac is null) continue;',
            'if (vac is null) { foreach (var r in dst.Regions) { double o = Territory.Opportunity(r, _coef); if (double.IsFinite(o) && (dstR is null || o > dstO)) { dstR = r; dstO = o; } } goto MUSKIP; }'),
           (M,'dstR = vac[0].R; dstO = ow / vacSum;','dstR = vac[0].R; dstO = ow / vacSum; MUSKIP:;')],
}
for f,o,n in R[mu]:
    p=t+'/'+f; b=open(p,'rb').read(); ob=o.encode(); nb=n.encode()
    assert b.count(ob)==1,(mu,f,b.count(ob))
    open(p,'wb').write(b.replace(ob,nb))
print('applied',mu)
