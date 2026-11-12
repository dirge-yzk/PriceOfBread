import csv,collections,sys,statistics as st
# (1076) 英の艦の世界比（全ての国の fleetUnitsStock の和に対する割合・各年の最初の行・種の中央）と上位 5 国
K='D:/PoB_work/keep_1069'
arm=sys.argv[1]; seeds=range(11,21) if arm=='a11' else range(1,11); d='base_ins' if arm=='base' else arm
YRS=['warm',1785,1790,1795,1800,1805,1810,1820,1830]
sh=collections.defaultdict(list); tot=collections.defaultdict(list); top=collections.defaultdict(collections.Counter)
for s in seeds:
    seen=set(); v=collections.defaultdict(dict)
    with open(f'{K}/{d}/stock_s{s}.tsv',encoding='utf-8') as f:
        for r in csv.DictReader(f,delimiter='\t'):
            t=r['time']; y='warm' if t=='warm' else (None if t=='open' else int(float(r['year'])))
            if y not in YRS: continue
            k=(y,r['nation'])
            if k in seen: continue
            seen.add(k)
            try: v[y][r['nation']]=float(r['fleetUnitsStock'])
            except: pass
    for y in YRS:
        T=sum(x for x in v[y].values() if x==x)
        tot[y].append(T); sh[y].append(v[y].get('イギリス',0)/T if T>0 else float('nan'))
        for n,x in sorted(v[y].items(),key=lambda a:-a[1])[:5]: top[y][n]+=1
print(arm,'年\t世界の艦\t英の世界比\t上位5国に入った種の数（多い順）')
for y in YRS:
    print(y,f'{st.median(tot[y]):.0f}',f'{st.median(sh[y]):.3f}',' '.join(f'{n}{c}' for n,c in top[y].most_common(6)),sep='\t')
