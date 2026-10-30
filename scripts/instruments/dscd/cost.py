import sys,re
def load(p):
    d={}
    for line in open(p,encoding='utf-8'):
        if not line.startswith('  ') or '\t' not in line: continue
        f=line.strip().split('\t')
        if len(f)<4 or f[0]=='名前': continue
        try: d[f[0][:14]]=(f[0],float(f[1]),float(f[2]))
        except: pass
    return d
# usage: on.cost / off.cost = origin/dsc6 canary_flagon_batch_s2s3.cost / census_flagoff_batch_s2s3.cost; m685/n1/n2 = measurements/b10_20261030_m685{,_n1,_n2}_ins.cost
import os
D=os.environ.get('DSCD_DIR','D:/PoB_work/dscd_691/')
on,off,m=load(D+'on.cost'),load(D+'off.cost'),load(D+'m685.cost')
n1,n2=load(D+'n1.cost'),load(D+'n2.cost')
tot=0;rows=[]
for k,(nm,c_on,u) in on.items():
    c_off=off.get(k,(nm,0,0))[1]
    um=m.get(k,(nm,0,u))[2] if k in m else u
    dc=c_on-c_off; s=dc*um/1e6
    tot+=s; rows.append((s,nm[:40],dc,um))
for s,nm,dc,um in sorted(rows,key=lambda r:-abs(r[0]))[:14]:
    print(f"{s:+8.2f}s  {dc:+.3e}  u={um:.5f}  {nm}")
print('total repriced at m685 units:',round(tot,2))
# null spread per row
print('--- null arm spread (n1,n2 vs m685) for the main rows')
for k in [n[:14] for n,_,_ in sorted(m.values(),key=lambda v:-v[1]*v[2])[:5]]:
    kk=k[:14]
    vals=[x[kk][1]*x[kk][2]/1e6 for x in (m,n1,n2)]
    print(k, [round(v,2) for v in vals])
