# (1403) 陸の州の組の上限超えの内訳（読むだけ・r1395 の a8_land.tsv と a8_route.tsv）
import csv,collections,sys
f=lambda x: float('inf') if x in('inf','') else float(x)
base=sys.argv[1] if len(sys.argv)>1 else 'D:/PoB_work/r1395_work/out'
rows=list(csv.DictReader(open(base+'/a8_land.tsv',encoding='utf-8'),delimiter='\t'))
ov=[r for r in rows if r['over']=='1']
bins=collections.Counter()
for r in ov:
    q=f(r['simLandKm'])/f(r['realGridKm'])
    bins['<1.2' if q<1.2 else '<1.5' if q<1.5 else '<2' if q<2 else '>=2']+=1
print('組',len(rows),'超え',len(ov),dict(bins))
big=[r for r in ov if f(r['simLandKm'])/f(r['realGridKm'])>=1.5]
c=collections.Counter()
for r in big: c[r['regionA']]+=1; c[r['regionB']]+=1
print('比1.5以上',len(big),c.most_common(12))
sm=sorted(f(r['realGridKm']) for r in ov if f(r['simLandKm'])/f(r['realGridKm'])<1.2)
print('比1.2未満の格子km 中央',sm[len(sm)//2])
fin=sorted(f(r['simLandKm'])/f(r['realGridKm']) for r in rows if r['simLandKm']!='inf' and r['realGridKm']!='inf' and f(r['realGridKm'])>0)
n=len(fin); print('全ての有限の組の比 中央',fin[n//2],'p75',fin[int(n*.75)],'p90',fin[int(n*.9)])
sea={}
for r in csv.DictReader(open(base+'/a8_route.tsv',encoding='utf-8'),delimiter='\t'): sea[(r['regionA'],r['regionB'])]=f(r['simSeaRouteKm'])
for k in (12,3):
    m=sum(1 for r in ov if f(r['simLandKm'])*k < sea.get((r['regionA'],r['regionB']), sea.get((r['regionB'],r['regionA']), float('inf'))))
    print('陸と海の費用の比',k,'超えの組のうち陸の道が安い見込み（粗い代理）',m)
