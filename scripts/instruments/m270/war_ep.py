"""(270) 線B2：解禁エピソードの終わり方（交戦継続中の復員か戦争の終わりか）・長さ・戦争の塊の長さ。python war_ep.py <計器の出力ディレクトリ> <nationIndex>。"""
import csv,collections,statistics,sys
D=sys.argv[1]; NAT=sys.argv[2]
rows=[]
for s in range(1,11):
    with open(f'{D}/nation_s{s}.tsv',encoding='utf-8',errors='replace') as f:
        for r in csv.DictReader(f,delimiter='\t'):
            if r['nationIndex']==NAT: r['seed']=s; rows.append(r)
# episodes of open
eps=[];cur=None
for i,r in enumerate(rows):
    op=r['warStageOpenAfterGovFlag']=='1'
    if op:
        if cur and cur['end']==i-1 and rows[cur['end']]['seed']==r['seed']: cur['end']=i
        else: cur={'start':i,'end':i}; eps.append(cur)
ended=collections.Counter(); lens_by=collections.defaultdict(list)
war_len=[]
for e in eps:
    j=e['end']+1
    if j>=len(rows) or rows[j]['seed']!=rows[e['end']]['seed']: ended['seed end']+=1; continue
    nr=rows[j]
    # at war at next quarter's decision?
    aw=nr['atWarAtDecisionFlag']=='1'
    # also atWarMonths in this quarter
    kind='復員(交戦継続)' if aw else '講和/戦争終了'
    ended[kind]+=1; lens_by[kind].append(e['end']-e['start']+1)
    # war run length containing
    a=e['start']
    while a>0 and rows[a-1]['seed']==rows[a]['seed'] and rows[a-1]['atWarAtDecisionFlag']=='1': a-=1
    b=e['end']
    while b+1<len(rows) and rows[b+1]['seed']==rows[b]['seed'] and rows[b+1]['atWarAtDecisionFlag']=='1': b+=1
    war_len.append((b-a+1, e['start']-a, e['end']-e['start']+1, kind))
print(ended)
for k,v in lens_by.items(): print(k,'median ep len',statistics.median(v), 'n',len(v), 'ep len>=8:',sum(x>=8 for x in v))
print('war run len median',statistics.median([w[0] for w in war_len]),'start offset median',statistics.median([w[1] for w in war_len]))
# episodes ending 講和 : would longer duration be possible? number with run remaining
print('episodes with ep len == war remaining (ended with war)', sum(1 for w in war_len if w[3]=='講和/戦争終了'))
# how many war runs in total, with any open
runs=[];a=None
for i,r in enumerate(rows):
    aw=r['atWarAtDecisionFlag']=='1'
    if aw and (a is None or rows[i-1]['seed']!=r['seed']): a=i
    if a is not None and (not aw or i==len(rows)-1 or rows[i+1]['seed']!=r['seed']):
        runs.append((a,i if aw else i-1)); a=None
print('war runs',len(runs),'median len',statistics.median([b-a+1 for a,b in runs]))
withopen=[ (a,b) for a,b in runs if any(rows[k]['warStageOpenAfterGovFlag']=='1' for k in range(a,b+1))]
print('war runs with open',len(withopen),'of',len(runs))
print('open quarters in runs of len>=8:', sum(1 for a,b in runs if b-a+1>=8 for k in range(a,b+1) if rows[k]['warStageOpenAfterGovFlag']=='1'))
