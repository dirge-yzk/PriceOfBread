# (1403) 港の表を入れた後の州の組（海の道）の上限超えの見込み：格子の海＋決め打ちの出口（real2）と、格子の海＋どの港でも（real1）を比べる（読むだけ）
import csv,sys
f=lambda x: float('inf') if x in('inf','','NaN') else float(x)
base=sys.argv[1] if len(sys.argv)>1 else 'D:/PoB_work/r1395_work/out'
n=o_now=o_tab=0
for r in csv.DictReader(open(base+'/a8_route.tsv',encoding='utf-8'),delimiter='\t'):
    r1=f(r['real1AnyPortKm']); r2=f(r['real2SameExitsKm']); s=f(r['simSeaRouteKm'])
    if not (r1<float('inf') and r2<float('inf') and s<float('inf')): continue
    n+=1; b=max(850,0.1*r1)
    if abs(s-r1)>b: o_now+=1
    if abs(r2-r1)>b: o_tab+=1
print('組',n,'今の図＋決め打ち 対 格子＋どの港でも 超え',o_now,round(o_now/n,3),'｜格子の表＋決め打ち 超え',o_tab,round(o_tab/n,3))
# 追記：km の短い方どうし（陸と海の短い方）で比べる＝道の種類の混ざりを除いた粗い見込み
n=o=0
for r in csv.DictReader(open(base+'/a8_route.tsv',encoding='utf-8'),delimiter='\t'):
    s=min(f(r['simSeaRouteKm']),f(r['simLandKm'])); g=min(f(r['real1AnyPortKm']),f(r['realLandKm']))
    if not (s<float('inf') and g<float('inf')): continue
    n+=1
    if abs(s-g)>max(850,0.1*g): o+=1
print('km の短い方どうし 組',n,'超え',o,round(o/n,3))
