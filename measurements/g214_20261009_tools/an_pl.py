import sys,statistics as st
sys.path.insert(0,'.')
from an_g214 import *
def hdrix(d):
    for ln in open(d+'/s1.txt',encoding='utf-8'):
        if ln.startswith('#N'): return {k:i for i,k in enumerate(ln[1:].rstrip('\n').split('|'))}
ix=hdrix('../out_S1')
arms={'P0':load('../out_P0'),'S1':load('../out_S1')}
def val(N,nm,yr,col):
    r=[c for c in N if c[4]==nm and int(c[2])==yr]
    if not r: return float('nan')
    return float(r[-1][ix[col]])
def res(W,yr):
    qs=[q for q,c in W.items() if int(c[2])==yr]
    return f(W[max(qs)][29]) if qs else float('nan')
for seed in (1,3,4):
    print('seed',seed)
    for yr in (1823,1843,1873,1893,1913):
        s=f"  {yr} 銀の準備(世界) P0 {res(arms['P0'][seed][0],yr):.3g} S1 {res(arms['S1'][seed][0],yr):.3g} |"
        for nm in ('アメリカ','清'):
            for a in ('P0','S1'):
                N=arms[a][seed][2]
                s+=f" {nm[:2]}{a} PL{val(N,nm,yr,'PL'):.3g} 名目GDP{val(N,nm,yr,'nomGdp'):.3g} 法{int(val(N,nm,yr,'law(0不換1銀2金)'))} E{val(N,nm,yr,'E'):.2f};"
        print(s)
