"""(197) 往復の読み手（読むだけ）＝反証役 vp1#3（徴兵法の下げと上げ直し）・vp2#3（総動員の入り・切り・また入り）。
python an_osc197.py <out の dir> <タグ>
 A. 徴兵法（m193/inj_cnl193 の K 行）＝平時に下げた一歩の後の次の段の変化（上げ直しまでの四半期・その時の交戦・読んだ割合）と、
    平時に門で止まった下げの目標（施行の前の目標 tgt2＜法）が残る国×四半期。
 B. 総動員（m194/inj_mob194 の M 行）＝交戦の続き（四半期が連続する M 行の塊）の中の入り・切り・また入り（切った後に同じ続きの中で入った回数）と、
    切ってからまた入るまでの四半期。"""
import sys, os, glob, statistics as st
from collections import defaultdict
O = sys.argv[1]; TAG = sys.argv[2] if len(sys.argv) > 2 else ''
MAJ = ['イギリス', 'アメリカ', 'フランス', 'プロイセン', 'ロシア', '日本', '清', 'オーストリア', 'スペイン', 'オスマン']
def I(x):
    try: return int(x)
    except Exception: return -9
def F(x):
    try: return float(x)
    except Exception: return float('nan')
# ---- A
rows = defaultdict(list); seeds = set(); COL = None
for f in glob.glob(os.path.join(O, 'cnl', 'c*.txt')):
    sd = int(os.path.basename(f)[1:-4]); seeds.add(sd)
    for ln in open(f, encoding='utf-8'):
        if ln.startswith('#'): COL = ln[1:].rstrip('\n').split('\t'); continue
        d = dict(zip(COL, ln.rstrip('\n').split('\t'))); rows[(sd, d['nation'])].append(d)
NS = len(seeds) or 1
print(f'# (197) 往復 {TAG}｜種 {NS}')
print('## A. 徴兵法＝平時に下げた一歩の後（主要10か国＋_他）：下げ｜次の変化が上げ（平時）｜うち8四半期以内｜うち20四半期以内｜次の変化が上げ（交戦中）｜次の変化なし／下げ｜上げ直しまでの四半期の中央｜平時の上げ直しの時に読んだ割合の中央')
agg = defaultdict(lambda: defaultdict(int)); gaps = defaultdict(list); shs = defaultdict(list)
pend = defaultdict(lambda: [0, 0])   # 平時の国×四半期のうち、施行の前の目標が法より下（門で止まった下げ）の数・全体
dnshare = defaultdict(list)
for (sd, n), L in rows.items():
    key = n if n in MAJ else '_他'
    L.sort(key=lambda d: I(d['q']))
    ev = []
    for d in L:
        a, l0, l1 = I(d['atWar']), I(d['law0']), I(d['law1'])
        if a == 0:
            pend[key][1] += 1
            if I(d['tgt2']) >= 0 and I(d['tgt2']) < l0 and l1 == l0: pend[key][0] += 1
        if l1 == l0: continue
        p0, p1 = I(d['pre0']), I(d['pre1'])
        lapse = a == 0 and p0 >= 0 and l1 < l0 and p1 < 0 and l1 == p0
        ev.append((I(d['q']), a, l0, l1, lapse, F(d['share'])))
    for i, (q, a, l0, l1, lapse, sh) in enumerate(ev):
        if a != 0 or lapse or l1 >= l0: continue
        agg[key]['下げ'] += 1; dnshare[key].append(sh)
        nx = ev[i + 1] if i + 1 < len(ev) else None
        if nx is None or nx[3] < nx[2]: agg[key]['なし／下げ'] += 1; continue
        g = nx[0] - q
        if nx[1] == 1: agg[key]['上げ（交戦中）'] += 1; gaps[key].append(g); continue
        agg[key]['上げ（平時）'] += 1; gaps[key].append(g); shs[key].append(nx[5])
        if g <= 8: agg[key]['8以内'] += 1
        if g <= 20: agg[key]['20以内'] += 1
