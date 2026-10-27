"""(609) 合流の記録（読むだけ）：艦の数の年代（stock_s の fleetUnitsStock・7 国）と、最終の表の新しい物差し（鉄の産出の世界の割合＝score_s の share3Iron）での順位。
使い方：python -X utf8 -I rec609.py <腕名>=<計器の出力フォルダ（…/ins）> ...（種は 1〜10）
⚠ 造船の産出の列は main の計器に無い（線 navf の計器の列は枝 navf に残った）＝艦の数だけ。"""
import csv, os, sys, statistics as st, collections
csv.field_size_limit(10**9)
SEVEN=['イギリス','フランス','ロシア','オーストリア','日本','プロイセン','アメリカ']
NINE=['イギリス','フランス','プロイセン','アメリカ','日本','清','ロシア','オスマン','オーストリア']
YEARS=[1790,1800,1815,1850,1880,1913,1950]
def rows(p):
    if not os.path.exists(p): return
    with open(p,encoding='utf-8',newline='') as f:
        r=csv.reader(f,delimiter='\t'); h=next(r)
        for x in r:
            if len(x)==len(h): yield dict(zip(h,x))
def fl(v):
    try: return float(v)
    except: return float('nan')
for arm in sys.argv[1:]:
    name,d=arm.split('=',1)
    print(f'######## {name}  {d}')
    # 艦の数：年の最後の行（その国のその年の最後の time）
    ships=collections.defaultdict(list)   # (国,年) -> 種ごとの値
    tot=collections.defaultdict(list)
    for s in range(1,11):
        last={}
        for r in rows(os.path.join(d,f'stock_s{s}.tsv')):
            n=r['nation']; y=int(fl(r['year']))
            if n in SEVEN and y in YEARS: last[(n,y)]=fl(r['fleetUnitsStock'])
        for k,v in last.items(): ships[k].append(v)
        for y in YEARS:
            vs=[last.get((n,y)) for n in SEVEN]
            if all(v is not None for v in vs): tot[y].append(sum(vs))
    print('# 艦の数（fleetUnitsStock・年の最後の行・種の中央値）')
    print('  国\t'+'\t'.join(map(str,YEARS)))
    for n in SEVEN: print(f'  {n}\t'+'\t'.join(f'{st.median(ships[(n,y)]):.0f}' if ships[(n,y)] else '-' for y in YEARS))
    print('  7 国の和\t'+'\t'.join(f'{st.median(tot[y]):.0f}' if tot[y] else '-' for y in YEARS))
    # 鉄の産出の世界の割合の順位（最終の四半期）
    ranks=collections.defaultdict(list); top=collections.Counter(); shares=collections.defaultdict(list)
    for s in range(1,11):
        fin={}; qmax=-1
        for r in rows(os.path.join(d,f'score_s{s}.tsv')):
            q=int(fl(r['quarter']))
            if q>qmax: qmax=q; fin={}
            if q==qmax: fin[r['nation']]=fl(r['share3Iron'])
        order=sorted(fin,key=lambda k:-fin[k] if fin[k]==fin[k] else 0)
        if order: top[order[0]]+=1
        for n in NINE:
            m=[k for k in order if k==n or k.endswith(n)]
            if m: ranks[n].append(order.index(m[0])+1); shares[n].append(fin[m[0]])
    print('# 最終の表の物差し（鉄の産出の世界の割合・最終の四半期）：国の順位の中央値〔種の数〕・割合の中央値')
    print('  '+'・'.join(f'{n} {st.median(ranks[n]):.0f}位〔{len(ranks[n])}〕{st.median(shares[n]):.3f}' for n in NINE if ranks[n]))
    print('  首位の国（種の数）：'+'・'.join(f'{k} {v}' for k,v in top.most_common()))
