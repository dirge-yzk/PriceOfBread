# ★(411) 読む線（opus・読むだけ・2026-10-18）の読み手の写し＝(412) 合流で scratchpad から scripts/instruments/rd411/ へ移した（README §1-2 道具は scripts/instruments/）。
# 港の容量と潜在量の和を 2 つの生で比べる（⚠合流の差が混ざる＝(411) では採っていない）。引数＝2 つの計器の生のフォルダ。
# シミュを回さない・計器の TSV を読むだけ。
import csv,sys,collections,statistics as st
NAT=['清','ロシア','イギリス','フランス','プロイセン','日本','オスマン帝国','オーストリア']
YEARS=[1800,1830,1860,1890,1913]
def load(d):
    res=collections.defaultdict(list)  # (nat,yr)->list per seed of (pot,cap,thr_w,through,nbind,n)
    for s in range(1,11):
        acc=collections.defaultdict(lambda:[0,0,0,0,0,0])
        with open(f'{d}/port_s{s}.tsv',encoding='utf-8') as f:
            r=csv.DictReader(f,delimiter='\t')
            for row in r:
                y=int(row['year']); q=int(row['quarter'])
                if y not in YEARS or q%4!=3: continue
                n=row['nation']
                if n not in NAT: continue
                a=acc[(n,y)]
                p=float(row['tradePotentialWeight']); th=float(row['tradeThrottleIndex'])
                a[0]+=p; a[1]+=float(row['portCapacityEnd']); a[2]+=p*th; a[3]+=float(row['portThroughput']); a[4]+= 1 if th<0.999 else 0; a[5]+=1
        for k,v in acc.items(): res[k].append(v)
    return res
A=load(sys.argv[1]); B=load(sys.argv[2])
print('nat year | pot A/B med | portcap A/B | thrW A/B | bindFrac A/B')
for n in NAT:
  for y in YEARS:
    a=A.get((n,y),[]); b=B.get((n,y),[])
    if not a or not b: continue
    f=lambda L,i: st.median([x[i] for x in L])
    g=lambda L: st.median([x[2]/x[0] if x[0]>0 else 1 for x in L])
    h=lambda L: st.median([x[4]/x[5] for x in L])
    print(f'{n} {y} | {f(a,0):.0f} / {f(b,0):.0f} | {f(a,1):.0f} / {f(b,1):.0f} | {g(a):.2f} / {g(b):.2f} | {h(a):.2f} / {h(b):.2f}')
