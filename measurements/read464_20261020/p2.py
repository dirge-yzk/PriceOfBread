import sys,csv,statistics as st,collections
sys.stdout.reconfigure(encoding='utf-8')
D='D:/PoB_work/keep_457/m457_ins/'
T=['open','warm','q0','q20','q40','q80','q120','q160','q240']
NAT=['イギリス','フランス','プロイセン','オーストリア','ロシア','オスマン','清','日本']
sz=collections.defaultdict(list); sv=collections.defaultdict(list)
tot=collections.defaultdict(list)
for s in range(1,11):
  acc=collections.defaultdict(float); accs=collections.defaultdict(float)
  rows={}
  with open(D+f'class_s{s}.tsv',encoding='utf-8') as f:
    r=csv.DictReader(f,delimiter='\t')
    for x in r:
      if x['time'] not in T or x['nation'] not in NAT: continue
      k=(x['nation'],x['time'],x['className'])
      rows[k]=(float(x['sizeK']),float(x['savingsStock']))
  for (n,t,c),(a,b) in rows.items():
    tsum=sum(v[0] for (n2,t2,c2),v in rows.items() if n2==n and t2==t)
    sz[(n,t,c)].append(a/tsum if tsum>0 else 0); sv[(n,t,c)].append(b)
classes=['貴族','農民','工場労働者','資本家','技師','職人','使用人','商人','兵士','教師','官僚','医者','聖職者']
def md(L): return st.median(L) if L else float('nan')
for n in NAT:
  print('##',n,'class share % of pop (median over seeds)')
  print('class\t'+'\t'.join(T))
  for c in classes:
    print(c+'\t'+'\t'.join(f"{100*md(sz[(n,t,c)]):.2f}" for t in T))
  print('savings (median, k)')
  for c in classes:
    print(c+'\t'+'\t'.join(f"{md(sv[(n,t,c)])/1000:.0f}" for t in T))
