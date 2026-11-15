# (1163) 形に固有の誤りの量 E（案）：平時の中核の州のうち、開幕（四半期 0）に外国の取り分 0 で持ち主の取り分≧0.5 の州の外国の取り分の和（窓の平均）。種 1・2 と全ての種（ヌル腕）。
import collections, statistics as st, math, sys
WIN = int(sys.argv[1]) if len(sys.argv) > 1 else 80   # 668＝全長（ヌル腕だけ）
def fl(x):
    try: return float(x)
    except ValueError: return float('nan')
A={'M57':'D:/PoB_work/keep_1157/base_ins','N1_50':'D:/PoB_work/keep_1150/n1','N2_50':'D:/PoB_work/keep_1150/n2','CTL':'D:/PoB_work/keep_1157_rej/c113cr_mut/CTL/ins'}
def E(d,s):
    war=set()
    with open(f'{d}/nation_s{s}.tsv',encoding='utf-8') as f:
        h=f.readline().rstrip('\n').split('\t');ix={k:j for j,k in enumerate(h)}
        for l in f:
            c=l.rstrip('\n').split('\t');q=int(c[0])
            if q>=WIN: break
            if fl(c[ix['atWarMonths']])>0: war.add((q,c[ix['nation']]))
    strong=set(); tot=collections.Counter(); cnt=collections.Counter()
    with open(f'{d}/rulepower_s{s}.tsv',encoding='utf-8') as f:
        h=f.readline().rstrip('\n').split('\t');ix={k:j for j,k in enumerate(h)}
        for l in f:
            c=l.rstrip('\n').split('\t');q=int(c[0])
            if q>=WIN: break
            rg=c[ix['region']];n=c[ix['nation']]
            if q==0 and c[ix['tier']]=='Core' and fl(c[ix['nowRule2Share']])==0 and fl(c[ix['nowRule0Share']])>=0.5: strong.add((rg,n))
            if (rg,n) in strong and (q,n) not in war:
                x=fl(c[ix['nowRule2Share']]); tot[q]+=x if not math.isnan(x) else 0
                if x>=0.05: cnt[q]+=1
    return sum(tot.values())/WIN, max(cnt.values(),default=0), len(strong)
for a,d in A.items():
    if WIN>80 and a=='CTL': continue
    seeds=(1,2) if a=='CTL' else range(1,11)
    v=[E(d,s) for s in seeds]
    print(a,'E（種ごと）',[round(x[0],5) for x in v],'≧0.05 の州の数の最大',[x[1] for x in v],'強い州の数',v[0][2])
