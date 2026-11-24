# 問い 5 の補助: 抜ける段で FW の流出を規則ごとの gMax の比で縮め、他の増減（自然増・越境・流入）は人口あたりの率のまま置いて、最小÷ピークを数え直す〈推〉（1 州だけの粗い反実仮想）
import sys,json,math,collections
d,seed,reg,qa=sys.argv[1],int(sys.argv[2]),sys.argv[3],int(sys.argv[4])
P={p['name']:(p['lat'],p['lon']) for p in json.load(open('C:/Users/dirge/Documents/PriceOfBread/PriceOfBread.SimCore/places.json',encoding='utf-8'))}
def km(a,b):
    (la1,lo1),(la2,lo2)=P.get(a,(0,0)),P.get(b,(0,0))
    dl=math.radians(la2-la1); dn=math.radians(lo2-lo1)
    x=math.sin(dl/2)**2+math.cos(math.radians(la1))*math.cos(math.radians(la2))*math.sin(dn/2)**2
    return 6371*2*math.atan2(math.sqrt(min(1,x)),math.sqrt(max(0,1-x)))
byq=collections.defaultdict(list); me={}
with open(f'{d}/region_s{seed}.tsv',encoding='utf-8') as f:
    h=f.readline().rstrip('\n').split('\t'); ix={c:i for i,c in enumerate(h)}
    rows=[l.rstrip('\n').split('\t') for l in f]
nat={int(a[0]):a[ix['nation']] for a in rows if a[ix['region']]==reg}
for a in rows:
    q=int(a[0])
    if q>=qa-1 and nat.get(q)==a[ix['nation']]: byq[q].append(a)
    if a[ix['region']]==reg: me[q]=a
v=lambda a,c: float(a[ix[c]])
def gmax(q,rule):
    L=byq[q]; m=me[q]; so,sh,sf=v(m,'opportunityIndex'),v(m,'holdingFactor'),v(m,'foodFillShare')
    stay=so*sh*(sf if rule else 1.0); best=0
    for a in L:
        if a[ix['region']]==reg: continue
        g=v(a,'opportunityIndex')*(v(a,'foodFillShare') if rule==2 else 1.0)-stay-0.15-0.15*km(reg,a[ix['region']])/10000
        best=max(best,g)
    return best
pop={0:v(me[qa-1],'populationK'),1:v(me[qa-1],'populationK'),2:v(me[qa-1],'populationK')}; mn={k:pop[k] for k in pop}
pk=max(v(me[q],'populationK') for q in me)
for q in range(qa,668):
    p_obs_prev=v(me[q-1],'populationK'); p_obs=v(me[q],'populationK'); out=v(me[q],'migrationOutKPerQuarter')
    other=(p_obs-p_obs_prev+out)/p_obs_prev   # 流出以外の増減の率
    g1=gmax(q,1); outrate=out/p_obs_prev
    for r in (0,1,2):
        k=gmax(q,r)/g1 if g1>0 else 1.0
        pop[r]=pop[r]*(1+other-outrate*k); mn[r]=min(mn[r],pop[r])
print(f'ピーク {pk:.2f}・最小÷ピーク（数え直し）R0 旧 {mn[0]/pk:.3f}・R1 今の hf {mn[1]/pk:.3f}（観測の再現の確かめ）・R2 対称 {mn[2]/pk:.3f}')
