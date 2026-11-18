# (1211) 支配の力 §3-2 の A を読む台本（読むだけ・コミットしない）。
# 呼び方: python -X utf8 -I r1211_ana.py <生のフォルダ> <種の下> <種の上> <出力のフォルダ>
# 読む表: rulepower_s（全ての国・州×四半期）・secession_s（選んだ国だけ＝闘争度と中核の割合の結合に使う）・rponset_s（外国の取り分の対の動き）。
# A の反実仮想は計器と同じ割合の式（InstrumentsRulePower.cs RulePowerClasses）を台本が自分で書き直し、
# まず A＝0 で計器の ownerSideShare ほかと一致するかを数える（エンジンの値との突き合わせ）。
import sys, os, math
from collections import defaultdict, Counter

raw, s0, s1, out = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4]
os.makedirs(out, exist_ok=True)

def fl(x):
    try:
        v = float(x)
    except ValueError:
        return float('nan')
    return v

def classes(fs, fi, fr, fsum, fu, kappa):
    k = 1.0 if kappa > 1 else (kappa if kappa > 0 else 0.0)
    fsum = fsum if fsum > 0 else 0.0
    tot = fs + fi + fr + fsum + fu
    if not (tot > 0):
        return None
    o = (fs + k * fi) / tot; l = ((1 - k) * fi + fr) / tot; f = fsum / tot; u = fu / tot
    best = 0; bv = o
    if l > bv: best, bv = 1, l
    if f > bv: best, bv = 2, f
    if u > bv: best = 3
    return (o, l, f, u, best)

NAMES = ['owner', 'local', 'foreign', 'ungov']
BINS = [(-1, 0.0, 's=0'), (0.0, 0.05, '(0,0.05]'), (0.05, 0.2, '(0.05,0.2]'), (0.2, 0.5, '(0.2,0.5]'), (0.5, 9, '>0.5')]
def binof(s):
    for lo, hi, nm in BINS:
        if s > lo and s <= hi: return nm
    return 's=0'

tot = defaultdict(float)
binstat = defaultdict(lambda: [0, 0.0, 0.0, 0])   # n, sum shadow foreign, sum gap, n(shadow>now)
flips = {v: Counter() for v in ('V1', 'V2', 'V3', 'V4', 'V5')}
mism = []; ub_ratio = []; xbest = Counter()
ratio_rows = []   # A/fForeignSum の分布（X>0 の行）
shadow_foreign_best = Counter()   # (nation, region) → 四半期の数
shadow_foreign_now = []           # 影で外国が最大の行の今の外国の取り分
per_seed_lines = []
pair_rise = Counter(); pair_rise_size = Counter(); pair_max = []; pair_peace_rise = 0
pair_odd = Counter()

