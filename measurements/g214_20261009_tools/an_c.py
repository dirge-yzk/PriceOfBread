import sys,statistics as st
sys.path.insert(0,'.')
from an_g214 import *
def hdrix(d):
    for ln in open(d+'/s1.txt',encoding='utf-8'):
        if ln.startswith('#N'): return {k:i for i,k in enumerate(ln[1:].rstrip('\n').split('|'))}
S1=load('../out_S1'); P0=load('../out_P0'); S0=load('../out_S0')
ix=hdrix('../out_S1')
for seed,(W,O,N) in S1.items():
    op=[int(o[2]) for o in O if o[5]=='銀']
    y0=op[0] if op else 1850
    def med(R,lo,hi):
        v=[float(c[ix['expInfl']]) for c in R[seed][2] if lo<=int(c[2])<=hi and int(c[ix['law(0不換1銀2金)']])==1 and float(c[ix['pop']])>0 and c[ix['expInfl']]!='nan']
        return st.median(v) if v else float('nan')
    print(f"seed{seed} 開いた{y0}  洪水の20年 {y0}-{y0+20}: S1 銀の国の期待インフレ中央 {med(S1,y0,y0+20):+.4f}  P0 {med(P0,y0,y0+20):+.4f} | 1850-1870: S1 {med(S1,1850,1870):+.4f} P0 {med(P0,1850,1870):+.4f}")
