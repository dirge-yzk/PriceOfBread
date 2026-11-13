# (1102) 門 C5 の材料：ΣO÷ΣL（q_t・平時も交戦も今の W で）と国ごとの O の世界の取り分の最大（C9）を、3 腕×全ての四半期で 1 行ずつ書く（種の中央・最小・最大）。表を読むだけ。
# 使い方: python -X utf8 -I c5_series.py <nat_M.tsv> <nat_N1.tsv> <nat_N2.tsv> > c5_series.tsv
import sys, collections, statistics as st
import sys as _s; _s.stdout.reconfigure(newline='\n')
def load(p):
    data = collections.defaultdict(dict)
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: j for j, k in enumerate(h)}
        for line in f:
            c = line.rstrip('\n').split('\t')
            data[(int(c[0]), int(c[1]))][c[ix['nation']]] = (float(c[ix['L']]), float(c[ix['WA']]), float(c[ix['popK']]))
    out = collections.defaultdict(lambda: ([], []))
    for (s, q), nat in data.items():
        L = sum(v[0] for v in nat.values()); P = sum(v[2] for v in nat.values())
        if not (L > 0 and P > 0): continue
        qt = L / P; os_ = []
        for l, w, p in nat.values():
            wu = min(w, l); os_.append(max(0.0, l - wu - min(max(0.0, l - wu), p * qt)))
        so = sum(os_)
        out[q][0].append(so / L); out[q][1].append(max(os_) / so if so > 0 else 0.0)
    return out
arms = [load(p) for p in sys.argv[1:4]]
print('quarter\t' + '\t'.join(f'{a}_{k}_{m}' for a in ('M', 'N1', 'N2') for k in ('sumOsumL', 'maxOshare') for m in ('med', 'min', 'max')))
for q in sorted(set().union(*[set(a) for a in arms])):
    row = [str(q)]
    for a in arms:
        for i in (0, 1):
            v = a[q][i] if q in a else []
            row += [f'{st.median(v):.6g}', f'{min(v):.6g}', f'{max(v):.6g}'] if v else ['nan'] * 3
    print('\t'.join(row))
