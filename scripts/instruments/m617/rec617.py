"""(617) 合流の記録（読むだけ）：計器つきの生から、国債と国庫の年代・英の崩れの種の数・関税の目標と貿易÷GDP・正統性・巨大建造物の着工の年代を出す。
使い方：python -X utf8 -I rec617.py <腕名>=<計器の出力フォルダ（…/ins）> ...（種は 1〜10）
⚠ 国債は nation_s の govDebtStock（全ての国・四半期）・国庫は fiscal_s の treasuryEndStock（NATIONS の 9 国・月）＝年の最後の行。名目 GDP で割った比も出す（nominalGdpPerYear）。
⚠ 英の崩れ＝nation_s の イギリス（表示名の完全一致）の実質 GDP（realGdpPerYear）が、1790 年以後のそれまでの最大の 5% を割った四半期がある種（m609 の生で種 5・9 の 2 種＝(609) の崩れと一致する物差し・人口の 1/2 は植民地の喪失も拾うので使わない）。年は最初にそうなった年。
⚠ scalars_s の megaStartedCount は〈自国が所有して着工済みの巨大建造物の数〉（在庫の数）＝期間の終わりの値と最初に正になった年。
⚠ scalars_s・fiscal_s の国名は表示名（改名で別の名になる）＝9 国は末尾一致で拾う（〈立憲王政イギリス〉も英）。"""
import csv, os, sys, statistics as st, collections
csv.field_size_limit(10**9)
NINE=['イギリス','フランス','プロイセン','アメリカ','日本','清','ロシア','オスマン','オーストリア']
DEC=[1800,1820,1850,1880,1913,1930,1950]
def rows(p):
    if not os.path.exists(p): return
    with open(p,encoding='utf-8',newline='') as f:
        r=csv.reader(f,delimiter='\t'); h=next(r)
        for x in r:
            if len(x)==len(h): yield dict(zip(h,x))
def fl(v):
    try: return float(v)
    except: return float('nan')
def who(n):
    for k in NINE:
        if n==k or n.endswith(k): return k
    return None
def med(v):
    v=[x for x in v if x==x]
    return st.median(v) if v else float('nan')
for arm in sys.argv[1:]:
    name,d=arm.split('=',1)
    print(f'######## {name}  {d}')
    debt=collections.defaultdict(list); debtr=collections.defaultdict(list); tre=collections.defaultdict(list); trer=collections.defaultdict(list)
    ukc=[]; tar=collections.defaultdict(list); trd=collections.defaultdict(list); leg=collections.defaultdict(list)
    mega_first=collections.defaultdict(list); mega_end=collections.defaultdict(list)
    for s in range(1,11):
        last={}; gdp={}; ukmax=0.0; ukyr=None
        for r in rows(os.path.join(d,f'nation_s{s}.tsv')):
            n=r['nation']; y=int(fl(r['year']))
            if n=='イギリス' and y>=1790:
                p=fl(r['realGdpPerYear'])
                if p>ukmax: ukmax=p
                if ukyr is None and ukmax>0 and p<0.05*ukmax: ukyr=y
            k=who(n)
            if k and y in DEC:
                last[(k,y)]=fl(r['govDebtStock']); gdp[(k,y)]=fl(r['nominalGdpPerYear'])
        ukc.append(ukyr)
        for (k,y),v in last.items():
            debt[(k,y)].append(v)
            g=gdp.get((k,y),float('nan'))
            debtr[(k,y)].append(v/g if g>0 else float('nan'))
        tl={}
        for r in rows(os.path.join(d,f'fiscal_s{s}.tsv')):
            k=who(r['nation']); y=int(fl(r['year']))
            if k and y in DEC: tl[(k,y)]=fl(r['treasuryEndStock'])
        for (k,y),v in tl.items():
            tre[(k,y)].append(v); g=gdp.get((k,y),float('nan')); trer[(k,y)].append(v/g if g>0 else float('nan'))
        firstq={}; seen=set(); endv={}; per=collections.defaultdict(list)
        for r in rows(os.path.join(d,f'scalars_s{s}.tsv')):
            k=who(r['nation']); y=int(fl(r['year']))
            if not k: continue
            key=(k,y)
            if key not in seen and y in DEC+[1783,1790]:
                seen.add(key); tar[key].append(fl(r['tariffTargetIndex'])); trd[key].append(fl(r['tradeToGdpRatio']))
            per[(k,y)].append(fl(r['legitimacy']))
            m=fl(r.get('megaStartedCount','nan'))   # m609 の生には無い列（(613) で足した）＝nan
            if m>0 and k not in firstq: firstq[k]=y
            for e in DEC:
                if y<=e: endv[(k,e)]=m
        for k in NINE:
            mega_first[k].append(firstq.get(k))
        for key,v in endv.items(): mega_end[key].append(v)
        for (k,y),v in per.items():
            for a,b in [(1783,1819),(1820,1869),(1870,1913),(1914,1950)]:
                if a<=y<=b: leg[(k,a)].append(st.mean(v))
    print('# 国債の残高÷名目 GDP（年の最後の四半期・種の中央値）')
    print('  国\t'+'\t'.join(map(str,DEC)))
    for k in NINE: print(f'  {k}\t'+'\t'.join(f'{med(debtr[(k,y)]):.2f}' for y in DEC))
    print('# 国庫÷名目 GDP（fiscal_s の年の最後の月・種の中央値）')
    for k in NINE: print(f'  {k}\t'+'\t'.join(f'{med(trer[(k,y)]):.2f}' for y in DEC))
    n=sum(1 for x in ukc if x is not None)
    print(f'# 英の崩れ（実質 GDP がそれまでの最大の 5% を割った種）：{n}/10　年＝'+'・'.join(f'種{i+1} {x}' for i,x in enumerate(ukc) if x is not None))
    print('# 関税の目標（tariffTargetIndex・年の最初の四半期・国の中央値の種の中央値）・貿易÷GDP（9 国の平均の種の中央値）')
    for y in [1790]+DEC:
        ts=[med(tar[(k,y)]) for k in NINE if tar[(k,y)]]; rs=[med(trd[(k,y)]) for k in NINE if trd[(k,y)]]
        print(f'  {y}: 関税の目標 {med(ts):.3f}｜貿易÷GDP {st.mean(rs)*100 if rs else float("nan"):.1f}%')
    print('# 正統性（scalars_s の legitimacy・期間の平均・種の中央値）')
    print('  国\t1783-1819\t1820-1869\t1870-1913\t1914-1950')
    for k in NINE: print(f'  {k}\t'+'\t'.join(f'{med(leg[(k,a)]):.3f}' for a in [1783,1820,1870,1914]))
    print('# 巨大建造物：自国が所有して着工済みの数（期間の終わり・種の中央値）と最初に正になった年（種の中央値〔正になった種の数〕）')
    for k in NINE:
        fs=[x for x in mega_first[k] if x is not None]
        print(f'  {k}\t最初 {med(fs) if fs else "-"}〔{len(fs)}〕\t'+'\t'.join(f'{y}:{med(mega_end[(k,y)]):.0f}' for y in DEC))
