# (1254) 合流の注：残差率は国の中央を引いていない生の値（出生−死亡を含む）＝機会の高い州の自然増との交絡があり得る（反証役 v の低・〈推・中〉）。
# 州の残差率（(ΔP−記帳された移動)/前のP）を、前の四半期の末にその州が国の中で機会が最大だったか否かで 2 群に分けて並べる。
# 使い方: opp_spike.py <region_sN.tsv> <州> <q0> <q1>
import sys,csv,collections,statistics
p,reg0,q0,q1=sys.argv[1],sys.argv[2],int(sys.argv[3]),int(sys.argv[4])
f=open(p,encoding='utf-8',newline=''); r=csv.reader(f,delimiter='\t'); h=next(r); ix={c:i for i,c in enumerate(h)}
own={}; opp=collections.defaultdict(dict); rows={}; prevP=None
for x in r:
    q=int(x[ix['quarter']]); reg=x[ix['region']]
    o=float(x[ix['opportunityIndex']]); opp[(q,x[ix['nation']])][reg]=o if o==o else float('-inf')
    if reg==reg0:
        P=float(x[ix['populationK']]); own[q]=x[ix['nation']]
        net=float(x[ix['migrationInKPerQuarter']])-float(x[ix['migrationOutKPerQuarter']])+float(x[ix['crossBorderInKPerQuarter']])-float(x[ix['crossBorderOutKPerQuarter']])
        if prevP: rows[q]=(P-prevP-net)/prevP*100
        prevP=P
g={True:[],False:[]}
for q in range(q0,q1+1):
    if q not in rows or (q-1) not in own: continue
    d=opp[(q-1,own[q-1])]; top=max(d,key=d.get)==reg0
    g[top].append(rows[q])
for k in (True,False):
    v=g[k]; 
    if v: print(f"{reg0}\t機会が国の最大={k}\t四半期{len(v)}\t残差率の中央{statistics.median(v):.2f}%\t残差率>1%の数{sum(1 for a in v if a>1)}")
