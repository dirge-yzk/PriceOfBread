# (510) 線 ul：発見の解禁の条件（世界の広がりの項）の読み手。計器の生 engineer_s<seed>.tsv（T 行＝8 四半期ごとの全ての国×行の断面・E 行＝国が行を得た四半期）と
# 腕 B だけに出る discspill_s<seed>.tsv を読む。Z・最初の開き・追う国の遅れは scripts/instruments/de502/judge.py の定義をそのまま使う（import）。
# 使い方：PYTHONUTF8=1 python judge.py <腕の計器の生のフォルダ> [種の数=10] [比べる側のフォルダ ...]（比べる側＝基線・N1・N2。先頭の腕に門 G1〜G4・G6 を出す）
# 足した定義（measurements/ul510_20261023_prereg.txt [5]）：
#   H1＝最後の断面（Q663）で、A＞0（親の産業が立つ）・未解禁・その行を他のどこかの国が持つ（種，国，行）の数（decade＞1780 の行）。
#   一斉度（burst）＝行ごと（追う国が 6 以上の（種，行））の 最大の 1 四半期に開いた追う国の数÷追う国の数。その中央値を出す。
#   幅（width）＝同じ（種，行）の追う国の開いた四半期の p90−p10（四半期）の中央値。
import os, sys, csv, collections, statistics as ST, importlib.util
_here = os.path.dirname(os.path.abspath(__file__))
_p = os.path.join(_here, '..', 'de502', 'judge.py')
if not os.path.exists(_p): _p = os.path.join(_here, 'de502_judge.py')
_spec = importlib.util.spec_from_file_location('de502judge', _p); de = importlib.util.module_from_spec(_spec); _spec.loader.exec_module(de)
num = de.num

def extra(d, nseeds):
    h1 = collections.Counter(); bursts = []; widths = []; nfoll_rows = 0; sameq8 = 0
    for s in range(1, nseeds + 1):
        T, E = de.read(d, s)
        if not T: continue   # None（ファイルが無い）も空（census が 1 行も無い）も飛ばす
        lastq = max(int(x[1]) for x in T)
        byrow = collections.defaultdict(list)
        for x in T:
            if int(x[1]) == lastq and int(x[5]) > 1780: byrow[x[4]].append(x)
        for rw, L in byrow.items():
            if not any(int(x[8]) for x in L): continue
            for x in L:
                if int(x[8]): continue
                a = num(x[6])
                if a > 0: h1['H1'] += 1
        ev = collections.defaultdict(list)
        for x in E:
            if int(x[5]) > 1780: ev[x[4]].append(int(x[1]))
        for rw, L in ev.items():
            L.sort(); q0 = L[0]; fol = [q for q in L if q != q0]
            if len(fol) < 6: continue
            c = collections.Counter(fol)
            bursts.append(max(c.values()) / len(fol)); sameq8 += max(c.values()) >= 8
            fs = sorted(fol); widths.append(fs[min(len(fs) - 1, int(len(fs) * 0.9))] - fs[int(len(fs) * 0.1)])
    out = {'H1': h1['H1'], 'rows_with_ge6_followers': len(bursts), 'burst_median': ST.median(bursts) if bursts else None,
           'burst_share_ge_0.5': (sum(1 for b in bursts if b >= 0.5) / len(bursts)) if bursts else None,
           'rows_with_ge8_in_one_quarter': sameq8, 'width_p90_p10_median_quarters': ST.median(widths) if widths else None}
    return out

