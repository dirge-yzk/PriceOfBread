# (897) c76db 読む線の 4 本目（読むだけ）：前面の awk で見た数を台本と出力に残す
#   python -X utf8 debt897d.py <名>=<ins フォルダ> ... [--seeds 1 10]
#   (1) 格ごと×importPayerKind ごとの州×四半期と importPaid＞0 の数（cearn_s）。
#   (2) 名指しの州の商人の POP の植民地格の最後の行（cpop_s：種 5 セイロン・バタヴィア・種 4 中核ベンガル）と、種 1 ポンディシェリ・ジブラルタルの系列の抜き書き（q0〜2・q100〜111）。
import sys, os
from collections import defaultdict
args = sys.argv[1:]
s0, s1 = 1, 10
if '--seeds' in args:
    i = args.index('--seeds'); s0, s1 = int(args[i + 1]), int(args[i + 2]); del args[i:i + 3]
def fl(x):
    try: return float(x)
    except ValueError: return float('nan')
NAMED = {5: ('セイロン', 'バタヴィア'), 4: ('中核ベンガル',)}
SERIES = {1: ('ポンディシェリ', 'ジブラルタル')}
COLS = ('quarter', 'nation', 'popK', 'debt', 'savings', 'inCreditPerQuarter', 'inMarketPerQuarter', 'estateOutPerQuarter', 'outImportPerQuarter', 'outOtherPerQuarter')
for tag, d in [a.split('=', 1) for a in args]:
    n = defaultdict(int); p = defaultdict(int)
    print(f'# {tag} {d} 種 {s0}-{s1}')
    for s in range(s0, s1 + 1):
        pc = os.path.join(d, f'cearn_s{s}.tsv')
        if not os.path.exists(pc): print(f'# {tag} 種 {s} cearn_s なし'); continue
        with open(pc, encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); it, ik, ipd = h.index('tier'), h.index('importPayerKind'), h.index('importPaidPerQuarter')
            for line in f:
                x = line.rstrip('\n').split('\t'); k = (x[it], x[ik]); n[k] += 1
                if fl(x[ipd]) > 0: p[k] += 1
        want = NAMED.get(s, ()) + SERIES.get(s, ())
        pp = os.path.join(d, f'cpop_s{s}.tsv')
        if want and os.path.exists(pp):
            last = {}; ser = defaultdict(list)
            with open(pp, encoding='utf-8') as f:
                h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
                for line in f:
                    hd = line.split('\t', 9)
                    if hd[ix['className']] != '商人' or hd[ix['region']] not in want: continue
                    x = line.rstrip('\n').split('\t'); r = x[ix['region']]
                    row = [x[ix[c]] for c in COLS]
                    last[r] = row
                    if r in SERIES.get(s, ()) and (int(x[ix['quarter']]) <= 2 or 100 <= int(x[ix['quarter']]) <= 111): ser[r].append(row)
            for r in NAMED.get(s, ()):
                if r in last: print(f'{tag}\t種 {s}\t{r}\t植民地格の最後の行\t' + '\t'.join(f'{c}={v}' for c, v in zip(COLS, last[r])))
            for r, rows in ser.items():
                for row in rows: print(f'{tag}\t種 {s}\t{r}\t系列\t' + '\t'.join(f'{c}={v}' for c, v in zip(COLS, row)))
    for k in sorted(n): print(f'{tag}\t格 {k[0]}\timportPayerKind {k[1]}\t州×四半期 {n[k]}\timportPaid＞0 {p[k]}\t割合 {p[k] / n[k]:.4f}')
