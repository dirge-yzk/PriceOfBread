# (538) 線 ul3：発見の伝わりの積み上げ（率の形）の読み手。計器の生 engineer_s<seed>.tsv（T 行＝8 四半期ごとの全ての国×行の断面・E 行＝国が行を得た四半期）と
# discspill_s<seed>.tsv（E 行・C 行・R 行。腕 A〔係数 0〕にも出る）を読む。Z・最初の開き・追う国の遅れは scripts/instruments/de502/judge.py の定義をそのまま使う（import）。
# 使い方：PYTHONUTF8=1 python judge.py <腕の計器の生のフォルダ> [種の数=10] [A=<腕 A の生>] [RATE=<係数>] [比べる側のフォルダ ...]（比べる側＝基線 m533_ins・N1・N2。先頭の腕に門を出す）
# 定義（measurements/ul538_20261025_prereg.txt [5]）：
#   H1・一斉度・幅＝(510) judge.py と同じ。G6c の (ii)＝rows_with_ge8_in_one_quarter（追う国が 6 以上の（種，行）のうち 1 四半期に 8 国以上が開いた行の数）。
#   discspill_s の E 行：tech/need/contact/spill＝解禁を決めた Score の値（spill＝その四半期の判定の前の積み上げ）。tn＝tech+need。
#     技術力＋必要性だけで開いた＝tn≧100／接触が要った＝tn<100≦tn+contact／積み上げだけが要った＝tn+contact<100≦tn+spill／接触と積み上げの両方が要った＝それ以外。
#   G6b＝積み上げだけが要った件数÷E の件数（記録の門）。G6c(i)＝積み上げが要った開き（tn+contact<100）のうち、同じ（種，行）の最初の E の四半期から 4 四半期以内に開いた割合。
#   M1＝積み上げが要った開きの、開く直前の census（C 行）の欠け 100−tech−need と開いた四半期の順位相関（（種，行）ごと・5 件以上）の中央値。M2＝積み上げが要った開きの最初の開きからの遅れの中央値。
#   W1＝同じ（種，国，行）の C 行で spill が非減少。W2＝afterKnown＝0 の C 行の spill＝0。W3＝連続する C 行の spill の差≦RATE×（四半期の差）。
import os, sys, csv, collections, statistics as ST, importlib.util
_here = os.path.dirname(os.path.abspath(__file__))
_p = os.path.join(_here, '..', 'de502', 'judge.py')
if not os.path.exists(_p): _p = os.path.join(_here, 'de502_judge.py')
_spec = importlib.util.spec_from_file_location('de502judge', _p); de = importlib.util.module_from_spec(_spec); _spec.loader.exec_module(de)
num = de.num

def extra(d, nseeds):
    h1 = collections.Counter(); bursts = []; widths = []; sameq8 = 0
    for s in range(1, nseeds + 1):
        T, E = de.read(d, s)
        if not T: continue
        lastq = max(int(x[1]) for x in T)
        byrow = collections.defaultdict(list)
        for x in T:
            if int(x[1]) == lastq and int(x[5]) > 1780: byrow[x[4]].append(x)
        for rw, L in byrow.items():
            if not any(int(x[8]) for x in L): continue
            for x in L:
                if int(x[8]): continue
                if num(x[6]) > 0: h1['H1'] += 1
        ev = collections.defaultdict(list)
        for x in E:
            if int(x[5]) > 1780: ev[x[4]].append(int(x[1]))
        for rw, L in ev.items():
            L.sort(); q0 = L[0]; fol = [q for q in L if q != q0]
            if len(fol) < 6: continue
            c = collections.Counter(fol)
            bursts.append(max(c.values()) / len(fol)); sameq8 += max(c.values()) >= 8
            fs = sorted(fol); widths.append(fs[min(len(fs) - 1, int(len(fs) * 0.9))] - fs[int(len(fs) * 0.1)])
    return {'H1': h1['H1'], 'rows_with_ge6_followers': len(bursts), 'burst_median': ST.median(bursts) if bursts else None,
            'burst_share_ge_0.5': (sum(1 for b in bursts if b >= 0.5) / len(bursts)) if bursts else None,
            'rows_with_ge8_in_one_quarter': sameq8, 'width_p90_p10_median_quarters': ST.median(widths) if widths else None}

