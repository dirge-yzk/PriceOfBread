import sys
names = sys.argv[1:]
cols = ['nation','popK','gdpY','stockVal','stockPerGdp','foodVal','foodPerGdp','milVal','ready','equipfill','morale','stkRifle','stkAmmo','stkArt','stkShip','shipPrice','soldiers','regions']
ix = {c:i for i,c in enumerate(cols)}
def load(f):
    secs={}; cur=None
    for l in open(f,encoding='utf-8',errors='replace'):
        l=l.rstrip('\n')
        if l.startswith('## '): cur=l[3:]; secs[cur]=[]; continue
        if l.startswith('nation\t') or not cur: continue
        secs[cur].append(l.split('\t'))
    return secs
data={n:load('pr_'+n+'.out') for n in names}
def q(xs,p):
    xs=sorted(xs); return xs[min(len(xs)-1,int(len(xs)*p))]
def vals(rows,c,small=None):
    out=[]
    rs=rows
    if small:
        rs=sorted(rows,key=lambda r:float(r[ix['popK']]))[:small]
    for r in rs:
        try:
            v=float(r[ix[c]])
            if v==v: out.append(v)
        except: pass
    return out
for sec in ['construct','afterwarm','Q1','Q4','Q20','Q40','Q100']:
    print('=====',sec)
    for c,sm in [('stockPerGdp',None),('stockPerGdp',10),('foodPerGdp',None),('foodPerGdp',10),('ready',None),('equipfill',None),('shipPrice',None),('stkShip',None),('stkRifle',None),('morale',None)]:
        line=f"{c+('(小10)' if sm else ''):18s}"
        for n in names:
            v=vals(data[n].get(sec,[]),c,sm)
            if not v: line+=f" | {n}: -"; continue
            line+=f" | {n}: p10 {q(v,.1):7.2f} med {q(v,.5):7.2f} p90 {q(v,.9):7.2f} max {max(v):8.2f}"
        print(line)
