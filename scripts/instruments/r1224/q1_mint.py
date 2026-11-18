# (1224) 問い 1 形 乙：PaySales（造幣・清算・その他）の代金の地代を〈穴〉の近似に足して分け直す（大農園×月・母集合＝P に行のある大農園×月）。
#  地代の近似＝RentShareOf×(1−StateProfitShare)×代金×Output の比（PaySales の割り方と同じ）。rentTest＝LastRevenue−LastCost（帳）。
#  使い方: python -X utf8 -I q1_mint.py <P の tsv 旗0> <P の tsv 旗1> <出力>
import sys, collections
L = []
for tag, p in (('旗0', sys.argv[1]), ('旗1', sys.argv[2])):
    rows = collections.defaultdict(lambda: {'kinds': collections.Counter()})
    with open(p, encoding='utf-8') as f:
        f.readline(); h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for l in f:
            c = l.rstrip('\n').split('\t'); k = (c[ix['seed']], c[ix['month']], c[ix['region']], c[ix['good']])
            r = rows[k]; sh = float(c[ix['prodShare']]); sh = 0.0 if sh != sh else sh
            r['rt'] = float(c[ix['lastRevenue']]) - float(c[ix['lastCost']])
            r['kinds'][c[ix['kind']]] += float(c[ix['rentShare']]) * (1 - float(c[ix['stateShare']])) * float(c[ix['amount']]) * sh
            r['rev'] = float(c[ix['lastRevenue']]); r['cost'] = float(c[ix['lastCost']]); r['law'] = c[ix['monetaryLaw']]
    by = collections.defaultdict(collections.Counter)
    for k, r in rows.items():
        g = (k[2], k[3]); rt = r['rt']; ec = r['kinds']['clear'] + r['kinds']['other']; em = r['kinds']['mint']
        b = by[g]; b['行'] += 1
        if rt < -1e-9:
            b['帳は負'] += 1
            if rt + ec >= 0: b['清算の地代で ≥0'] += 1
            elif rt + ec + em >= 0: b['造幣の地代を足して ≥0'] += 1
            else: b['足しても負'] += 1
        b['帳の地代の和'] += rt; b['清算の地代の近似の和'] += ec; b['造幣の地代の近似の和'] += em
    L.append(f'== {tag}（大農園×月・種 1〜10・月 1〜12）')
    for g, b in sorted(by.items(), key=lambda x: -x[1]['造幣の地代の近似の和'])[:8]:
        L.append(f'  {g[0]} {g[1]}: ' + '・'.join(f'{k} {v:.4g}' if isinstance(v, float) else f'{k} {v}' for k, v in b.items()))
open(sys.argv[3], 'w', encoding='utf-8', newline='\n').write('\n'.join(L) + '\n'); print('\n'.join(L))
