import sys
sys.path.insert(0,'.')
from an_g214 import *
d=sys.argv[1]; R=load(d)
nm=sys.argv[2]
for seed,(W,O,N) in R.items():
    first={}
    laws={}
    for c in N:
        if c[4]!=nm: continue
        yr=int(c[2]); law=int(c[5])
        laws.setdefault(yr,law)
    ch=[(y,l) for y,l in sorted(laws.items())]
    trans=[]; prev=None
    for y,l in ch:
        if l!=prev: trans.append((y,l)); prev=l
    # silver output peak year
    pk=max(W.items(), key=lambda kv: f(kv[1][19]))
    print('seed',seed,nm,'law transitions',trans[:8],'| Ag peak',pk[1][2],f(pk[1][19]))
