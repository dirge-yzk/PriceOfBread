import sys,csv,statistics as st,collections
sys.stdout.reconfigure(encoding='utf-8')
D='D:/PoB_work/keep_457/m457_ins/'
NAT=['イギリス','フランス','プロイセン','オーストリア','ロシア','オスマン','清','日本']
T=['q0','q4','q8','q20','q40','q80','q160','q240']
M=collections.defaultdict(list); E=collections.defaultdict(list); P=collections.defaultdict(list)
cl=['農民','工場労働者','職人','使用人','資本家','貴族']
for s in range(1,11):
  with open(D+f'class_s{s}.tsv',encoding='utf-8') as f:
    for x in csv.DictReader(f,delimiter='\t'):
      if x['time'] not in T or x['nation'] not in NAT or x['className'] not in cl: continue
      try:
        m=float(x['savingsToEarnedMonths']); e=float(x['earnedPerMonth']); p=float(x['paidPerMonth'])
      except: continue
      k=(x['nation'],x['time'],x['className'])
      M[k].append(m); 
      if e>0: P[k].append(p/e)
def md(L): return st.median(L) if L else float('nan')
print('# savings / earned per month (months) ; paid/earned')
for c in cl:
  print('##',c); print('nation\t'+'\t'.join(T))
  for n in NAT:
    print(n+'\t'+'\t'.join(f"{md(M[(n,t,c)]):.1f}|{md(P[(n,t,c)]):.2f}" for t in T))
