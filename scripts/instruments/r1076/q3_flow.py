import csv,collections,sys,statistics as st
# (1076) 問い3：1783〜1810 の艦の出入りの近似（年ごと・種の中央）
# 損耗＝年の初めの艦×(1−0.995^12)（月 0.5%＝stock_s の四半期の比で確かめた）
# 買った近似＝造船の産出（首都＋首都以外・月の値×3 を四半期の行ごとに足す）＋市場の在庫の取り崩し（首都＋港＋植民地＋内陸の艦の財の在庫の減り）
#   ＝他国へ売れた分・他国から買った分を区別できない＝近似（計器の列が無い）
# 残り（摩耗と割譲の近似）＝年の初めの艦 − 損耗 + 買った近似 − 次の年の初めの艦 − 割譲で出た分
K='D:/PoB_work/keep_1069'
arm=sys.argv[1]; N=sys.argv[2]
seeds=range(11,21) if arm=='a11' else range(1,11); d='base_ins' if arm=='base' else arm
Y=range(1783,1811)
acc=collections.defaultdict(lambda:collections.defaultdict(list))
STK=['capitalWarshipStock','portFleetGoodsStock','colonialFleetGoodsStock','inlandFleetGoodsStock']
for s in seeds:
    first={}; out=collections.defaultdict(float); paid=collections.defaultdict(float); bud=collections.defaultdict(float); war=collections.defaultdict(list); stk={}; ced={}
    with open(f'{K}/{d}/stock_s{s}.tsv',encoding='utf-8') as f:
        for r in csv.DictReader(f,delimiter='\t'):
            if r['nation']!=N or r['time'] in('open','warm'): continue
            y=int(float(r['year']))
            g=lambda c: float(r[c]) if r[c] not in('','nan','NaN') else 0.0
            if y not in first:
                first[y]=g('fleetUnitsStock'); stk[y]=sum(g(c) for c in STK); ced[y]=g('fleetUnitsMovedOutHomeCum')+g('fleetUnitsMovedOutColonialCum')+g('fleetUnitsMovedOutCededCum')
            out[y]+=3*(g('capitalFleetGoodsOutputPerMonth')+g('otherFleetGoodsOutputPerMonth'))
            paid[y]+=3*g('navyPaidPerMonth'); bud[y]+=3*(g('navalBudgetCapitalPerMonth')+g('navalBudgetOtherPerMonth')); war[y].append(g('atWarFlag'))
    for y in Y:
        if y not in first or y+1 not in first: continue
        f0=first[y]; f1=first[y+1]; wear=f0*(1-0.995**12)
        draw=stk[y]-stk[y+1]; bought=out[y]+max(0.0,draw)
        cd=ced[y+1]-ced[y]
        rest=f0-wear+bought-f1-cd
        rest0=f0-wear+out[y]-f1-cd   # 下限：取り崩しを買いに数えない（平時の予算 0 の年に 残り＝取り崩し となり、取り崩しは買いではないと読めた）
        for k,v in (('艦',f0),('損耗',wear),('産出',out[y]),('在庫取崩',draw),('割譲で出',cd),('残り_取崩を買いに数える',rest),('残り_産出だけ',rest0),('予算',bud[y]),('支払',paid[y]),('支払/予算',paid[y]/bud[y] if bud[y]>0 else float('nan')),('戦の割合',sum(war[y])/len(war[y]))):
            acc[y][k].append(v)
K2=['艦','損耗','産出','在庫取崩','割譲で出','残り_取崩を買いに数える','残り_産出だけ','予算','支払','支払/予算','戦の割合']
print(N,arm,'年ごと・種の中央（年の値＝四半期の行の和・月の値×3）')
print('年\t'+'\t'.join(K2))
for y in Y:
    a=acc[y]
    if not a: continue
    print(y,'\t'.join(f"{st.median([x for x in a[k] if x==x]):.3g}" if [x for x in a[k] if x==x] else 'nan' for k in K2),sep='\t')
Y0,Y1=(int(sys.argv[3]),int(sys.argv[4])) if len(sys.argv)>4 else (1790,1799)
print(f'# {Y0}〜{Y1} の和（各年の種の中央の和）')
for k in ['損耗','産出','在庫取崩','割譲で出','残り_取崩を買いに数える','残り_産出だけ']:
    print('#',k,f"{sum(st.median([x for x in acc[y][k] if x==x]) for y in range(Y0,Y1+1) if acc[y][k]):.0f}")
print('# 艦',Y0,f"{st.median(acc[Y0]['艦']):.0f}",'→',Y1+1,f"{st.median(acc[Y1+1]['艦']):.0f}" if acc[Y1+1]['艦'] else 'nan')
