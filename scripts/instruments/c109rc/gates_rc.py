# (1123) c109rc 段 C′ の門（事前登録 measurements/m1123_20261114_prereg_rc.txt §2）。(1117) の gates_rc.py を写し、計器の見出しの直しに合わせて写し替えを外し、
# C3 の幅を引数に・C8c／C8d を実際の nowRule0Share の変化へ・C8e（州が 1 つの国の寄せ）・G1（交戦していない国の中核の州の外国の取り分 ≧0.25）・G2（交戦中の首都の t0Eng）を足した。表を読むだけ・エンジンを呼ばない。
# 使い方: python -X utf8 -I gates_rc.py <腕の生のフォルダ> <c5_series.tsv（ヌル腕の系列）> [<基準の生> <N1 の生> <N2 の生>] [--seeds 1-10] [--c3 lo-hi] [--g1max N]
#        python -X utf8 -I gates_rc.py --g1only <生のフォルダ> [--seeds 1-10]   （旗 0 の生で G1 の数だけ）
#   〈実際の側〉＝エンジンの列（*Eng*）・期待の側＝計器が自分で数えた〈D〉の列。許容差は相対 1e-9（絶対 1e-12 の床）。
#   C8b のヌル腕の値は 3 本の生を渡したときだけ数える（rhoNoFreezeIndex と nowRule0Share）。
import sys, os, math, statistics as st, collections
sys.stdout.reconfigure(newline='\n')
VALOPT = ('--seeds', '--c3', '--g1max')
args = [a for i, a in enumerate(sys.argv[1:], 1) if not a.startswith('--') and sys.argv[i - 1] not in VALOPT]
seeds = range(1, 11); c3lo = c3hi = None; g1max = None
for i, a in enumerate(sys.argv):
    if a == '--seeds':
        lo, hi = sys.argv[i + 1].split('-'); seeds = range(int(lo), int(hi) + 1)
    if a == '--c3':
        lo, hi = sys.argv[i + 1].split('-'); c3lo, c3hi = int(lo), int(hi)
    if a == '--g1max': g1max = int(sys.argv[i + 1])
nulls = args[2:5]
TOL = 1e-9
def fl(x):
    try: return float(x)
    except ValueError: return float('nan')
def neq(a, b):
    if math.isnan(a) or math.isnan(b): return not (math.isnan(a) and math.isnan(b))
    return abs(a - b) > TOL * max(abs(a), abs(b)) + 1e-12
def finite(*v): return all(not (math.isnan(x) or math.isinf(x)) for x in v)

def g1_count(folder):
    """G1：交戦していない持ち主（wNationIndex＝0 かつ wEngageNationIndex＝0）の中核の州（tier Core）で nowRule2Share ≧0.25 の州の数。
    種×四半期ごとに数え、(種ごとの最大の最大, 種ごとの最大の和, 種の数) を返す。旗 0 の生でも読める。"""
    per = []
    for s in seeds:
        p = os.path.join(folder, f'rulepower_s{s}.tsv')
        if not os.path.exists(p): continue
        byq = collections.Counter()
        with open(p, encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); ix = {k: j for j, k in enumerate(h)}
            iq, it, iw, ie, i2 = ix['quarter'], ix['tier'], ix['wNationIndex'], ix['wEngageNationIndex'], ix['nowRule2Share']
            for line in f:
                c = line.rstrip('\n').split('\t')
                if c[it] != 'Core' or fl(c[iw]) != 0 or fl(c[ie]) != 0: continue
                if fl(c[i2]) >= 0.25: byq[int(c[iq])] += 1
        per.append(max(byq.values()) if byq else 0)
    return (max(per) if per else 0, sum(per), len(per))

if '--g1only' in sys.argv:
    print('G1', args[0], g1_count(args[0])); sys.exit(0)
arm, c5p = args[0], args[1]

