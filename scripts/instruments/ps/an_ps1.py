"""(373) 政治のスカラーの削除の読み手（読むだけ）。事前登録 measurements/ps_20261016_prereg.txt の [6]・追記1〜3 の定義どおり。
使い方：python an_ps1.py A=<出力>/ins B=<出力>/ins N1=<出力>/ins N2=<出力>/ins [rA=<A の報告> rB=… rN1=… rN2=…]
  読む表＝scalars_s<種>.tsv（M1・M2・H2・R3・R5・R6）／nation_s<種>.tsv（補給崩壊・H1・H1w・H3・R5）。表が無ければ該当の節を飛ばして〈無し〉と印字する。
  腕の結果を見てから式を書かない＝式は事前登録の定義だけ（幅の数字も事前登録）。
"""
import csv, glob, math, os, re, sys, collections

csv.field_size_limit(1 << 30)
YEAR_END = 1951  # H2：教育法が Public 以上へ届かない種（終局の年＋1）

H1_EVENTS = [
    ('a フランス 1789-1799', 'フランス', 1789, 1799),
    ('b ロシア 1905-1920', 'ロシア', 1905, 1920),
    ('c オーストリア 1917-1921', 'オーストリア', 1917, 1921),
    ('d プロイセン 1917-1921', 'プロイセン', 1917, 1921),
    ('e オスマン 1908-1923', 'オスマン', 1908, 1923),
    ('f 清 1910-1915', '清', 1910, 1915),
]
H2_NATIONS = ['清', 'オスマン', 'ロシア', '日本']
R5_NATIONS = ['イギリス', 'フランス', 'ロシア', '清', 'オスマン', '日本', 'プロイセン', 'オーストリア']
R5_YEARS = [1783, 1850, 1914, 1950]


def fl(x, default=float('nan')):
    try:
        return float(x)
    except Exception:
        return default


