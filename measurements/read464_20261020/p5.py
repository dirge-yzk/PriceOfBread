import sys,csv,statistics as st,collections
sys.stdout.reconfigure(encoding='utf-8')
D='D:/PoB_work/keep_457/m457_ins/'
NAT=['イギリス','フランス','プロイセン','オーストリア','ロシア','オスマン','清','日本']
T=['w0','w2','w5','w10','w19','q0','q4','q8','q12','q20','q28','q36','q47']
R=collections.defaultdict(list); SH=collections.defaultdict(list); PD=collections.defaultdict(list); LV=collections.defaultdict(list)
skip={'ガス','ラジオ','缶詰','自転車'}
for s in range(1,11):
  per=collections.defaultdict(list); sh=collections.defaultdict(list); pd=collections.defaultdict(list)
  with open(D+f'pxgoods_s{s}.tsv',encoding='utf-8') as f:
    for x in csv.DictReader(f,delimiter='\t'):
      t=x['time']
      if t not in T or x['nation'] not in NAT or x['goodName'] in skip: continue
      try: v=float(x['priceOverRefTimesLevelMedianRatio'])
      except: continue
      if v!=v: continue
      k=(x['nation'],t,x['tier']=='Staple')
      per[k].append(v)
      d=float(x['lastDemandQty']); p=float(x['lastProducedQty'])
      if d>0: sh[k].append(float(x['lastShortageQty'])/d); pd[k].append(p/d)
      LV[(x['nation'],t)].append(float(x['regionPriceLevelMeanIndex']))
  for k,L in per.items(): R[k].append(st.median(L))
  for k,L in sh.items(): SH[k].append(st.median(L))
  for k,L in pd.items(): PD[k].append(st.median(L))
def md(L): return st.median(L) if L else float('nan')
for stp,lab in [(False,'Daily+Luxury'),(True,'Staple')]:
  print(f'# {lab}: median good price/(ref*level) | shortage share | produced/demand')
  print('nation\t'+'\t'.join(T))
  for n in NAT:
    print(n+'\t'+'\t'.join(f"{md(R[(n,t,stp)]):.2f}" for t in T))
  for n in NAT:
    print(n+' sh\t'+'\t'.join(f"{md(SH[(n,t,stp)]):.2f}" for t in T))
  for n in NAT:
    print(n+' p/d\t'+'\t'.join(f"{md(PD[(n,t,stp)]):.2f}" for t in T))
print('# region price level index mean')
for n in NAT: print(n+'\t'+'\t'.join(f"{md(LV[(n,t)]):.2f}" for t in T))