for seed in range(s0, s1 + 1):
    # --- secession_s：闘争度と中核の割合（選んだ国だけ） ---
    sec = {}
    p = os.path.join(raw, f'secession_s{seed}.tsv')
    with open(p, encoding='utf-8', newline='') as f:
        h = f.readline().rstrip('\r\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            c = line.rstrip('\r\n').split('\t')
            sec[(c[0], c[2], c[3])] = (fl(c[ix['militancyMeanIndex']]), fl(c[ix['coreShareMeanShare']]))
    # --- rulepower_s ---
    p = os.path.join(raw, f'rulepower_s{seed}.tsv')
    n_rows = n_match = n_cmp = 0; n_join = 0; n_x = 0; n_xjoin = 0
    sb = Counter(); nb = Counter()
    with open(p, encoding='utf-8', newline='') as f:
        h = f.readline().rstrip('\r\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        I = lambda k: ix[k]
        iq, inat, ireg, iN = I('quarter'), I('nation'), I('region'), I('nIndex')
        iFS, iFI, iFR, iA, iFU = I('fStateIndex'), I('fInterIndex'), I('fResistIndex'), I('fPushbackIndex'), I('fUngovIndex')
        iFsum, iK = I('fForeignSumIndex'), I('kappaIndex')
        iO, iL, iF, iU, iB, iNB = I('ownerSideShare'), I('localSideShare'), I('foreignSideShare'), I('ungovSideShare'), I('bestClass'), I('nowBestClass')
        iR2 = I('nowRule2Share'); iTop = I('foreignTopKey')
        for line in f:
            c = line.rstrip('\r\n').split('\t')
            n_rows += 1
            fs, fi, fr, a, fu = fl(c[iFS]), fl(c[iFI]), fl(c[iFR]), fl(c[iA]), fl(c[iFU])
            fsum, kap = fl(c[iFsum]), fl(c[iK])
            x = fl(c[iR2]); x = x if x > 0 else 0.0
            N = fl(c[iN]); N = N if N > 0 else 0.0
            if a != 0: tot['A_nonzero'] += 1
            sb[c[iB]] += 1; nb[c[iNB]] += 1
            r = classes(fs, fi, fr, fsum, fu, kap)
            so, sf = fl(c[iO]), fl(c[iF])
            if r is not None and not math.isnan(so):
                n_cmp += 1
                if abs(r[0] - so) <= 1e-9 and abs(r[2] - sf) <= 1e-9 and NAMES[r[4]] == c[iB]:
                    n_match += 1
            # 影の写像 T(s)：今の外国の取り分の帯ごとの影の外国の割合
            if not math.isnan(sf):
                b = binstat[binof(x)]; b[0] += 1; b[1] += sf; b[2] += sf - x; b[3] += (1 if sf > x + 1e-12 else 0)
            if c[iB] == 'foreign':
                shadow_foreign_best[(seed, c[inat], c[ireg])] += 1
                shadow_foreign_now.append(x)
            if r is not None and not math.isnan(so) and not (abs(r[0] - so) <= 1e-9 and abs(r[2] - sf) <= 1e-9 and NAMES[r[4]] == c[iB]) and len(mism) < 12:
                mism.append((seed, c[iq], c[inat], c[ireg], c[iB], NAMES[r[4]], so, r[0], sf, r[2]))
            if x > 0:
                n_x += 1
                if r is not None:
                    Amax = N * x
                    ub_ratio.append(Amax / fsum if fsum > 0 else float('inf'))
                    for v, (fs2, fr2) in (('V4', (fs + Amax, fr)), ('V5', (fs, fr + Amax))):
                        r2 = classes(fs2, fi, fr2, fsum, fu, kap)
                        if r2 is not None:
                            flips[v][(NAMES[r[4]], NAMES[r2[4]])] += 1
                    xbest[NAMES[r[4]]] += 1
            key = (c[iq], c[inat], c[ireg])
            if key in sec:
                n_join += 1
                m, core = sec[key]
                if x > 0 and r is not None and not math.isnan(m) and not math.isnan(core):
                    n_xjoin += 1
                    core = min(1.0, max(0.0, core)); m = m if m > 0 else 0.0
                    A = N * m * x
                    if fsum > 0: ratio_rows.append(A / fsum)
                    else: ratio_rows.append(float('inf') if A > 0 else 0.0)
                    vs = {'V1': (fs + core * A, fr + (1 - core) * A),
                          'V2': (fs, fr + (1 - core) * A),
                          'V3': (fs + core * A, fr)}
                    for v, (fs2, fr2) in vs.items():
                        r2 = classes(fs2, fi, fr2, fsum, fu, kap)
                        if r2 is not None:
                            flips[v][(NAMES[r[4]], NAMES[r2[4]])] += 1
    tot['rows'] += n_rows; tot['cmp'] += n_cmp; tot['match'] += n_match; tot['join'] += n_join; tot['x'] += n_x; tot['xjoin'] += n_xjoin
    per_seed_lines.append(f'seed {seed}: rows {n_rows} 突き合わせ {n_match}/{n_cmp} secession_s と結合 {n_join} 外国の取り分＞0 の行 {n_x}（結合 {n_xjoin}） 影の最大 {dict(sb)} 今の最大 {dict(nb)}')
    # --- rponset_s の対の行：エンジンの外国の取り分の動き ---
    p = os.path.join(raw, f'rponset_s{seed}.tsv')
    pmax = defaultdict(float)
    with open(p, encoding='utf-8', newline='') as f:
        h = f.readline().rstrip('\r\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            c = line.rstrip('\r\n').split('\t')
            if c[ix['rowKind']] != 'pair': continue
            rise = fl(c[ix['riseS']]); se = fl(c[ix['sEnd']]); cl = c[ix['classHead']]; war = c[ix['atWarFlag']]
            k2 = (seed, c[2], c[3], c[ix['foreignKey']])
            if se > pmax[k2]: pmax[k2] = se
            if rise > 1e-12:
                pair_rise[(cl, war)] += 1
                pair_rise_size[round(rise, 2)] += 1
                pair_odd[(c[2], c[ix['foreignKey']])] += 1
    pair_max.extend(pmax.values())

def q(vals, ps):
    v = sorted(vals); n = len(v)
    return [v[min(n - 1, int(p * n))] if n else float('nan') for p in ps]

with open(os.path.join(out, f'summary_s{s0}-{s1}.txt'), 'w', encoding='utf-8', newline='\n') as o:
    w = lambda s='': o.write(s + '\n')
    w(f'# r1211 種 {s0}〜{s1}・生 {raw}')
    for l in per_seed_lines: w(l)
    w(f'合計 rows {int(tot["rows"])} 突き合わせ一致 {int(tot["match"])}/{int(tot["cmp"])} A≠0 の行 {int(tot["A_nonzero"])}')
    w('## 影の写像 T(s)：今の外国の取り分 s の帯 → 行数・影の外国の割合の平均・(影−今) の平均・影＞今 の割合')
    for _, _, nm in BINS:
        b = binstat[nm]
        if b[0]: w(f'{nm}\t{b[0]}\t{b[1]/b[0]:.6g}\t{b[2]/b[0]:.6g}\t{b[3]/b[0]:.4f}')
    w(f'## 影で外国が最大の行 {len(shadow_foreign_now)}・州 {len(shadow_foreign_best)}・その行の今の s の分位 10/50/90% {q(shadow_foreign_now,[.1,.5,.9])}')
    for k, v in shadow_foreign_best.most_common(15): w(f'  {k}\t{v}')
    w(f'## 反実仮想 A＝N×闘争度×X（X＝今の外国の取り分の和・同じ主流の文化の除外は無し）・結合できた X＞0 の行 {int(tot["xjoin"])}')
    w(f'  A÷ΣF_f の分位 10/50/90/99% {q(ratio_rows,[.1,.5,.9,.99])}')
    w(f'  上限 N×X÷ΣF_f の分位 10/50/90/99% {q(ub_ratio,[.1,.5,.9,.99])}・X＞0 の行の影の最大 {dict(xbest)}')
    w('  V1＝中核の分を F_S・残りを F_R／V2＝中核でない分だけ F_R／V3＝中核の分だけ F_S（V1〜V3 は secession_s と結合できた行）／V4＝上限 N×X を全て F_S／V5＝上限 N×X を全て F_R（全ての X＞0 の行）')
    for v, cnt in flips.items():
        ch = {k: n for k, n in cnt.items() if k[0] != k[1]}
        w(f'  {v} 最大の区分の変わり {sum(ch.values())}/{sum(cnt.values())} 内訳 {ch}')
    w('## 突き合わせの不一致の行（最大 12）')
    for m in mism: w(f'  {m}')
    w('## エンジンの外国の取り分の対（rponset_s の pair 行）')
    w(f'  増えた行（区分の頭, 交戦の印）{dict(pair_rise)}')
    w(f'  増えた量の丸め {dict(pair_rise_size.most_common(8))}')
    w(f'  対の数 {len(pair_max)}・最大 s の分位 50/90/100% {q(pair_max,[.5,.9,.999999])}')
    w(f'  付与の (持ち主の国, 外国) 上位 {pair_odd.most_common(12)}')
