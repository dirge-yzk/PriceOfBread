# (1270) c130rc 記録：発見の側の技師（engineer_s の E 行の engineerPerMonth の和・行を跨いだ和＝相対の比べだけ）を 1797・1851・1913 年の最初の四半期で腕÷基準。読むだけ。
# 使い方: rec_w.py <腕の生> <基準の生> <種の始め> <種の終わり>
import sys,csv,statistics
A,B,s0,s1=sys.argv[1],sys.argv[2],int(sys.argv[3]),int(sys.argv[4])
Y=(1797,1851,1913)
def load(d):
    out={}
    for s in range(s0,s1+1):
        f=open(f"{d}/engineer_s{s}.tsv",encoding='utf-8',newline=''); r=csv.reader(f,delimiter='\t'); h=next(r); ix={c:i for i,c in enumerate(h)}
        first={}; acc={}
        for x in r:
            if x[ix['kind']]!='E': continue
            y=int(x[ix['year']])
            if y not in Y: continue
            q=int(x[ix['quarter']])
            if y not in first: first[y]=q
            if q!=first[y]: continue
            try: v=float(x[ix['engineerPerMonth']])
            except: continue
            if v==v: acc[y]=acc.get(y,0)+v
        f.close()
        for y,v in acc.items(): out.setdefault(y,[]).append(v)
    return out
a=load(A); b=load(B)
print(f"# rec_w 腕={A} 基準={B} 種={s0}..{s1}")
for y in Y:
    ma=statistics.median(a[y]) if y in a else float('nan'); mb=statistics.median(b[y]) if y in b else float('nan')
    print(f"{y}\t腕 {ma:.1f}\t基準 {mb:.1f}\t比 {ma/mb if mb>0 else float('nan'):.4f}")
