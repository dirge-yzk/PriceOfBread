import sys,csv,statistics as st,collections
sys.stdout.reconfigure(encoding='utf-8')
D='D:/PoB_work/keep_457/m457_ins/'
NAT=['イギリス','フランス','プロイセン','オーストリア','ロシア','オスマン','清','日本']
Q=[0,4,8,20,40,60,80,120,160,200,260]
cols=['capacityRefValuePerPopK','outputRefValuePerMonth','inputFillShare','materialFillShare','priceFactorShare','laborRationMeanShare','plainLaborRationMeanShare','advLaborRationMeanShare','factoryActiveCount','factoryIdleCount','factoryBankruptCount','industrializationMean']
V=collections.defaultdict(list)
for s in range(1,11):
  with open(D+f'factory_s{s}.tsv',encoding='utf-8') as f:
    r=csv.DictReader(f,delimiter='\t')
    for x in r:
      q=int(x['quarter'])
      if q not in Q or x['nation'] not in NAT: continue
      for c in cols:
        try: V[(x['nation'],q,c)].append(float(x[c]))
        except: pass
      try:
        lb=float(x['laborBoundLossRefValuePerMonth']); ob=float(x['otherBoundLossRefValuePerMonth']); de=float(x['desiredRefValuePerMonth'])
        V[(x['nation'],q,'laborLoss/desired')].append(lb/de if de>0 else 0)
        V[(x['nation'],q,'otherLoss/desired')].append(ob/de if de>0 else 0)
        V[(x['nation'],q,'desired/capacity')].append(de/float(x['capacityRefValuePerMonth']))
      except: pass
def md(L): return st.median(L) if L else float('nan')
for n in NAT:
  print('##',n); print('col\t'+'\t'.join(f"q{q}" for q in Q))
  for c in cols+['laborLoss/desired','otherLoss/desired','desired/capacity']:
    print(c+'\t'+'\t'.join(f"{md(V[(n,q,c)]):.3g}" for q in Q))
