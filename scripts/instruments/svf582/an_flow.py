import csv,sys,collections
d=sys.argv[1]; s=sys.argv[2]
mint={}; fiat={}; M={}
for r in csv.DictReader(open(f'{d}/money_s{s}.tsv',encoding='utf-8'),delimiter='\t'):
    t=r['time']; q=0 if t=='w0' else (int(t[1:]) if t.startswith('q') else None)
    if q is None: continue
    mint[(r['nation'],q)]=float(r['cumulativeGoldMintedStock']); M[(r['nation'],q)]=float(r['broadMoneyStock'])
gdp={}
for r in csv.DictReader(open(f'{d}/nation_s{s}.tsv',encoding='utf-8'),delimiter='\t'):
    gdp[(r['nation'],int(r['quarter']))]=float(r['nominalGdpPerYear'])
print('mint flow per year / nominal GDP-year, windows')
wins=[(0,8),(8,16),(16,24),(24,39),(39,60),(60,79)]
for n in ['清','ロシア','オスマン','フランス','イギリス','プロイセン','オーストリア','日本','アメリカ']:
    out=[]
    for a,b in wins:
        if (n,a) in mint and (n,b) in mint:
            dm=mint[(n,b)]-mint[(n,a)]; yrs=(b-a)/4
            g=sum(gdp.get((n,q),0) for q in range(a+1,b+1))/max(1,b-a)
            out.append(f'{dm/yrs/g:6.3f}' if g else '   nan')
    print(f'{n:8s}',' '.join(out), ' M/GDP q1:', round(M[(n,1)]/gdp[(n,1)],3))
