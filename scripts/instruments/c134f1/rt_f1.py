# (1296) c134f1 周辺の点検（読むだけ）：urxrt_s の呼び手ごとの都市への入りと出（計器の 9 国・全期間の和・種の中央値）を腕と基準で並べる。
#   使い方: python -X utf8 -I rt_f1.py <腕の ins> <基準の ins> <種の起点>
import sys, os, csv, statistics as st, collections
def read(d, sb):
    acc = collections.defaultdict(list)
    for s in range(sb, sb + 10):
        p = os.path.join(d, f'urxrt_s{s}.tsv')
        if not os.path.exists(p): continue
        v = collections.defaultdict(lambda: [0.0, 0.0])
        with open(p, encoding='utf-8') as f:
            for r in csv.DictReader(f, delimiter='\t'):
                a = v[r['route']]; a[0] += float(r['urbanInK']); a[1] += float(r['urbanOutK'])
        for k, (i, o) in v.items(): acc[(k, 'in')].append(i); acc[(k, 'out')].append(o); acc[(k, 'net')].append(i - o)
    return acc
A = read(sys.argv[1], int(sys.argv[3])); B = read(sys.argv[2], int(sys.argv[3]))
print('route\t量\t腕\t基準\t腕÷基準')
for k in sorted(set(A) | set(B)):
    a = st.median(A[k]) if A.get(k) else float('nan'); b = st.median(B[k]) if B.get(k) else float('nan')
    print(f'{k[0]}\t{k[1]}\t{a:.1f}\t{b:.1f}\t{(a / b if b else float("nan")):.3f}')
