"""(199) 徴兵法の門の支持の構成と幽霊戦線の効き（読むだけ・m199/inj_cnl199 の K 行の行末の列）。
python an_cnl199.py <out の dir> <タグ>
 ① 主要10か国の平時の 30年の窓の中央＝3勢力の政治力 t/m/s・軍の政治力・徴兵法を下げる一歩の純支持（netDn）と上げる一歩の純支持（netUp）
 ② 平時に門で止まった目標（下げ＝施行の前の目標＜法・上げ＝施行の前の目標＞法・段は動かず）が残る国×四半期の割合と、
    その四半期の政治の押しの予算 push＝(1/N)^0.5 から戦線の数 N を戻し、徴兵法の戦線が閉じていたときの予算 (1/(N−1))^0.5 との比＝
    他の全ての段の法とスライダー（Move の速度）がその四半期に何割遅くなったか（Government.Step の PoliticalPushBudget）。
 ③ 平時に下げる目標が置かれた国×四半期（施行の前の目標＜法）で下げが施行された割合（国ごと）。"""
import sys, os, glob, statistics as st
from collections import defaultdict
O = sys.argv[1]; TAG = sys.argv[2] if len(sys.argv) > 2 else ''
MAJ = ['イギリス', 'アメリカ', 'フランス', 'プロイセン', 'ロシア', '日本', '清', 'オーストリア', 'スペイン', 'オスマン']
GATE = 0.5   # Coefficients.PoliticalBudgetGate
def I(x):
    try: return int(x)
    except Exception: return -9
def F(x):
    try: return float(x)
    except Exception: return float('nan')
rows = []; seeds = set(); COL = None
for f in glob.glob(os.path.join(O, 'cnl', 'c*.txt')):
    sd = int(os.path.basename(f)[1:-4]); seeds.add(sd)
    for ln in open(f, encoding='utf-8'):
        if ln.startswith('#'): COL = ln[1:].rstrip('\n').split('\t'); continue
        d = dict(zip(COL, ln.rstrip('\n').split('\t'))); d['_s'] = sd; rows.append(d)
NS = len(seeds) or 1
print(f'# (199) 徴兵法の門の支持と幽霊戦線 {TAG}｜種 {NS}｜行 {len(rows)}')
if not rows or 'push' not in COL: print('!! 列が無い（inj_cnl199 でない）'); sys.exit()
W0 = [1784, 1814, 1844, 1874, 1904, 1934]
def win(y):
    for w0 in reversed(W0):
        if y >= w0: return w0
    return W0[0]
print('## ① 平時の中央（30年の窓）＝政治力 t/m/s/軍｜下げる一歩の純支持｜上げる一歩の純支持')
acc = defaultdict(lambda: defaultdict(list))
for d in rows:
    if I(d['atWar']) != 0 or d['nation'] not in MAJ: continue
    k = (d['nation'], win(I(d['yr'])))
    for c in ('pwT', 'pwM', 'pwS', 'pwArmy', 'netDn', 'netUp'):
        v = F(d[c])
        if v == v: acc[k][c].append(v)
def md(L): return st.median(L) if L else float('nan')
print('            ' + ''.join(f'{w:>34}' for w in W0))
for n in MAJ:
    cells = []
    for w0 in W0:
        a = acc[(n, w0)]
        if not a['pwT']: cells.append(f'{"-":>34}'); continue
        cells.append(f"{md(a['pwT']):.2f}/{md(a['pwM']):.2f}/{md(a['pwS']):.2f}/{md(a['pwArmy']):.2f}|{md(a['netDn']):+.2f}|{md(a['netUp']):+.2f}".rjust(34))
    print(f'  {n:<6}' + ''.join(cells))
print('## ② 平時に門で止まった目標が残る国×四半期（施行の前の目標≠法・段は動かず）÷平時の国×四半期｜その時の戦線の数 N の中央｜徴兵法の戦線が閉じていたときに比べた押しの予算の中央（1未満＝他の法とスライダーが遅くなる）')
G = defaultdict(lambda: [0, 0, 0, [], []])   # 下げ・上げ・平時・N・比
for d in rows:
    if I(d['atWar']) != 0: continue
    key = d['nation'] if d['nation'] in MAJ else '_他'
    g = G[key]; g[2] += 1
    t2, l0, l1 = I(d['tgt2']), I(d['law0']), I(d['law1'])
    if t2 < 0 or l1 != l0 or t2 == l0: continue
    if t2 < l0: g[0] += 1
    else: g[1] += 1
    p = F(d['push'])
    if p == p and p > 0:
        N = round(p ** (-1 / GATE))
        g[3].append(N)
        if N > 1: g[4].append(p / (1.0 / (N - 1)) ** GATE)
tot = [0, 0, 0, [], []]
for k in MAJ + ['_他']:
    g = G[k]
    for i in range(3): tot[i] += g[i]
    tot[3] += g[3]; tot[4] += g[4]
    if g[2] and (g[0] or g[1]):
        print(f'  {k}: 下げ {g[0]}・上げ {g[1]}／{g[2]}（{100 * (g[0] + g[1]) / g[2]:.1f}%）｜N {md(g[3])}｜比 {md(g[4]):.3f}')
if tot[2]:
    print(f'  全ての国: 下げ {tot[0]}・上げ {tot[1]}／{tot[2]}（{100 * (tot[0] + tot[1]) / tot[2]:.2f}%）｜N {md(tot[3])}｜比 {md(tot[4]):.3f}（10分位 {sorted(tot[4])[len(tot[4]) // 10] if tot[4] else float("nan"):.3f}）')
    allp = [F(d['push']) for d in rows if I(d['atWar']) == 0 and F(d['push']) == F(d['push'])]
    print(f'  平時の全ての国×四半期の押しの予算の中央 {md(allp):.3f}（戦線の数の中央 {md([round(x ** (-1 / GATE)) for x in allp if x > 0])}）')
print('## ③ 平時に下げる目標が置かれた国×四半期で下げが施行された割合（施行の前の目標＜法）')
H = defaultdict(lambda: [0, 0])
for d in rows:
    if I(d['atWar']) != 0: continue
    t2, l0, l1 = I(d['tgt2']), I(d['law0']), I(d['law1'])
    if t2 < 0 or not (t2 < l0): continue
    key = d['nation'] if d['nation'] in MAJ else '_他'
    H[key][1] += 1
    if l1 < l0: H[key][0] += 1
for k in MAJ + ['_他']:
    a, b = H[k]
    if b: print(f'  {k}: {a}/{b}（{100 * a / b:.0f}%）')
