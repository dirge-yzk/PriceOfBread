# (645) 合流の記録（読むだけ）：①基線 m627 で 1/4 未満だった 12 件の州の行方（種・州・持ち主で region_s を引く）②工場の産出の年代（factory_s の outputRefValuePerMonth・年の最後の四半期・種の中央値・6 国）
# 使い方：python -X utf8 -I rec645.py <腕名>=<計器の出力フォルダ（…/ins）> ...（種は 1〜10）
# ⚠ ① は州の名と持ち主（region_s の nation 列）で引く＝持ち主が変わった四半期は数えない（名前が同じ別の植民地を拾わないため）。持ち主の行が無ければ名だけで引いた値を〈名〉と札を付けて出す。
import csv, os, sys, statistics as st, collections
csv.field_size_limit(10**9)
TW = [(5,'セントマイケル','ジェノヴァ','移民'),(7,'バロー','フランス','移民'),(8,'ジブラルタル','アシャンティ王国','移民'),(9,'セントジョンズ','両シチリア','移民'),
      (9,'フォートウィリアム','両シチリア','移民'),(9,'フォートユーコン','清','移民'),(9,'バーミンガム','両シチリア','移民'),
      (4,'ベルリン','プロイセン','飢え'),(6,'ロンドン','イギリス','飢え'),(7,'アムステルダム','ネーデルラント','飢え'),(10,'アムステルダム','ネーデルラント','飢え'),(6,'アムステルダム','ネーデルラント','その他')]
NAT = ['イギリス','フランス','プロイセン','アメリカ','日本','清']
YF = [1820,1850,1880,1913,1949]
def rows(p):
    if not os.path.exists(p): return
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k:i for i,k in enumerate(h)}
        for x in r:
            if len(x) == len(h): yield ix, x
def fl(v):
    try: return float(v)
    except: return float('nan')
arms = [a.split('=',1) for a in sys.argv[1:]]
print('## ① 基線 m627 で 1/4 未満だった 12 件の州の行方（ピーク・最小÷ピーク・1850／1900／1949 の人口千）')
need = collections.defaultdict(set)
for s,reg,own,_ in TW: need[s].add(reg)
for name, d in arms:
    print(f'#### {name}')
    for s in sorted(need):
        own_t = collections.defaultdict(list); any_t = collections.defaultdict(dict)
        for ix, x in rows(os.path.join(d, f'region_s{s}.tsv')):
            reg = x[ix['region']]
            if reg not in need[s]: continue
            q = int(x[ix['quarter']]); y = int(x[ix['year']]); pop = fl(x[ix['populationK']]); nat = x[ix['nation']]
            own_t[(reg, nat)].append((q, y, pop)); any_t[reg][q] = any_t[reg].get(q, 0) + pop
        for ss, reg, own, why in TW:
            if ss != s: continue
            t = own_t.get((reg, own)); tag = '持ち主'
            if not t:
                t = [(q, None, p) for q, p in sorted(any_t.get(reg, {}).items())]; tag = '名'
            if not t: print(f'  種{s} {reg}〔{own}〕：行なし'); continue
            pk = max(p for _,_,p in t); qpk = [q for q,_,p in t if p == pk][0]
            after = [p for q,_,p in t if q >= qpk]; mn = min(after) if after else float('nan')
            ys = {}
            for q, y, p in t:
                yy = y if y is not None else 1783 + q // 4
                ys[yy] = p
            pick = ' '.join(f'{yy}:{ys[yy]:.0f}' if yy in ys else f'{yy}:-' for yy in (1850,1900,1949))
            print(f'  種{s} {reg}〔{own}・m627 の箱 {why}〕[{tag}] ピーク {pk:.0f}（q{qpk}）・最小÷ピーク {mn/pk if pk>0 else float("nan"):.3f}・{pick}・最後の行 q{t[-1][0]}')
print('## ② 工場の産出 outputRefValuePerMonth（国の工場の和・年の最後の四半期・10 種の中央値）')
for name, d in arms:
    acc = collections.defaultdict(list)
    for s in range(1, 11):
        last = {}
        for ix, x in rows(os.path.join(d, f'factory_s{s}.tsv')):
            nat = x[ix['nation']]
            if nat not in NAT: continue
            y = int(x[ix['year']]); q = int(x[ix['quarter']])
            if y not in YF: continue
            k = (nat, y)
            if k not in last or q > last[k][0]: last[k] = (q, 0.0)
            if q == last[k][0]: last[k] = (q, last[k][1] + fl(x[ix['outputRefValuePerMonth']]))
        for k, (q, v) in last.items(): acc[k].append(v)
    print(f'#### {name}')
    for n in NAT:
        print('  ' + n + ' ' + ' '.join(f'{y}:{st.median(acc[(n,y)]):.4g}' if acc[(n,y)] else f'{y}:-' for y in YF))
