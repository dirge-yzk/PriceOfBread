import sys,csv,statistics as st,collections
sys.stdout.reconfigure(encoding='utf-8')
D='D:/PoB_work/keep_457/m457_ins/'
NAT=['イギリス','フランス','プロイセン','オーストリア','ロシア','オスマン','清','日本']
Q=list(range(0,48,4))+[47]
T=['q%d'%q for q in Q]
M=collections.defaultdict(list); R=collections.defaultdict(list)
for s in range(1,11):
  with open(D+f'class_s{s}.tsv',encoding='utf-8') as f:
    for x in csv.DictReader(f,delimiter='\t'):
      if x['time'] in T and x['nation'] in NAT and x['className']=='農民':
        try: M[(x['nation'],x['time'])].append(float(x['savingsToEarnedMonths']))
        except: pass
  per=collections.defaultdict(list); W=collections.defaultdict(float); WV=collections.defaultdict(float)
  with open(D+f'pxgoods_s{s}.tsv',encoding='utf-8') as f:
    for x in csv.DictReader(f,delimiter='\t'):
      if x['time'] in T and x['nation'] in NAT and x['tier']!='Staple':
        try:
          pm=float(x['priceMean']); rf=float(x['refPriceIndex']); lv=float(x['regionPriceLevelMeanIndex']); sold=float(x['lastSoldQty'])
        except: continue
        if sold>0 and rf>0 and lv>0:
          k=(x['nation'],x['time']); W[k]+=sold*rf; WV[k]+=sold*rf*(pm/(rf*lv))
  for k in W: R[k].append(WV[k]/W[k])
def md(L): return st.median(L) if L else float('nan')
print('# peasant savings months | Daily+Lux sold-weighted price/(ref*level)')
print('nation\t'+'\t'.join(T))
for n in NAT:
  print(n+'\t'+'\t'.join(f"{md(M[(n,t)]):.0f}|{md(R[(n,t)]):.2f}" for t in T))
