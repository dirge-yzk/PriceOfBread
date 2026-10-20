import sys,csv,statistics as st,collections
sys.stdout.reconfigure(encoding='utf-8')
D='D:/PoB_work/keep_457/m457_ins/'
T=['w0','w4','w10','w19','q0','q4','q8','q16','q24','q32','q40','q47']
R=collections.defaultdict(list)   # (good,time)->ratio list
P=collections.defaultdict(list); Dm=collections.defaultdict(list); S=collections.defaultdict(list); Sh=collections.defaultdict(list)
tier={}
for s in range(1,11):
  with open(D+f'pxgoods_s{s}.tsv',encoding='utf-8') as f:
    r=csv.DictReader(f,delimiter='\t')
    for x in r:
      t=x['time']
      if t not in T: continue
      g=x['goodName']; tier[g]=x['tier']
      try:
        v=float(x['priceOverRefTimesLevelMedianRatio'])
      except: continue
      if v!=v: continue
      key=(g,t)
      R[key].append(v)
      P[key].append(float(x['lastProducedQty'])); Dm[key].append(float(x['lastDemandQty'])); S[key].append(float(x['stockQty']))
      d=float(x['lastDemandQty']); Sh[key].append(float(x['lastShortageQty'])/d if d>0 else 0)
goods=sorted(set(g for g,_ in R), key=lambda g:(tier[g],g))
def md(L): return st.median(L) if L else float('nan')
print('good\ttier\t'+'\t'.join(T))
for g in goods:
  print(g+'\t'+tier[g]+'\t'+'\t'.join(f"{md(R[(g,t)]):.2f}" for t in T))
print('\n# produced / demand / shortage share / stock (median over 8 nations x 10 seeds)')
for g in goods:
  row=[]
  for t in ['w0','w19','q0','q24','q47']:
    row.append(f"{md(P[(g,t)]):.0f}/{md(Dm[(g,t)]):.0f}/{md(Sh[(g,t)]):.2f}/{md(S[(g,t)]):.0f}")
  print(g+'\t'+'\t'.join(row))
