import sys,statistics as st
sys.path.insert(0,'.')
from an_g214 import *
d=sys.argv[1]; R=load(d)
hdr=None
for ln in open(d+'/s1.txt',encoding='utf-8'):
    if ln.startswith('#N'): hdr=ln[1:].rstrip('\n').split('|'); break
ix={k:i for i,k in enumerate(hdr)}
print(ix['wishUp'],ix['bloc'],ix['law(0不換1銀2金)'])
def pct(a,p): a=sorted(a); return a[int(p*(len(a)-1))]
for lo,hi,law in ((1783,1842,1),(1843,1872,1),(1873,1913,1),(1783,1842,2),(1873,1913,2)):
    v=[];vs=[]
    for seed,(W,O,N) in R.items():
        for c in N:
            y=int(c[2])
            if lo<=y<=hi and int(c[ix['law(0不換1銀2金)']])==law and float(c[ix['pop']])>0:
                x=float(c[ix['wishUp']]) if c[ix['wishUp']]!='nan' else None
                if x is not None: v.append(abs(x)); vs.append(x)
    if v: print(f"{lo}-{hi} law{law} n{len(v)} |stance| 中央{st.median(v):.3f} p75 {pct(v,.75):.3f} p90 {pct(v,.9):.3f} max {max(v):.2f}  上げ(>0)割合 {sum(1 for x in vs if x>0)/len(vs):.2f}  中央(符号付) {st.median(vs):.3f}")
print('--- 銀の国 1783-1842 の上げの立場が帯を超える国×四半期の割合')
for band in (0.35,0.675,1.0,2.0,3.0):
    for lo,hi in ((1783,1842),(1843,1872),(1873,1913)):
        tot=0;up=0;dn=0;nat=set()
        for seed,(W,O,N) in R.items():
            for c in N:
                y=int(c[2])
                if lo<=y<=hi and int(c[ix['law(0不換1銀2金)']])==1 and float(c[ix['pop']])>0 and c[ix['wishUp']]!='nan':
                    x=float(c[ix['wishUp']]); tot+=1
                    if x>band: up+=1; nat.add((seed,c[4]))
                    if -x>band: dn+=1
        print(f"帯{band} {lo}-{hi}: 上げ {up/tot:.3f}  下げ {dn/tot:.3f}  上げが一度でも立った国×種 {len(nat)}")
