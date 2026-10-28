# (636) 記録：都市の核（都市の割合の高い州）の人口・都市の割合・主食の充足の年代（種の中央値）と最小÷ピーク（読むだけ）
# 使い方: python urbcore.py <計器の出力フォルダ> [種の数=10]
import csv,sys,statistics as st,collections,os
d=sys.argv[1]; N=int(sys.argv[2]) if len(sys.argv)>2 else 10
YRS=[1783,1803,1823,1843,1863,1883,1903,1923,1943]
data=collections.defaultdict(dict)   # (region)->{seed:{year:(pop,urb,food)}}
traj=collections.defaultdict(dict)   # region->{seed:[pop...]}
for s in range(1,N+1):
    p=f'{d}/region_s{s}.tsv'
    if not os.path.exists(p): continue
    last={}
    for r in csv.DictReader(open(p,encoding='utf-8'),delimiter='\t'):
        if r['tier']!='Core': continue
        y=int(r['year']); reg=r['region']
        pop=float(r['populationK']); rur=float(r['ruralPopulationK'])
        traj[reg].setdefault(s,[]).append(pop)
        last[(reg,y)]=(pop,(1-rur/pop) if pop>0 else 0.0,float(r['foodFillShare']),r['nation'])
    for (reg,y),v in last.items():
        if y in YRS: data[reg].setdefault(s,{})[y]=v
# 1843 年の都市の割合（種の中央値）が高い州の上位 6（人口 100 千人以上）＋アムステルダム
def med(xs): return st.median(xs) if xs else float('nan')
score={}
for reg,bys in data.items():
    u=[v[1843][1] for v in bys.values() if 1843 in v and v[1843][0]>=100]
    if len(u)>=N//2: score[reg]=med(u)
top=sorted(score,key=lambda r:-score[r])[:6]
if 'アムステルダム' not in top: top=['アムステルダム']+top
print('# 年:人口千/都市の割合/主食の充足（種の中央値）｜最小÷ピーク（種の最小・中央）｜1/10 未満の種')
for reg in top:
    bys=data[reg]; nat=next(iter(next(iter(bys.values())).values()))[3]
    cells=[]
    for y in YRS:
        vs=[v[y] for v in bys.values() if y in v]
        cells.append(f"{y}:{med([v[0] for v in vs]):.0f}/{med([v[1] for v in vs]):.2f}/{med([v[2] for v in vs]):.2f}")
    rat=[min(t)/max(t) for t in traj[reg].values() if max(t)>0]
    low=[s for s,t in traj[reg].items() if max(t)>0 and min(t[t.index(max(t)):])<0.1*max(t)]
    print(f"{reg}（{nat}） "+' '.join(cells)+f"｜{min(rat):.3f}・{med(rat):.3f}｜{low}")
