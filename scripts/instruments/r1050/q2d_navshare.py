import csv,collections,sys,statistics as st
K='D:/PoB_work/keep_1044'
arm=sys.argv[1]; seeds=range(11,21) if arm=='a11' else range(1,11); d='base_ins' if arm=='base' else arm
def rows(p):
    with open(p,encoding='utf-8') as f: return list(csv.DictReader(f,delimiter='\t'))
YRS=[1783,1790,1800,1815,1850,1900]
acc=collections.defaultdict(list); rank=collections.defaultdict(list)
for s in seeds:
    by=collections.defaultdict(dict)
    for r in rows(f'{K}/{d}/stock_s{s}.tsv'):
        y=int(r['year'])
        if y in YRS:
            try: by[y][r['nation']]=float(r['navalPowerIndex'])
            except: pass
    for y in YRS:
        tot=sum(v for v in by[y].values() if v==v)
        if tot>0:
            acc[y].append(by[y].get('イギリス',0)/tot)
            rank[y].append(sorted(by[y].items(),key=lambda kv:-kv[1])[:3])
for y in YRS:
    print(y,'英の艦隊の世界比 中央',round(st.median(acc[y]),3),'最小',round(min(acc[y]),3),'最大',round(max(acc[y]),3),' 種1の上位3',[(n,round(v,2)) for n,v in rank[y][0]])
