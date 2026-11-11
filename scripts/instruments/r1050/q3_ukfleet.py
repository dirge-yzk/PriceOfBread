import csv,collections,sys,statistics as st
K='D:/PoB_work/keep_1044'
arm=sys.argv[1]; N=sys.argv[2] if len(sys.argv)>2 else 'イギリス'
seeds=range(11,21) if arm=='a11' else range(1,11); d='base_ins' if arm=='base' else arm
COLS=['fleetUnitsStock','fleetGaugeIndex','navalPowerIndex','navyUpkeepShare','capitalWarshipStock','portFleetGoodsStock','capitalFleetGoodsOutputPerMonth','otherFleetGoodsOutputPerMonth','navalBudgetCapitalPerMonth','navalBudgetOtherPerMonth','navyPaidPerMonth','fleetUnitsMovedOutHomeCum','fleetUnitsMovedOutColonialCum','fleetUnitsMovedOutCededCum','fleetUnitsReceivedCededCum','soldierK','gdpRealPerYear','atWarFlag']
YRS=[1783,1785,1788,1790,1793,1796,1800,1805,1810,1820,1850,1900]
acc=collections.defaultdict(lambda:collections.defaultdict(list))
for s in seeds:
    seen=set()
    with open(f'{K}/{d}/stock_s{s}.tsv',encoding='utf-8') as f:
        for r in csv.DictReader(f,delimiter='\t'):
            if r['nation']!=N: continue
            y=int(r['year'])
            if y in YRS and y not in seen:
                seen.add(y)
                for c in COLS:
                    try: acc[y][c].append(float(r[c]))
                    except: pass
print(N,arm,'（各年の最初の行・種の中央）')
print('年\t'+'\t'.join(COLS))
for y in YRS:
    print(y,'\t'.join(f'{st.median(acc[y][c]):.3g}' if acc[y][c] else 'nan' for c in COLS),sep='\t')
