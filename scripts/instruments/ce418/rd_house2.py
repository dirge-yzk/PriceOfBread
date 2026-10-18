import csv,sys
f=sys.argv[1]; names=sys.argv[2:]
rows=list(csv.DictReader(open(f,encoding='utf-8'),delimiter='\t'))
ks=['Clothing','Furniture','PreservedMeat','DriedFish','Beer','Spirits','Cheese','LeatherGoods']
for nat in names:
  rs=[r for r in rows if r['nation']==nat]
  print(nat,len(rs),sorted(set(int(r['quarter']) for r in rs))[:12])
  for r in rs:
    q=int(r['quarter'])
    if q in (3,7,11,19,31,43,79,119,159):
      print(q,'rows',r['homeRowsCount'],'w',r['homeLaborWeightShare'][:5],' '.join(k[:4]+'='+r['homeShare'+k][:4] for k in ks),'FL b/p',r['farmLaborBoughtPerMonth'][:7],r['farmLaborProducedPerMonth'][:7],'use',r['homeFarmLaborUsePerMonth'][:7])
