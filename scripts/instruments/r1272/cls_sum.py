# (1272) 植民地格の州の工場労働者の割合と食べ物の充足を腕ごと・時期ごとに比べる・州×種の対で比の変化との関係を見る
# 引数: al の cls 基準の cls al の dist 基準の dist
import sys,csv,collections,statistics as st
EXC={'イカルイト','チャーチル','ナイン','ヌーク','バロー','フォートシンプソン','フォートユーコン'}
def cls(p):
    d=collections.defaultdict(list)
    for r in csv.DictReader(open(p,encoding='utf-8'),delimiter='\t'):
        if r['tier']!='Colonial' or r['region'] in EXC: continue
        d[int(r['year'])//20*20].append(r)
    return d
def F(x):
    try: return float(x)
    except: return float('nan')
for nm,p in (('al',sys.argv[1]),('base',sys.argv[2])):
    d=cls(p); line=[]
    for per in sorted(d):
        R=d[per]; pop=sum(F(r['popK']) for r in R); fw=sum(F(r['fwK']) for r in R); pe=sum(F(r['peK']) for r in R)
        ffw=sum(F(r['fwK'])*F(r['ffFw']) for r in R if F(r['ffFw'])==F(r['ffFw']))/max(1e-9,sum(F(r['fwK']) for r in R if F(r['ffFw'])==F(r['ffFw'])))
        ffp=sum(F(r['peK'])*F(r['ffPe']) for r in R if F(r['ffPe'])==F(r['ffPe']))/max(1e-9,sum(F(r['peK']) for r in R if F(r['ffPe'])==F(r['ffPe'])))
        line.append(f"{per}: 工場労働者の割合 {fw/pop:.3f}・充足 工{ffw:.2f}/農{ffp:.2f}")
    print(nm,'植民地格（州×2年の全て）｜'+' ｜'.join(line))
# 州×種の対：最大の工場労働者の割合の差と比の差
def mx(p):
    m=collections.defaultdict(float); t={}
    for r in csv.DictReader(open(p,encoding='utf-8'),delimiter='\t'):
        k=(r['seed'],r['region']); pp=F(r['popK'])
        if pp>0.5: m[k]=max(m[k],F(r['fwK'])/pp)
        t[k]=r['tier']
    return m,t
def dist(p): return {(r['seed'],r['region']):F(r['ratio']) for r in csv.DictReader(open(p,encoding='utf-8'),delimiter='\t')}
ma,ta=mx(sys.argv[1]); mb,tb=mx(sys.argv[2]); da=dist(sys.argv[3]); db=dist(sys.argv[4])
pairs=[(ma[k]-mb.get(k,0),da[k]-db[k],k) for k in ma if k in db and k in da and ta.get(k)=='Colonial' and k[1] not in EXC]
up=[x for x in pairs if x[0]>0.05]; dn=[x for x in pairs if x[0]<=0.05]
print(f"植民地格の州×種の対 {len(pairs)}｜工場労働者の最大の割合が 0.05 超 増えた対 {len(up)}：比の差の中央 {st.median([x[1] for x in up]):+.3f}・比が 0.1 超 下がった対 {sum(x[1]<-0.1 for x in up)}｜それ以外 {len(dn)}：比の差の中央 {st.median([x[1] for x in dn]):+.3f}・0.1 超 下がった対 {sum(x[1]<-0.1 for x in dn)}")
k=('17','カルメン・デ・パタゴネス'); print('パタゴネス 種17：工場労働者の最大の割合',f"{ma.get(k,0):.3f}/{mb.get(k,0):.3f}",'比',da.get(k),db.get(k))
