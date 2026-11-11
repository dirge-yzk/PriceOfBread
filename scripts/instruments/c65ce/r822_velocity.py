"""(822) 記録（門ではない・事前登録の後に足した）：凍結の窓 W_in の〈お金の回り〉＝四半期の市場の所得の側 inMarket÷州の貯蓄 savings の中央と、
同じ持ち主・同じ種・同じ年の植民地格で床に当たり凍っていない州の中央を並べる。お金が抜けて凍ったのか（貯蓄が痩せる）・お金が有るのに回らないのか（貯蓄が残り回りが 0）を分ける。
使い方: python -I r822_velocity.py <cearn の生> <zero_all.tsv> <出力.tsv>"""
import sys, os, statistics, collections
def fl(x):
    try: return float(x)
    except: return float('nan')
raw, zero_all, outp = sys.argv[1], sys.argv[2], sys.argv[3]
T = []
with open(zero_all, encoding='utf-8') as f:
    h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
    for line in f:
        a = line.rstrip('\n').split('\t')
        if a[ix['set']] == 'base' and int(a[ix['longestRunQ']]) >= 40:
            T.append((int(a[ix['seed']]), a[ix['region']], int(a[ix['runStartYear']]), int(a[ix['runEndYear']])))
o = open(outp, 'w', encoding='utf-8', newline='\n')
o.write('seed\tregion\tfrozenVelocityMed\tfrozenSavingsMed\tfrozenSavPerCapMainlandMed\tnonFrozenFloorVelocityMed\tnonFrozenRows\tvelocityPreWindowMed\n')
for sd in sorted({t[0] for t in T}):
    rows = []
    with open(os.path.join(raw, 'cearn_s%d.tsv' % sd), encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t')
        for line in f: rows.append(dict(zip(h, line.rstrip('\n').split('\t'))))
    by = collections.defaultdict(list)
    for d in rows: by[d['region']].append(d)
    for (s, reg, y0, y1) in [t for t in T if t[0] == sd]:
        Win = [d for d in by[reg] if y0 <= int(d['year']) <= y1]
        Wpre = [d for d in by[reg] if y0 - 10 <= int(d['year']) < y0]
        v = lambda d: fl(d['inMarketPerQuarter']) / fl(d['savings']) if fl(d['savings']) > 0 else float('nan')
        fv = [v(d) for d in Win if v(d) == v(d)]
        pv = [v(d) for d in Wpre if v(d) == v(d)]
        ny = {(d['nation'], d['year']) for d in Win}
        cv = [v(d) for d in rows if d['region'] != reg and d['tier'] == 'Colonial' and (d['nation'], d['year']) in ny and d['localPriceAtFloorFlag'] == '1' and d['frozenFlag'] != '1' and v(d) == v(d)]
        med = lambda x: '%.3g' % statistics.median(x) if x else '対照なし'
        o.write('\t'.join([str(sd), reg, med(fv), med([fl(d['savings']) for d in Win]), med([fl(d['savingsPerCapitaOverMainland']) for d in Win]), med(cv), str(len(cv)), med(pv)]) + '\n')
o.close()
print(open(outp, encoding='utf-8').read())
