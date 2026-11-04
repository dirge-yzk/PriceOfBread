"""(822) 記録（門ではない）：全ての植民地格の州×四半期のお金の回り（inMarket÷savings・四半期）の分布を、凍結の行・凍結の入りの前 40 四半期・それ以外で並べる（種 1〜10）。
次の波の観測量（お金が有るのに回らない）の物差しの材料。使い方: python -I r822_velall.py <cearn の生> <出力.txt>"""
import sys, os, collections
def fl(x):
    try: return float(x)
    except: return float('nan')
raw, outp = sys.argv[1], sys.argv[2]
buck = collections.defaultdict(list)
for sd in range(1, 11):
    p = os.path.join(raw, 'cearn_s%d.tsv' % sd)
    if not os.path.exists(p): continue
    rows = collections.defaultdict(list)
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            a = line.rstrip('\n').split('\t')
            if a[ix['tier']] != 'Colonial': continue
            s = fl(a[ix['savings']]); m = fl(a[ix['inMarketPerQuarter']])
            rows[a[ix['region']]].append((int(a[ix['quarter']]), a[ix['frozenFlag']] == '1', m / s if s > 0 else float('nan')))
    for reg, rr in rows.items():
        rr.sort()
        fq = [q for q, fr, v in rr if fr]
        first = fq[0] if fq else None
        for q, fr, v in rr:
            if v != v: continue
            if fr: buck['frozen'].append(v)
            elif first is not None and first - 40 <= q < first: buck['pre40'].append(v)
            else: buck['other'].append(v)
def qs(v):
    v = sorted(v)
    return ' '.join('p%d=%.3g' % (k, v[int(k / 100 * (len(v) - 1))]) for k in (1, 10, 50, 90)) if v else '-'
with open(outp, 'w', encoding='utf-8', newline='\n') as o:
    for k in ('frozen', 'pre40', 'other'):
        v = buck[k]
        o.write('%s n=%d %s 1e-3未満の割合=%.3f\n' % (k, len(v), qs(v), sum(1 for x in v if x < 1e-3) / max(1, len(v))))
print(open(outp, encoding='utf-8').read())
