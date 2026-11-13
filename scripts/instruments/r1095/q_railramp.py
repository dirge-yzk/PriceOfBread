# (1095) 鉄道の容量の立ち上がり：国ごとに railCapacityStock ÷ (人口 × TransportTargetPerPop 0.08) を年ごとに数える（target の倍率〔脅威・混雑〕を除いた基礎の目標に対する比＝充足の上の側の見積もり）。
# 使い方: python -X utf8 -I q_railramp.py <表のフォルダ> > out   ・stock_s{1..10}.tsv を読む。人口の単位＝totalPopulationK＝w.TotalPopulation（Instruments.cs:2741・千人）＝PreRailCapacity と同じ単位。
import sys, os, collections, statistics as st
sys.stdout.reconfigure(newline='\n')
d = sys.argv[1]
NAT = ['イギリス', 'フランス', 'プロイセン', 'オーストリア', 'ロシア', 'アメリカ', '清', '日本', 'オスマン']
YS = [1800, 1820, 1830, 1840, 1850, 1860, 1870, 1880, 1900, 1920, 1940]
val = collections.defaultdict(list); first = collections.defaultdict(list)
for s in range(1, 11):
    p = os.path.join(d, f'stock_s{s}.tsv')
    with open(p, encoding='utf-8') as fi:
        h = fi.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        seen = {}
        for line in fi:
            c = line.rstrip('\n').split('\t')
            n = c[ix['nation']]
            try: y = float(c[ix['year']]); rail = float(c[ix['railCapacityStock']]); pk = float(c[ix['totalPopulationK']])
            except ValueError: continue
            yi = int(y)
            if rail > 0 and (s, n) not in seen: seen[(s, n)] = y; first[n].append(y)
            if n in NAT and yi in YS and (s, n, yi) not in seen:
                seen[(s, n, yi)] = 1
                val[(n, yi)].append(rail / (pk * 0.08) if pk > 0 else float('nan'))
print('国\t最初に鉄道>0 の年（種の中央）\t' + '\t'.join(str(y) for y in YS))
for n in NAT:
    f = st.median(first[n]) if first[n] else float('nan')
    row = [f'{st.median(val[(n, y)]):.3g}' if val[(n, y)] else '-' for y in YS]
    print(f'{n}\t{f:.1f}\t' + '\t'.join(row))
allf = [st.median(v) for v in first.values() if v]
print('全ての国：最初に鉄道>0 の年の中央の分布', 'n', len(allf), '中央', f'{st.median(allf):.1f}' if allf else '-')
