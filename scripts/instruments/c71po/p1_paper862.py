# -*- coding: utf-8 -*-
# (862) c71po が (856) の p1_paper856.py を写した（中身は同じ）。←(856) c70po が (850) c69po の p1_paper850.py を写した（中身は同じ）。元：(850) c69po 読むだけの台本：州ごとの治安 P1-1 の紙と門の物差し（事前登録 measurements/m850_20261105_prereg_po.txt の [2]・[3]）。
# 入力＝計器つきの生（secession_s・rule_s・種 1〜10）。エンジンを呼ばない・世界へ書かない。出力＝標準出力（呼び手が UTF-8 のファイルへ書く）。
#   python -X utf8 -I p1_paper850.py [--base] [--new] <名前>=<ins フォルダ> ...
#   各腕：S（[2] の統計＝m_r の上位 1/4 − 下位 1/4 の ruleUngovShare）・C（[3] G2b の持続した近似の対照の母集合の ruleUngovShare の平均）・G3（国の popK 加重の ruleUngovShare の平均）。
#   --base を付けた腕（基線）だけ：瞬間の差の桁・紙の予言 ΔS_pred（近似 a・b）・遅れだけの ΔS（突然変異 M1）・目標の差の国の平均・対照の件数。
#   --new を付けた腕（secession_s に (850) の末尾の列がある木）だけ：T（tNew−tOld の上位−下位）・G0 点検・G2a・t_eng の読みの確かめ。
# 限界（自己申告）：
#   ①secession_s は首都と 1 州の国を持たない＝S と C は首都を除く州。紙の m̄ は近似（a＝首都を除く crowdK 加重・b＝首都を rule_s の四半期の末の militancyRegionIndex と popK×(国の Σcrowd÷Σpop) で足す）。
#   ②ruleUngovShare は四半期の末の値＝Rule の書き手は CheckSecession の 1 箇所だけ（grep 済み）なので CheckSecession の後の値と同じ。州が国を移った四半期は突き合わせで落ちる。
#   ③挙げた国は毎四半期・他の国は年末の行＝持続（40 四半期）は行の間が 4 四半期を超えたら切る。
import sys, os, math, statistics as st
sys.stdout.reconfigure(encoding='utf-8')
ORDER_PER_POP = 0.8   # Coefficients.CrimeOrderPerPop の既定
SPEED = 0.025         # Coefficients.DominanceSpeed の既定
PERSIST = 40
TOL = 1e-8          # G0・G2a の許容（事前登録 [10] の訂正＝G10 の 10 桁の丸めが約 4e-9 乗る）
SEEDS = range(1, 11)

def c01(x): return 0.0 if not (x > 0) else (1.0 if x > 1 else x)
def fl(s):
    try: return float(s)
    except ValueError: return float('nan')
def q(vals, p):
    if not vals: return float('nan')
    s = sorted(vals); i = min(len(s) - 1, max(0, int(round(p * (len(s) - 1))))); return s[i]
def fmt(x): return 'nan' if x != x else ('%.6g' % x)
def line(name, v):
    print('  %s  件 %d  p10 %s  中央 %s  p90 %s  平均 %s' % (name, len(v), fmt(q(v, .1)), fmt(q(v, .5)), fmt(q(v, .9)), fmt(st.mean(v) if v else float('nan'))))

def read(path):
    with open(path, 'rb') as f:
        head = f.readline().decode('utf-8').rstrip('\r\n').split('\t')
        ix = {c: i for i, c in enumerate(head)}
        rows = [raw.decode('utf-8').rstrip('\r\n').split('\t') for raw in f]
    return ix, rows

