import pickle, statistics as st, sys
sys.path.insert(0,'.')
V=['act','now','a_gc','a_gc_noera','a_rt','b1','b1_12','b2']
P={a:pickle.load(open(f'res_{a}.pkl','rb')) for a in ['base_ins','n1','n2']}
def med(a,c,v,i): return st.median(P[a][sd][0][c][v][i] for sd in range(1,11))
def mean(a,c,v,i): return st.mean(P[a][sd][0][c][v][i] for sd in range(1,11))
print('## 再構成の外（g≦0 で流れた量の割合・種の中央値）', {a:round(st.median(P[a][sd][1][0]/P[a][sd][1][1] for sd in range(1,11)),3) for a in P})
print('## 実測（act）：3 本の腕の 種中央値')
for c in ['world','colsrc']:
    for i,nm in [(0,'総量'),(1,'3000km超'),(2,'組×四半期')]:
        print(c,nm,{a:round(med(a,c,'act',i)) for a in P})
print('## 基線の紙の比（変種÷now・種ごとの比の中央値）')
cats=list(P['base_ins'][1][0].keys())
for c in cats:
    row=[]
    for v in V[2:]:
        r0=[P['base_ins'][sd][0][c][v][0]/P['base_ins'][sd][0][c]['now'][0] for sd in range(1,11) if P['base_ins'][sd][0][c]['now'][0]>0]
        r1=[P['base_ins'][sd][0][c][v][1]/P['base_ins'][sd][0][c]['now'][1] for sd in range(1,11) if P['base_ins'][sd][0][c]['now'][1]>0]
        r2=[P['base_ins'][sd][0][c][v][2]/P['base_ins'][sd][0][c]['now'][2] for sd in range(1,11) if P['base_ins'][sd][0][c]['now'][2]>0]
        row.append(f"{v} 量{st.median(r0) if r0 else float('nan'):.3f} 遠{st.median(r1) if r1 else float('nan'):.3f} 組{st.median(r2) if r2 else float('nan'):.3f}")
    print(c, f"act中央 {med('base_ins',c,'act',0):.0f} 平均 {mean('base_ins',c,'act',0):.0f} 遠 {med('base_ins',c,'act',1):.0f}", ' | '.join(row))
print('## 和の比（10種の和・変種÷now）')
for c in cats:
    s=lambda v,i: sum(P['base_ins'][sd][0][c][v][i] for sd in range(1,11))
    if s('now',0)>0: print(c, ' '.join(f"{v} {s(v,0)/s('now',0):.3f}" for v in V[2:]))
print('## g′≦0 になった組の割合（再構成の組・10種の和）')
for a in P:
    tot={v:[0,0] for v in P[a][1][2]}
    for sd in range(1,11):
        for v,x in P[a][sd][2].items(): tot[v][0]+=x[0]; tot[v][1]+=x[1]
    print(a,{v:round(x[0]/x[1],3) for v,x in tot.items()})
