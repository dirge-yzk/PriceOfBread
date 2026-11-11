# (998) c91td の反証役が書いた集計（q4b を金建てへ＝nation_s の fxRate を掛けた版・(1001) 合流が反証役のスクラッチから写した・元の q4b_chem_gap.py は書き換えていない）
import sys, os, statistics as st
RAW=sys.argv[1]; W=float(sys.argv[2])
YEARS=[1830,1870,1913,1948]
def rows(p):
    with open(p,encoding='utf-8') as f:
        h=f.readline().rstrip('\n').split('\t'); ix={c:i for i,c in enumerate(h)}
        for l in f: yield ix,l.rstrip('\n').split('\t')
def fl(x):
    try:
        v=float(x); return v if v==v else 0.0
    except: return 0.0
R={}; NN={}; FXR={}
for s in range(1,11):
    fx={}
    for ix,r in rows(os.path.join(RAW,f'nation_s{s}.tsv')):
        y=int(r[ix['year']])
        if y in YEARS: fx[(r[ix['quarter']],r[ix['nation']])]=fl(r[ix['fxRate']])
    loc={}; gold={}; fxs={}
    for ix,r in rows(os.path.join(RAW,f'chemgood_s{s}.tsv')):
        y=int(r[ix['year']])
        if y not in YEARS: continue
        k=(r[ix['good']],y,r[ix['nation']]); p=fl(r[ix['priceMean']]); f=fx.get((r[ix['quarter']],r[ix['nation']]),float('nan'))
        a=loc.setdefault(k,[0,0]); a[0]+=p; a[1]+=1
        b=gold.setdefault(k,[0,0])
        if f==f and f>0 and p>0: b[0]+=p*f; b[1]+=1
        fxs.setdefault((y,r[ix['nation']]),[]).append(f)
    for nm,d in (('loc',loc),('gold',gold)):
        by={}
        for (g,y,n),a in d.items():
            if a[1] and a[0]>0: by.setdefault((g,y),[]).append(a[0]/a[1])
        for (g,y),l in by.items():
            rs=[max(l[i],l[j])/min(l[i],l[j]) for i in range(len(l)) for j in range(i+1,len(l))]
            R.setdefault((nm,g,y),[]).append(sum(1 for x in rs if x>W)/len(rs))
            NN.setdefault((nm,g,y),[]).append(len(l))
    for k,v in fxs.items(): FXR.setdefault(k[0],[]).extend([x for x in v if x==x])
for k in sorted(R): print(k, 'share_med=%.3f'%st.median(R[k]), 'nations=',sorted(set(NN[k])))
for y in sorted(FXR): print(y,'fx min %.3f max %.3f'%(min(FXR[y]),max(FXR[y])))
