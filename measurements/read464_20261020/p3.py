import sys,csv,statistics as st,collections
sys.stdout.reconfigure(encoding='utf-8')
D='D:/PoB_work/keep_457/m457_ins/'
NAT={'イギリス','フランス','プロイセン','オーストリア','ロシア','オスマン','清','日本'}
B=[1783,1788,1793,1798,1803,1808,1813,1818,1823,1828,1833,1838,1843,1848,1858,1868,1878]
def bin_(y):
  b=None
  for x in B:
    if y>=x: b=x
  return b
rep=collections.defaultdict(list); cst=collections.defaultdict(list); steady=collections.defaultdict(list); steadyc=collections.defaultdict(list)
for s in range(1,11):
  with open(D+f'disc_s{s}.tsv',encoding='utf-8') as f:
    r=csv.reader(f,delimiter='\t'); h=next(r)
    for x in r:
      if x[0]!='T' or x[3] not in NAT: continue
      try: v=float(x[8]); c=float(x[9])
      except: continue
      if not(v>0): continue
      y=int(x[2]); row=x[4]
      if 1850<=y<1900: steady[row].append(v); steadyc[row].append(c) if c>0 else None
      b=bin_(y)
      if b and y<1888: rep[(row,b)].append(v); 
      if b and y<1888 and c>0: cst[(row,b)].append(c)
rows=sorted(set(r for r,_ in rep))
print('row\tsteady(1850-99)\t'+'\t'.join(str(b) for b in B)+'\tsettleYr(<=1.3x)')
for row in rows:
  if not steady[row]: continue
  sv=st.median(steady[row])
  vals=[(b,st.median(rep[(row,b)])/sv) if rep[(row,b)] else (b,float('nan')) for b in B]
  sett='-'
  for i,(b,v) in enumerate(vals):
    if all((vv<=1.3) for _,vv in vals[i:] if vv==vv): sett=b; break
  print(f"{row}\t{sv:.3g}\t"+'\t'.join(f"{v:.2f}" for _,v in vals)+f"\t{sett}")
print('\n# cost of new recipe, ratio to 1850-99')
for row in rows:
  if not steadyc[row]: continue
  sv=st.median(steadyc[row])
  vals=[st.median(cst[(row,b)])/sv if cst[(row,b)] else float('nan') for b in B]
  print(f"{row}\t{sv:.3g}\t"+'\t'.join(f"{v:.2f}" for v in vals))
