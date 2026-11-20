# (1265) 全ての国の国債の不履行（nation_s の sovereignDefaultCumulativeCount の増え）を 1 件ずつ並べ、その前の四半期の状態を付ける。
# 群＝前の四半期の mintcap_s の onGold（1＝A 金本位・0＝B）。全ての国で読める（fiscal_s は計器の 9 国だけ）。
# 列：種・国・年・四半期・群・本位の法・金本位でない続きの四半期・前の 4 四半期の戦争の月・内戦の月・債務÷年の歳入・利払い÷歳入・対外の割合・前の 4 四半期の切り下げ（ledgerDevalueCumulative の増え÷年の歳入）
# 使い方: python -X utf8 -I alldef2.py <生> <種0> <種1> <ラベル>
import sys, csv, collections
raw, s0, s1, lab = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4]
def num(x):
    try: return float(x)
    except (TypeError, ValueError): return float('nan')
print(f'# {lab} {raw} 種 {s0}〜{s1}')
print('seed\tnation\tyear\tquarter\tgroup\tlaw\toffGoldQ\twarM4\tcivilM4\tdebtToRevYear\tintToRev\tforeignShare\tdevalToRevYear4')
dec = collections.Counter(); grp = collections.Counter(); war = collections.Counter(); n = 0
for s in range(s0, s1 + 1):
    gold = {}
    for r in csv.DictReader(open(f'{raw}/mintcap_s{s}.tsv', encoding='utf-8'), delimiter='\t'):
        gold[(r['nation'], int(r['q']))] = (num(r['onGold']), r['law'])
    hist = collections.defaultdict(list)
    for r in csv.DictReader(open(f'{raw}/nation_s{s}.tsv', encoding='utf-8'), delimiter='\t'):
        h = hist[r['nation']]; h.append(r)
        if len(h) < 2 or not num(r['sovereignDefaultCumulativeCount']) > num(h[-2]['sovereignDefaultCumulativeCount']): continue
        p = h[-2]; q = int(p['quarter'])
        g, law = gold.get((r['nation'], q), (float('nan'), '?'))
        off = 0
        for x in reversed(h[:-1]):
            gg = gold.get((r['nation'], int(x['quarter'])), (float('nan'),))[0]
            if gg > 0: break
            off += 1
        w4 = sum(num(x['atWarMonths']) for x in h[-5:]); c4 = sum(num(x['civilWarMonths']) for x in h[-5:])
        rev = num(p['revenueBaseMeanPerMonth']) * 12
        debt = num(p['govDebtStock'])
        dv = num(h[-2]['ledgerDevalueCumulative']) - num(h[max(0, len(h) - 6)]['ledgerDevalueCumulative'])
        G = 'A' if g > 0 else ('B' if g == 0 else '?')
        yr = int(r['year']); n += 1; dec[yr // 10 * 10] += 1; grp[G] += 1
        if yr // 10 * 10 == 1930: grp['1930' + G] += 1
        war['war' if w4 > 0 else 'peace'] += 1
        print('\t'.join([str(s), r['nation'], r['year'], r['quarter'], G, law, str(off), f'{w4:.0f}', f'{c4:.0f}',
                         f'{debt / rev:.2f}' if rev > 0 else 'inf', f'{num(p["interestMeanPerMonth"]) * 12 / rev:.2f}' if rev > 0 else 'inf',
                         f'{num(p["foreignDebt"]) / debt:.2f}' if debt > 0 else 'nan', f'{dv / rev:.3f}' if rev > 0 else 'inf']))
print(f'# 計 {n}・群 A {grp["A"]}・B {grp["B"]}・? {grp["?"]}・1930 年代 A {grp["1930A"]}・B {grp["1930B"]}・? {grp["1930?"]}・戦争の内 {war["war"]}・平時 {war["peace"]}・年代 {dict(sorted(dec.items()))}')
