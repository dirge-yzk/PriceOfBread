"""(270) 線B2：一国の解禁エピソードの静的な反事実（歩幅を政治予算の係数で割り戻して再生）。python cf_b2.py <計器の出力ディレクトリ> <nationIndex>。腕Bの木では政治予算を既に掛けていないので反事実は意味を持たない（腕Aの木で読む）。"""
import csv,collections,statistics,sys,math
D=sys.argv[1]; NAT=sys.argv[2]
def fl(x):
    try: return float(x)
    except: return float('nan')
rows=[]
for s in range(1,11):
    with open(f'{D}/nation_s{s}.tsv',encoding='utf-8',errors='replace') as f:
        for r in csv.DictReader(f,delimiter='\t'):
            if r['nationIndex']==NAT: r['seed']=s; rows.append(r)
def stage(c): return min(4,max(1,int(math.floor(c*4+0.5))))
eps=[];cur=None
for r in rows:
    q=int(r['quarter']);op=r['warStageOpenAfterGovFlag']=='1'
    if op:
        if cur and cur['seed']==r['seed'] and cur['last']==q-1: cur['rows'].append(r);cur['last']=q
        else: cur={'seed':r['seed'],'last':q,'rows':[r]};eps.append(cur)
# baseline counts
base_reach=sum(1 for e in eps if any(stage(fl(r['militarySpendingAfterGovShare']))==4 for r in e['rows']))
base_vessel=sum(1 for e in eps for r in e['rows'] if fl(r['militarySpendingAfterGovShare'])>0.75 and int(r['militaryStageTargetAfterGovStage'])==4)
print('episodes',len(eps),'open q',sum(len(e['rows']) for e in eps),'base reach4',base_reach,'base vessel(q)',base_vessel)
for mode in ['nopush','nopush_ungated']:
    reach=0;vessel=0;lens=[]
    for e in eps:
        c=fl(e['rows'][0]['militaryMoveCurrentShare']); reached=False
        for r in e['rows']:
            f=fl(r['militaryMoveStepFraction']); pb=fl(r['militaryMovePushBudgetFactor'])
            f2=f/pb if pb>0 else f
            if mode=='nopush': tgt=min(stage(c)+1,4)/4
            else: tgt=1.0
            c=c+f2*(tgt-c)
            if stage(c)==4: reached=True
            if c>0.75 and tgt==1.0: vessel+=1
        reach+=reached
    print(mode,'reach',reach,'vessel q',vessel)
# length distribution
L=[len(e['rows']) for e in eps]
print('len>=8',sum(l>=8 for l in L),'len>=5',sum(l>=5 for l in L),'len>=3',sum(l>=3 for l in L))
