# (1265) 〈対外債務を印刷で払える〉の前提の読み：本位から外れた四半期（mintcap_s の onGold 0）のうち、利払い÷歳入≧0.67（借入の上限＝不履行の導火線の条件）の四半期を国×種で数え、
#   その間の対外の割合と印刷（ledgerDevalueCumulative の増え÷年の歳入）・その続きの中の不履行を並べる。
# 使い方: python -X utf8 -I offstd.py <生> <種0> <種1> <ラベル>
import sys, csv, collections
raw, s0, s1, lab = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4]
def num(x):
    try: return float(x)
    except (TypeError, ValueError): return float('nan')
print(f'# {lab} 種 {s0}〜{s1}')
print('seed\tnation\tquarters\tfirstYear\tlastYear\tlaw\tforeignShareMed\tintToRevMed\tdevalToRevYearMed\tdefaults')
tq = 0; td = 0; nq = 0
for s in range(s0, s1 + 1):
    gold = {}
    for r in csv.DictReader(open(f'{raw}/mintcap_s{s}.tsv', encoding='utf-8'), delimiter='\t'):
        gold[(r['nation'], r['q'])] = (r['onGold'], r['law'])
    ep = collections.defaultdict(list); prev = {}
    for r in csv.DictReader(open(f'{raw}/nation_s{s}.tsv', encoding='utf-8'), delimiter='\t'):
        n = r['nation']; p = prev.get(n); prev[n] = r
        g, law = gold.get((n, r['quarter']), ('?', '?'))
        rev = num(r['revenueBaseMeanPerMonth']) * 12; debt = num(r['govDebtStock'])
        if g != '0' or not rev > 0 or p is None: continue
        nq += 1
        itr = num(r['interestMeanPerMonth']) * 12 / rev
        if not itr >= 0.67: continue
        dv = (num(r['ledgerDevalueCumulative']) - num(p['ledgerDevalueCumulative'])) * 4 / rev
        dfl = num(r['sovereignDefaultCumulativeCount']) > num(p['sovereignDefaultCumulativeCount'])
        ep[n].append((int(r['year']), law, num(r['foreignDebt']) / debt if debt > 0 else float('nan'), itr, dv, dfl))
    for n, xs in ep.items():
        med = lambda i: sorted(x[i] for x in xs)[len(xs) // 2]
        d = sum(x[5] for x in xs); tq += len(xs); td += d
        print('\t'.join([str(s), n, str(len(xs)), str(xs[0][0]), str(xs[-1][0]), xs[0][1], f'{med(2):.2f}', f'{med(3):.2f}', f'{med(4):.2f}', str(d)]))
print(f'# 本位から外れた国×四半期 {nq}・そのうち利払い÷歳入≧0.67 {tq}・その中の不履行 {td}')
