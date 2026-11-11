# 必要性の確かめ：級 E と級 N の単位費用（産出の重み・和÷和）・unitCostIfAbundance
import sys,json,glob
from collections import defaultdict
RAW=sys.argv[1]; G=json.load(open(sys.argv[2],encoding='utf-8-sig'))
gn={g['id']:g['name'] for g in G}
acc=defaultdict(lambda:[0.0,0.0,0.0,0.0])
for s in range(1,11):
    f=open(f'{RAW}/recipeout_s{s}.tsv',encoding='utf-8'); h=f.readline().rstrip('\n').split('\t'); ix={c:i for i,c in enumerate(h)}
    for l in f:
        r=l.rstrip('\n').split('\t')
        if r[ix['class']] not in('E','N','X'): continue
        y=int(r[0]);
        if y not in(1790,1870,1913): continue
        o=float(r[ix['outputSum']]); u=float(r[ix['unitCostOutW']]); a=float(r[ix['unitCostIfAbundance']]); sc=float(r[ix['selfCostOutW']])
        if not(o>0) or u!=u: continue
        k=(y,r[ix['class']]); A=acc[k]; A[0]+=o; A[1]+=o*u; A[2]+=o*a; A[3]+=o*sc
        k=(y,r[ix['class']],r[ix['good']]); A=acc[k]; A[0]+=o; A[1]+=o*u; A[2]+=o*a; A[3]+=o*sc
for k in sorted(acc,key=str):
    if len(k)==2 or k[2] in ('鉄鉱石','石炭','銅鉱','木','畜産','麦','硫黄','金','漁獲') :
        A=acc[k]; nm=k[2] if len(k)==3 else 'ALL'
        print(k[0],k[1],nm,f'out={A[0]:.0f} unit={A[1]/A[0]:.4f} ifAb={A[2]/A[0]:.4f} self={A[3]/A[0]:.4f}')
