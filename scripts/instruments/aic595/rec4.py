import sys,glob,collections,csv
# 記録④：供給0（産出＜1・輸入＜1）で、値段÷(参照×物価水準)の中央が閾値以上に張り付いた (国,時刻,財) の行
d=sys.argv[1]; thr=float(sys.argv[2]) if len(sys.argv)>2 else 20.0
tot=0;rows=collections.Counter();goods=collections.Counter();seeds=set();nat=collections.Counter()
allrows=0
for f in sorted(glob.glob(d+'/pxgoods_s*.tsv')):
    sd=f.split('_s')[-1].split('.')[0]; seeds.add(sd)
    with open(f,encoding='utf-8') as fh:
        r=csv.reader(fh,delimiter='\t'); h=next(r); ix={k:i for i,k in enumerate(h)}
        for row in r:
            allrows+=1
            try:
                p=float(row[ix['lastProducedQty']]); im=float(row[ix['lastImportQty']]); m=float(row[ix['priceOverRefTimesLevelMedianRatio']])
            except: continue
            if p<1 and im<1 and m>=thr:
                rows[sd]+=1; goods[row[ix['goodName']]]+=1; nat[row[ix['nation']]]+=1
print('dir',d,'seeds',len(seeds),'rows total',allrows,'pinned supply0 rows',sum(rows.values()))
print('per seed',dict(sorted(rows.items(),key=lambda kv:int(kv[0]))))
print('top goods',goods.most_common(12))
print('by nation',nat.most_common(10))