def c8b_value(folder, rhocol):
    vals = []
    for s in seeds:
        p = os.path.join(folder, f'rulepower_s{s}.tsv')
        if not os.path.exists(p): continue
        first = {}; A = []; B = []
        with open(p, encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); ix = {k: j for j, k in enumerate(h)}
            if rhocol not in ix: return None
            iq, ir, irho, i0, ii0 = ix['quarter'], ix['region'], ix[rhocol], ix['nowRule0Share'], ix['ruleInit0Share']
            for line in f:
                c = line.rstrip('\n').split('\t')
                rho = fl(c[irho])
                if not finite(rho): continue
                reg = c[ir]
                if reg not in first: first[reg] = rho
                q = int(c[iq])
                if q < 40 or not (first[reg] > 0): continue
                ratio = rho / first[reg]; d = fl(c[i0]) - fl(c[ii0])
                if not finite(d): continue
                if ratio >= 1.5: A.append(d)
                elif 0.95 <= ratio <= 1.05: B.append(d)
        if A and B: vals.append(sum(A) / len(A) - sum(B) / len(B))
    return (st.median(vals), len(vals)) if vals else (float('nan'), 0)

out = []
def P(*a): out.append(' '.join(str(x) for x in a))

cnt = collections.Counter()
c3 = 0; c3_seen = 0
c8a_in = c8a_tot = c8a_over = 0
c5_q_differs = []
nat_series = collections.defaultdict(lambda: collections.defaultdict(list))   # q -> metric -> per seed values
c8c_vals, c8d_vals = [], []
c8e_in = c8e_tot = 0
g2_war, g2_peace = [], []   # 首都（国の最初の行）の t0Eng：交戦中（wNationIndex＞0）と平時（wNationIndex＝0 かつ wEngageNationIndex＝0）
for s in seeds:
    p = os.path.join(arm, f'rulepower_s{s}.tsv')
    if not os.path.exists(p): P('種', s, 'の rulepower_s が無い'); continue
    first_q = None; q0 = None; qdiff = set(); seen_reg = set()
    prev = {}   # region -> (quarter, nowRule0)
    natq = collections.defaultdict(dict)   # q -> nation -> [L, O, sum nowRule0, n]（(1123) C8c・C8d は実際の取り分）
    natrows = collections.Counter(); natc8 = collections.defaultdict(list); natcap = {}
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: j for j, k in enumerate(h)}
        need = ['bestClassEng', 'bestClassD', 'rhoEngIndex', 'rhoDIndex', 'rhoNewIndex', 'rhoNoFreezeIndex', 'qEngIndex', 'qNowIndex', 'fForeignEngIndex', 'fForeignVbIndex',
                'iEngShare', 'iDShare', 'iProbeShare', 't0EngShare', 't0DShare', 't1EngShare', 't1DShare', 't3EngShare', 't3DShare', 'nowRule0Share', 'oEngNationIndex', 'lNationIndex', 'wNationIndex', 'wEngageNationIndex']
        miss = [k for k in need if k not in ix]
        if miss: P('種', s, '列が無い', miss); continue
        g = {k: ix[k] for k in need}; iq, ir, inat = ix['quarter'], ix['region'], ix['nation']
        for line in f:
            c = line.rstrip('\n').split('\t')
            q = int(c[iq]); reg = c[ir]
            be, bd = c[g['bestClassEng']], c[g['bestClassD']]
            if be != 'none' or bd != 'none':
                cnt['C1_rows'] += 1
                if be != bd: cnt['C1_bad'] += 1
            if c[g['rhoNewIndex']] != c[g['rhoNoFreezeIndex']]: cnt['C2_pop'] += 1
            re_, rd = fl(c[g['rhoEngIndex']]), fl(c[g['rhoDIndex']])
            if neq(re_, rd): cnt['C2_bad'] += 1
            if be != 'none':
                if first_q is None: first_q = q
                if q == first_q:
                    c3_seen += 1
                    if be != 'owner': c3 += 1
            qe, qd = fl(c[g['qEngIndex']]), fl(c[g['qNowIndex']])
            if neq(qe, qd): cnt['C5_bad'] += 1
            if finite(qe):
                if q0 is None: q0 = qe
                if neq(qe, q0): qdiff.add(q)
            fe, fd = fl(c[g['fForeignEngIndex']]), fl(c[g['fForeignVbIndex']])
            if neq(fe, fd): cnt['C6_bad'] += 1
            if fd > 0: cnt['C6_pop'] += 1
            ie, idd, ip = fl(c[g['iEngShare']]), fl(c[g['iDShare']]), fl(c[g['iProbeShare']])
            solve_row = reg not in seen_reg; seen_reg.add(reg)
            if not solve_row and neq(idd, ip) and finite(idd, ip):
                cnt['C7_pop'] += 1
                if neq(ie, idd): cnt['C7_bad'] += 1
            elif neq(ie, idd): cnt['C7_bad_other'] += 1
            any10 = False
            for t in ('t0', 't1', 't3'):
                if neq(fl(c[g[t + 'EngShare']]), fl(c[g[t + 'DShare']])): any10 = True
            cnt['C10_rows'] += 1
            if any10: cnt['C10_bad'] += 1
            # C8a：前の四半期の nowRule0 から寄せが効いたか
            t0 = fl(c[g['t0EngShare']]); now0 = fl(c[g['nowRule0Share']])
            pv = prev.get(reg)
            if pv is not None and pv[0] == q - 1 and finite(t0, now0, pv[1]):
                gap = t0 - pv[1]
                if abs(gap) > 1e-3:
                    want = 0.025 * gap; got = now0 - pv[1]
                    c8a_tot += 1
                    okin = abs(got - want) <= 0.5 * abs(want)
                    if okin: c8a_in += 1
                    if (got / want if want != 0 else 0) > 2: c8a_over += 1
                    natc8[(q, c[inat])].append(okin)
            prev[reg] = (q, now0)
            nat = c[inat]; L = fl(c[g['lNationIndex']]); O = fl(c[g['oEngNationIndex']])
            natrows[(q, nat)] += 1
            if (q, nat) not in natcap: natcap[(q, nat)] = (t0, fl(c[g['wNationIndex']]), fl(c[g['wEngageNationIndex']]))
            e = natq[q].get(nat)
            if e is None: e = natq[q][nat] = [L, O, 0.0, 0]
            if finite(now0): e[2] += now0; e[3] += 1
    c5_q_differs.append(len(qdiff))
    # C8e：州が 1 つの国の行だけで C8a と同じ読み
    for key, lst in natc8.items():
        if natrows[key] == 1: c8e_tot += len(lst); c8e_in += sum(lst)
    # G2：首都の t0Eng（交戦中と平時）
    for key, (t0c, wn, we) in natcap.items():
        if not finite(t0c): continue
        if wn > 0: g2_war.append(t0c)
        elif wn == 0 and we == 0: g2_peace.append(t0c)
    # 国の系列：ΣO÷ΣL・O の世界の取り分の最大
    qs = sorted(natq)
    for q in qs:
        L = sum(v[0] for v in natq[q].values() if finite(v[0])); Os = [v[1] for v in natq[q].values() if finite(v[1])]
        if Os and L > 0:
            nat_series[q]['sumOsumL'].append(sum(Os) / L)
            so = sum(Os); nat_series[q]['maxOshare'].append(max(Os) / so if so > 0 else 0.0)
    # C8c・C8d：L が前の 4 四半期の平均の 0.8 倍以下（落ち）と 0.95〜1.05 倍（変わらない）の、国の平均 nowRule0Share の 4 四半期の変化（(1123) 実際の取り分）
    dropO0, stabO0, dropO1, stabO1 = [], [], [], []
    for qi, q in enumerate(qs):
        if qi < 4: continue
        for nat, v in natq[q].items():
            back = [natq[qs[qi - j]].get(nat) for j in range(1, 5)]
            if any(b is None for b in back) or v[3] == 0 or back[3][3] == 0: continue
            mL = sum(b[0] for b in back) / 4
            if not (mL > 0 and finite(v[0], v[1])): continue
            r = v[0] / mL; dt = v[2] / v[3] - back[3][2] / back[3][3]
            grp = (dropO0, stabO0) if v[1] == 0 else (dropO1, stabO1)
            if r <= 0.8: grp[0].append(dt)
            elif 0.95 <= r <= 1.05: grp[1].append(dt)
    if dropO0 and stabO0: c8c_vals.append(sum(dropO0) / len(dropO0) - sum(stabO0) / len(stabO0))
    if dropO1 and stabO1: c8d_vals.append(sum(dropO1) / len(dropO1) - sum(stabO1) / len(stabO1))

