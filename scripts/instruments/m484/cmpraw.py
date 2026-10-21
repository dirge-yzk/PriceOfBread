import sys,os
from collections import Counter
A,B=sys.argv[1],sys.argv[2]
LABOR_EXTRA={'stepBelowShare','floorEndOverStepIndex','priceEndOverStepIndex','belowNoStepShare','belowPriceMovedShare','belowFloorMovedShare'}
def rows(p,drop):
    L=[x.rstrip(b'\r') for x in open(p,'rb').read().split(b'\n')]
    h=L[0].decode().split('\t'); keep=[i for i,c in enumerate(h) if c not in drop]
    out=[]
    for r in L:
        f=r.split(b'\t')
        out.append(b'\t'.join(f[i] for i in keep if i<len(f)))
    return out
fa=sorted(os.listdir(A)); fb=sorted(os.listdir(B))
same=diff=0; notes=[]
for f in sorted(set(fa)|set(fb)):
    if f not in fa or f not in fb: notes.append(('only',f, f in fa)); continue
    pa,pb=os.path.join(A,f),os.path.join(B,f)
    if open(pa,'rb').read()==open(pb,'rb').read(): same+=1; continue
    base=f.split('_s')[0]
    diff+=1
    if base in ('burscan','clock'): notes.append(('wallclock',f)); continue
    if base=='flow': ok=rows(pa,{'sourceLine'})==rows(pb,{'sourceLine'})
    elif base=='labor': ok=rows(pa,LABOR_EXTRA)==rows(pb,LABOR_EXTRA)
    else: ok=False
    notes.append(('colsok' if ok else 'DIFF',f))
print('same',same,'differ',diff)
print(Counter((n[0], n[1].split('_s')[0]) for n in notes))
for n in notes:
    if n[0] in ('DIFF','only'): print(n)
