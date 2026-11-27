# (1417) c151fy 帰属の読み：dcont（国×四半期）を旗 0（base_ins・a11）と旗 1（c150sdB）で並べる。読むだけ。
import sys, csv, statistics as st
ARMS = {
 'flag0_M':  ('D:/PoB_work/keep_1411/base_ins', range(1,11)),
 'flag0_S11':('D:/PoB_work/keep_1411/a11', range(11,21)),
 'flag1_sdB':('D:/PoB_work/keep_1411_rej/c150sdB/ins', range(1,11)),
}
def f(x):
    try: return float(x)
    except: return float('nan')
def q(v, p):
    v = sorted(x for x in v if x == x)
    if not v: return float('nan')
    return v[min(len(v)-1, int(p*(len(v)-1)+0.5))]
for name,(d,seeds) in ARMS.items():
    rows=[]
    for s in seeds:
        try: fh=open(f'{d}/dcont_s{s}.tsv', newline='', encoding='utf-8')
        except FileNotFoundError: continue
        r=csv.DictReader(fh, delimiter='\t'); rows += [dict(x, seed=s) for x in r]
    n=len(rows)
    if n==0: print(name,'no rows'); continue
    G=lambda k:[f(x[k]) for x in rows]
    fac=G('factionsCount'); gate=G('facCohGateCount'); strong=G('facStrongCount'); rmax=G('strengthRatioMaxIndex')
    dis=G('discontentMeanShare'); mil=G('militancyMeanShare'); mass=G('massPopK'); den=G('partDenK')
    pNew=G('partNewK'); pMil=G('partMilK'); cNew=G('clusterCountNew'); cMil=G('clusterCountMil'); dgt=G('dGtMShare')
    mut=G('facCohMutCount')
    partD=[a*b for a,b in zip(dis,mass)]  # 大衆の層だけの D の参加（下限の近似）
    floor=[0.01*x for x in den]
    belowAll=sum(1 for a,b in zip(partD,floor) if a<b)
    print(f'== {name} rows {n}')
    print(f' factionsCount mean {st.mean(fac):.3f}  facCohMut(>=0.3) mean {st.mean(mut):.3f}  facCohGate(>=0.8) mean {st.mean(gate):.3f}  rows gate>0 {sum(1 for x in gate if x>0)}')
    print(f' rows strong>0 {sum(1 for x in strong if x>0)}  sum strong {sum(strong):.0f}')
    rg=[r for r,g in zip(rmax,gate) if g>0]
    print(f' ratioMax | gate>0 : n {len(rg)} p50 {q(rg,.5):.4g} p90 {q(rg,.9):.4g} p99 {q(rg,.99):.4g} max {max(rg) if rg else float("nan"):.4g}  P(strong|gate) {sum(1 for x in rg if x>1)/max(1,len(rg)):.3f}')
    print(f' D mean {st.mean(dis):.4f} p50 {q(dis,.5):.4f}   mil mean {st.mean(mil):.4f} p50 {q(mil,.5):.4f}   dGtM p50 {q(dgt,.5):.3f}')
    sm=lambda v:sum(x for x in v if x==x)
    print(f' sum partDen {sm(den):.4g}  partNew(max(D,mil)) {sm(pNew):.4g}  partMil {sm(pMil):.4g}  partD(mass) {sm(partD):.4g}')
    print(f' share of pop: partNew {sm(pNew)/sm(den):.4f} partMil {sm(pMil)/sm(den):.4f} partD {sm(partD)/sm(den):.4f}  partD/partNew {sm(partD)/sm(pNew):.4f}')
    print(f' rows where whole-nation partD < 1% floor: {belowAll/n:.3f}   rows partNew<floor {sum(1 for a,b in zip(pNew,floor) if a<b)/n:.3f}')
    print(f' clusterCountNew(max rule, floor) mean {st.mean(cNew):.3f}  clusterCountMil mean {st.mean(cMil):.3f}')
    rS=[a/b for a,b in zip(dis,mil) if b>1e-9 and a==a]
    print(f' rS=D/mil (row) p50 {q(rS,.5):.4f} p90 {q(rS,.9):.4f} mean {st.mean(rS):.4f}')
