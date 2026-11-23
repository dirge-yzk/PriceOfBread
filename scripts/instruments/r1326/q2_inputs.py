# (1326) 問い2：電灯網の投入（送電網・電化製品・鋼）とガス灯の投入（ガス）・電化インフラの財の行（goods_s）と、投入財の作り手の能力（prodcap_s）を 1913 年で並べる
import csv,sys,collections,statistics as st
R=sys.argv[1]; out=open(sys.argv[2],'w',encoding='utf-8'); Y=sys.argv[3] if len(sys.argv)>3 else '1913'
G=['送電網','電化製品','鋼','ガス','電力','電化インフラ','インフラ','セメント']
out.write('種\t財\t国数\t作った\t需要\t売れた\t不足\t在庫\t値段の中央\n')
for s in range(1,11):
    acc=collections.defaultdict(lambda: [0,0,0,0,0,0,[]])
    lastq=None
    rows=[]
    with open(f'{R}/goods_s{s}.tsv',encoding='utf-8') as f:
        for r in csv.DictReader(f,delimiter='\t'):
            if r['year']!=Y or r['goodName'] not in G: continue
            rows.append(r)
    if not rows: continue
    lq=max(int(r['quarter']) for r in rows)
    for r in rows:
        if int(r['quarter'])!=lq: continue
        a=acc[r['goodName']]; a[0]+=1
        for i,c in enumerate(('lastProducedQty','lastDemandQty','lastSoldQty','lastShortageQty','stockQty')):
            v=r[c]; a[i+1]+=float(v) if v not in ('','NaN') else 0
        if r['priceMean'] not in ('','NaN'): a[6].append(float(r['priceMean']))
    for g in G:
        a=acc.get(g)
        if a: out.write(f's{s}\t{g}\t{a[0]}\t{a[1]:.4g}\t{a[2]:.4g}\t{a[3]:.4g}\t{a[4]:.4g}\t{a[5]:.4g}\t{(st.median(a[6]) if a[6] else float("nan")):.4g}\n')
# 投入財の作り手
out.write('\n種\t財\trecipe\t行\tcap和\tout和\tscale和\tfill中央\n')
for s in range(1,11):
    acc=collections.defaultdict(lambda: [0,0,0,0,[]]); rows=[]
    with open(f'{R}/prodcap_s{s}.tsv',encoding='utf-8') as f:
        for r in csv.DictReader(f,delimiter='\t'):
            if r['year']==Y and r['good'] in ('送電網','電化製品','電力','ガス'): rows.append(r)
    if not rows: continue
    lq=max(r['q'] for r in rows)
    for r in rows:
        if r['q']!=lq: continue
        a=acc[(r['good'],r['recipe'])]; a[0]+=1
        for i,c in enumerate(('capacity','output','scale')):
            v=r[c]; a[i+1]+=float(v) if v not in ('','NaN') else 0
        if r['lastInputFill'] not in ('','NaN'): a[4].append(float(r['lastInputFill']))
    for k in sorted(acc):
        a=acc[k]; out.write(f's{s}\t{k[0]}\t{k[1]}\t{a[0]}\t{a[1]:.4g}\t{a[2]:.4g}\t{a[3]:.4g}\t{(st.median(a[4]) if a[4] else float("nan")):.3g}\n')
