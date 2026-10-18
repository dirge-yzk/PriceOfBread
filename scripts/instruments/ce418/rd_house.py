import csv,sys
f=sys.argv[1]
rows=list(csv.DictReader(open(f,encoding='utf-8'),delimiter='\t'))
print(len(rows))
ks=['Clothing','Furniture','PreservedMeat','DriedFish','Beer','Spirits','Cheese','LeatherGoods']
for nat in sorted(set(r['nation'] for r in rows)):
  rs=[r for r in rows if r['nation']==nat]
  print(nat,len(rs))
  for q in (0,1,4,8,20,40,80,120,159):
    r=[x for x in rs if int(x['quarter'])==q]
    if not r: continue
    r=r[0]
    print(q,'rows',r['homeRowsCount'],'w',r['homeLaborWeightShare'][:5],' '.join(k[:4]+'='+r['homeShare'+k][:4] for k in ks),'FL b/p',r['farmLaborBoughtPerMonth'][:7],r['farmLaborProducedPerMonth'][:7],'use',r['homeFarmLaborUsePerMonth'][:7])