def spill(d, nseeds):
    """discspill_s（腕 B だけ）＝census（C）と開き（E）。列は prereg [7]。無ければ None。"""
    p0 = os.path.join(d, 'discspill_s1.tsv')
    if not os.path.exists(p0): return None
    c = collections.Counter(); sp_open = []; used = collections.Counter()
    ev_steel = {}; rrows = collections.defaultdict(list)   # R（退場の記録）：(種,国,後継>財) -> [(四半期, 後継を知るか, 退場レシピの産出, 同財の他レシピの産出)]・E の転炉鋼を広がりで開いた国と四半期
    for s in range(1, nseeds + 1):
        p = os.path.join(d, 'discspill_s%d.tsv' % s)
        if not os.path.exists(p): continue
        with open(p, encoding='utf-8') as f:
            rd = csv.reader(f, delimiter='\t'); h = next(rd); ix = {k: i for i, k in enumerate(h)}
            for x in rd:
                if x[ix['kind']] == 'R':
                    rrows[(s, x[ix['nation']], x[ix['row']])].append((int(x[ix['quarter']]), int(x[ix['known']]), num(x[ix['oldOut']]), num(x[ix['newOut']])))
                elif x[ix['kind']] == 'E':
                    c['E'] += 1; tn = num(x[ix['tech']]) + num(x[ix['need']]); t = tn + num(x[ix['contact']]); sp = num(x[ix['spill']])
                    if tn >= 100 - 1e-6: c['E_by_tech_need'] += 1                      # 記録④：技術力＋必要性だけで開いた
                    elif t >= 100 - 1e-6: c['E_needed_contact'] += 1                   #   接触が要った（広がり無しで足りた）
                    elif tn + sp >= 100 - 1e-6: c['E_needed_spill_only'] += 1          #   広がりが要った（接触無しで足りた）
                    else: c['E_needed_both'] += 1                                      #   接触と広がりの両方が要った（重なりの大きさ）
                    if x[ix['row']] == '転炉鋼': ev_steel[(s, x[ix['nation']])] = (int(x[ix['quarter']]), t < 100 - 1e-6)
                    if t < 100 - 1e-6: c['E_by_spill'] += 1
                    if num(x[ix['holders']]) == 0: c['E_no_holders'] += 1; c['E_no_holders_spill_nonzero'] += (num(x[ix['spill']]) > 0)
                elif x[ix['kind']] == 'C':
                    sp = num(x[ix['spill']]); c['C'] += 1; c['C_extreme_all'] += (sp == 0 or sp >= 90)
                    if num(x[ix['holders']]) > 0 and not int(x[ix['known']]): c['C_unk_holders'] += 1; c['C_extreme_unk_holders'] += (sp == 0 or sp >= 90)
                    if int(x[ix['known']]):
                        c['known_obs'] += 1; used['known'] += 1; used['used'] += (num(x[ix['outputPerMonth']]) > 0)
    # 退場の記録：転炉鋼を広がりで開いた国の、開いた四半期の前の最後の census と後の最初の census（後の最初は開いた四半期より後）の産出の比（退場するレシピ・同財の他のレシピ）
    ret = collections.defaultdict(lambda: {'n': 0, 'old_before_pos': 0, 'old_after_zero': 0, 'new_ratio': [], 'total_ratio': []})
    for (sd, nat, rw), L in rrows.items():
        evs = ev_steel.get((sd, nat))
        if not evs: continue
        q0, by_spill = evs; L.sort()
        bef = [y for y in L if y[0] < q0]; aft = [y for y in L if y[0] > q0]
        if not bef or not aft: continue
        key = rw + ('・広がりで開いた' if by_spill else '・他の道で開いた'); r = ret[key]; r['n'] += 1
        if bef[-1][2] > 0:
            r['old_before_pos'] += 1; r['old_after_zero'] += (aft[0][2] <= 0)
            if bef[-1][3] > 0: r['new_ratio'].append(aft[0][3] / bef[-1][3])
        tb = bef[-1][2] + bef[-1][3]
        if tb > 0: r['total_ratio'].append((aft[0][2] + aft[0][3]) / tb)   # G8：同じ財の産出の和（退場するレシピ＋他のレシピ）の 後÷前
    retire = {k: {'n': v['n'], 'old_before_pos': v['old_before_pos'], 'old_after_zero': v['old_after_zero'], 'new_after_over_before_median': (ST.median(v['new_ratio']) if v['new_ratio'] else None),
                                  'total_pairs': len(v['total_ratio']), 'total_after_over_before_median': (ST.median(v['total_ratio']) if v['total_ratio'] else None)} for k, v in sorted(ret.items())}
    return {'E_tech_need_alone': c['E_by_tech_need'], 'E_needed_contact': c['E_needed_contact'], 'E_needed_spill_only': c['E_needed_spill_only'], 'E_needed_both': c['E_needed_both'], 'retire_steel': retire,
            'E': c['E'], 'E_opened_needing_spill': c['E_by_spill'], 'E_no_holders': c['E_no_holders'], 'E_no_holders_spill_nonzero': c['E_no_holders_spill_nonzero'],
            'G7_census_obs': c['C'], 'G7_extreme_share_all': (c['C_extreme_all'] / c['C']) if c['C'] else None,
            'G7_obs_unknown_with_holders': c['C_unk_holders'], 'G7_extreme_share_unknown_with_holders': (c['C_extreme_unk_holders'] / c['C_unk_holders']) if c['C_unk_holders'] else None,
            'known_obs': used['known'], 'known_and_output_positive_share': (used['used'] / used['known']) if used['known'] else None}

def judge(d, n):
    r = de.judge(d, n); r.update(extra(d, n)); sp = spill(d, n)
    if sp: r['spill'] = sp
    return r

