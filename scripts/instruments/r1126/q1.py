# (1126) 読む線：最低賃金の施行（MinimumWage>=1）の国×四半期を、その続きが始まった上げの〈効いていた政体と移行からの距離〉で分ける。
#   python -X utf8 -I q1.py <出力の頭> <名=生 ins> [...]
#   上げの四半期 q の Government.Step の時の政体＝行 q-1 の polityCode（Politics.Step は Government.Step の後）。
#   距離 d＝q-1 の行が属する選挙政体の続きの最初の行からの距離＋1（非選挙→選挙の移行の行 q0 の次の四半期の上げが d=1＝開院の空白）。
#   区分：q0（試合の最初の行で既に施行）・birth（国の最初の行が試合の頭でない）・gap1（非選挙→選挙の d=1）・ee1（選挙→選挙の移行の d=1 は無い＝政体の変化が選挙どうし）・
#         elect（選挙政体のその他）・nonel（非選挙政体）・civ（不明）。
import sys, os, csv, collections
sys.stdout.reconfigure(newline='\n')
ELECT = {3, 4}
P = (1920, 1951)
if sys.argv[1].startswith('--years='):
    P = tuple(int(v) for v in sys.argv.pop(1)[len('--years='):].split('-'))

def rows(p):
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for x in r:
            if len(x) >= len(h): yield ix, x

def alive(d, s):
    p = os.path.join(d, f'prds_s{s}.tsv'); a = {}
    if not os.path.exists(p): return None
    for ix, x in rows(p):
        try: a[(int(x[ix['quarter']]), x[ix['nation']])] = float(x[ix['popK']]) > 0
        except ValueError: pass
    return a

out = sys.argv[1]
arms = [a.split('=', 1) for a in sys.argv[2:]]
fo = open(out + '_nation.tsv', 'w', encoding='utf-8', newline='\n')
fo.write('arm\tnation\tseeds\trows\taliveRows\tmwRows\tshareAlive\t' + '\t'.join('mw_' + c for c in ('q0', 'birth', 'gap1', 'elect', 'nonel')) + '\telectRowsP\ttransNE2E\tseedsWithMwP\n')
ft = open(out + '_ups.tsv', 'w', encoding='utf-8', newline='\n')
ft.write('arm\tseed\tnation\tquarter\tyear\tfrom\tto\tcat\tdist\tpolPrev\n')
summ = []
for name, d in arms:
    agg = collections.defaultdict(lambda: collections.Counter())
    seedsP = collections.defaultdict(set)
    seeds = sorted(int(f[len('monlaw_s'):-4]) for f in os.listdir(d) if f.startswith('monlaw_s') and f.endswith('.tsv'))
    distc = collections.Counter()
    for s in seeds:
        al = alive(d, s)
        by = collections.defaultdict(list)
        q0g = None
        for ix, x in rows(os.path.join(d, f'monlaw_s{s}.tsv')):
            q = int(x[ix['quarter']]); y = int(x[ix['year']]); n = x[ix['nation']]
            by[n].append((q, y, int(x[ix['polityCode']]), int(x[ix['MinimumWage']])))
            q0g = q if q0g is None else min(q0g, q)
        for n, L in by.items():
            L.sort()
            # 選挙政体の続きの最初の行と、その直前が非選挙か
            runStart = [None] * len(L); fromNon = [False] * len(L)
            for i, (q, y, pc, mw) in enumerate(L):
                if pc in ELECT:
                    if i > 0 and L[i - 1][2] in ELECT and L[i - 1][0] == q - 1:
                        runStart[i] = runStart[i - 1]; fromNon[i] = fromNon[i - 1]
                    else:
                        runStart[i] = q; fromNon[i] = i > 0
            cat = None; c = agg[n]
            for i, (q, y, pc, mw) in enumerate(L):
                prev = L[i - 1] if i > 0 else None
                pmw = prev[3] if prev else 0
                if mw > pmw and prev:
                    pp = prev[2]
                    if pp in ELECT:
                        dd = q - runStart[i - 1]
                        k = 'gap1' if (dd == 1 and fromNon[i - 1]) else 'elect'
                    else:
                        dd = -1; k = 'nonel'
                    distc[(k, min(dd, 17) if dd >= 0 else -1)] += 1
                    ft.write(f'{name}\t{s}\t{n}\t{q}\t{y}\t{pmw}\t{mw}\t{k}\t{dd}\t{pp}\n')
                if mw >= 1:
                    if i == 0: cat = 'q0' if q == q0g else 'birth'
                    elif pmw == 0: cat = k
                else:
                    cat = None
                if P[0] <= y <= P[1]:
                    c['rows'] += 1
                    ok = al.get((q, n), False) if al is not None else True
                    c['alive'] += ok
                    if pc in ELECT: c['electP'] += 1
                    if mw >= 1:
                        c['mw'] += 1; c['mw_' + cat] += 1; seedsP[n].add(s)
            c['trans'] += sum(1 for i in range(1, len(L)) if L[i][2] in ELECT and L[i - 1][2] not in ELECT)
            c['seeds'] += 1
    T = collections.Counter()
    for n in sorted(agg, key=lambda k: -agg[k]['rows']):
        c = agg[n]; T.update(c)
        sh = c['mw'] / c['alive'] if c['alive'] else float('nan')
        fo.write(f"{name}\t{n}\t{c['seeds']}\t{c['rows']}\t{c['alive']}\t{c['mw']}\t{sh:.4f}\t" + '\t'.join(str(c['mw_' + k]) for k in ('q0', 'birth', 'gap1', 'elect', 'nonel')) + f"\t{c['electP']}\t{c['trans']}\t{len(seedsP[n])}\n")
    summ.append((name, T, distc))
with open(out + '_sum.txt', 'w', encoding='utf-8', newline='\n') as f:
    f.write('arm\trowsP\taliveP\tmwP\tshareRows\tshareAlive\tmw_q0\tmw_birth\tmw_gap1\tmw_elect\tmw_nonel\tups_gap1\tups_elect\tups_nonel\tups_d0\n')
    for name, T, dc in summ:
        g = lambda k: sum(v for (kk, _), v in dc.items() if kk == k)
        f.write(f"{name}\t{T['rows']}\t{T['alive']}\t{T['mw']}\t{T['mw']/T['rows']:.4f}\t{T['mw']/T['alive']:.4f}\t" + '\t'.join(str(T['mw_' + k]) for k in ('q0', 'birth', 'gap1', 'elect', 'nonel'))
                + f"\t{g('gap1')}\t{g('elect')}\t{g('nonel')}\t{dc[('elect', 0)]}\n")
    for name, T, dc in summ:
        f.write(name + ' dist(elect/gap1): ' + ' '.join(f'{k}{dd}:{v}' for (k, dd), v in sorted(dc.items())) + '\n')
