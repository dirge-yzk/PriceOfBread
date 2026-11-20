# 国の超過の残差（ΔP−記帳された移動−国の中央の率×前のP）が最大の州と、前の四半期の末の機会（opportunityIndex）が最大の州が一致する率。
# 使い方: argmax_opp.py <region_sN.tsv> <国> <q0> <q1> [残差の下限K=0.5]
import sys,csv,collections
p,nat,q0,q1=sys.argv[1],sys.argv[2],int(sys.argv[3]),int(sys.argv[4]); th=float(sys.argv[5]) if len(sys.argv)>5 else 0.5
f=open(p,encoding='utf-8',newline=''); r=csv.reader(f,delimiter='\t'); h=next(r); ix={c:i for i,c in enumerate(h)}
prev={}; res=collections.defaultdict(dict); opp=collections.defaultdict(dict); prevP=collections.defaultdict(dict)
for x in r:
    q=int(x[ix['quarter']]); reg=x[ix['region']]; P=float(x[ix['populationK']])
    if x[ix['nation']]!=nat: prev[reg]=None; continue
    net=float(x[ix['migrationInKPerQuarter']])-float(x[ix['migrationOutKPerQuarter']])+float(x[ix['crossBorderInKPerQuarter']])-float(x[ix['crossBorderOutKPerQuarter']])
    o=float(x[ix['opportunityIndex']]); opp[q][reg]=o if o==o else float('-inf')
    if prev.get(reg) is not None: res[q][reg]=P-prev[reg]-net; prevP[q][reg]=prev[reg]
    prev[reg]=P
hit=n=0; rows=[]
for q in range(q0,q1+1):
    if q not in res or q-1 not in opp or not res[q]: continue
    import statistics
    med=statistics.median(res[q][g]/max(prevP[q][g],1e-9) for g in res[q])
    ex={g:res[q][g]-med*prevP[q][g] for g in res[q]}
    rmax=max(ex,key=ex.get)
    if ex[rmax]<th: continue
    omax=max(opp[q-1],key=opp[q-1].get); n+=1; hit+=rmax==omax
    rows.append(f"{q}\t{rmax}\t{res[q][rmax]:.2f}\t{omax}")
print(f"#{nat} q{q0}-{q1} 残差が最大(≥{th}K)の四半期 {n}・前の四半期の機会が最大の州と一致 {hit}")
print("\n".join(rows[:12]))