def gate(name, ok, detail): P(f'{name}\t{"緑" if ok else "赤"}\t{detail}')
P('# (1123) gates_rc.py', arm)
gate('C1 同点', cnt['C1_rows'] >= 100000 and cnt['C1_bad'] == 0, f"比べた行 {cnt['C1_rows']}（≧100000）・食い違い {cnt['C1_bad']}（＝0）")
gate('C2 凍結', cnt['C2_pop'] >= 10000 and cnt['C2_bad'] == 0, f"凍結で ρ が違う行 {cnt['C2_pop']}（≧10000）・rhoEng≠rhoD {cnt['C2_bad']}（＝0）")
if c3lo is None: P(f'C3 開幕	未	最初の表の四半期で bestClassEng≠owner {c3}・見た州 {c3_seen}（幅は短い走りの後の追記で書く）')
else: gate('C3 開幕', c3lo <= c3 <= c3hi, f"最初の表の四半期で bestClassEng≠owner {c3}（{c3lo}〜{c3hi}）・見た州 {c3_seen}")
gate('C5 q', cnt['C5_bad'] == 0 and c5_q_differs and min(c5_q_differs) >= 600, f"qEng≠qNow {cnt['C5_bad']}（＝0）・q が最初と違う四半期の種の最小 {min(c5_q_differs) if c5_q_differs else 'なし'}（≧600）")
gate('C6 海軍', cnt['C6_bad'] == 0 and cnt['C6_pop'] >= 1000, f"fForeignEng≠fForeignVb {cnt['C6_bad']}（＝0）・外国の力＞0 の行 {cnt['C6_pop']}（≧1000）")
gate('C7 i', cnt['C7_pop'] >= 100000 and cnt['C7_bad'] == 0, f"解く行の外で iD≠iProbe {cnt['C7_pop']}（≧100000）・iEng≠iD {cnt['C7_bad']}（＝0）・その他の食い違い {cnt['C7_bad_other']}（記録）")
gate('C10 目標の写し', cnt['C10_rows'] >= 100000 and cnt['C10_bad'] == 0, f"行 {cnt['C10_rows']}・t0/t1/t3 の Eng≠D {cnt['C10_bad']}（＝0）")
fr_in = c8a_in / c8a_tot if c8a_tot else float('nan'); fr_over = c8a_over / c8a_tot if c8a_tot else float('nan')
gate('C8a 寄せ', c8a_tot >= 10000 and fr_in >= 0.9 and fr_over <= 0.02, f"行 {c8a_tot}（≧10000）・±50% の内 {fr_in:.4g}（≧0.9）・2 倍を超える {fr_over:.4g}（≦0.02）")
# 系列の門（C5〈壊さない〉・C9′）
ser = collections.defaultdict(dict)
with open(c5p, encoding='utf-8') as f:
    h = f.readline().rstrip('\n').split('\t')
    for line in f:
        c = line.rstrip('\n').split('\t'); ser[int(c[0])] = {h[j]: fl(c[j]) for j in range(1, len(h))}
