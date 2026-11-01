# (727) 合流：m719/prereg_base.py の写し＋§4 rdh（far_share）。prereg_base.sh の読み手の出力を 1 枚の表へ（読むだけ・値は読み手の出力の写し・幅＝基線・N1・N2 の最大−最小・a11 は別の種なので幅に入れない）。
import sys, re, os, statistics
sys.stdout.reconfigure(encoding='utf-8')
O = sys.argv[1]
ARMS = ['base_ins', 'n1', 'n2', 'a11']
LAB = {'base_ins': '基線', 'n1': 'N1', 'n2': 'N2', 'a11': 'a11'}
def rd(p):
    return open(p, encoding='utf-8').read() if os.path.exists(p) else ''
def fm(x, d=4):
    return 'NA' if x is None else (f'{x:.{d}f}' if isinstance(x, float) else str(x))
def row(nm, v, d=4):
    w3 = [x for x in v[:3] if x is not None]
    w = (max(w3) - min(w3)) if len(w3) == 3 else None
    print(f'| {nm} | ' + ' | '.join(fm(x, d) for x in v[:3]) + f' | {fm(w, d)} | {fm(v[3], d)} |')
print('# (727) 波51 の事前登録の材料（読むだけ・読み手 scripts/instruments/m727/prereg_base.sh）。生＝D:/PoB_work/keep_727（(727) の最後の恒等の木の計器つき 10 種＝基線 m702 と同じ軌道・prds_s の末尾 6 列つき・n1／n2＝ヌル腕 SupplySlope 0.50000001／0.49999999・a11＝種 11〜20）。')
print('# 幅＝基線・N1・N2 の最大−最小（a11 は別の種なので幅に入れない＝並べるだけ）。値の定義は各読み手の頭（cesd/cess_read.py・r1/judge_gate.py・hol/an_h2.py・urb3/base.py）。')
print('# U4 の bind と影の N は世界が m702 と同じ＝ measurements/m711_20261031_prereg_base.txt §2・§3 のまま（取り直していない）。')
print()
# ---- 1. cesd
print('## 1. cesd §4-2 の門の量（cess_s・講和で移った州・10 種の和）')
C = {}
for a in ARMS:
    t = rd(os.path.join(O, f'cess_{a}.txt')); d = {}; hdr = None
    for ln in t.splitlines():
        p = ln.split('\t')
        if p[0] == 'key': hdr = p; continue
        if hdr and len(p) == len(hdr):
            d[p[0]] = dict(zip(hdr[1:], p[1:]))
        elif len(p) == 2:
            d[p[0]] = {'all': p[1]}
    C[a] = d
def g(a, k, col):
    try:
        v = C[a][k][col]; return None if v in ('-', '') else float(v)
    except KeyError: return None
def ratio(a, k1, k2, col):
    x, y = g(a, k1, col), g(a, k2, col)
    return None if x is None or not y else x / y
print('| 量 | 基線 | N1 | N2 | 幅 | a11 |')
print('|---|---|---|---|---|---|')
row('講和の回数', [g(a, 'peaces', 'all') for a in ARMS], 0)
row('移った州の行（全経路）', [g(a, 'n', 'all') for a in ARMS], 0)
row('本国格の講和移転（経路3・4）', [g(a, 'n', 'home') for a in ARMS], 0)
row('本国格のうち勝者と陸で隣り合わない（notAdj）', [g(a, 'notAdj', 'home') for a in ARMS], 0)
row('↑の割合', [ratio(a, 'notAdj', 'n', 'home') for a in ARMS])
row('本国格・同じ陸塊で隣り合わない（限界 b）', [g(a, 'sameLandmass_notAdj', 'home') for a in ARMS], 0)
row('本国格・隣り合わず港でもない（G2 の量）', [g(a, 'notAdj_inland', 'home') for a in ARMS], 0)
row('案 E2 で残る本国格（keepE2）÷本国格', [ratio(a, 'keepE2', 'n', 'home') for a in ARMS])
row('案 E2＋予算 1 本で残る本国格（keepE2Budget）÷本国格', [ratio(a, 'keepE2Budget', 'n', 'home') for a in ARMS])
row('植民地格の移転（経路3・4）', [g(a, 'n', 'cede_col') for a in ARMS], 0)
row('植民地格の遠く弱い移転（farweak＝3000km 以上・勝者の世界の艦の比 0.5 未満）', [g(a, 'farweak', 'cede_col') for a in ARMS], 0)
row('全経路の遠く弱い移転（farweak）', [g(a, 'farweak', 'all') for a in ARMS], 0)
row('経路1 の e2Ok（計器の列）', [g(a, 'e2Ok', 'r1') for a in ARMS], 0)
row('経路1 の行', [g(a, 'n', 'r1') for a in ARMS], 0)
print()
# ---- 2. r1
print('## 2. r1 の再測の材料：開幕の 9 国の工場労働者の割合と world.json の差（|warm−open|・種ごとの 9 国の中央値）と空洞化の件数')
R = {}
for a in ARMS:
    meds = []; mxs = []
    for ln in rd(os.path.join(O, f'r1gate_{a}.txt')).splitlines():
        p = ln.split('\t')
        if len(p) == 5 and p[0].isdigit(): meds.append(float(p[1])); mxs.append(float(p[2]))
    R[a] = (meds, mxs)
