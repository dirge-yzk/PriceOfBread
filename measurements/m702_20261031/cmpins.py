import os,sys,hashlib
A,B=sys.argv[1],sys.argv[2]
fa=set(os.listdir(A)); fb=set(os.listdir(B))
print('only A',sorted(x.split('_s')[0] for x in fa-fb).__len__(), sorted(set(x.split('_s')[0] for x in fa-fb)),'only B',sorted(fb-fa))
same=diff=0; dl=[]
for f in sorted(fa&fb):
    a=open(os.path.join(A,f),'rb').read(); b=open(os.path.join(B,f),'rb').read()
    if a==b: same+=1; continue
    if f.startswith('region_s'):
        la=a.replace(b'\r',b'').split(b'\n'); lb=b.replace(b'\r',b'').split(b'\n')
        hb=lb[0].split(b'\t'); n=len(hb)
        ok=len(la)==len(lb) and all(x.split(b'\t')[:n]==y.split(b'\t') for x,y in zip(la,lb))
        if ok: same+=1; continue
    diff+=1; dl.append(f)
print('same',same,'diff',diff)
import collections
print(collections.Counter(x.split('_s')[0] for x in dl))
