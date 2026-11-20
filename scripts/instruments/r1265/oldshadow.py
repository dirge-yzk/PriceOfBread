# (1265) 同じ盤面で旧の規則（bd の前＝全ての月で 1−r を積む）の累計を置き直し、旧なら不履行に届いた月を数える（計器の 9 国・fiscal_s）。
#   積む条件＝エンジンと同じ：r＜1・国債＞0・利払いが上限に届いた（interestAtLimitFlag 1）。条件が偽の月は 0 へ戻す。届いたら 0 へ戻す（不履行の後の軌道は旧と違う＝最初の 1 件の後は近似）。
#   新（エンジン）の不履行と同じ月なら both、旧だけなら oldOnly。oldOnly の月の金本位（前の月の onGold）と年代を数える。
# 使い方: python -X utf8 -I oldshadow.py <生> <種0> <種1> <ラベル>
import sys, csv, collections
raw, s0, s1, lab = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4]
def num(x):
    try: return float(x)
    except (TypeError, ValueError): return float('nan')
print(f'# {lab} 種 {s0}〜{s1}')
print('seed\tnation\tyear\tmonth\tkind\tprevOnGold\tconv')
dec = collections.Counter(); kinds = collections.Counter()
for s in range(s0, s1 + 1):
    hist = {}; cum = {}
    for r in csv.DictReader(open(f'{raw}/fiscal_s{s}.tsv', encoding='utf-8'), delimiter='\t'):
        n = r['nation']; h = hist.setdefault(n, []); h.append(r)
        newdef = len(h) > 1 and num(r['defaultCount']) > num(h[-2]['defaultCount'])
        rr = num(r['payRateShare'])
        if rr < 1 and num(h[-2]['govDebtStock'] if len(h) > 1 else r['govDebtStock']) > 1e-9 and num(r['interestAtLimitFlag']) > 0: cum[n] = cum.get(n, 0) + (1 - rr)
        else: cum[n] = 0
        olddef = cum[n] >= 1.0
        if olddef: cum[n] = 0
        if newdef: cum[n] = 0
        if not (olddef or newdef): continue
        k = 'both' if (olddef and newdef) else ('oldOnly' if olddef else 'newOnly')
        g = h[-2]['onGold'] if len(h) > 1 else '?'
        kinds[k] += 1; dec[(k, int(r['year']) // 10 * 10)] += 1
        print('\t'.join([str(s), n, r['year'], r['monthIndex'], k, g, r['convertibilityShare'][:6]]))
print(f'# {dict(kinds)}・1930 年代 both {dec[("both",1930)]}・oldOnly {dec[("oldOnly",1930)]}・newOnly {dec[("newOnly",1930)]}')
print(f'# 年代 {dict(sorted(dec.items()))}')
