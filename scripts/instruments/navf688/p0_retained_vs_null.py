import csv,sys,collections,statistics as st
SEVEN=['イギリス','フランス','プロイセン','ロシア','オーストリア','アメリカ','日本']
def f(x):
    try:return float(x)
    except:return float('nan')
ARMS={'m685':'D:/PoB_work/keep_685/m685_ins','N1':'D:/PoB_work/keep_685/n1','N2':'D:/PoB_work/keep_685/n2'}
def load(D,s):
    rows=list(csv.DictReader(open(f'{D}/stock_s{s}.tsv',encoding='utf-8-sig'),delimiter='\t'))
    byq=collections.defaultdict(dict)
    for r in rows:
        t=r['time']
        if t.startswith('q') and t[1:].isdigit(): byq[int(t[1:])][r['nation']]=r
    return byq
res={}
for arm,D in ARMS.items():
    fl={(n,y):[] for n in SEVEN for y in (1850,1913)}
    gain={(n,y):[] for n in SEVEN for y in (1850,1913)}
    for s in range(1,11):
        byq=load(D,s)
        qs=sorted(byq)
        yq={}
        for q in qs:
            for n,r in byq[q].items():
                yq.setdefault(int(f(r['year'])),q)
        # year-end quarter: last quarter of year
        ye={}
        for q in qs:
            for r in byq[q].values():
                ye[int(f(r['year']))]=q; break
        for y in (1850,1913):
            qy=ye[y]
            for n in SEVEN:
                if n in byq[qy]: fl[(n,y)].append(f(byq[qy][n]['fleetUnitsStock']))
                prev=0.0; g=0.0
                for q in qs:
                    if q>qy: break
                    r=byq[q].get(n)
                    if r is None: continue
                    c=f(r['fleetUnitsMovedOutHomeCum'])
                    d=c-prev; prev=c
                    if d>0: g+=d*0.995**(3*(qy-q))
                gain[(n,y)].append(g)
    res[arm]=(fl,gain)
print('nation year | fleet median m685 N1 N2 | null width(max-min of 3 medians) | sender retained-gain upper bound (median over seeds, m685) and per-seed max')
for y in (1850,1913):
  for n in SEVEN:
    meds=[st.median(res[a][0][(n,y)]) for a in ARMS]
    gm=st.median(res['m685'][1][(n,y)]); gx=max(res['m685'][1][(n,y)])
    print(y,n,[round(m) for m in meds],'width',round(max(meds)-min(meds)),'gain_med',round(gm),'gain_max',round(gx),'gain/width',round(gm/max(1,max(meds)-min(meds)),2))