def _rank(a):
    idx = sorted(range(len(a)), key=lambda i: a[i]); r = [0.0] * len(a); i = 0
    while i < len(idx):
        j = i
        while j + 1 < len(idx) and a[idx[j + 1]] == a[idx[i]]: j += 1
        for k in range(i, j + 1): r[idx[k]] = (i + j) / 2
        i = j + 1
    return r

def _spearman(x, y):
    if len(x) < 5: return None
    rx, ry = _rank(x), _rank(y); mx = sum(rx) / len(rx); my = sum(ry) / len(ry)
    n = sum((a - mx) * (b - my) for a, b in zip(rx, ry)); dd = (sum((a - mx) ** 2 for a in rx) * sum((b - my) ** 2 for b in ry)) ** .5
    return n / dd if dd else None

def spill(d, nseeds, rate):
    """discspill_s（腕 A・B の両方に出る）＝census（C）・開き（E）・退場の記録（R）。無ければ None。"""
    p0 = os.path.join(d, 'discspill_s1.tsv')
    if not os.path.exists(p0): return None
    c = collections.Counter(); used = collections.Counter()
    ev_steel = {}; rrows = collections.defaultdict(list); d99 = collections.Counter()
    lags = []; rhos = []; w1_bad = 0; w2_bad = 0; w3_bad = 0; w_checked = 0
    for s in range(1, nseeds + 1):
        p = os.path.join(d, 'discspill_s%d.tsv' % s)
        if not os.path.exists(p): continue
        crow = collections.defaultdict(list)   # (国,行) -> [(四半期, known, spill, afterKnown, tech, need)]
        erows = collections.defaultdict(list)  # 行 -> [(四半期, 国, tn, contact, spill)]
        with open(p, encoding='utf-8') as f:
            rd = csv.reader(f, delimiter='\t'); h = next(rd); ix = {k: i for i, k in enumerate(h)}
            for x in rd:
                k = x[ix['kind']]
                if k == 'R':
                    rrows[(s, x[ix['nation']], x[ix['row']])].append((int(x[ix['quarter']]), int(x[ix['known']]), num(x[ix['oldOut']]), num(x[ix['newOut']])))
                elif k == 'E':
                    c['E'] += 1; tn = num(x[ix['tech']]) + num(x[ix['need']]); ct = num(x[ix['contact']]); sp = num(x[ix['spill']]); q = int(x[ix['quarter']])
                    if tn >= 100 - 1e-6: c['E_by_tech_need'] += 1
                    elif tn + ct >= 100 - 1e-6: c['E_needed_contact'] += 1
                    elif tn + sp >= 100 - 1e-6: c['E_needed_spill_only'] += 1
                    else: c['E_needed_both'] += 1
                    if tn + ct < 100 - 1e-6: c['E_needed_spill'] += 1
                    if x[ix['row']] == '転炉鋼': ev_steel[(s, x[ix['nation']])] = (q, tn + ct < 100 - 1e-6)
                    if tn <= 1 + 1e-6: d99[(s, x[ix['row']], q)] += 1   # 追記 2：欠け≧99（技術力＋必要性≦1）の国が同じ四半期に開いた件数
                    if num(x[ix['holders']]) == 0:
                        c['E_no_holders'] += 1; c['E_no_holders_spill_nonzero'] += (sp > 0)
                    erows[x[ix['row']]].append((q, x[ix['nation']], tn, ct, sp))
                elif k == 'C':
                    sp = num(x[ix['spill']]); c['C'] += 1; c['C_extreme_all'] += (sp == 0 or sp >= 90)
                    kn = int(x[ix['known']]); ak = int(x[ix['afterKnown']]) if x[ix['afterKnown']] not in ('nan', '') else 1
                    if ak == 0 and not kn:
                        c['C_after_unknown'] += 1
                        if sp != 0: w2_bad += 1
                    if num(x[ix['holders']]) > 0 and not kn: c['C_unk_holders'] += 1; c['C_extreme_unk_holders'] += (sp == 0 or sp >= 90)
                    if kn: c['known_obs'] += 1; used['known'] += 1; used['used'] += (num(x[ix['outputPerMonth']]) > 0)
                    crow[(x[ix['nation']], x[ix['row']])].append((int(x[ix['quarter']]), kn, sp, ak, num(x[ix['tech']]), num(x[ix['need']])))
        for key, L in crow.items():   # W1・W3
            L.sort()
            for a, b in zip(L, L[1:]):
                w_checked += 1
                if b[2] < a[2] - 1e-9: w1_bad += 1
                if rate > 0 and b[2] - a[2] > rate * (b[0] - a[0]) + 1e-6: w3_bad += 1
        for rw, L in erows.items():   # G6c(i)・M1・M2：同じ（種，行）の最初の E の四半期（接触・積み上げの有無を問わない）を基準にする
            q0 = min(q for q, _, _, _, _ in L)
            xs = []; ys = []
            for q, nat, tn, ct, sp in L:
                if tn + ct >= 100 - 1e-6: continue
                c['G6c_den'] += 1; c['G6c_le4'] += (q - q0 <= 4); lags.append(q - q0)
                prev = [t for t in crow.get((nat, rw), []) if t[0] <= q and not t[1]]
                if prev:
                    t = max(prev); xs.append(100 - t[4] - t[5]); ys.append(q)
            rho = _spearman(xs, ys)
            if rho is not None: rhos.append(rho)
    ret = collections.defaultdict(lambda: {'n': 0, 'old_before_pos': 0, 'old_after_zero': 0, 'new_ratio': [], 'total_ratio': []})
    for (sd, nat, rw), L in rrows.items():
        evs = ev_steel.get((sd, nat))
        if not evs: continue
        q0, by_spill = evs; L.sort()
        bef = [y for y in L if y[0] < q0]; aft = [y for y in L if y[0] > q0]
        if not bef or not aft: continue
        key = rw + ('・積み上げが要った' if by_spill else '・他の道で開いた'); r = ret[key]; r['n'] += 1
        if bef[-1][2] > 0:
            r['old_before_pos'] += 1; r['old_after_zero'] += (aft[0][2] <= 0)
            if bef[-1][3] > 0: r['new_ratio'].append(aft[0][3] / bef[-1][3])
        tb = bef[-1][2] + bef[-1][3]
        if tb > 0: r['total_ratio'].append((aft[0][2] + aft[0][3]) / tb)
    retire = {k: {'n': v['n'], 'old_before_pos': v['old_before_pos'], 'old_after_zero': v['old_after_zero'], 'new_after_over_before_median': (ST.median(v['new_ratio']) if v['new_ratio'] else None),
                  'total_pairs': len(v['total_ratio']), 'total_after_over_before_median': (ST.median(v['total_ratio']) if v['total_ratio'] else None)} for k, v in sorted(ret.items())}
    E = c['E']
    return {'E': E, 'E_tech_need_alone': c['E_by_tech_need'], 'E_needed_contact': c['E_needed_contact'], 'E_needed_spill_only': c['E_needed_spill_only'], 'E_needed_both': c['E_needed_both'],
            'E_needed_spill_total': c['E_needed_spill'],
            'G6b_spill_only_share': (c['E_needed_spill_only'] / E) if E else None, 'spill_needed_share': (c['E_needed_spill'] / E) if E else None,
            'G6c_i_within4_share': (c['G6c_le4'] / c['G6c_den']) if c['G6c_den'] else None, 'G6c_den': c['G6c_den'],
            'M2_lag_median_quarters': ST.median(lags) if lags else None, 'M1_deficit_rank_corr_median': ST.median(rhos) if rhos else None, 'M1_rows': len(rhos),
            'W1_nonmonotone': w1_bad, 'W2_after_unknown_nonzero': w2_bad, 'W2_checked': c['C_after_unknown'], 'W3_over_rate': w3_bad, 'W_pairs_checked': w_checked,
            'deficit99_same_quarter_max': max(d99.values()) if d99 else 0, 'deficit99_same_quarter_ge8': sum(1 for v in d99.values() if v >= 8), 'deficit99_total': sum(d99.values()),
            'E_no_holders': c['E_no_holders'], 'E_no_holders_spill_nonzero': c['E_no_holders_spill_nonzero'],
            'retire_steel': retire,
            'G7_census_obs': c['C'], 'G7_extreme_share_all': (c['C_extreme_all'] / c['C']) if c['C'] else None,
            'G7_obs_unknown_with_holders': c['C_unk_holders'], 'G7_extreme_share_unknown_with_holders': (c['C_extreme_unk_holders'] / c['C_unk_holders']) if c['C_unk_holders'] else None,
            'known_obs': used['known'], 'known_and_output_positive_share': (used['used'] / used['known']) if used['known'] else None}

