# (502) 線 de：袋小路の線（伝わりの分母）の読み手。計器の生 engineer_s<seed>.tsv だけを読む（T 行＝8 四半期ごとの全ての国×行の断面・E 行＝国が行を得た四半期）。
# 使い方：PYTHONUTF8=1 python judge.py <計器の生のフォルダ> [種の数=10] [別のフォルダ ...]（先頭は腕、残りは基線・ヌル腕 N1・N2 など。各フォルダを読んで並べ、最後に門 G1・G3・G6 の判定を出す）
#   門の判定は先頭を腕 B、2 番目以降を比べる側（基線・N1・N2）として出す。比べる側が 0 本なら門は出さない。
# 列（engineer_s）：kind quarter year nation row decade engineerPerMonth techTerm knownFlag needTerm replaced cost。
# 定義（measurements/de_20261023_prereg.txt [5]）：
#   S（時計の段）＝その断面の行ごとに、未解禁で A＞0・技術力＞0 の国の 技術力÷(70×A÷(A＋Am)) の最大（Am＝DiscoveryOwnHalfPerMonth 既定 12.5677）。
#   Z（出口の無い追う国×行）＝最後の断面で、行を持つ国が 1 つ以上あり・S≧0.99・その国が未解禁・必要性≧30 の（種，国，行）。内訳は A の大きさ（A＝0／0＜A＜1.4／1.4≦A＜12.6／12.6≦A）。
#   最初の開き＝行ごとの E 行の最小の四半期。追う国の開き＝それより後の E 行。「接触が要った」＝開いた四半期の 技術力＋必要性＜100。
#   ★decade＞1780 の行だけを読む（Z・最初の開き・追う国の開き・上位 10 国のばらつき）＝蒸気機関（decade 1780・開幕から英が持つ）は時計が立つ前の別の規則（時計の外・最初の開きが無い）なので別の集計 steam_* に出す（追記 2）。
#   discbuy_s（旗 DiscoveryContactDenominatorOwn が正の腕だけに出る）＝買い手×行×四半期の 分子・旧の分母・新の分母・国数。x_旧＝分子÷旧・x_新＝分子÷新。
import csv, collections, statistics as ST, sys, os
AM = 12.5677
def num(v): return 0.0 if v in ('nan', '') else float(v)

def read(d, seed):
    T = []; E = []
    p = os.path.join(d, 'engineer_s%d.tsv' % seed)
    if not os.path.exists(p): return None, None
    with open(p, encoding='utf-8') as f:
        for x in csv.reader(f, delimiter='\t'):
            if x[0] == 'T': T.append(x)
            elif x[0] == 'E': E.append(x)
    return T, E

def discbuy(d, nseeds):
    """discbuy_s（腕 B だけ）を読む＝分子が正の（買い手，行，四半期）。x_旧＝分子÷旧の分母・x_新＝分子÷新の分母。saturated＝x_新≧0.5（伝わり＝70×x÷(x＋0.05) ≧ 63.6＝ほぼ飽和）。
    thin＝新の分母が 旧の分母÷国数 より小さい（買いが世界の 1 国あたりの平均より細い）。無い腕は None。"""
    c = collections.Counter(); xo = []; xn = []; found = 0
    for s in range(1, nseeds + 1):
        p = os.path.join(d, 'discbuy_s%d.tsv' % s)
        if not os.path.exists(p): continue
        found += 1
        with open(p, encoding='utf-8') as f:
            rd = csv.reader(f, delimiter='	'); h = next(rd)
            ix = {k: h.index(k) for k in ('numerator', 'denOld', 'denNew', 'nNations')}
            for x in rd:
                nu = num(x[ix['numerator']]); do = num(x[ix['denOld']]); dn = num(x[ix['denNew']]); nn = num(x[ix['nNations']])
                if not (nu > 0 and do > 0 and dn > 0): continue
                a = nu / do; b = nu / dn
                c['n'] += 1; xo.append(a); xn.append(b)
                if b >= 0.5:
                    c['sat'] += 1
                    if dn < do / nn: c['sat_thin'] += 1
                if dn < do / nn: c['thin'] += 1
                if b < a - 1e-9 or b > 1 + 1e-9: c['bound_violation'] += 1   # x_旧≦x_新≦1 の破れ（番人と同じ述語）
    if not found: return None
    xo.sort(); xn.sort()
    return {'seeds': found, 'obs': c['n'], 'x_old_median': ST.median(xo) if xo else None, 'x_new_median': ST.median(xn) if xn else None,
            'share_sat': c['sat'] / c['n'] if c['n'] else None, 'share_sat_thin': c['sat_thin'] / c['sat'] if c['sat'] else None,
            'share_thin': c['thin'] / c['n'] if c['n'] else None, 'bound_violations': c['bound_violation']}

