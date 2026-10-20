import sys,csv,statistics as st,collections
sys.stdout.reconfigure(encoding='utf-8')
D='D:/PoB_work/keep_457/m457_ins/'
NAT=['イギリス','フランス','プロイセン','オーストリア','ロシア','オスマン','清','日本']
B=[1783,1788,1793,1798,1803,1808,1813,1818,1823,1828,1833,1838,1843,1848,1858,1868,1878,1898]
def bn(y):
  b=None
  for x in B:
    if y>=x: b=x
  return b
cap=collections.defaultdict(list); pl=collections.defaultdict(list); wk=collections.defaultdict(list); ksv=collections.defaultdict(list); fill=collections.defaultdict(list); rows=collections.defaultdict(list)
for s in range(1,11):
  with open(D+f'factory_s{s}.tsv',encoding='utf-8') as f:
    for x in csv.DictReader(f,delimiter='\t'):
      if x['nation'] in NAT:
        b=bn(int(x['year']))
        try: cap[(x['nation'],b)].append(float(x['capacityRefValuePerPopK'])); fill[(x['nation'],b)].append(float(x['inputFillShare']))
        except: pass
  with open(D+f'nation_s{s}.tsv',encoding='utf-8') as f:
    for x in csv.DictReader(f,delimiter='\t'):
      if x['nation'] in NAT:
        try: pl[(x['nation'],bn(int(x['year'])))].append(float(x['priceLevel']))
        except: pass
  sizes=collections.defaultdict(dict); kmon={}
  with open(D+f'class_s{s}.tsv',encoding='utf-8') as f:
    for x in csv.DictReader(f,delimiter='\t'):
      if x['nation'] in NAT and x['time'].startswith('q'):
        k=(x['nation'],int(x['year']),x['time'])
        sizes[k][x['className']]=float(x['sizeK'])
  for (n,y,t),d in sizes.items():
    tot=sum(d.values())
    if tot>0: wk[(n,bn(y))].append(d.get('工場労働者',0)/tot)
  with open(D+f'engineer_s{s}.tsv',encoding='utf-8') as f:
    r=csv.reader(f,delimiter='\t'); next(r)
    for x in r:
      if x[0]=='T' and x[3] in NAT:
        try: v=float(x[10])
        except: continue
        if v>0: rows[(x[4],x[3],bn(int(x[2])))].append(v)
def md(L): return st.median(L) if L else float('nan')
# common factor: per row x nation normalized by its 1858-1898 median, then median over rows
cf=collections.defaultdict(list)
rn=set((r,n) for r,n,b in rows)
for r,n in rn:
  base=rows.get((r,n,1858),[])+rows.get((r,n,1868),[])+rows.get((r,n,1878),[])
  if not base: continue
  bv=md(base)
  for b in B:
    if rows.get((r,n,b)): cf[(n,b)].append(md(rows[(r,n,b)])/bv)
print('# per nation: commonPriceFactor(real, /1858-87) | capacity/popK (/1858-87) | worker share (/1858-87) | priceLevel | inputFill')
print('nation\t'+'\t'.join(map(str,B)))
for n in NAT:
  cb=md(cap[(n,1858)]+cap[(n,1868)]+cap[(n,1878)]); wb=md(wk[(n,1858)]+wk[(n,1868)]+wk[(n,1878)])
  print(n+' price\t'+'\t'.join(f"{md(cf[(n,b)]):.2f}" for b in B))
  print(n+' cap\t'+'\t'.join(f"{md(cap[(n,b)])/cb:.2f}" for b in B))
  print(n+' wk\t'+'\t'.join(f"{md(wk[(n,b)])/wb:.2f}" for b in B))
  print(n+' PL\t'+'\t'.join(f"{md(pl[(n,b)]):.2f}" for b in B))
  print(n+' fill\t'+'\t'.join(f"{md(fill[(n,b)]):.2f}" for b in B))