def judge(d, n, rate):
    r = de.judge(d, n); r.update(extra(d, n)); sp = spill(d, n, rate)
    if sp: r['spill'] = sp
    return r

def gates(B, comps, armA=None, rate=0.0):
    out = []
    zb = B['Z_total']; floor = {k: min(c['Z_total'].get(k, 0) for c in comps.values()) for k in ('Z', 'ZA0', 'ZA<1.4')}
    out.append(('G1 Z・A=0 の群・0<A<1.4 の群が比べる側（基線・N1・N2）の最小より小さい', all(zb.get(k, 0) < floor[k] for k in floor), {k: (zb.get(k, 0), floor[k]) for k in floor}))
    h1f = min(c['H1'] for c in comps.values()); out.append(('G1b H1（親の産業が立つのに最後まで開かない）が比べる側の最小より小さい', B['H1'] < h1f, (B['H1'], h1f)))
    fb = B['first_openers']; out.append(('G2 最初の開きは技術力＋必要性≧100', fb.get('techNeedOK') == fb.get('n'), (fb.get('techNeedOK'), fb.get('n'))))
    lg = B.get('follower_lag_quarters', {})
    out.append(('G3 遅れの中央値≧2 かつ 1 四半期以内＜50%', lg.get('median', 0) >= 2 and lg.get('share_le_1', 1) < 0.5, (lg.get('median'), lg.get('share_le_1'))))
    sp = B.get('spill')
    bm = B.get('burst_median'); bw = B.get('width_p90_p10_median_quarters')
    bmin = [c.get('burst_median') for c in comps.values() if c.get('burst_median') is not None]
    out.append(('G6 一斉に開かない（一斉度の中央値＜0.5 かつ 幅の中央値≧8 四半期）', bm is not None and bm < 0.5 and bw is not None and bw >= 8, (bm, bw, 'base', bmin)))
    if sp:
        out.append(('G4 持つ国が 0 の開きで積み上げが正の件数＝0', sp['E_no_holders_spill_nonzero'] == 0, (sp['E_no_holders_spill_nonzero'], sp['E_no_holders'])))
        g8l = [c.get('rows_with_ge8_in_one_quarter', 0) for c in comps.values()]; g8 = max(g8l); g8thr = g8 + (g8 - min(g8l))   # 追記 2：基線の最大＋比べる側の幅
        i_ok = sp['G6c_i_within4_share'] is not None and sp['G6c_i_within4_share'] <= 0.10
        ii_ok = B.get('rows_with_ge8_in_one_quarter', 0) <= g8thr
        out.append(('G6c(ii) 連鎖の門：1 四半期に 8 国以上が開く行の数≦基線の最大＋比べる側の幅（追記 2）', ii_ok, (B.get('rows_with_ge8_in_one_quarter'), g8thr, 'base', g8l)))
        out.append(('配線 G6c(i)：積み上げが要った開きのうち最初の開きから 4 四半期以内≦0.10（作りでほぼ自明・追記 2）', i_ok, (sp['G6c_i_within4_share'], sp['G6c_den'])))
        out.append(('記録（追記 2）：欠け≧99 の群が同じ四半期に開いた件数の最大・8 国以上の（種，行，四半期）の数', None, (sp['deficit99_same_quarter_max'], sp['deficit99_same_quarter_ge8'], sp['deficit99_total'])))
        out.append(('W1 同じ（国，行）の census で積み上げは非減少', sp['W1_nonmonotone'] == 0, (sp['W1_nonmonotone'], sp['W_pairs_checked'])))
        out.append(('W2 前提が未解禁の（国，行）の積み上げ＝0', sp['W2_after_unknown_nonzero'] == 0 and sp['W2_checked'] > 0, (sp['W2_after_unknown_nonzero'], sp['W2_checked'])))
        out.append(('W3 連続する census の積み上げの差≦係数×四半期の差', sp['W3_over_rate'] == 0 if rate > 0 else None, (sp['W3_over_rate'], rate)))
        m1 = sp['M1_deficit_rank_corr_median']
        out.append(('配線 M1 積み上げが要った開きは欠けの小さい順（順位相関の中央値＞0.5）', m1 is not None and m1 > 0.5, (m1, sp['M1_rows'])))
        m2 = sp['M2_lag_median_quarters']
        out.append(('配線 M2 積み上げが要った開きの最初の開きからの遅れの中央値≧8 四半期', m2 is not None and m2 >= 8, m2))
        sa3 = armA['spill']['E_needed_spill_total'] if armA and armA.get('spill') else 0
        out.append(('配線 M3 積み上げが要った開きの数が腕 A の同じ census より多い（腕 A は作りの上で 0）', sp['E_needed_spill_total'] > sa3 if rate > 0 else None, (sp['E_needed_spill_total'], sa3)))
        sh = sp['known_and_output_positive_share']; out.append(('G5 記録：解禁した国のうち産出が正の割合＜1', sh is not None and sh < 1.0, sh))
        if armA and armA.get('spill'):
            sa = armA['spill']['known_and_output_positive_share']
            out.append(('G5 記録：腕 B の割合が腕 A と 0.2 以上離れていない（高い低い両方向）', sh is not None and sa is not None and abs(sh - sa) < 0.2, (sh, sa)))
        out.append(('G6b 記録（正の定義＝積み上げだけが要った・広い定義は括弧の 2 つ目）：割合＜0.5', sp['G6b_spill_only_share'] is not None and sp['G6b_spill_only_share'] < 0.5, (sp['G6b_spill_only_share'], sp['spill_needed_share'])))
        g7 = sp['G7_extreme_share_unknown_with_holders']
        out.append(('G7 記録：積み上げが 0 か満点の 0.9 以上のどちらかである割合（持つ国が居る未解禁の観測）＜70%', g7 is not None and g7 < 0.70, g7))
        spl = [v for k, v in sp['retire_steel'].items() if k.endswith('・積み上げが要った')]
        pairs = sum(v['total_pairs'] for v in spl); meds = [v['total_after_over_before_median'] for v in spl if v['total_pairs'] >= 1 and v['total_after_over_before_median'] is not None]
        out.append(('G8 記録：積み上げで転炉鋼を開いた国の 鋳鉄の機関の産出の和（後÷前）の中央値≧0.5（組が 5 未満なら判定不能）', None if pairs < 5 else min(meds) >= 0.5, (pairs, meds)))
    else: out.append(('discspill_s が無い', False, None))
    return out

if __name__ == '__main__':
    args = [a for a in sys.argv[1:] if not a.startswith('RATE=')]
    rate = 0.0
    for a in sys.argv[1:]:
        if a.startswith('RATE='): rate = float(a[5:])
    d = args[0]; n = int(args[1]) if len(args) > 1 else 10
    r = judge(d, n, rate)
    for k, v in r.items():
        if not k.startswith('_'): print(k, v)
    comps = {}; armA = None
    for cd in args[2:]:
        if cd.startswith('A='):
            armA = judge(cd[2:], n, 0.0); print('--- 腕 A %s ---' % cd[2:])
            for k, v in armA.items():
                if not k.startswith('_'): print(k, v)
            continue
        b = judge(cd, n, 0.0); comps[os.path.basename(cd.replace(chr(92), '/').rstrip('/'))] = b
        print('--- 比べる側 %s ---' % cd)
        for k, v in b.items():
            if not k.startswith('_'): print(k, v)
    if comps:
        print('=== 門の判定（腕＝%s）===' % d)
        for name, ok, val in gates(r, comps, armA, rate): print('PASS' if ok else ('N/A ' if ok is None else 'FAIL'), name, val)
