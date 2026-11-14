# (1102) C5・C9′ の閾値：1 本の腕の種の中央を、他の 2 本の腕の中央の幅で読んだときに外に出る四半期の数（揺らしだけで外れる数）。
# 使い方: python -X utf8 -I loo.py c5_series.tsv > loo_out.txt
import sys, csv
import sys as _s; _s.stdout.reconfigure(newline='\n')
r = list(csv.DictReader(open(sys.argv[1], encoding='utf-8'), delimiter='\t'))
A = ('M', 'N1', 'N2')
for k in ('sumOsumL', 'maxOshare'):
    for a in A:
        o = [b for b in A if b != a]; n = 0; n2 = 0
        for x in r:
            v = float(x[f'{a}_{k}_med']); lo = min(float(x[f'{b}_{k}_med']) for b in o); hi = max(float(x[f'{b}_{k}_med']) for b in o)
            n += not (lo <= v <= hi)
            lo2 = min(float(x[f'{b}_{k}_min']) for b in o); hi2 = max(float(x[f'{b}_{k}_max']) for b in o)
            n2 += not (lo2 <= v <= hi2)
        print(k, a, 'を他の 2 腕の幅で読むと外の四半期', n, '/', len(r), '・種の最小〜最大の幅で', n2)
