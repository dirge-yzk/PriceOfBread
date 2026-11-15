# (1163) 組み直しの不一致の内訳（四半期 40・種 1）
import collections, math
D='D:/PoB_work/keep_1157_rej/c113cr_mut/CTL/ins'
def fl(x):
    try: return float(x)
    except ValueError: return float('nan')
O={}
with open(D+'/rulepower_s1.tsv',encoding='utf-8') as f:
    h=f.readline().rstrip('\n').split('\t');ix={k:j for j,k in enumerate(h)}
    for l in f:
        c=l.rstrip('\n').split('\t')
        if int(c[0])==40: O.setdefault(c[ix['nation']],fl(c[ix['oEngNationIndex']]))
rows=collections.defaultdict(list)
with open(D+'/rulepoweralloc_s1.tsv',encoding='utf-8') as f:
    h=f.readline().rstrip('\n').split('\t');ix={k:j for j,k in enumerate(h)}
    for l in f:
        c=l.rstrip('\n').split('\t')
        if int(c[0])!=40 or c[ix['candEngFlag']]!='1': continue
        rows[c[ix['nation']]].append((c[ix['region']],fl(c[ix['wEngIndex']]),fl(c[ix['allocEngIndex']]),fl(c[ix['rhoF']]),fl(c[ix['fForeignEngIndex']])))
for n,rs in rows.items():
    sw=sum(r[1] for r in rs); sa=sum(r[2] for r in rs); o=O.get(n,float('nan'))
    bad=[r for r in rs if not (abs(o*r[1]/sw-r[2])<=1e-9*max(1,abs(r[2])) and abs(r[2]*r[3]-r[4])<=1e-9*max(1,abs(r[4])))] if sw>0 else rs
    if bad: print(n,'O',o,'Σalloc',sa,'Σw',sw,'行',len(rs),'不一致',len(bad),bad[0])
