# (1439) 合流：労使の交渉の記録（sbu_s の kappaSum・wageGain の種ごとの和・反証役 v2 の中の記録）。読むだけ。
#   使い方: python sbu_sum.py <名=ins のフォルダ> ... [SB=種の起点]   （種の数 10）
import sys, os, csv
csv.field_size_limit(1 << 30)
args = [a for a in sys.argv[1:] if '=' in a and not a.startswith('SB=')]
sb = int(next((a[3:] for a in sys.argv[1:] if a.startswith('SB=')), '1'))
print('arm\tseeds\tkappaSum\twageGain')
for a in args:
    n, d = a.split('=', 1); k = w = 0.0; got = 0
    for s in range(sb, sb + 10):
        p = os.path.join(d, f'sbu_s{s}.tsv')
        if not os.path.exists(p): continue
        got += 1
        with open(p, encoding='utf-8', newline='') as f:
            r = csv.reader(f, delimiter='\t'); h = next(r); ik = h.index('kappaSum'); iw = h.index('wageGain')
            for x in r: k += float(x[ik]); w += float(x[iw])
    print(f'{n}\t{got}\t{k:.1f}\t{w:.1f}')
