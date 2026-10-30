import csv,sys,statistics as st,collections,glob,os
arms=[a.split('=',1) for a in sys.argv[1:]]
NAT=['イギリス','フランス','プロイセン','ロシア','オーストリア','アメリカ','日本','清']
print('# 艦の流出の累計（stock_s・最後の行・種の中央値）：国 腕 [出た艦 Home・割譲で出た Ceded・首都へ戻した Returned]')
for lab,d in arms:
    acc=collections.defaultdict(lambda: collections.defaultdict(list))
    for s in range(1,11):
        p=f'{d}/stock_s{s}.tsv'
        if not os.path.exists(p): continue
        last={}
        for x in csv.DictReader(open(p,encoding='utf-8-sig'),delimiter='\t'):
            if x['nation'] in NAT: last[x['nation']]=x
        for n,x in last.items():
            for c in ('fleetUnitsMovedOutHomeCum','fleetUnitsMovedOutCededCum','fleetUnitsReturnedToCapitalCum'):
                if c in x and x[c] not in ('',None): acc[n][c].append(float(x[c]))
    print(lab,' '.join(f"{n}[{'/'.join(str(round(st.median(acc[n][c]))) if acc[n][c] else '-' for c in ('fleetUnitsMovedOutHomeCum','fleetUnitsMovedOutCededCum','fleetUnitsReturnedToCapitalCum'))}]" for n in NAT[:5]))
print('# 本国格の州の都市の割合（region_s・Core/Integrated・1−農村÷人口・年の最後の四半期・種の中央値）')
YRS=(1800,1820,1850,1880,1900,1913,1930)
for lab,d in arms:
    out=collections.defaultdict(list)
    for s in range(1,11):
        p=f'{d}/region_s{s}.tsv'; q=f'{d}/nation_s{s}.tsv'
        if not os.path.exists(p): continue
        idx={}
        for x in csv.DictReader(open(q,encoding='utf-8-sig'),delimiter='\t'):
            idx[(x['quarter'],x['nationIndex'])]=x['nation']
        agg=collections.defaultdict(lambda:[0.0,0.0])
        for x in csv.DictReader(open(p,encoding='utf-8-sig'),delimiter='\t'):
            if x.get('tier')=='Colonial': continue
            qq=int(x['quarter']); y=1783+qq//4
            if qq%4!=3 or y not in YRS: continue
            n=idx.get((x['quarter'],x['nationIndex']))
            if n not in NAT: continue
            a=agg[(n,y)]; a[0]+=float(x['populationK']); a[1]+=float(x['ruralPopulationK'])
        for k,(P,R) in agg.items():
            if P>0: out[k].append(1-R/P)
    print(lab,' | '.join(n+' '+' '.join(f"{y}:{st.median(out[(n,y)]):.2f}" if out[(n,y)] else f'{y}:-' for y in YRS) for n in ['イギリス','フランス','プロイセン','アメリカ','日本','清']))
