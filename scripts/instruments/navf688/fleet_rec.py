import csv, sys, statistics as st, collections
SEVEN=['イギリス','フランス','プロイセン','ロシア','オーストリア','アメリカ','日本']
arms=[(a.split('=')[0],a.split('=')[1].split(',')[0],int(a.split('=')[1].split(',')[1])) for a in sys.argv[1:]]
YEARS=(1790,1815,1850,1913)
print('国x年: 艦の数の種中央値（腕の並び='+' / '.join(a[0] for a in arms)+'）')
data={}
for lab,d,sb in arms:
    v=collections.defaultdict(list); ret=collections.defaultdict(list)
    for s in range(sb,sb+10):
        for x in csv.DictReader(open(f'{d}/stock_s{s}.tsv',encoding='utf-8-sig'),delimiter='\t'):
            t=x['time']
            if t.startswith('q') and t[1:].isdigit() and int(t[1:])%4==3:
                y=int(x['year'])
                if y in YEARS and x['nation'] in SEVEN: v[(x['nation'],y)].append(float(x['fleetUnitsStock']))
            if t.startswith('q') and t[1:].isdigit() and int(t[1:])==667 and x['nation'] in SEVEN:
                for c in ('fleetUnitsReturnedToCapitalCum','fleetUnitsMovedOutCededCum','fleetUnitsMovedOutHomeCum'):
                    if c in x: ret[(x['nation'],c)].append(float(x[c]))
    data[lab]=(v,ret)
for n in SEVEN:
    for y in YEARS:
        print(n,y,' / '.join(str(round(st.median(data[l][0][(n,y)]))) if data[l][0][(n,y)] else '-' for l,_,_ in arms))
print('累計（終端 q667・種中央値）：')
for n in SEVEN:
    for c in ('fleetUnitsReturnedToCapitalCum','fleetUnitsMovedOutCededCum','fleetUnitsMovedOutHomeCum'):
        print(n,c,' / '.join(str(round(st.median(data[l][1][(n,c)]))) if data[l][1][(n,c)] else '-' for l,_,_ in arms))