pairs = [('N1', 'N2'), ('M', 'N2'), ('M', 'N1')]
for k, name, thr in (('sumOsumL', 'C5 壊さない', 1), ('maxOshare', 'C9′ 壊さない', 2)):
    worst = 0; per = []
    for pr in pairs:
        n = 0
        for q, v in nat_series.items():
            if q not in ser or not v[k]: continue
            m = st.median(v[k]); lo = min(ser[q][f'{b}_{k}_min'] for b in pr); hi = max(ser[q][f'{b}_{k}_max'] for b in pr)
            n += not (lo <= m <= hi)
        per.append(n); worst = max(worst, n)
    gate(name, worst <= thr and len(nat_series) > 0, f"幅の外の四半期（2 本の組 {pairs}）{per}・最大 {worst}（≦{thr}）・四半期 {len(nat_series)}")
c4pop = c4bad = c4up = c4rows = 0
for s in seeds:
    p = os.path.join(arm, f'rulepoweralloc_s{s}.tsv')
    if not os.path.exists(p): continue
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: j for j, k in enumerate(h)}
        if 'wEngIndex' not in ix: break
        for line in f:
            c = line.rstrip('\n').split('\t')
            we, wd, ae, rf, fe = fl(c[ix['wEngIndex']]), fl(c[ix['wDIndex']]), fl(c[ix['allocEngIndex']]), fl(c[ix['rhoFDIndex']]), fl(c[ix['fForeignEngIndex']])
            if math.isnan(we) and math.isnan(wd): continue
            c4rows += 1
            if neq(we, wd): c4bad += 1
            if ae > 0 and finite(rf) and neq(rf, 1.0): c4pop += 1
            if ae > 0 and neq(fe, ae * rf): c4up += 1
