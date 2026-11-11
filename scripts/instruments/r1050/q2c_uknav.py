import csv,collections,sys,statistics as st
K='D:/PoB_work/keep_1044'
arm=sys.argv[1]; seeds=range(11,21) if arm=='a11' else range(1,11); d='base_ins' if arm=='base' else arm
def rows(p):
    with open(p,encoding='utf-8') as f: return list(csv.DictReader(f,delimiter='\t'))
UK='イギリス'
out=[]
allnav=[];allex=[];allbl=[]
for s in seeds:
    st_=rows(f'{K}/{d}/stock_s{s}.tsv')
    tcol='time'
    nav={}; worldnav=collections.Counter(); fu={}
    for r in st_:
        q=int(r[tcol][1:]) if r[tcol].startswith('q') else -1
        try: v=float(r['navalPowerIndex'])
        except: v=float('nan')
        worldnav[q]+= v if v==v else 0
        if r['nation']==UK: nav[q]=v; fu[q]=float(r['fleetUnitsStock'] or 'nan')
    sc={int(r['quarter']):r for r in rows(f'{K}/{d}/scalars_s{s}.tsv') if r['nation']==UK}
    ml={int(r['quarter']):float(r['blockadeRiskShare'] or 'nan') for r in rows(f'{K}/{d}/monlaw_s{s}.tsv') if r['nation']==UK}
    for r in rows(f'{K}/{d}/imp_s{s}.tsv'):
        if r['loser']!=UK: continue
        q=int(r['quarter'])
        def g(dct,qq): return dct.get(qq,float('nan'))
        sh=lambda qq: g(nav,qq)/worldnav[qq] if worldnav.get(qq) else float('nan')
        out.append((s,r['year'],r['winner'],g(nav,q-8),g(nav,q-1),g(nav,q),sh(q-8),sh(q-1),g(fu,q-8),g(fu,q-1),float(sc[q-1]['warExhaustion']) if q-1 in sc else float('nan'),g(ml,q-1),g(ml,q-4)))
    # baseline: UK quarters at war
    for q,r in sc.items():
        if r['atWar'] in ('1','True','true'):
            allex.append(float(r['warExhaustion'])); allbl.append(ml.get(q,float('nan')))
print('種 年 勝者 艦隊(前8) 艦隊(前1) 艦隊(当) 世界比(前8) 世界比(前1) 艦数(前8) 艦数(前1) 厭戦(前1) 封鎖リスク(前1) 封鎖リスク(前4)')
for o in out: print(*[f'{x:.3g}' if isinstance(x,float) else x for x in o])
f=lambda L:[x for x in L if x==x]
print('-- 英の艦隊の世界比(前8)の中央',st.median(f([o[6] for o in out])),'(前1)',st.median(f([o[7] for o in out])))
print('-- 艦隊(前1)/艦隊(前8) の中央',st.median(f([o[4]/o[3] for o in out if o[3]==o[3] and o[3]>0])))
print('-- 英の交戦中の四半期の封鎖リスクの中央',st.median(f(allbl)) if f(allbl) else None,'・矯正の前1の中央',st.median(f([o[11] for o in out])))