def median(v):
    v = sorted(v)
    n = len(v)
    if n == 0:
        return float('nan')
    return v[n // 2] if n % 2 else 0.5 * (v[n // 2 - 1] + v[n // 2])


def pct(v, p):
    v = sorted(v)
    if not v:
        return float('nan')
    return v[max(0, math.ceil(p * len(v)) - 1)]


def seeds_of(d, stem):
    out = []
    for f in glob.glob(os.path.join(d, stem + '_s*.tsv')):
        m = re.search(r'_s(\d+)\.tsv$', f)
        if m:
            out.append(int(m.group(1)))
    return sorted(out)


def read_tsv(path, cols=None):
    with open(path, encoding='utf-8', errors='replace', newline='') as fh:
        r = csv.DictReader(fh, delimiter='\t')
        for x in r:
            yield x if cols is None else {c: x.get(c, '') for c in cols}


class Arm:
    def __init__(self, tag, d):
        self.tag, self.d = tag, d
        self.sca = {}   # seed -> [row dict]
        self.nat = {}   # seed -> [row dict (few cols)]
        for s in seeds_of(d, 'scalars'):
            self.sca[s] = list(read_tsv(os.path.join(d, f'scalars_s{s}.tsv')))
        cols = ['quarter', 'year', 'nationIndex', 'nation', 'populationK', 'atWarMonths', 'supplyFillShare', 'polityCode']
        for s in seeds_of(d, 'nation'):
            self.nat[s] = list(read_tsv(os.path.join(d, f'nation_s{s}.tsv'), cols))
        self.m = {}  # 数値の指標（比較用）

    # ---------------- 補給崩壊（人口 100 以上・supplyFillShare < 0.3） ----------------
    def supply(self):
        per = {}
        for s, rows in self.nat.items():
            c = 0
            for x in rows:
                if fl(x['populationK']) >= 100 and fl(x['supplyFillShare']) < 0.3:
                    c += 1
            per[s] = c
        return per

    # ---------------- nation_s の系列（種×国 index） ----------------
    def series(self):
        out = {}
        for s, rows in self.nat.items():
            by = collections.defaultdict(list)
            for x in rows:
                by[x['nationIndex']].append(x)
            for k in by:
                by[k].sort(key=lambda x: int(fl(x['quarter'], 0)))
            out[s] = by
        return out


def p(*a):
    print(*a)


def load_arms(args):
    arms = {}
    reports = {}
    for a in args:
        if '=' not in a:
            continue
        k, v = a.split('=', 1)
        if k.startswith('r') and k[1:] in ('A', 'B', 'N1', 'N2'):
            reports[k[1:]] = v
        elif k in ('A', 'B', 'N1', 'N2'):
            arms[k] = Arm(k, v)
    return arms, reports


# ---------------------------------------------------------------- M1 / M2
def sec_m1(arms):
    p('=== M1：旗が効いている（旗が 1 の行だけで、その旗の列が 0）===')
    p('  （追記4：切り分け腕でも読めるようテーマごとに〈その旗が 1 の行〉だけを見る。閾値は変えない。flagD の列が無い表〔制度疲労の削除後の木〕は該当の節を飛ばす）')
    for tag, a in arms.items():
        if not a.sca:
            p(f'  {tag}: scalars_s 無し')
            continue
        rows = [x for r in a.sca.values() for x in r]
        n = len(rows)
        res = []
        for flag, label, cols in (('flagD', '疲労', ('decay', 'decayCeiling')), ('flagC', '汚職', ('dmGraft', 'corrErosion')), ('flagW', '厭戦', ('milWarMean', 'dmWar'))):
            if flag not in rows[0]:
                res.append(f'{label}: 列なし')
                continue
            on = [x for x in rows if fl(x[flag]) == 1]
            off = [x for x in rows if fl(x[flag]) == 0]
            bad = sum(1 for x in on if not all(fl(x[c]) == 0 for c in cols))
            ctrl = sum(1 for x in off if not all(fl(x[c]) == 0 for c in cols))
            res.append(f'{label}: 旗1の行 {len(on)}（0でない {bad}）・旗0の行 {len(off)}（0でない対照 {ctrl}）')
        p(f'  {tag}: 行 {n}・' + '・'.join(res))
    b = arms.get('B')
    if b and b.sca:
        rows = [x for r in b.sca.values() for x in r]
        ok = True
        for flag, cols in (('flagD', ('decay', 'decayCeiling')), ('flagC', ('dmGraft', 'corrErosion')), ('flagW', ('milWarMean', 'dmWar'))):
            if flag not in rows[0]:
                continue
            on = [x for x in rows if fl(x[flag]) == 1]
            if on and any(not all(fl(x[c]) == 0 for c in cols) for x in on):
                ok = False
        p(f'  M1 判定（B の腕）：{"合格" if ok else "不合格"}')


def sec_m2(arms):
    p('=== M2：何もしない機構でない（A の scalars_s）===')
    a = arms.get('A')
    if not a or not a.sca:
        p('  A: scalars_s 無し')
        return
    rows = [x for r in a.sca.values() for x in r]
    c_decay = sum(1 for x in rows if fl(x['decay']) > 0.05)
    c_war = sum(1 for x in rows if x['atWar'] == '1' and fl(x['warExhaustion']) > 0.05 and fl(x['milWarMean']) > 0)
    c_graft = sum(1 for x in rows if fl(x['dmGraft']) < 0)
    c_ero = sum(1 for x in rows if fl(x['corrErosion']) > 0)
    c_dmwar = sum(1 for x in rows if fl(x['dmWar']) < 0)
    p(f'  行 {len(rows)}')
    p(f'  decay>0.05: {c_decay}（登録時 ≧1000: {"合格" if c_decay >= 1000 else "不合格"}・追記1 ≧100: {"合格" if c_decay >= 100 else "不合格"}）')
    p(f'  atWar&warExh>0.05&milWarMean>0: {c_war}（≧500: {"合格" if c_war >= 500 else "不合格"}）')
    p(f'  dmGraft<0: {c_graft}（≧1000: {"合格" if c_graft >= 1000 else "不合格"}）')
    p(f'  corrErosion>0: {c_ero}（≧1000: {"合格" if c_ero >= 1000 else "不合格"}）')
    p(f'  （参考）dmWar<0: {c_dmwar}')
    # 初期の疲労の国別（開幕の過渡）
    first = {}
    for s, r in a.sca.items():
        for x in r:
            if x['quarter'] == '0':
                first.setdefault(x['nation'], []).append(fl(x['decay']))
    top = sorted(((median(v), k) for k, v in first.items()), reverse=True)[:6]
    p('  開幕の decay の中央値（上位）：' + '・'.join(f'{k} {v:.3f}' for v, k in top))


# ---------------------------------------------------------------- 補給崩壊（止める線・R2）
def sec_supply(arms):
    p('=== R2：補給崩壊（人口 100 以上・supplyFillShare<0.3）===')
    res = {}
    for tag, a in arms.items():
        if not a.nat:
            p(f'  {tag}: nation_s 無し')
            continue
        per = a.supply()
        tot = sum(per.values())
        k1000 = sum(1 for v in per.values() if v >= 1000)
        res[tag] = (tot, k1000)
        a.m['supply_total'] = tot
        p(f'  {tag}: 合計 {tot}・千件級の種 {k1000}/{len(per)}・種ごと {[per[s] for s in sorted(per)]}')
    if all(t in res for t in ('A', 'N1', 'N2', 'B')):
        mx = max(res[t][0] for t in ('A', 'N1', 'N2'))
        stop1 = res['B'][1] >= 2
        stop2 = res['B'][0] > 2 * mx  # 頻度が倍以上（max(A,N1,N2) の 2 倍）
        p(f'  止める線：B の千件級 ≧2/10 → {"掛かる" if stop1 else "掛からない"}／〈壊す〉の物差し（頻度が max(A,N1,N2) の 2 倍超）→ {"超える" if stop2 else "超えない"}（記録）')


# ---------------------------------------------------------------- H1
def first_index_by_name(series):
    """四半期 0 の名前 → nationIndex（種ごと）。"""
    out = {}
    for s, by in series.items():
        m = {}
        for k, rows in by.items():
            if rows and rows[0]['quarter'] == '0':
                m[rows[0]['nation']] = k
        out[s] = m
    return out


def h1_counts(a):
    """事象ごとに (k_any, k_away, n_alive) を返す。"""
    ser = a.series()
    idx = first_index_by_name(ser)
    res = {}
    for label, nm, y0, y1 in H1_EVENTS:
        k_any = k_away = n_alive = 0
        for s, by in ser.items():
            k = idx[s].get(nm)
            if k is None:
                continue
            rows = by[k]
            if any(y0 <= int(fl(x['year'], 0)) <= y1 for x in rows):
                n_alive += 1
            first_pol = rows[0]['polityCode']
            ev_any = ev_away = False
            for i in range(1, len(rows)):
                y = int(fl(rows[i]['year'], 0))
                if y0 <= y <= y1 and rows[i]['polityCode'] != rows[i - 1]['polityCode']:
                    ev_any = True
                    if rows[i]['polityCode'] != first_pol:
                        ev_away = True
            k_any += ev_any
            k_away += ev_away
        res[label] = (k_any, k_away, n_alive)
    return res


def war_transitions(a):
    """交戦中（atWarMonths>0 の四半期または直前の四半期）の polityCode 遷移の総数。"""
    ser = a.series()
    tot = 0
    for s, by in ser.items():
        for k, rows in by.items():
            for i in range(1, len(rows)):
                if rows[i]['polityCode'] != rows[i - 1]['polityCode'] and (fl(rows[i]['atWarMonths'], 0) > 0 or fl(rows[i - 1]['atWarMonths'], 0) > 0):
                    tot += 1
    return tot


def war_militancy_median(a):
    v = []
    for r in a.sca.values():
        for x in r:
            if x['atWar'] == '1':
                v.append(fl(x['militancyMean']))
    return median([z for z in v if not math.isnan(z)])


def sec_h1(arms):
    p('=== H1：政体の遷移（nation_s の polityCode）===')
    need = ('A', 'B', 'N1', 'N2')
    if not all(t in arms and arms[t].nat for t in need):
        p('  nation_s が足りない腕がある＝飛ばす')
        return
    cnt = {t: h1_counts(arms[t]) for t in need}
    p('  事象（k_any/k_away/n_alive）  A | B | N1 | N2  → min(基線)−2 と判定')
    n_det = 0
    n_fail = 0
    undet = []
    for label, *_ in H1_EVENTS:
        ks = {t: cnt[t][label] for t in need}
        base_min = min(ks[t][0] for t in ('A', 'N1', 'N2'))
        thr = base_min - 2
        if thr <= 0:
            verdict = '判定不能'
            undet.append(label.split()[0])
        else:
            n_det += 1
            ok = ks['B'][0] >= thr
            verdict = '合格' if ok else '不合格'
            n_fail += (not ok)
        p(f'  {label}: ' + ' | '.join(f'{t} {ks[t][0]}/{ks[t][1]}/{ks[t][2]}' for t in need) + f'  → 閾値 {thr}・{verdict}')
    allowed = (n_det // 6)
    p(f'  判定できる事象 {n_det} 件・落ちた {n_fail} 件・許される落ち {allowed} 件（n/6 未満＝n=6 で 1 件・n<6 で 0 件）')
    p(f'  H1 判定：{"合格" if n_fail <= allowed and n_det > 0 else ("判定不能（判定できる事象が無い）" if n_det == 0 else "不合格")}')
    # H1w
    bcd = {'b', 'c', 'd'}
    if bcd <= set(undet):
        p('  (b)(c)(d) が全て判定不能＝補助の線 H1w で厭戦の内政の効きを読む')
        wt = {t: war_transitions(arms[t]) for t in need}
        wm = {t: war_militancy_median(arms[t]) for t in need} if all(arms[t].sca for t in need) else None
        base = [wt[t] for t in ('A', 'N1', 'N2')]
        mean = sum(base) / 3
        tol = max(2 * abs(wt['N1'] - wt['N2']), 0.3 * mean)
        ok1 = abs(wt['B'] - mean) <= tol
        p(f'  H1w 交戦中の遷移の総数：A {wt["A"]}・B {wt["B"]}・N1 {wt["N1"]}・N2 {wt["N2"]}・基線平均 {mean:.1f}±{tol:.1f} → {"合格" if ok1 else "不合格"}')
        if wm:
            base = [wm[t] for t in ('A', 'N1', 'N2')]
            mean = sum(base) / 3
            tol = max(2 * abs(wm['N1'] - wm['N2']), 0.1 * mean)
            ok2 = abs(wm['B'] - mean) <= tol
            p(f'  H1w 交戦中の militancyMean の中央値：A {wm["A"]:.4f}・B {wm["B"]:.4f}・N1 {wm["N1"]:.4f}・N2 {wm["N2"]:.4f}・基線平均 {mean:.4f}±{tol:.4f} → {"合格" if ok2 else "不合格"}')
            p(f'  H1w 判定：{"合格" if ok1 and ok2 else "不合格"}')
        else:
            p('  H1w の militancyMean：scalars_s 無し')
    else:
        wt = {t: war_transitions(arms[t]) for t in need}
        p('  （参考）交戦中の遷移の総数：' + '・'.join(f'{t} {wt[t]}' for t in need))


# ---------------------------------------------------------------- H2
def h2_years(a, nation):
    out = []
    unreached = 0
    for s, rows in sorted(a.sca.items()):
        y = None
        for x in sorted((x for x in rows if x['nation'] == nation), key=lambda x: int(fl(x['quarter'], 0))):
            if fl(x['educationLaw'], 0) >= 2:
                y = int(fl(x['year'], 0))
                break
        if y is None:
            y = YEAR_END
            unreached += 1
        out.append(y)
    return out, unreached


def sec_h2(arms):
    p('=== H2：教育法が Public 以上へ初めて変わる年の中央値（届かない種は 1951）===')
    need = ('A', 'B', 'N1', 'N2')
    if not all(t in arms and arms[t].sca for t in need):
        p('  scalars_s が足りない腕がある＝飛ばす')
        return
    ok_n = 0
    early = []
    for nm in H2_NATIONS:
        med = {}
        un = {}
        for t in need:
            ys, u = h2_years(arms[t], nm)
            med[t] = median(ys)
            un[t] = u
        tol = max(15, 2 * abs(med['N1'] - med['N2']))
        ok = abs(med['B'] - med['A']) <= tol
        ok_n += ok
        if not ok and med['B'] < med['A']:
            early.append(nm)
        p(f'  {nm}: ' + ' | '.join(f'{t} {med[t]:.1f}（届かない {un[t]}）' for t in need) + f'  |B−A|={abs(med["B"] - med["A"]):.1f}・幅 {tol:.1f} → {"合格" if ok else "不合格"}')
    p(f'  H2 判定：{ok_n}/4 国が合格 → {"合格" if ok_n >= 3 else "不合格"}')
    if early:
        p(f'  ★B が A より前倒しで外れた国（確証のある向きの後退）：{"・".join(early)}')


# ---------------------------------------------------------------- H3
def war_clumps(a):
    ser = a.series()
    out = []
    for s, by in ser.items():
        for k, rows in by.items():
            run = 0
            for x in rows:
                if fl(x['atWarMonths'], 0) > 0:
                    run += 1
                else:
                    if run:
                        out.append(run)
                    run = 0
            if run:
                out.append(run)
    return out


def sec_h3(arms):
    p('=== H3：戦争の長さ（nation_s の atWarMonths>0 が連続する四半期数＝交戦の塊）===')
    need = ('A', 'B', 'N1', 'N2')
    if not all(t in arms and arms[t].nat for t in need):
        p('  nation_s が足りない腕がある＝飛ばす')
        return
    st = {}
    for t in need:
        c = war_clumps(arms[t])
        st[t] = (len(c), median(c), pct(c, 0.9))
        arms[t].m['war_clumps'] = len(c)
        arms[t].m['war_med'] = st[t][1]
        p(f'  {t}: 塊 {st[t][0]}・中央値 {st[t][1]}・上位 10%（p90） {st[t][2]}')
    ok = True
    for i, nm in ((1, '中央値'), (2, '上位10%')):
        base = [st[t][i] for t in ('A', 'N1', 'N2')]
        lo, hi = 0.8 * min(base), 1.25 * max(base)
        good = lo <= st['B'][i] <= hi
        ok &= good
        p(f'  {nm}：B {st["B"][i]}・許す範囲 [{lo:.2f}, {hi:.2f}] → {"合格" if good else "不合格"}')
    p(f'  H3 判定：{"合格" if ok else "不合格"}')


# ---------------------------------------------------------------- R3 / R5 / R6
def sec_r3(arms):
    p('=== R3：アヘン戦争型の敗北（清が 1838〜1846 に defeatShock ≧ 0.5 の行がある種の数）===')
    res = {}
    for t, a in arms.items():
        if not a.sca:
            continue
        k = 0
        for s, rows in a.sca.items():
            if any(x['nation'] == '清' and 1838 <= int(fl(x['year'], 0)) <= 1846 and fl(x['defeatShock']) >= 0.5 for x in rows):
                k += 1
        res[t] = (k, len(a.sca))
    p('  ' + '・'.join(f'{t} {k}/{n}' for t, (k, n) in res.items()))
    if 'A' in res and 'B' in res and res['A'][0] * 10 >= 3 * res['A'][1] and res['B'][0] == 0:
        p('  ★A で 3/10 以上かつ B で 0/10＝アヘン戦争の創発が疲労の軍の質の経路に依存していた（報告の先頭に書く）')


def sec_r5(arms):
    p('=== R5：国別の人口（千人）の中央値（種をまたぐ）===')
    for nm in R5_NATIONS:
        line = []
        for t in ('A', 'B', 'N1', 'N2'):
            a = arms.get(t)
            if not a or not a.nat:
                continue
            vals = []
            for y in R5_YEARS:
                v = []
                for s, rows in a.nat.items():
                    for x in rows:
                        if x['nation'] == nm and int(fl(x['year'], 0)) == y:
                            v.append(fl(x['populationK']))
                            break
                vals.append(median(v))
            line.append(f'{t} ' + '/'.join(f'{z:.0f}' for z in vals))
        p(f'  {nm}（{"/".join(map(str, R5_YEARS))}）: ' + ' | '.join(line))


def sec_r6(arms):
    p('=== R6：実効汚職・天命・正統性（scalars_s）===')
    for t, a in arms.items():
        if not a.sca:
            continue
        rows = [x for r in a.sca.values() for x in r]
        ec = sorted(fl(x['effCorruption']) for x in rows)
        thr = ec[int(len(ec) * 2 / 3)] if ec else float('nan')
        hi = [x for x in rows if fl(x['effCorruption']) >= thr]
        dm = [fl(x['dynMandate']) for x in rows]
        dmh = [fl(x['dynMandate']) for x in hi]
        lg = [fl(x['legitimacy']) for x in rows]
        p(f'  {t}: 実効汚職の平均 {sum(fl(x["effCorruption"]) for x in rows) / len(rows):.4f}・天命の平均 {sum(dm) / len(dm):.4f}（汚職上位1/3の行 {sum(dmh) / max(1, len(dmh)):.4f}）・天命≧0.99 の行 {sum(1 for z in dm if z >= 0.99) / len(dm):.3%}・正統性の平均 {sum(lg) / len(lg):.4f}・militancyMean の平均 {sum(fl(x["militancyMean"]) for x in rows) / len(rows):.5f}')


def sec_r4(arms):
    p('=== R4：カオス性（|B−A| ÷ |N1−N2|・3 以下なら B の効きはカオスの雑音の内）===')
    for key, lab in (('supply_total', '補給崩壊の総数'), ('war_clumps', '交戦の塊の数'), ('war_med', '交戦の長さの中央値')):
        try:
            v = {t: arms[t].m[key] for t in ('A', 'B', 'N1', 'N2')}
        except KeyError:
            continue
        d = abs(v['N1'] - v['N2'])
        r = abs(v['B'] - v['A']) / d if d else float('inf')
        p(f'  {lab}: A {v["A"]}・B {v["B"]}・N1 {v["N1"]}・N2 {v["N2"]}・比 {r:.2f}')


def sec_reports(reports):
    p('=== R1：成績表の抜粋（exact・near・兌換停止・国家の出入り）===')
    pats = ['政体exact:', '政体near:', '金本位の強制離脱', '国家の出入り', 'ハーネス不合格を含む']
    for t, path in reports.items():
        try:
            txt = open(path, encoding='utf-8', errors='replace').read().split('\n')
        except Exception as e:
            p(f'  {t}: 読めない {e}')
            continue
        p(f'  [{t}]')
        for ln in txt:
            if any(q in ln for q in pats):
                p('    ' + ln.strip()[:160])


def main():
    arms, reports = load_arms(sys.argv[1:])
    if not arms:
        print(__doc__)
        return
    sec_m1(arms)
    sec_m2(arms)
    sec_supply(arms)
    sec_h1(arms)
    sec_h2(arms)
    sec_h3(arms)
    sec_r3(arms)
    sec_r4(arms)
    sec_r5(arms)
    sec_r6(arms)
    if reports:
        sec_reports(reports)


if __name__ == '__main__':
    main()