def gates(B, comps, armA=None):
    out = []
    zb = B['Z_total']; floor = {k: min(c['Z_total'].get(k, 0) for c in comps.values()) for k in ('Z', 'ZA0', 'ZA<1.4')}
    out.append(('G1 Z・A=0 の群・0<A<1.4 の群が比べる側（基線・N1・N2）の最小より小さい', all(zb.get(k, 0) < floor[k] for k in floor), {k: (zb.get(k, 0), floor[k]) for k in floor}))
    h1f = min(c['H1'] for c in comps.values()); out.append(('G1b H1（親の産業が立つのに最後まで開かない）が比べる側の最小より小さい', B['H1'] < h1f, (B['H1'], h1f)))
    fb = B['first_openers']; out.append(('G2 最初の開きは技術力＋必要性≧100', fb.get('techNeedOK') == fb.get('n'), (fb.get('techNeedOK'), fb.get('n'))))
    lg = B.get('follower_lag_quarters', {})
    out.append(('G3 遅れの中央値≧2 かつ 1 四半期以内＜50%', lg.get('median', 0) >= 2 and lg.get('share_le_1', 1) < 0.5, (lg.get('median'), lg.get('share_le_1'))))
    sp = B.get('spill')
    if sp:
        out.append(('G4 持つ国が 0 の開きで広がりの項が正の件数＝0', sp['E_no_holders_spill_nonzero'] == 0, (sp['E_no_holders_spill_nonzero'], sp['E_no_holders'])))
        sh = sp['known_and_output_positive_share']; out.append(('G5 解禁した国のうち新しいレシピの産出が正の割合＜1（知っていて使わない国がいる）', sh is not None and sh < 1.0, sh))
        if armA and armA.get('spill'):
            sa = armA['spill']['known_and_output_positive_share']   # 腕 A（旗 0・同じ census）＝比べる側。0.2 以上高ければ記録（使われ方の偏り）
            out.append(('G5 記録：腕 B の割合が腕 A より 0.2 以上高くない（高ければ〈使われ方の偏り〉と記録）', sh is not None and sa is not None and sh - sa < 0.2, (sh, sa)))
        spl = [v for k, v in sp['retire_steel'].items() if k.endswith('・広がりで開いた')]
        pairs = sum(v['total_pairs'] for v in spl); meds = [v['total_after_over_before_median'] for v in spl if v['total_pairs'] >= 1 and v['total_after_over_before_median'] is not None]
        if pairs < 5: out.append(('G8 広がりで転炉鋼を開いた国の 小銃・大砲・蒸気機関の産出の和（後÷前）の中央値≧0.5（組が 5 未満なら判定不能＝記録）', None, (pairs, meds)))
        else: out.append(('G8 広がりで転炉鋼を開いた国の 小銃・大砲・蒸気機関の産出の和（後÷前）の財ごとの中央値の最小≧0.5', min(meds) >= 0.5, (pairs, meds)))
        g7 = sp['G7_extreme_share_unknown_with_holders']   # 分母は〈持つ国が居る未解禁〉に固定（世界で最初の発見の前の S＝0 を構造として含む全観測は使わない）
        out.append(('G7 記録：広がりが 0 か満点の 0.9 以上のどちらかである割合（持つ国が居る未解禁の観測）＜70%', g7 is not None and g7 < 0.70, g7))
    else: out.append(('G4/G5 discspill_s が無い', False, None))
    bm = B.get('burst_median'); bw = B.get('width_p90_p10_median_quarters')
    bmin = [c.get('burst_median') for c in comps.values() if c.get('burst_median') is not None]
    out.append(('G6 一斉に開かない（一斉度の中央値＜0.5 かつ 幅の中央値≧8 四半期）', bm is not None and bm < 0.5 and bw is not None and bw >= 8, (bm, bw, 'base', bmin)))
    return out

if __name__ == '__main__':
    d = sys.argv[1]; n = int(sys.argv[2]) if len(sys.argv) > 2 else 10
    r = judge(d, n)
    for k, v in r.items():
        if not k.startswith('_'): print(k, v)
    comps = {}; armA = None
    for cd in sys.argv[3:]:
        if cd.startswith('A='):   # 腕 A（旗 0・INS=1）の生のフォルダ＝G5 の比べる側（m508_ins には discspill_s が無い）。種の数は先頭と同じ n 以下でよい
            armA = judge(cd[2:], n); print('--- 腕 A %s ---' % cd[2:])
            for k, v in armA.items():
                if not k.startswith('_'): print(k, v)
            continue
        b = judge(cd, n); comps[os.path.basename(cd.replace(chr(92), '/').rstrip('/'))] = b
        print('--- 比べる側 %s ---' % cd)
        for k, v in b.items():
            if not k.startswith('_'): print(k, v)
    if comps:
        print('=== 門の判定（腕＝%s）===' % d)
        for name, ok, val in gates(r, comps, armA): print('PASS' if ok else ('N/A ' if ok is None else 'FAIL'), name, val)
