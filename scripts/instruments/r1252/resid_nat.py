# 国の全州の四半期の残差（ΔP−移動の純）を州ごとに並べる。使い方: resid_nat.py <region_sN.tsv> <国> <q0> <q1> [州の数の上限]
import sys,csv,collections
p,nat,q0,q1=sys.argv[1],sys.argv[2],int(sys.argv[3]),int(sys.argv[4]); top=int(sys.argv[5]) if len(sys.argv)>5 else 12
f=open(p,encoding='utf-8',newline=''); r=csv.reader(f,delimiter='\t'); h=next(r); ix={c:i for i,c in enumerate(h)}
prev={}; res=collections.defaultdict(dict); popq=collections.defaultdict(dict)
for x in r:
    q=int(x[ix['quarter']]); reg=x[ix['region']]; P=float(x[ix['populationK']])
    net=float(x[ix['migrationInKPerQuarter']])-float(x[ix['migrationOutKPerQuarter']])+float(x[ix['crossBorderInKPerQuarter']])-float(x[ix['crossBorderOutKPerQuarter']])
    if reg in prev and x[ix['nation']]==nat and q0<=q<=q1: res[reg][q]=(P-prev[reg]-net)/max(prev[reg],1e-9)*100; popq[reg][q]=P
    prev[reg]=P
regs=sorted(res,key=lambda g:-max(popq[g].values()))[:top]
print('q\t'+'\t'.join(regs)); 
for q in range(q0,q1+1): print(str(q)+'\t'+'\t'.join(f"{res[g].get(q,float('nan')):.2f}" for g in regs))
