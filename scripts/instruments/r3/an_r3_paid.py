"""(305) 線 r3：英の階級ごとの支払い（paidPerMonth）の Q1〜Q4 の B÷A と Paid÷Earned（読むだけ）。間接税の谷がどの階級の消費の落ち込みか。
  python an_r3_paid.py A=<ins> B=<ins> [Q=4]   （class_s の英の行・四半期の月平均・種の中央）"""
import sys, glob, re, statistics, collections
sys.stdout.reconfigure(encoding='utf-8')
ARMS = collections.OrderedDict(a.split('=', 1) for a in sys.argv[1:] if '=' in a and not a.startswith('Q='))
QN = 4
for a in sys.argv[1:]:
    if a.startswith('Q='): QN = int(a[2:])
def fl(x):
    try: return float(x)
    except Exception: return float('nan')
def med(xs):
    xs = [x for x in xs if x == x]
    return statistics.median(xs) if xs else float('nan')
D = {}
for a, d in ARMS.items():
    D[a] = {}
    for p in glob.glob(d + '/class_s*.tsv'):
        sd = int(re.search(r'class_s(\d+)\.tsv$', p).group(1))
        with open(p, encoding='utf-8') as fh:
            h = fh.readline().rstrip('\n').split('\t'); ix = {x: i for i, x in enumerate(h)}
            for ln in fh:
                r = ln.split('\t')
                m = re.fullmatch(r'[Qq](\d+)', r[ix['time']])
                if r[ix['nation']] == 'イギリス' and m and int(m.group(1)) < QN:
                    D[a].setdefault(sd, {})[(int(m.group(1)), int(r[ix['classId']]))] = (fl(r[ix['sizeK']]), fl(r[ix['savingsStock']]), fl(r[ix['earnedPerMonth']]), fl(r[ix['paidPerMonth']]))
seeds = sorted(set.intersection(*[set(D[a]) for a in D]))
classes = sorted({c for a in D for sd in D[a] for (_, c) in D[a][sd]})
A, B = list(D)
print(f'# (305) 英の階級ごとの Q1〜Q{QN}（種 {seeds}・種の中央）')
print('| 階級 | ' + ' | '.join(f'Q{q+1} 人口K' for q in range(1)) + ' | ' + ' | '.join(f'Q{q+1} 支払い B÷A' for q in range(QN)) + ' | ' + ' | '.join(f'Q{q+1} Paid÷Earned A→B' for q in range(QN)) + ' |')
print('|---|---|' + '---|' * (2 * QN))
for c in classes:
    szs = med([D[A][sd].get((0, c), (float('nan'),) * 4)[0] for sd in seeds])
    pr = [med([D[B][sd][(q, c)][3] / D[A][sd][(q, c)][3] if (q, c) in D[A][sd] and (q, c) in D[B][sd] and D[A][sd][(q, c)][3] > 0 else float('nan') for sd in seeds]) for q in range(QN)]
    pe = []
    for q in range(QN):
        ea = med([D[A][sd][(q, c)][3] / D[A][sd][(q, c)][2] if (q, c) in D[A][sd] and D[A][sd][(q, c)][2] > 0 else float('nan') for sd in seeds])
        eb = med([D[B][sd][(q, c)][3] / D[B][sd][(q, c)][2] if (q, c) in D[B][sd] and D[B][sd][(q, c)][2] > 0 else float('nan') for sd in seeds])
        pe.append(f'{ea:.2f}→{eb:.2f}')
    print(f'| {c} | {szs:.0f} | ' + ' | '.join(f'{x:.3f}' for x in pr) + ' | ' + ' | '.join(pe) + ' |')
tot = [med([sum(D[B][sd][(q, c)][3] for c in classes if (q, c) in D[B][sd]) / sum(D[A][sd][(q, c)][3] for c in classes if (q, c) in D[A][sd]) for sd in seeds]) for q in range(QN)]
print('| 全階級 | | ' + ' | '.join(f'{x:.3f}' for x in tot) + ' | ' + ' | '.join('' for _ in range(QN)) + ' |')
