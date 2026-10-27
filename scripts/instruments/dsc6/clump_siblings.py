import re,sys,os,collections
sys.path.insert(0,os.path.dirname(os.path.abspath(__file__)))
import gates_dsc6 as g6
from gates_dsc6 import MAJOR,SPREAD
edges={}
for l in open(os.path.join(os.path.dirname(os.path.abspath(__file__)),'..','..','..','PriceOfBread.SimCore','Catalog.cs'),encoding='utf-8'):
    m=re.search(r'new\("([^"]+)",.*DscAfter: "([^"]+)"(?:, DscAfter2: "([^"]+)")?',l)
    if m: edges[m.group(1)]=[x for x in m.groups()[1:] if x]
print(len(edges),'rows with prerequisites')
ins=sys.argv[1]   # 使い方：python clump_siblings.py <ins ディレクトリ>（国ごとの最大の窓〔M>=5〕の出来事が、前提のある行〔兄弟の同時解放〕か前提の無い行かを数える＝記録）
inwin=collections.Counter(); allc=collections.Counter()
def tag(k,q,op):
    ps=edges.get(k[1])
    if not ps: return 'noparent'
    lag=[q-op[(k[0],p)] for p in ps if (k[0],p) in op]
    if not lag: return 'parent-startknown'
    return 'child-lag<=8' if min(lag)<=8 else 'child-lag>8'
for s in range(21,31):
    a=g6.analyse(ins,s)
    op={k:q for k,q in a['ev'].items() if k[1] not in SPREAD}
    # note: start-known parents absent from op
    for k,q in op.items(): allc[tag(k,q,op)]+=1
    for n in MAJOR:
        l=sorted((q,k) for k,q in op.items() if k[0]==n)
        best=0;bw=None
        for i,(q0,_) in enumerate(l):
            w=[(q,k) for q,k in l[i:] if q<=q0+3]
            if len(w)>best: best=len(w);bw=w
        if best>=5:
            for q,k in bw: inwin[tag(k,q,op)]+=1
print('all',dict(allc)); print('clump windows (M>=5)',dict(inwin))
