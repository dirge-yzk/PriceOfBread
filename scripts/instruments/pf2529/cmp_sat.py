import sys, collections
sys.stdout.reconfigure(encoding='utf-8')
A, B = sys.argv[1], sys.argv[2]   # A=flag1 (m524), B=flag0
seeds = [int(x) for x in sys.argv[3].split(',')]
def load(base):
    d={}
    for s in seeds:
        with open(f"{base}/svcbuy_s{s}.tsv",encoding='utf-8') as fh:
            h=fh.readline().rstrip('\n').split('\t'); ix={k:i for i,k in enumerate(h)}
            for l in fh:
                c=l.rstrip('\n').split('\t')
                if c[ix['classId']]!='1': continue
                d.setdefault((s,int(c[ix['quarter']]),c[ix['nation']]),(int(c[ix['year']]),float(c[ix['popK']]),float(c[ix['satisfactionMean']])))
    return d
a=load(A); b=load(B)
eras=[(1783,1800),(1800,1825),(1825,1850),(1850,1875),(1875,1900),(1900,1925),(1925,1951)]
def summ(d,keys,a0,b0,nat=None):
    sel=[d[k] for k in keys if a0<=d[k][0]<b0 and (nat is None or k[2]==nat)]
    tot=sum(x[1] for x in sel)
    if tot==0: return None
    return (sum(x[1] for x in sel if x[2]<0.02)/tot, sum(x[1] for x in sel if x[2]<0.3)/tot, sum(x[1]*x[2] for x in sel)/tot, tot/ max(1,len(sel)))
keys=[k for k in a if k in b]
print("common nation-quarters",len(keys))
print("era | flag1(m524): <0.02 <0.3 mean | flag0: <0.02 <0.3 mean | meanFarmerK flag1 flag0")
for a0,b0 in eras:
    x=summ(a,keys,a0,b0); y=summ(b,keys,a0,b0)
    if x and y: print(f"{a0}-{b0} | {x[0]:.3f} {x[1]:.3f} {x[2]:.3f} | {y[0]:.3f} {y[1]:.3f} {y[2]:.3f} | {x[3]:.0f} {y[3]:.0f}")
nats=sorted({k[2] for k in keys})
print("per nation, all years: flag1 <0.02/mean ; flag0 <0.02/mean ; farmerK flag1/flag0")
for n in nats:
    x=summ(a,keys,1783,1951,n); y=summ(b,keys,1783,1951,n)
    print(f"{n}: {x[0]:.3f}/{x[2]:.3f} ; {y[0]:.3f}/{y[2]:.3f} ; {x[3]:.0f}/{y[3]:.0f}")