tot = defaultdict(int)
for k in MAJ + ['_他']:
    c = agg[k]
    for x, v in c.items(): tot[x] += v
    if not c['下げ']: continue
    g = gaps[k]; s = [x for x in shs[k] if x == x]
    print(f"  {k}: {c['下げ'] / NS:.1f}｜{c['上げ（平時）'] / NS:.2f}｜{c['8以内'] / NS:.2f}｜{c['20以内'] / NS:.2f}｜{c['上げ（交戦中）'] / NS:.2f}｜{c['なし／下げ'] / NS:.2f}｜{st.median(g) if g else '-'}｜{st.median(s) if s else '-'}")
print(f"  全ての国: 下げ {tot['下げ'] / NS:.1f}・平時に上げ直し {tot['上げ（平時）'] / NS:.2f}（8四半期以内 {tot['8以内'] / NS:.2f}・20以内 {tot['20以内'] / NS:.2f}）・交戦中に上げ直し {tot['上げ（交戦中）'] / NS:.2f}・次の変化なしか下げ {tot['なし／下げ'] / NS:.2f}（種あたり）")
allsh = [x for k in dnshare for x in dnshare[k] if x == x]
if allsh: print(f'  平時に下げた時に読んだ割合（全ての国）の中央 {st.median(allsh):.3f}・最大 {max(allsh):.3f}・0.4 以上 {sum(1 for x in allsh if x >= 0.4)}/{len(allsh)}')
print('## A2. 平時に門で止まった下げの目標が残る国×四半期（施行の前の目標＜法・段は動かず）÷平時の国×四半期')
pp = [0, 0]
for k in MAJ + ['_他']:
    a, b = pend[k]; pp[0] += a; pp[1] += b
    if b: print(f'  {k}: {a}/{b}（{100 * a / b:.1f}%）')
if pp[1]: print(f'  全ての国: {pp[0]}/{pp[1]}（{100 * pp[0] / pp[1]:.1f}%）')
# ---- B
mrows = defaultdict(list)
for f in glob.glob(os.path.join(O, 'mob', 'm*.txt')):
    sd = int(os.path.basename(f)[1:-4])
    for ln in open(f, encoding='utf-8'):
        t = ln.rstrip('\n').split('|')
        if t[0] != 'M' or len(t) < 42: continue
        mrows[(sd, t[3])].append((int(t[1]), int(t[5]), int(t[6]), F(t[42]) if len(t) > 42 else float('nan')))
print('## B. 総動員＝交戦の続き（連続する交戦の四半期）の中の切り替え（種あたり）：入り｜交戦中の切り｜また入り（同じ続きの中で切った後に入った）｜また入りまでの四半期の中央｜また入りのある続き÷入りのある続き')
B = defaultdict(lambda: defaultdict(int)); rg = defaultdict(list)
for (sd, n), L in mrows.items():
    key = n if n in MAJ else '_他'
    L.sort()
    ep = []; prevq = None
    for r in L:
        if prevq is None or r[0] != prevq + 1: ep.append([])
        ep[-1].append(r); prevq = r[0]
    for e in ep:
        offq = None; ent = 0; re_ = 0
        for q, m0, m1, wz in e:
            if m0 == 0 and m1 == 1:
                B[key]['入り'] += 1; ent += 1
                if offq is not None: B[key]['また入り'] += 1; re_ += 1; rg[key].append(q - offq)
            if m0 == 1 and m1 == 0: B[key]['切り'] += 1; offq = q
        if ent: B[key]['入りのある続き'] += 1
        if re_: B[key]['また入りのある続き'] += 1
TB = defaultdict(int); allg = []
for k in MAJ + ['_他']:
    c = B[k]
    for x, v in c.items(): TB[x] += v
    allg += rg[k]
    if not c['入り']: continue
    print(f"  {k}: {c['入り'] / NS:.1f}｜{c['切り'] / NS:.1f}｜{c['また入り'] / NS:.1f}｜{st.median(rg[k]) if rg[k] else '-'}｜{c['また入りのある続き']}/{c['入りのある続き']}")
print(f"  全ての国: 入り {TB['入り'] / NS:.1f}・交戦中の切り {TB['切り'] / NS:.1f}・また入り {TB['また入り'] / NS:.1f}（四半期の中央 {st.median(allg) if allg else '-'}）・また入りのある続き {TB['また入りのある続き']}/{TB['入りのある続き']}")