gate('C4 ρ の回数', c4pop >= 1000 and c4bad == 0 and c4up == 0, f"配り＞0 かつ ρ_f≠1 {c4pop}（≧1000）・wEng≠wD {c4bad}（＝0）・F≠配り×ρ_f {c4up}（＝0）・行 {c4rows}")
c8c = st.median(c8c_vals) if c8c_vals else float('nan'); c8d = st.median(c8d_vals) if c8d_vals else float('nan')
gate('C8c 力の落ち', finite(c8c) and -0.3 <= c8c <= -0.002, f"O＝0 の国の実際の nowRule0 の差の種の中央 {c8c:.4g}（−0.3〜−0.002・追記 1）・種 {len(c8c_vals)}")
fr_e = c8e_in / c8e_tot if c8e_tot else float('nan')
gate('C8e 州が1つの国の寄せ', c8e_tot >= 1000 and fr_e >= 0.9, f"行 {c8e_tot}（≧1000）・±50% の内 {fr_e:.4g}（≧0.9）")
mw = st.median(g2_war) if g2_war else float('nan'); mp = st.median(g2_peace) if g2_peace else float('nan')
gate('G2 交戦中の首都', len(g2_war) >= 100 and finite(mw, mp) and mp > 0 and mw >= 0.5 * mp, f"交戦中の首都の t0Eng の中央 {mw:.4g}（行 {len(g2_war)}・≧100）・平時の中央 {mp:.4g}（行 {len(g2_peace)}）・比 {mw / mp if finite(mw, mp) and mp > 0 else float('nan'):.4g}（≧0.5）・交戦中で t0Eng＜0.05 の割合 {sum(1 for x in g2_war if x < 0.05) / len(g2_war) if g2_war else float('nan'):.4g}（記録）")
g1 = g1_count(arm)
if g1max is None: P(f'G1 中核の外国	未	交戦していない国の中核の州で外国の取り分 ≧0.25 の州の数（種×四半期の最大・種ごとの最大の和・種）{g1}（上側は短い走りの後の追記で書く）')
else: gate('G1 中核の外国', g1[0] <= g1max, f"交戦していない国の中核の州で外国の取り分 ≧0.25 の州の数（種×四半期の最大・種ごとの最大の和・種）{g1}（最大 ≦{g1max}）")
gate('C8d 対照', finite(c8c, c8d) and abs(c8d) < abs(c8c) / 2, f"O＞0 の国の差の種の中央 {c8d:.4g}（|·|＜|C8c|÷2）・種 {len(c8d_vals)}")
if len(nulls) == 3:
    a = c8b_value(arm, 'rhoEngIndex'); ns = [c8b_value(nf, 'rhoNoFreezeIndex') for nf in nulls]
    nv = [x[0] for x in ns if x and finite(x[0])]
    gate('C8b 届き', a is not None and finite(a[0]) and len(nv) == 3 and a[0] > max(nv), f"腕 {a}・基準・N1・N2 {ns}（腕＞3 本の最大）")
else:
    P('C8b 届き\t未\tヌル腕の生を渡していない')
print('\n'.join(out))
