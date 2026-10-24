import csv, glob, re, sys, statistics as st, collections
csv.field_size_limit(1<<30)
def load(D, tbl, cols):
    out=collections.defaultdict(list)
    for f in glob.glob(f'{D}/{tbl}_s*.tsv'):
        s=int(re.search(r'_s(\d+)\.tsv',f).group(1))
        with open(f,encoding='utf-8',newline='') as fh:
            for r in csv.DictReader(fh,delimiter='\t'):
                out[(s,r['nation'],int(r['year']) if r['year'].isdigit() else -1)].append({c:r.get(c) for c in cols})
    return out
def fl(x):
    try: return float(x)
    except: return float('nan')
A='D:/PoB_work/keep_533/m533_ins'; B='D:/PoB_work/wp_round2/out_B_10/ins'
cols=['capitalEquipFillShare','militaryReadinessShare','capitalRifleStock','capitalArtilleryStock','fleetUnitsStock','landPowerIndex','navalPowerIndex']
a=load(A,'stock',cols); b=load(B,'stock',cols)
eras=[(1783,1810),(1810,1840),(1840,1873),(1873,1900),(1900,1914),(1914,1950)]
for col in cols:
  print(col)
  for lo,hi in eras:
    va=[fl(r[col]) for k,v in a.items() if lo<=k[2]<hi for r in v]
    vb=[fl(r[col]) for k,v in b.items() if lo<=k[2]<hi for r in v]
    va=[x for x in va if x==x]; vb=[x for x in vb if x==x]
    print(f'  {lo}-{hi}: base median {st.median(va):.3f} mean {st.mean(va):.3f} (n={len(va)}) | wp median {st.median(vb):.3f} mean {st.mean(vb):.3f} (n={len(vb)})')