def judge(d, nseeds):
    out = collections.OrderedDict()
    z = collections.Counter(); zs = []; first = collections.Counter(); foll = collections.Counter(); lag = []
    g4 = collections.Counter(); seeds = 0
    steamq = []; qing_first = collections.Counter()
    openq = collections.defaultdict(list)   # 行 -> 追う国の開いた四半期の一覧（記録）
    firstq = collections.defaultdict(list)
    for s in range(1, nseeds + 1):
        T, E = read(d, s)
        if T is None: continue
        seeds += 1
        lastq = max(int(x[1]) for x in T)
        byrow = collections.defaultdict(list)
        for x in T:
            if int(x[1]) == lastq and int(x[5]) > 1780: byrow[x[4]].append(x)
        zz = collections.Counter()
        for rw, L in byrow.items():
            if not any(int(x[8]) for x in L): continue
            S = 0.0
            for x in L:
                a = num(x[6]); t = num(x[7])
                if a > 0 and t > 0 and not int(x[8]): S = max(S, t / (70 * a / (a + AM)))
            if S < 0.99: continue
            for x in L:
                if int(x[8]): continue
                a = num(x[6]); nd = num(x[9])
                if nd >= 30:
                    zz['Z'] += 1
                    zz['ZA0' if a <= 0 else 'ZA<1.4' if a < 1.4 else 'ZA<12.6' if a < 12.6 else 'ZA>=12.6'] += 1
        z.update(zz); zs.append(zz['Z'])
        # 上位 10 国の必要性と技術力のばらつき（傾斜の期間＝0.01＜S＜0.99・未解禁の国）
        snaps = collections.defaultdict(list)
        for x in T:
            if int(x[5]) > 1780 and not int(x[8]): snaps[(int(x[1]), x[4])].append((num(x[6]), num(x[7]), num(x[9])))
        for (q, rw), L in snaps.items():
            S = 0.0
            for a, t, nd in L:
                if a > 0 and t > 0: S = max(S, t / (70 * a / (a + AM)))
            if not (0.01 < S < 0.99): continue
            top = sorted(L, reverse=True)[:10]
            if len(top) < 10: continue
            st = ST.pstdev([t for a, t, nd in top]); sn = ST.pstdev([nd for a, t, nd in top])
            g4['n'] += 1; g4['needGtTech'] += sn > st; g4['sdTech'] += st; g4['sdNeed'] += sn
        ev = collections.defaultdict(list)
        for x in E:
            if int(x[5]) > 1780: ev[x[4]].append((int(x[1]), x[3], num(x[7]), num(x[9])))
            else: steamq.append(int(x[1]))   # 蒸気機関（decade 1780）の追う国の開き（別の集計）
        for rw, L in ev.items():
            L.sort(); q0 = L[0][0]
            for q, n, t, nd in L:
                if q == q0:
                    qing_first['n'] += 1; qing_first['qing'] += (n == '清')   # 記録③：最初の開きの国が清の割合（清の強さは採否に使わない）
                    first['n'] += 1; first['techNeedOK'] += (t + nd >= 100 - 1e-6); firstq[rw].append(q)
                else:
                    foll['n'] += 1; lag.append(q - q0); foll['needsTrans'] += (t + nd < 100 - 1e-6); openq[rw].append(q)
    out['seeds'] = seeds
    out['Z_total'] = dict(z); out['Z_per_seed'] = zs
    out['first_openers'] = dict(first)
    out['followers'] = dict(foll)
    if lag:
        ls = sorted(lag)
        out['follower_lag_quarters'] = {'median': ST.median(ls), 'p25': ls[len(ls) // 4], 'p75': ls[(3 * len(ls)) // 4], 'share_le_0': sum(1 for l in ls if l <= 0) / len(ls), 'share_le_1': sum(1 for l in ls if l <= 1) / len(ls)}
    if g4['n']:
        out['top10_dispersion'] = {'snapshots': g4['n'], 'share_need_gt_tech': g4['needGtTech'] / g4['n'], 'mean_sd_tech': g4['sdTech'] / g4['n'], 'mean_sd_need': g4['sdNeed'] / g4['n']}
    if steamq:
        ss = sorted(steamq); out['steam_follower_open_quarters'] = {'n': len(ss), 'median': ST.median(ss), 'p25': ss[len(ss) // 4], 'p75': ss[(3 * len(ss)) // 4]}
    if qing_first['n']: out['first_opener_qing_share'] = qing_first['qing'] / qing_first['n']
    db = discbuy(d, nseeds)
    if db: out['discbuy'] = db
    out['_median_follower_open_quarter_by_row'] = {rw: ST.median(v) for rw, v in sorted(openq.items())}
    out['_median_first_open_quarter_by_row'] = {rw: ST.median(v) for rw, v in sorted(firstq.items())}
    return out

def gates(B, comps):
    """門の判定（事前登録 [5]・追記 2）。B＝腕の読み・comps＝{名前: 読み}（基線・N1・N2）。"""
    out = []
    zb = B['Z_total']; floor = {k: min(c['Z_total'].get(k, 0) for c in comps.values()) for k in ('Z', 'ZA0', 'ZA<1.4')}
    g1 = all(zb.get(k, 0) < floor[k] for k in floor)
    out.append(('G1 Z・A=0 の群・0<A<1.4 の群が比べる側の最小より小さい', g1, {k: (zb.get(k, 0), floor[k]) for k in floor}))
    fb = B['first_openers']; out.append(('G2 最初の開きは技術力＋必要性≧100', fb.get('techNeedOK') == fb.get('n'), (fb.get('techNeedOK'), fb.get('n'))))
    lg = B.get('follower_lag_quarters', {})
    out.append(('G3 遅れの中央値≧2 かつ 1 四半期以内＜50%', lg.get('median', 0) >= 2 and lg.get('share_le_1', 1) < 0.5, (lg.get('median'), lg.get('share_le_1'))))
    db = B.get('discbuy')
    if db:
        out.append(('G4 x_旧≦x_新≦1（破れ 0）', db['bound_violations'] == 0, db['bound_violations']))
        ok = db['share_sat'] is not None and db['share_sat'] < 0.5
        out.append(('G6 飽和（x_新≧0.5）の割合＜50%', ok, (db['share_sat'], db['share_sat_thin'], db['obs'])))
    else:
        out.append(('G4/G6 discbuy_s が無い', False, None))
    return out

if __name__ == '__main__':
    d = sys.argv[1]; n = int(sys.argv[2]) if len(sys.argv) > 2 else 10
    r = judge(d, n)
    for k, v in r.items():
        if k.startswith('_'): continue
        print(k, v)
    print('open_quarter_by_row (中央値・first / follower):')
    f1 = r['_median_first_open_quarter_by_row']; f2 = r['_median_follower_open_quarter_by_row']
    for rw in sorted(set(f1) | set(f2)):
        print('  ', rw, f1.get(rw), f2.get(rw))
    comps = {}
    for cd in sys.argv[3:]:
        b = judge(cd, n); comps[os.path.basename(cd.replace(chr(92), '/').rstrip('/'))] = b
        print('--- 比べる側 %s ---' % cd)
        for k, v in b.items():
            if not k.startswith('_'): print(k, v)
    if comps:
        print('=== 門の判定（腕＝%s）===' % d)
        for name, ok, val in gates(r, comps): print('PASS' if ok else 'FAIL', name, val)
