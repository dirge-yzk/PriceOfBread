# (917) c79db 読む線：商人の〈塵の債務〉（0 ＜ debt ≦1e-9）の読み。読むだけ・門を作らない。
#   python -X utf8 -I read917_dust.py <名> <ins フォルダ> [--seeds 1 10]
#   なぜ：BilateralMarket.cs:580 の bill＝!(Debt ＞0) は厳密な 0 を要る。Pop.Take（Model.cs:358）は take ≦1e-12 か保有 ≦1e-9 なら 0 を返す
#   ＝1e-12 以下の債務は決済 ①（:579）でも B5（EmptyPopEstate.cs:22）でも返らない＝手形を振り出せないまま残る疑い。
#   人の居る商人と空の商人に分け、値の段（≦1e-15・≦1e-12・≦1e-9）・続きの長さ・その四半期の inCredit ＞0 の割合を数える。
import sys, os
from collections import defaultdict
args = sys.argv[1:]
s0, s1 = 1, 10
if '--seeds' in args:
    i = args.index('--seeds'); s0, s1 = int(args[i + 1]), int(args[i + 2]); del args[i:i + 3]
tag, d = args[0], args[1]
DEAD = 1e-12
def fl(x):
    try: return float(x)
    except ValueError: return float('nan')
def q(v, p):
    if not v: return float('nan')
    v = sorted(v); return v[min(len(v) - 1, int(p * (len(v) - 1)))]
bins = {k: defaultdict(int) for k in ('live', 'empty')}
rows = defaultdict(int); creditInDust = defaultdict(int); creditAll = defaultdict(int); zero = defaultdict(int)
runs = {'live': [], 'empty': []}; nat = defaultdict(lambda: [0, 0]); savDust = []
seeds = 0
for s in range(s0, s1 + 1):
    p = os.path.join(d, f'cpop_s{s}.tsv')
    if not os.path.exists(p): continue
    seeds += 1
    ser = defaultdict(list)
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        iq, inat, ir, ipi, icl, ipk, icr, idb, isv = ix['quarter'], ix['nation'], ix['region'], ix['popIndex'], ix['className'], ix['popK'], ix['inCreditPerQuarter'], ix['debt'], ix['savings']
        for line in f:
            if '\t商人\t' not in line: continue
            x = line.rstrip('\n').split('\t')
            if x[icl] != '商人': continue
            k = 'live' if fl(x[ipk]) > DEAD else 'empty'
            D = fl(x[idb]); C = fl(x[icr]); rows[k] += 1
            if C > 0: creditAll[k] += 1
            if D == 0: zero[k] += 1
            dust = 0 < D <= 1e-9
            if dust:
                b = '≦1e-15' if D <= 1e-15 else '≦1e-12' if D <= 1e-12 else '≦1e-9'
                bins[k][b] += 1
                if C > 0: creditInDust[k] += 1
                if k == 'live':
                    nn = nat[x[inat]]; nn[0] += 1; savDust.append(fl(x[isv]))
            if k == 'live': nat[x[inat]][1] += 1
            ser[(x[ir], x[ipi], k)].append((int(x[iq]), dust))
    for (rg, pi, k), v in ser.items():
        run = 0; last = None
        for qq, c in v:
            if c and run and last is not None and qq == last + 1: run += 1
            else:
                if run: runs[k].append(run)
                run = 1 if c else 0
            last = qq
        if run: runs[k].append(run)
r = lambda a, b: a / b if b else float('nan')
print(f'# 名 {tag}・フォルダ {d}・種 {seeds}')
for k in ('live', 'empty'):
    nd = sum(bins[k].values()); rs = sorted(runs[k])
    print(f'{k}：行 {rows[k]}・debt＝0 {zero[k]}（{r(zero[k], rows[k]):.4f}）・塵 {nd}（{r(nd, rows[k]):.4f}）〔' + '・'.join(f'{b} {bins[k][b]}' for b in ('≦1e-15', '≦1e-12', '≦1e-9')) + f'〕・塵の行で inCredit ＞0 {creditInDust[k]}（{r(creditInDust[k], nd):.4f}）・全ての行で inCredit ＞0 {creditAll[k]}（{r(creditAll[k], rows[k]):.4f}）')
    print(f'  塵の続き {len(rs)} 本・中央 {q(rs, .5)}・90 点 {q(rs, .9)}・最大 {rs[-1] if rs else 0}')
print(f'人の居る商人の塵の行の末の貯蓄：10 点 {q(savDust, .1):.4g}・中央 {q(savDust, .5):.4g}・90 点 {q(savDust, .9):.4g}')
top = sorted(nat.items(), key=lambda kv: -kv[1][0])[:10]
print('人の居る商人の塵（国・塵の行/行）上位 10：' + '・'.join(f'{k} {c}/{n}' for k, (c, n) in top))