def qdiff(rows, key, val):
    # rows: list of dict-like tuples; 州を key の昇順（同じ値は州の名）で並べ上位 k − 下位 k の val の平均
    srt = sorted(rows, key=lambda r: (key(r), r['region']))
    k = max(1, len(srt) // 4)
    return st.mean(val(r) for r in srt[-k:]) - st.mean(val(r) for r in srt[:k])

args = [a for a in sys.argv[1:] if not a.startswith('--')]
flags = [a for a in sys.argv[1:] if a.startswith('--')]
summary = {}
for spec in args:
    name, d = spec.split('=', 1)
    base = '--base' in flags and spec == args[0]
    new = '--new' in flags
    Ds, Cs, G3s = [], [], []
    # 基線だけ
    inst = {'1e-6': 0, '1e-3': 0, '1e-1': 0, 'n': 0, 'max': 0.0}
    dpa, dpb, dlag, dtu_nat = [], [], [], []
    ctrl_both = ctrl_now_only = 0
    # 新しい列
    Ts = []; g0_n = g0_ok = g0_old_ok = 0; g2a_n = g2a_ok = 0; foreign_skip = 0; own_mr_ok = own_mr_n = 0
    has_new = None
    for s in SEEDS:
        sp = os.path.join(d, 'secession_s%d.tsv' % s); rp = os.path.join(d, 'rule_s%d.tsv' % s)
        if not (os.path.exists(sp) and os.path.exists(rp)): print('欠け', name, '種', s); continue
        rix, rrows = read(rp); six, srows = read(sp)
        if has_new is None: has_new = 'ungovTargetNewShare' in six
        rule = {}
        capm = {}
        natpop = {}
        for a in rrows:
            k = (a[rix['quarter']], a[rix['nation']], a[rix['region']])
            pop = fl(a[rix['popK']]); ung = fl(a[rix['ruleUngovShare']])
            rule[k] = (pop, ung, fl(a[rix['reachShare']]), fl(a[rix['publicOrderIndex']]),
                       fl(a[rix['militancyRegionIndex']]) if 'militancyRegionIndex' in rix else float('nan'), fl(a[rix['ruleForeignShare']]))
            nk = (a[rix['quarter']], a[rix['nation']])
            t = natpop.setdefault(nk, [0.0, 0.0]); t[0] += pop * ung; t[1] += pop
            if fl(a[rix['reachShare']]) == 1.0 and fl(a[rix['capitalDistanceKm']]) == 0.0:
                capm[nk] = (pop, fl(a[rix['militancyRegionIndex']]) if 'militancyRegionIndex' in rix else float('nan'))
        for nk, (num, den) in natpop.items():
            if den > 0: G3s.append(num / den)
        groups = {}
        for a in srows:
            k = (a[six['quarter']], a[six['nation']], a[six['region']])
            if k not in rule: continue
            pop, ung, rch, po, milEnd, fsh = rule[k]
            r = {'region': a[six['region']], 'q': int(a[six['quarter']]), 'mil': fl(a[six['militancyMeanIndex']]), 'crowd': fl(a[six['crowdK']]),
                 'pop': pop, 'ung': ung, 'rch': rch, 'po': po, 'milEnd': milEnd, 'fsh': fsh,
                 'R': fl(a[six['coerceIndex']]) / (fl(a[six['totalPopK']]) * ORDER_PER_POP) if fl(a[six['totalPopK']]) > 0 else float('nan')}
            if has_new:
                for c in ('orderRawNowIndex', 'orderOldIndex', 'militancyNationMeanIndex', 'militancyRegionOwnIndex', 'orderRegionNowIndex', 'reachOwnShare',
                          'ungovTargetOldShare', 'ungovTargetNewShare', 'ruleUngovBeforeShare', 'ruleForeignBeforeShare'):
                    r[c] = fl(a[six[c]])
            groups.setdefault((a[six['quarter']], a[six['nation']]), []).append(r)
        streak = {}
        for (qt, nat) in sorted(groups, key=lambda x: (int(x[0]), x[1])):
            rows = groups[(qt, nat)]
            wsum = sum(r['crowd'] for r in rows)
            mbar_a = sum(r['crowd'] * r['mil'] for r in rows) / wsum if wsum > 0 else 0.0
            cp = capm.get((qt, nat))
            psum = sum(r['pop'] for r in rows)
            if cp is not None and psum > 0 and cp[1] == cp[1]:
                ccrowd = cp[0] * (wsum / psum)
                mbar_b = (sum(r['crowd'] * r['mil'] for r in rows) + ccrowd * cp[1]) / (wsum + ccrowd) if (wsum + ccrowd) > 0 else 0.0
            else:
                mbar_b = mbar_a
            if len(rows) >= 4:
                Ds.append(qdiff(rows, lambda r: r['mil'], lambda r: r['ung']))
            # G2b：近似の対照の母集合と持続
            for r in rows:
                key = (nat, r['region'])
                inq = r['po'] >= 1 and r['R'] >= 1 and r['mil'] > 0 and r['R'] * mbar_a / r['mil'] >= 1
                prev = streak.get(key)
                if inq:
                    if prev is None or prev[1] is None or r['q'] - prev[0] > 4: streak[key] = (r['q'], r['q'])
                    else: streak[key] = (r['q'], prev[1])
                    if r['q'] - streak[key][1] >= PERSIST: Cs.append(r['ung'])
                else:
                    streak[key] = (r['q'], None)
            if base:
                for r in rows:
                    me = r['milEnd']; mi = r['mil']
                    if me == me and mi == mi:
                        inst['n'] += 1
                        rel = abs(me - mi) / max(abs(mi), 1e-300) if mi != 0 else (0.0 if me == 0 else math.inf)
                        inst['max'] = max(inst['max'], rel)
                        for tol in ('1e-6', '1e-3', '1e-1'):
                            if rel > float(tol): inst[tol] += 1
                R = rows[0]['R']
                if R == R:
                    for r in rows:
                        o_old = c01(r['po'])
                        for tag, mb in (('a', mbar_a), ('b', mbar_b)):
                            onew = 1.0 if not (r['mil'] > 0) else c01(R * mb / r['mil'])
                            r['dt' + tag] = (o_old - onew) * (1 - r['rch'])
                        r['dlag'] = (o_old - c01(R)) * (1 - r['rch'])
                        if r['mil'] > 0 and R * mbar_b / r['mil'] >= 1:
                            if r['po'] >= 1 and R >= 1: ctrl_both += 1
                            elif R >= 1: ctrl_now_only += 1
                    pw = sum(r['pop'] for r in rows)
                    if pw > 0: dtu_nat.append(sum(r['pop'] * r['dtb'] for r in rows) / pw)
                    if len(rows) >= 4:
                        dpa.append(qdiff(rows, lambda r: r['mil'], lambda r: r['dta']))
                        dpb.append(qdiff(rows, lambda r: r['mil'], lambda r: r['dtb']))
                        dlag.append(qdiff(rows, lambda r: r['mil'], lambda r: r['dlag']))
            if new and has_new:
                if len(rows) >= 4:
                    Ts.append(qdiff(rows, lambda r: r['mil'], lambda r: r['ungovTargetNewShare'] - r['ungovTargetOldShare']))
                for r in rows:
                    mo, mi = r['militancyRegionOwnIndex'], r['mil']
                    own_mr_n += 1
                    if mo == mi or abs(mo - mi) <= 1e-12 * max(1.0, abs(mi)): own_mr_ok += 1
                    if not (r['ruleUngovBeforeShare'] == r['ruleUngovBeforeShare']): continue   # Rule の長さが 4 でない州
                    if r['ruleForeignBeforeShare'] > 0 or r['fsh'] > 0: foreign_skip += 1; continue
                    t_eng = (r['ung'] - r['ruleUngovBeforeShare'] * (1 - SPEED)) / SPEED
                    g0_n += 1
                    if abs(t_eng - r['ungovTargetNewShare']) <= TOL: g0_ok += 1
                    if abs(t_eng - r['ungovTargetOldShare']) <= TOL: g0_old_ok += 1
                    if r['orderOldIndex'] >= 1 and r['orderRawNowIndex'] >= 1 and r['orderRegionNowIndex'] >= 1 and r['militancyRegionOwnIndex'] > 0:
                        g2a_n += 1
                        if abs(t_eng - r['ungovTargetOldShare']) <= TOL: g2a_ok += 1
    S = st.mean(Ds) if Ds else float('nan'); C = st.mean(Cs) if Cs else float('nan'); G3 = st.mean(G3s) if G3s else float('nan')
    summary[name] = (S, C, G3)
    print('== 腕', name, d)
    print('  S（上位1/4−下位1/4 の ruleUngovShare・国×四半期 %d）%s  C（持続の対照 %d 行）%s  G3（国の popK 加重の ruleUngovShare・%d）%s' % (len(Ds), fmt(S), len(Cs), fmt(C), len(G3s), fmt(G3)))
    if base:
        print('  [0] 瞬間の差：rule_s の militancyRegionIndex 対 secession_s の militancyMeanIndex  突き合わせ %d 行  相対 1e-6 超 %d  1e-3 超 %d  1e-1 超 %d  最大の相対差 %s' % (inst['n'], inst['1e-6'], inst['1e-3'], inst['1e-1'], fmt(inst['max'])))
        print('  [2] 紙の予言 ΔS_pred（同じ瞬間の R・m̄ 近似 a）%s  （近似 b）%s  ／遅れだけ（突然変異 M1：Clamp01(R)）の ΔS %s  国×四半期 %d' % (fmt(st.mean(dpa)), fmt(st.mean(dpb)), fmt(st.mean(dlag)), len(dpb)))
        line('ΔS_pred の分布（近似 b）', dpb)
        line('目標の差 tNew−tOld の国の popK 加重の平均（近似 b）', dtu_nat)
        print('  対照の件数（近似 b・m_r>0 かつ R m̄/m_r≥1）：遅れた R と今の R の両方が 1 以上 %d  今の R だけ 1 以上 %d' % (ctrl_both, ctrl_now_only))
    if new:
        if not has_new: print('  （新しい列が無い）')
        else:
            print('  T（tNew−tOld の上位−下位・国×四半期 %d）%s' % (len(Ts), fmt(st.mean(Ts) if Ts else float('nan'))))
            print('  m_r の自前の数と militancyMeanIndex の一致 %d / %d' % (own_mr_ok, own_mr_n))
            print('  G0 点検 t_eng＝tNew（1e-8）%d / %d（割合 %s）  t_eng＝tOld %d / %d  外国の区分で除いた行 %d' % (g0_ok, g0_n, fmt(g0_ok / g0_n if g0_n else float('nan')), g0_old_ok, g0_n, foreign_skip))
            print('  G2a P_ctrl %d 行  t_eng＝tOld（1e-8）%d（割合 %s）' % (g2a_n, g2a_ok, fmt(g2a_ok / g2a_n if g2a_n else float('nan'))))
if args and '--base' in flags:
    b = args[0].split('=', 1)[0]
    Sb, Cb, Gb = summary[b]
    print('== 基線との差（基線＝%s）' % b)
    for n, (S, C, G3) in summary.items():
        if n == b: continue
        print('  %s  ΔS %s  ΔC %s  ΔG3 %s' % (n, fmt(S - Sb), fmt(C - Cb), fmt(G3 - Gb)))
