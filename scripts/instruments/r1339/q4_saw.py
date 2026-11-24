# (1339) q4b：鋸の歯の型だけを数える（読むだけ）。型＝人口の比 0.9〜1.1・名目の比 ≧TH・建国から 5 四半期以上・直前 4 四半期のうち 2 以上で実質の比が 0.85^3±0.02（供給の追従の幾何の減り）
#   国ごとに、鋸の歯の減りの四半期（実質の比 0.85^3±0.02 かつ人口の比 0.9〜1.1）の数も数える（列 減りの四半期）
import csv, glob, math, os, sys
a = sys.argv[1:]; TH = float(a[a.index('--th') + 1]) if '--th' in a else 3.0
args = [x for x in a if '=' in x]; DEC = 0.85 ** 3
def num(v):
    try: return float(v)
    except ValueError: return float('nan')
print(f'# q4_saw TH={TH:g} 減り={DEC:.4f}±0.02 人口の比 0.9〜1.1 建国から≧5')
for arg in args:
    name, d = arg.split('=', 1)
    seeds = sorted(int(os.path.basename(p)[8:-4]) for p in glob.glob(os.path.join(d, 'nation_s*.tsv')))
    ev = []; decq = {}; nq = 0
    for s in seeds:
        with open(os.path.join(d, f'nation_s{s}.tsv'), encoding='utf-8', newline='') as f:
            r = csv.reader(f, delimiter='\t'); h = next(r); ix = {c: i for i, c in enumerate(h)}
            first = {}; prev = {}; hr = {}
            for x in r:
                u = x[ix['nationUid']]; q = int(x[ix['quarter']]); g = num(x[ix['nominalGdpPerYear']]); re = num(x[ix['realGdpPerYear']]); pp = num(x[ix['populationK']])
                if u not in first: first[u] = q
                if not math.isfinite(g): continue
                if u in prev and prev[u][0] > 0 and prev[u][1] > 0 and prev[u][2] > 0:
                    nq += 1
                    pg, pre, ppp = prev[u]; ra = g / pg; rre = re / pre; rp = pp / ppp
                    hist = hr.setdefault(u, [])
                    ok = 0.9 <= rp <= 1.1
                    if ok and abs(rre - DEC) <= 0.02: decq[(s, x[ix['nation']])] = decq.get((s, x[ix['nation']]), 0) + 1
                    if ok and ra >= TH and q - first[u] >= 5 and sum(abs(v - DEC) <= 0.02 for v in hist[-4:]) >= 2:
                        ev.append((s, x[ix['nation']], int(x[ix['year']]), q, ra, rre, pp))
                    hist.append(rre)
                prev[u] = (g, re, pp)
    nat = {}
    for e in ev: nat[(e[0], e[1])] = nat.get((e[0], e[1]), 0) + 1
    mx = max(ev, key=lambda e: e[4]) if ev else None
    top = sorted(decq.items(), key=lambda kv: -kv[1])[:5]
    print(f'{name}\t種 {len(seeds)}\t鋸の歯の跳び ≧{TH:g} {len(ev)}（≧10 {sum(e[4]>=10 for e in ev)}・≧100 {sum(e[4]>=100 for e in ev)}）\t国×種 {len(nat)}\t減りの四半期の合計 {sum(decq.values())}／国×四半期 {nq}'
          + (f'\t最大 種{mx[0]} {mx[1]} {mx[2]} q{mx[3]} ×{mx[4]:.4g}（人口 {mx[6]:.0f}千）' if mx else '') + '\t減りの多い国 ' + '・'.join(f'種{k[0]}{k[1]} {v}' for k, v in top))
    for k, v in sorted(nat.items(), key=lambda kv: -kv[1])[:8]:
        m = max(e[4] for e in ev if (e[0], e[1]) == k)
        print(f'   国\t{name}\t種{k[0]}\t{k[1]}\t跳び {v}\t最大 ×{m:.4g}')
