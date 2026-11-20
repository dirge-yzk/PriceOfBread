# 残差の外れ（州の残差% − 同じ国・同じ四半期の州の中央値%）が閾値を超える州×四半期を数える。
# 使い方: anom.py <生のフォルダ> <種の始め> <種の終わり> [閾値%=2]
import sys,csv,collections,statistics
d,s0,s1=sys.argv[1],int(sys.argv[2]),int(sys.argv[3]); th=float(sys.argv[4]) if len(sys.argv)>4 else 2.0
for s in range(s0,s1+1):
    f=open(f"{d}/region_s{s}.tsv",encoding='utf-8',newline=''); r=csv.reader(f,delimiter='\t'); h=next(r); ix={c:i for i,c in enumerate(h)}
    prev={}; by=collections.defaultdict(list)
    for x in r:
        q=int(x[ix['quarter']]); reg=x[ix['region']]; P=float(x[ix['populationK']])
        net=float(x[ix['migrationInKPerQuarter']])-float(x[ix['migrationOutKPerQuarter']])+float(x[ix['crossBorderInKPerQuarter']])-float(x[ix['crossBorderOutKPerQuarter']])
        if reg in prev and prev[reg][1]==x[ix['nation']] and prev[reg][0]>1: by[(q,x[ix['nation']])].append((reg,(P-prev[reg][0]-net)/prev[reg][0]*100,x[ix['tier']],P-prev[reg][0]-net))
        prev[reg]=(P,x[ix['nation']])
    hit=collections.Counter(); hk=collections.Counter(); tiers=collections.Counter()
    for k,v in by.items():
        if len(v)<3: continue
        med=statistics.median(a[1] for a in v)
        for reg,pc,t,ab in v:
            if pc-med>th: hit[(k[1],reg)]+=1; hk[(k[1],reg)]+=ab; tiers[t]+=1
    tot=sum(hit.values())
    print(f"種{s}\t外れ{tot}\t格{dict(tiers)}\t上位\t"+"; ".join(f"{n}/{g}:{c}回 {hk[(n,g)]:.0f}K" for (n,g),c in hit.most_common(5)))
