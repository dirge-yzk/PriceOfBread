# 問い 2: 種ごとに同じ州の軌道を 4 腕で並べ、分かれる最初の四半期を探す（読むだけ）
import sys,csv,os
K='D:/PoB_work/'
ARMS={'A_hf':K+'keep_1341/base_ins','A_sp':K+'keep_1341_rej/aS_spOnly/ins','spB':K+'keep_1341_rej/c140spB/ins','hfsp':K+'keep_1341_rej/mM_hfsp','N1':K+'keep_1341/n1','N2':K+'keep_1341/n2'}
reg=sys.argv[1]; seed=sys.argv[2]
def load(d):
    p=f'{d}/region_s{seed}.tsv'; out={}
    with open(p,encoding='utf-8') as f:
        h=f.readline().rstrip('\n').split('\t'); ix={c:i for i,c in enumerate(h)}
        for line in f:
            if '\t'+reg+'\t' not in line: continue
            a=line.rstrip('\n').split('\t')
            if a[ix['region']]!=reg: continue
            out[int(a[0])]=a; 
    return out,ix
D={}
for k,d in ARMS.items():
    if os.path.exists(f'{d}/region_s{seed}.tsv'): D[k]=load(d)
cols=['nation','populationK','opportunityIndex','holdingFactor','foodFillShare','migrationOutKPerQuarter','migrationInKPerQuarter','wagePremium','employmentShare','housingFactor']
def row(k,q):
    t,ix=D[k]; a=t.get(q)
    return None if a is None else [a[ix[c]] for c in cols]
# first divergence among pairs
for x,y in [('A_hf','hfsp'),('A_sp','hfsp'),('A_sp','spB'),('A_hf','A_sp'),('A_hf','N1')]:
    if x not in D or y not in D: continue
    first=None
    for q in range(668):
        if row(x,q)!=row(y,q): first=q;break
    print(f'# {x} vs {y}: first diff q={first}')
    if first is not None:
        for q in range(max(0,first-1),first+2):
            print('  ',q,x,row(x,q)); print('  ',q,y,row(y,q))
print('# 年ごと（第0四半期）')
print('q\tyear\t'+'\t'.join(f'{k}:own/pop/opp/food/out/in' for k in D))
for q in range(0,668,8):
    s=[str(q),str(1783+q//4)]
    for k in D:
        r=row(k,q)
        s.append('-' if r is None else f'{r[0][:3]}/{float(r[1]):.1f}/{float(r[2]):.2f}/{float(r[4]):.2f}/{float(r[5]):.2f}/{float(r[6]):.2f}')
    print('\t'.join(s))
