"""(270) 線B2：事前登録の線の数（英＝nationIndex 2 と全ての国）。python base_lines.py <計器の出力ディレクトリ> <タグ>。エピソード・届いた・器が立つ四半期（warStageEnvelopeShareAfterGov>0）・張り付き・不履行の累計の和。"""
import csv,collections,statistics,sys,math
D=sys.argv[1]; TAG=sys.argv[2] if len(sys.argv)>2 else ''
def fl(x):
    try: return float(x)
    except: return float('nan')
UK='2'
by=collections.defaultdict(list)
for s in range(1,11):
    with open(f'{D}/nation_s{s}.tsv',encoding='utf-8',errors='replace') as f:
        for r in csv.DictReader(f,delimiter='\t'):
            r['seed']=s; by[(s,r['nationIndex'])].append(r)
def stage(c): return min(4,max(1,int(math.floor(c*4+0.5))))
def stats(sel):
    T=collections.Counter()
    for (s,n),rows in by.items():
        if not sel(n): continue
        eps=[];cur=None
        for i,r in enumerate(rows):
            if r['warStageOpenAfterGovFlag']=='1':
                if cur is not None and cur[-1]==i-1: cur.append(i)
                else: cur=[i]; eps.append(cur)
        for e in eps:
            rs=[rows[k] for k in e]
            T['eps']+=1; T['open_q']+=len(rs)
            T['reach4']+=any(stage(fl(r['militarySpendingAfterGovShare']))==4 for r in rs)
            T['reach4_end']+=any(stage(fl(r['militarySpendingEndShare']))==4 for r in rs)
        for r in rows:
            if fl(r['warStageEnvelopeShareAfterGov'])>0: T['vessel_q']+=1
            if r['warStageOpenEndFlag']=='0' and fl(r['militarySpendingEndShare'])>0.75+1e-9: T['stick_q']+=1
            if stage(fl(r['militarySpendingEndShare']))==4: T['enf4_end_q']+=1
            if r['atWarAtDecisionFlag']=='1': T['atwar_q']+=1
        T['default_final']+=int(rows[-1]['sovereignDefaultCumulativeCount'])
        T['rows']+=len(rows)
    return T
print(TAG,'UK',dict(stats(lambda n:n==UK)))
print(TAG,'ALL',dict(stats(lambda n:True)))
big=['0','1','2','3','4','5','7','11']
