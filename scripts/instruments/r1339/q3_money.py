# (1339) q3：鋸の歯の減りの四半期と跳びが〈民間の貨幣の痩せた国〉に寄るかを数える（読むだけ）
#   民間の貨幣の比 rel＝その国の totalMoneyStock÷人口 を、同じ時点の全ての国（人口 ≧10 千人）の中央値で割った物。stock_s は 1 年に 1 行＝その四半期以前で最も近い行を使う
#   減りの四半期＝実質の比 0.85^3±0.02 かつ人口の比 0.9〜1.1。跳び＝その後の名目の比 ≧3（q4_saw と同じ型）
import csv, glob, math, os, sys, bisect, statistics
args = [x for x in sys.argv[1:] if '=' in x]; DEC = 0.85 ** 3
def num(v):
    try: return float(v)
    except ValueError: return float('nan')
BINS = [(-1, 1e-4), (1e-4, 1e-2), (1e-2, 1e-1), (1e-1, 1e99)]
print('# q3_money rel＝国の民間の貨幣÷人口 を同じ年の中央値で割った比・区間 [<1e-4, 1e-4〜1e-2, 1e-2〜0.1, ≧0.1]')
for arg in args:
    name, d = arg.split('=', 1)
    seeds = sorted(int(os.path.basename(p)[8:-4]) for p in glob.glob(os.path.join(d, 'nation_s*.tsv')))
    allc = [0]*4; decc = [0]*4; jmp = [0]*4
    for s in seeds:
        mon = {}; byq = {}
        with open(os.path.join(d, f'stock_s{s}.tsv'), encoding='utf-8', newline='') as f:
            r = csv.reader(f, delimiter='\t'); h = next(r); ix = {c: i for i, c in enumerate(h)}
            for x in r:
                if not x[0].startswith('q') or not x[0][1:].isdigit(): continue
                q = int(x[0][1:]); pk = num(x[ix['populationK']]); m = num(x[ix['totalMoneyStock']])
                if pk >= 10 and math.isfinite(m): byq.setdefault(q, []).append((x[ix['nation']], m / pk))
        med = {q: statistics.median(v for _, v in l) for q, l in byq.items()}
        rel = {}
        for q, l in byq.items():
            for nm, v in l: rel.setdefault(nm, []).append((q, v / med[q] if med[q] > 0 else float('nan')))
        for nm in rel: rel[nm].sort()
        def getrel(nm, q):
            l = rel.get(nm)
            if not l: return float('nan')
            i = bisect.bisect_right([a for a, _ in l], q) - 1
            return l[i][1] if i >= 0 and q - l[i][0] < 8 else float('nan')
        def bin_(v):
            for i, (lo, hi) in enumerate(BINS):
                if lo <= v < hi: return i
            return None
        with open(os.path.join(d, f'nation_s{s}.tsv'), encoding='utf-8', newline='') as f:
            r = csv.reader(f, delimiter='\t'); h = next(r); ix = {c: i for i, c in enumerate(h)}
            prev = {}; first = {}; hr = {}
            for x in r:
                u = x[ix['nationUid']]; q = int(x[ix['quarter']]); g = num(x[ix['nominalGdpPerYear']]); re = num(x[ix['realGdpPerYear']]); pp = num(x[ix['populationK']]); nm = x[ix['nation']]
                if u not in first: first[u] = q
                if u in prev and prev[u][0] > 0 and prev[u][1] > 0 and prev[u][2] >= 10 and math.isfinite(g):
                    pg, pre, ppp = prev[u]; ra = g / pg; rre = re / pre; rp = pp / ppp
                    b = bin_(getrel(nm, q))
                    hist = hr.setdefault(u, [])
                    if b is not None:
                        allc[b] += 1
                        if 0.9 <= rp <= 1.1 and abs(rre - DEC) <= 0.02: decc[b] += 1
                        if 0.9 <= rp <= 1.1 and ra >= 3 and q - first[u] >= 5 and sum(abs(v - DEC) <= 0.02 for v in hist[-4:]) >= 2: jmp[b] += 1
                    hist.append(rre)
                if math.isfinite(g): prev[u] = (g, re, pp)
    print(f'{name}\t国×四半期 ' + '／'.join(map(str, allc)) + '\t減りの四半期 ' + '／'.join(map(str, decc)) + '\t跳び≧3 ' + '／'.join(map(str, jmp))
          + '\t減りの率 ' + '／'.join(f'{decc[i]/allc[i]:.2g}' if allc[i] else '-' for i in range(4)))
