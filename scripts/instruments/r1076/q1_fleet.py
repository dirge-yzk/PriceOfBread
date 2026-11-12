import csv,collections,sys,statistics as st
# (1076) 問い1：keep_1069 の 4 腕で 5 国×1783〜1830 の艦隊・予算・支払い・兵・海の割合
K='D:/PoB_work/keep_1069'
arm=sys.argv[1]; NS=sys.argv[2].split(',') if len(sys.argv)>2 else ['イギリス','フランス','スペイン','ネーデルラント','ロシア']
seeds=range(11,21) if arm=='a11' else range(1,11); d='base_ins' if arm=='base' else arm
SC=['fleetUnitsStock','navalPowerIndex','armyUpkeepShare','navyUpkeepShare','soldierK','navalBudgetCapitalPerMonth','navalBudgetOtherPerMonth','navyPaidPerMonth','navalOrderStatesCount','atWarFlag','capitalWarshipStock','portFleetGoodsStock']
FC=['claimNavyPerMonth','claimSoldierPayPerMonth','ledgerNavyPerMonth']
MC=['needSeaIndex','navalSpendShareIdealShare']
YRS=list(range(1783,1831))
acc=collections.defaultdict(lambda:collections.defaultdict(list))
for s in seeds:
    seen=set()
    with open(f'{K}/{d}/stock_s{s}.tsv',encoding='utf-8') as f:
        for r in csv.DictReader(f,delimiter='\t'):
            n=r['nation']
            if n not in NS: continue
            t=r['time']
            key=(n,t) if t in('open','warm') else (n,int(float(r['year'])))
            if key in seen: continue
            seen.add(key)
            for c in SC:
                try: acc[key][c].append(float(r[c]))
                except: pass
    # fiscal：その年の月の平均
    tmp=collections.defaultdict(lambda:collections.defaultdict(list))
    with open(f'{K}/{d}/fiscal_s{s}.tsv',encoding='utf-8') as f:
        for r in csv.DictReader(f,delimiter='\t'):
            n=r['nation']
            if n not in NS: continue
            y=int(float(r['year']))
            for c in FC:
                try: tmp[(n,y)][c].append(float(r[c]))
                except: pass
    for k,v in tmp.items():
        for c,l in v.items(): acc[k][c].append(sum(l)/len(l))
    seen=set()
    with open(f'{K}/{d}/fm_s{s}.tsv',encoding='utf-8') as f:
        for r in csv.DictReader(f,delimiter='\t'):
            n=r['nation']
            if n not in NS: continue
            y=int(float(r['year'])); key=(n,y)
            if key in seen: continue
            seen.add(key)
            for c in MC:
                try: acc[key][c].append(float(r[c]))
                except: pass
COLS=SC+FC+MC
def m(l): return f'{st.median(l):.3g}' if l else 'nan'
def rg(l): return f'{min(l):.3g}~{max(l):.3g}' if l else ''
for n in NS:
    print(f'# {n} {arm} 種の中央（艦の数は［最小~最大］も）・fiscal は年の月平均・stock/fm は年の最初の行')
    print('年\t'+'\t'.join(COLS)+'\tfleet幅')
    for y in ['open','warm']+YRS:
        a=acc[(n,y)]
        print(y,'\t'.join(m(a[c]) for c in COLS),rg(a['fleetUnitsStock']),sep='\t')