H = {}
for a in ARMS:
    for tag, f in (('old', f'hol_{a}.txt'), ('rule', f'holrule_{a}.txt')):
        t = rd(os.path.join(O, f))
        m10 = re.search(r'## ピークの1/10 未満: (\d+) 件', t); m4 = re.search(r'## ピークの1/4 未満: (\d+) 件', t)
        H[(a, tag, 10)] = int(m10.group(1)) if m10 else None; H[(a, tag, 4)] = int(m4.group(1)) if m4 else None
print('| 量 | 基線 | N1 | N2 | 幅 | a11 |')
print('|---|---|---|---|---|---|')
row('工場労働者の差の種の中央値の最小', [min(R[a][0]) if R[a][0] else None for a in ARMS], 5)
row('工場労働者の差の種の中央値の中央', [statistics.median(R[a][0]) if R[a][0] else None for a in ARMS], 5)
row('工場労働者の差の種の中央値の最大', [max(R[a][0]) if R[a][0] else None for a in ARMS], 5)
row('工場労働者の差の 9 国の最大（種の最大）', [max(R[a][1]) if R[a][1] else None for a in ARMS], 5)
row('空洞化 1/10 未満（旧の名簿 4 州）', [H[(a, 'old', 10)] for a in ARMS], 0)
row('空洞化 1/4 未満（旧の名簿 4 州）', [H[(a, 'old', 4)] for a in ARMS], 0)
row('空洞化 1/10 未満（規則 A2・world.json の popK・波51 から止める線）', [H[(a, 'rule', 10)] for a in ARMS], 0)
row('空洞化 1/4 未満（規則 A2・記録）', [H[(a, 'rule', 4)] for a in ARMS], 0)
print()
# ---- 3. urbd
print('## 3. urbd（都市化の目標の式）：W1abs（(713) の門の定義）・W2・P・往復の戻す割合・向きの反転')
V = {}
for f, labs in (('urb3_base_3arms.txt', None), ('urb3_base_a11.txt', None)):
    cur = None
    for ln in rd(os.path.join(O, f)).splitlines():
        m = re.match(r'## (\S+)\s', ln)
        if m: cur = m.group(1); V.setdefault(cur, {}); continue
        if ln.startswith('# '): cur = None
        if cur is None: continue
        for key, pat in (('W2', r'^W2（'), ('W1', r'^W1（'), ('W1abs', r'^W1abs（'), ('P', r'^P（'), ('flip', r'^flowFlip（'), ('ret_ux', r'^戻す割合（urx_s'), ('ret_rt', r'^戻す割合（urxrt_s')):
            if re.match(pat, ln): V[cur][key] = float(ln.split('\t')[1])
print('| 量 | 基線 | N1 | N2 | 幅 | a11 |')
print('|---|---|---|---|---|---|')
for k, nm in (('W1abs', 'W1abs＝Σ wAbsGsym÷Σ sumTotal の窓の中央値'), ('W1', 'W1＝|Σtotal×g÷Σtotal|（旧の定義・記録）'), ('W2', 'W2＝名目の都市÷農村の窓の中央値'), ('P', 'P＝g が閉じたときの名目の比の見込み'), ('ret_ux', '戻す割合（urx_s）'), ('ret_rt', '戻す割合（urxrt_s）'), ('flip', '向きが前の月と逆の州×月の割合')):
    row(nm, [V.get(x, {}).get(k) for x in ('base', 'n1', 'n2', 'a11')])
print()
# ---- 4. rdh
print('## 4. rdh の結論が名指した観測量：州の間の移民（国内・migration_s）のうち大圏 3,000 km 超の割合と量（rdh/far_share.py・種の中央値）')
F = {}
for a in ARMS:
    rows = []
    for ln in rd(os.path.join(O, f'far_{a}.txt')).splitlines():
        p = ln.split('	')
        if len(p) == 4 and p[0].isdigit():
            try: rows.append((float(p[1]), float(p[2]), float(p[3])))
            except ValueError: pass
    F[a] = rows
def fmed(a, f):
    v = [f(r) for r in F[a]]
    return statistics.median(v) if v else None
def fmin(a, f):
    v = [f(r) for r in F[a]]
    return min(v) if v else None
print('| 量 | 基線 | N1 | N2 | 幅 | a11 |')
print('|---|---|---|---|---|---|')
row('国内の州の間の移民の総量（千人・種の中央値）', [fmed(a, lambda r: r[0]) for a in ARMS], 0)
row('3,000 km 超の割合（種の中央値）', [fmed(a, lambda r: r[1]) for a in ARMS])
row('3,000 km 超の割合（種の最小）', [fmin(a, lambda r: r[1]) for a in ARMS])
row('3,000 km 超の量（千人・種の中央値）', [fmed(a, lambda r: r[0] * r[1]) for a in ARMS], 0)
row('3,000 km 超の量（千人・種の最小）', [fmin(a, lambda r: r[0] * r[1]) for a in ARMS], 0)
row('7,000 km 超の割合（種の中央値）', [fmed(a, lambda r: r[2]) for a in ARMS])
print()
print('# ⚠ cesd の〈本国格〉は cess_read.py の群 home＝経路3・4 の Core・Integrated。予算 1 本の効きは e2AltAffordCount を払った後の予算と比べる＝低い側に偏りうる（設計書 §7-2）。規則 A2 は hol3（(722)）で world.json の popK で判定する形に直り、波51 から止める線（この表の判定は旧と新の両方を並べる）。rdh の距離は大圏（座標だけ・海越えを区別しない）。')
