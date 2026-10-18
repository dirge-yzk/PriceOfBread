"""〈バランスを壊す〉の物差し ①②③（README・2026-10-18 (397) で exact・near・NG と置き換えた・AI の推奨・閾値は AI の置き・作者の承認は未確認）の共通の読み手。
(402) で作った：(400) 線 op の読み手 an_op400.py の 217〜263 行の定義を、線に依らない形で抜き出した（腕の名前は自由・ヌル腕は N で始まる名前）。
入力：標準の計器の nation_s<seed>.tsv（全ての国・四半期ごと）だけ。使う列＝nation・quarter・totalPopulationK・coreRegions・realGdpPerYear・monopolyTierEndStage。
  ① 同じ国が毎回壊れる：最後の行がピークの 1/2 未満（人口か中核の州の数）の種が 8/10 以上・ヌル腕では 2/10 以下の国。
  ② 一国の発散：首位の国の世界の実質GDP の取り分。読み方を 2 つ出す（README の文は〈ヌル腕の最大を幅の 2 倍以上超え〉で、幅の取り方が書かれていない）。
     (ア) an_op400.py と同じ＝ヌル腕の全ての種の値を 1 つに集めた最大と幅（最大−最小）。
     (イ) 腕の中央値どうし＝ヌル腕の中央値の最大と、ヌル腕の中央値の差を幅にする（(ア) より鋭い）。どちらも 8/10 以上の種で首位が同じ国のときだけ鳴る。
  ③ 片方向の錠：最大の段（専売の段 5＝monopolyTierEndStage）に張り付く国×四半期がヌル腕の最大の 2 倍以上。
⚠限界（自己申告）：①は途中で行が切れる国（併合）の〈消える〉を最後の行で見るので拾えないことがある。③は専売の段だけ＝ほかの法の段の錠は見ない。
使い方：python -X utf8 -I an_brk.py A=<ins> B=<ins> N1=<ins> N2=<ins> [--q 524,667]"""
import sys, os, glob, re, statistics, collections

COLS = ('nation', 'quarter', 'totalPopulationK', 'coreRegions', 'realGdpPerYear', 'monopolyTierEndStage')
FIN = lambda x: x == x and abs(x) != float('inf')
med = lambda xs: statistics.median(xs) if xs else float('nan')


def num(x):
    try:
        return float(x)
    except Exception:
        return float('nan')


def load(D):
    out = {}
    for f in glob.glob(os.path.join(D, 'nation_s*.tsv')):
        m = re.match(r'nation_s(\d+)\.tsv$', os.path.basename(f))
        if not m:
            continue
        sd = int(m.group(1))
        with open(f, encoding='utf-8') as fh:
            head = fh.readline().rstrip('\n').split('\t')
            ix = [head.index(c) for c in COLS]
            N = out.setdefault(sd, {})
            for ln in fh:
                p = ln.rstrip('\n').split('\t')
                N.setdefault(p[ix[0]], {})[int(p[ix[1]])] = tuple(num(p[i]) for i in ix[2:])
    return out


def fmt(xs, p=3):
    xs = [x for x in xs if FIN(x)]
    return '-' if not xs else f'{med(xs):.{p}f}[{min(xs):.{p}f}~{max(xs):.{p}f}]'


args = sys.argv[1:]
QS = (524, 667)
if '--q' in args:
    i = args.index('--q'); QS = tuple(int(x) for x in args[i + 1].split(',')); del args[i:i + 2]
ARMS = dict(a.split('=', 1) for a in args)
ORDER = list(ARMS)
NULLS = [k for k in ORDER if k.startswith('N')]
DATA = {}
for k in ORDER:
    DATA[k] = load(ARMS[k])
    print(f'腕 {k}: {ARMS[k]}  種 {sorted(DATA[k])}')
print()

# ① 同じ国が毎回壊れる
def broken(arm):
    c = collections.Counter()
    for sd, N in DATA[arm].items():
        for nat, qs in N.items():
            rs = [qs[q] for q in sorted(qs)]
            pop = [r[0] for r in rs if FIN(r[0])]; core = [r[1] for r in rs if FIN(r[1])]
            if pop and max(pop) > 0 and pop[-1] < 0.5 * max(pop): c[nat] += 1
            elif core and max(core) > 0 and core[-1] < 0.5 * max(core): c[nat] += 1
    return c
BR = {k: broken(k) for k in ORDER}
print('== ① 人口か中核の州の半分以上を失う種の数（5 種以上の国だけ）')
for k in ORDER:
    print(f'  {k}\t{[(n, c) for n, c in BR[k].most_common() if c >= 5]}')
for k in ORDER:
    if k in NULLS: continue
    fl = [n for n, c in BR[k].items() if c >= 8 and all(BR[nk].get(n, 0) <= 2 for nk in NULLS)]
    print(f'  ① 判定 {k}: 8/10 以上で壊れ、ヌル腕は 2/10 以下の国 {fl or "なし"}')

# ② 一国の発散
def top_share(arm, q):
    out = []
    for sd, N in sorted(DATA[arm].items()):
        vals = {nat: qs[q][2] for nat, qs in N.items() if q in qs and FIN(qs[q][2])}
        tot = sum(vals.values())
        if tot > 0:
            n, v = max(vals.items(), key=lambda kv: kv[1]); out.append((n, v / tot))
    return out
print('\n== ② 首位の国の世界の実質GDP の取り分（中央値[最小~最大]）')
for q in QS:
    ts = {k: top_share(k, q) for k in ORDER}
    pooled = [s for k in NULLS for _, s in ts[k]]
    pmax = max(pooled) if pooled else float('nan'); prng = (max(pooled) - min(pooled)) if pooled else float('nan')
    nmeds = [med([s for _, s in ts[k]]) for k in NULLS]
    mmax = max(nmeds) if nmeds else float('nan'); mrng = (max(nmeds) - min(nmeds)) if nmeds else float('nan')
    for k in ORDER:
        names = collections.Counter(n for n, _ in ts[k]); sh = [s for _, s in ts[k]]
        same = names.most_common(1)[0][1] if names else 0
        tag = ''
        if k not in NULLS and NULLS:
            if med(sh) > pmax + 2 * prng and same >= 8: tag += ' ⚠(ア)'
            if med(sh) > mmax + 2 * mrng and same >= 8: tag += ' ⚠(イ)'
        print(f'  Q{q}\t{k}\t{fmt(sh)}\t首位 {dict(names.most_common(3))}\t同じ国 {same}/{len(sh)}{tag}')
    print(f'     (ア) ヌル腕の全種の最大 {pmax:.3f}・幅 {prng:.3f}＝線 {pmax + 2 * prng:.3f}　(イ) ヌル腕の中央値の最大 {mmax:.3f}・幅 {mrng:.3f}＝線 {mmax + 2 * mrng:.3f}')

# ③ 片方向の錠
mt = {k: sum(1 for N in DATA[k].values() for n in N.values() for r in n.values() if r[3] >= 5) for k in ORDER}
nm3 = max((mt[k] for k in NULLS), default=float('nan'))
print('\n== ③ 最大の段（専売の段 5）に張り付く国×四半期')
for k in ORDER:
    tag = '⚠（ヌル腕の最大の 2 倍以上）' if k not in NULLS and FIN(nm3) and mt[k] >= 2 * max(nm3, 1) else ''
    print(f'  {k}\t{mt[k]}\tヌル腕の最大 {nm3}\t{tag}')
