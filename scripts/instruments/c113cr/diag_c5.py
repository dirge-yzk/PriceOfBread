# 診断（門ではない）：c5_series の帯（10 種の最小〜最大）に、ヌル腕の種 1〜2 だけの中央（c5_series と同じ式で O を組む）が入るか（四半期 0〜79）。
import collections, statistics as st
def fl(x):
    try: return float(x)
    except ValueError: return float('nan')
ser = {}
with open('D:/PoB_work/wt_c113cr/measurements/m1151_20261115/paper/c5_series.tsv', encoding='utf-8') as f:
    h = f.readline().rstrip('\n').split('\t')
    for line in f:
        c = line.rstrip('\n').split('\t'); ser[int(c[0])] = {h[j]: fl(c[j]) for j in range(1, len(h))}
def load(p, seeds):
    data = collections.defaultdict(dict)
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: j for j, k in enumerate(h)}
        for line in f:
            c = line.rstrip('\n').split('\t'); s, q = int(c[0]), int(c[1])
            if s in seeds and q < 80: data[(s, q)][c[ix['nation']]] = (float(c[ix['L']]), float(c[ix['WA']]), float(c[ix['popK']]))
    out = collections.defaultdict(lambda: ([], []))
    for (s, q), nat in data.items():
        L = sum(v[0] for v in nat.values()); P = sum(v[2] for v in nat.values()); qt = L / P; os_ = []
        for l, w, p in nat.values():
            wu = min(w, l); os_.append(max(0.0, l - wu - min(max(0.0, l - wu), p * qt)))
        so = sum(os_); out[q][0].append(so / L); out[q][1].append(max(os_) / so if so > 0 else 0.0)
    return out
pairs = [('N1', 'N2'), ('M', 'N2'), ('M', 'N1')]
for lb, fn in (('M', 'nat_base_ins.tsv'), ('N1', 'nat_n1.tsv'), ('N2', 'nat_n2.tsv')):
    for seeds in ((1, 2), (3, 4), (5, 6)):
        r = load('D:/PoB_work/c113cr_work/paper/' + fn, seeds)
        res = []
        for i, k in enumerate(('sumOsumL', 'maxOshare')):
            per = []
            for pr in pairs:
                n = 0
                for q, v in r.items():
                    m = st.median(v[i]); lo = min(ser[q][f'{b}_{k}_min'] for b in pr); hi = max(ser[q][f'{b}_{k}_max'] for b in pr)
                    n += not (lo <= m <= hi)
                per.append(n)
            res.append((k, per, max(per)))
        print(lb, '種', seeds, res)
