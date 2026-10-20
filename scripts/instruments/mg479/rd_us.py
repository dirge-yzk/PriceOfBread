# (479) 段0の読み：米の州ごとの国外への出（P2〜P3）と機会・持ち分・土地係数。使い方：python rd_us.py <計器の出力フォルダ> <種>
import csv, sys, collections
base=sys.argv[1]; sd=sys.argv[2]
acc=collections.defaultdict(lambda:[0.0,0.0,0.0,0.0,0,0.0,0.0,0.0])
with open(f'{base}/region_s{sd}.tsv',encoding='utf-8') as f:
    for r in csv.DictReader(f,delimiter='\t'):
        if r['nation']!='アメリカ' or r['activeFlag']!='1': continue
        q=int(r['quarter'])
        if not (68<=q<=467): continue
        a=acc[r['region']]
        a[0]+=float(r['crossBorderOutKPerQuarter']); a[1]+=float(r['crossBorderInKPerQuarter']); a[2]+=float(r['opportunityIndex']); a[3]+=float(r['holdingFactor']); a[4]+=1
        a[5]+=float(r['populationK']); a[6]+=float(r['landFactor']); a[7]+=float(r['foodFillShare'])
print('region outK(total P2-P3) inK  meanO  meanHold  nq  meanPopK  landF  foodFill')
for k,a in sorted(acc.items(), key=lambda kv:-kv[1][0])[:12]:
    n=a[4]; print(k, f"{a[0]:.1f} {a[1]:.1f} {a[2]/n:.3f} {a[3]/n:.3f} {n} {a[5]/n:.0f} {a[6]/n:.3f} {a[7]/n:.3f}")
