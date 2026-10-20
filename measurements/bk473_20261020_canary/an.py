import sys,csv,statistics as st,collections
sys.stdout.reconfigure(encoding='utf-8')
D='D:/PoB_work/bk473/'
MAIN=['イギリス','フランス','プロイセン','オーストリア','ロシア','オスマン','清','日本']
def load(fn):
    rows=[];hdr=None
    for line in open(D+fn,encoding='utf-8'):
        if line.startswith('#'): continue
        x=line.rstrip('\n').split('\t')
        if hdr is None: hdr=x; continue
        rows.append(x)
    ix={h:i for i,h in enumerate(hdr)}
    d=collections.defaultdict(dict)  # (nation)->q->dict
    for x in rows:
        r={h:(float(x[i]) if h!='nation' else x[i]) for h,i in ix.items()}
        d[r['nation']][int(r['q'])]=r
    return d
def med(v):
    v=[a for a in v if a==a]; return st.median(v) if v else float('nan')
def series(d,col,names=None,agg='sum'):
    nm=names or list(d.keys()); qs=sorted(next(iter(d.values())).keys()); out=[]
    for q in qs:
        v=[d[n][q][col] for n in nm if n in d and q in d[n]]
        out.append(sum(a for a in v if a==a) if agg=='sum' else med(v))
    return out
def conv(cap,px,win=20,tc=0.02,tp=0.05):
    first=None; ok=[]
    for q in range(win,len(cap)):
        c=abs(cap[q]/cap[q-win]-1) if cap[q-win]>0 else 9; p=abs(px[q]/px[q-win]-1) if px[q-win]>0 else 9
        ok.append(c<tc and p<tp)
        if ok[-1] and first is None: first=q
    # sustained from first?
    sustained = first is not None and all(ok[first-win:])
    return first,sustained
mode=sys.argv[1]; d=load(mode+'.tsv')
names=[n for n in d if d[n] and True]
print('nations',len(names), 'main present',[n for n in MAIN if n in d])
ALL=names
for label,nm in (('WORLD(all nations)',ALL),('MAIN8',[n for n in MAIN if n in d])):
    cap=series(d,'capSum',nm); px=series(d,'dailyPx',nm,'med'); sx=series(d,'staplePx',nm,'med')
    f,s=conv(cap,px)
    print(f'## {label}: converged first q={f} sustained={s}')
    qs=[0,1,4,8,12,20,40,60,80,100,120,160,200,300,400]
    print('q\tcapSum\tcap/q20\tdailyPx(med)\tstaplePx(med)')
    for q in qs:
        if q<len(cap): print(f'{q}\t{cap[q]:.4g}\t{cap[q]/cap[20] if len(cap)>20 and cap[20] else 0:.3f}\t{px[q]:.3f}\t{sx[q]:.3f}')
# per-nation convergence
print('## per-nation convergence (cap<2% & px<5% over 20q)')
for n in MAIN:
    if n not in d: continue
    cap=series(d,'capSum',[n]); px=series(d,'dailyPx',[n]); f,s=conv(cap,px)
    # cap-only and px-only
    fc=conv(cap,[1]*len(cap),20,0.02,9)[0]; fp=conv([1]*len(cap),px,20,9,0.05)[0]
    print(n,'both',f,'sustained',s,'| cap-only',fc,'| px-only',fp, '| cap q400/q20 %.2f'%(cap[-1]/cap[20] if len(cap)>20 else 0))
cols=['capPerPopK','dailyPx','staplePx','dailyShortRatio','dailyShortMktFrac','dailyStockRatio','peasantSavMonths','peasantSpendOverEarn','capitalistSavMonths','workerShare','artisanShare','priceLevel','treasuryPreOverGdpY','govDebtOverGdpY','bankruptN']
qs=[0,4,20,40,80,120,200,400]
for c in cols:
    print('## ',c)
    print('nation\t'+'\t'.join('q%d'%q for q in qs))
    for n in MAIN:
        if n not in d: continue
        print(n+'\t'+'\t'.join(('%.3g'%d[n][q][c]) if q in d[n] else '-' for q in qs))
# capitalist savings multiple vs q0
print('## capitalistSavTotal / q0')
for n in MAIN:
    if n in d: print(n+'\t'+'\t'.join('%.2f'%(d[n][q]['capitalistSavTotal']/d[n][0]['capitalistSavTotal'] if d[n][0]['capitalistSavTotal']>0 and q in d[n] else float('nan')) for q in qs))
print('## peasant savings per head / q0')
for n in MAIN:
    if n in d: print(n+'\t'+'\t'.join('%.2f'%(d[n][q]['peasantSavPerHead']/d[n][0]['peasantSavPerHead'] if d[n][0]['peasantSavPerHead']>0 and q in d[n] else float('nan')) for q in qs))
print('## cap per pop / q0 and / q400')
for n in MAIN:
    if n in d: print(n+'\t'+'\t'.join('%.2f'%(d[n][q]['capPerPopK']/d[n][400]['capPerPopK'] if 400 in d[n] and d[n][400]['capPerPopK']>0 else float('nan')) for q in qs))
# all-nation spread at q20, q400: dailyPx
for q in (0,20,100,400):
    if q in d[names[0]]:
        v=sorted(d[n][q]['dailyPx'] for n in names if d[n][q]['dailyPx']==d[n][q]['dailyPx'])
        print(f'dailyPx all-nation q{q}: min {v[0]:.2f} p25 {v[len(v)//4]:.2f} med {med(v):.2f} p75 {v[3*len(v)//4]:.2f} max {v[-1]:.2f} n={len(v)}')
