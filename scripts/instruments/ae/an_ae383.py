"""(383) 行政効率と汚職の残りの削除＋扶養力の錨の半分の読み手（読むだけ）。事前登録 measurements/ae_20261017_prereg.txt の [5]・[8]・追記2〜4 の定義どおり。
使い方：python an_ae383.py A=<出力>/ins B=<出力>/ins N1=<出力>/ins N2=<出力>/ins [rA=<A の報告> rB=… rN1=… rN2=…] [sec=div,sup,r1,r2,r3,r4,ratio,asia,fz,pop,chaos]
  読む表＝nation_s・scalars_s・stock_s・anchor_s（<出力>/ins の *_s<種>.tsv）。報告（rX）は〈人口: 成長倍率〉と exact／near の行だけ読む。
  腕の結果を見てから式を書かない＝式は事前登録の定義だけ（閾値は追記4）。表が無ければ該当の節を飛ばして〈無し〉と印字する。
  記号：[止] ＝止める線（発散 ①'）／[記] ＝記録（採否に使わない・追記3 の（2））。
"""
import csv, glob, math, os, re, sys, collections

csv.field_size_limit(1 << 30)
YEAR_END = 1951


def fl(x, default=float('nan')):
    try:
        return float(x)
    except Exception:
        return default


def fin(x):
    return x == x and x not in (float('inf'), float('-inf'))


def median(v):
    v = sorted(v)
    n = len(v)
    if n == 0:
        return float('nan')
    return v[n // 2] if n % 2 else 0.5 * (v[n // 2 - 1] + v[n // 2])


def seeds_of(d, stem):
    out = []
    for f in glob.glob(os.path.join(d, stem + '_s*.tsv')):
        m = re.search(r'_s(\d+)\.tsv$', f)
        if m:
            out.append(int(m.group(1)))
    return sorted(out)


def read_tsv(path, cols):
    with open(path, encoding='utf-8', errors='replace', newline='') as fh:
        r = csv.DictReader(fh, delimiter='\t')
        for x in r:
            yield {c: x.get(c, '') for c in cols}


def p(*a):
    print(*a)


class Arm:
    def __init__(self, tag, d):
        self.tag, self.d = tag, d
        self._c = {}

    def table(self, stem, cols):
        """seed -> [row dict]（読み込みは表×列の組ごとに 1 回）"""
        key = (stem, tuple(cols))
        if key not in self._c:
            out = {}
            for s in seeds_of(self.d, stem):
                out[s] = list(read_tsv(os.path.join(self.d, f'{stem}_s{s}.tsv'), cols))
            self._c[key] = out
        return self._c[key]


NAT_COLS = ['quarter', 'year', 'nation', 'populationK', 'nominalGdpPerYear', 'supplyFillShare', 'priceLevel', 'bondRateMeanPerYear', 'bondMonthsCount',
            'directTaxNetMeanPerMonth', 'indirectRevenueMeanPerMonth']
SCA_COLS = ['quarter', 'year', 'nation', 'born', 'popK', 'educationLaw', 'bureaucracy', 'effCorruption', 'dynMandate', 'legitimacy',
            'adminEfficiency', 'adminPowerSmoothed', 'highAdminQuarters', 'landTaxUnlocked', 'crown', 'childLaborLegal', 'workHoursValue']


def load_arms(args):
    arms, reports = {}, {}
    secs = None
    for a in args:
        if '=' not in a:
            continue
        k, v = a.split('=', 1)
        if k == 'sec':
            secs = set(v.split(','))
        elif k.startswith('r') and k[1:] in ('A', 'B', 'N1', 'N2'):
            reports[k[1:]] = v
        elif k in ('A', 'B', 'N1', 'N2'):
            arms[k] = Arm(k, v)
    return arms, reports, secs


# ------------------------------------------------------------------ 発散（追記4 の（2））
def div_stats(arm):
    """populationK（≥100・有限）の種ごと国ごとの最大／切り上げ張り付き／NaN の数。"""
    nat = arm.table('nation', NAT_COLS)
    mx = collections.defaultdict(dict)   # nation -> seed -> max populationK
    tot = hit_bond = hit_pu = hit_pl = 0
    nan_pop = nan_price = nan_bond = excl_bond_nan = 0
    for s, rows in nat.items():
        for x in rows:
            pk, pr = fl(x['populationK']), fl(x['priceLevel'])
            if not fin(pk):
                nan_pop += 1
                continue
            if not fin(pr):
                nan_price += 1
            bm = fl(x['bondMonthsCount'], 0.0)
            br = fl(x['bondRateMeanPerYear'])
            if bm > 0 and not fin(br):
                nan_bond += 1
            if not (bm > 0) and not fin(br):
                excl_bond_nan += 1
            if pk < 100:
                continue
            tot += 1
            d = mx[x['nation']]
            if s not in d or pk > d[s]:
                d[s] = pk
            if bm > 0 and fin(br) and br >= 0.2999:
                hit_bond += 1
            if fin(pr) and pr >= 3.999:
                hit_pu += 1
            if fin(pr) and pr <= 0.4001:
                hit_pl += 1
    armval = {}
    for n, d in mx.items():
        if len(d) >= 3:
            armval[n] = median(list(d.values()))
    dropped = [n for n, d in mx.items() if len(d) < 3]
    return dict(armval=armval, dropped=len(dropped), tot=tot, bond=hit_bond / tot if tot else 0.0, pu=hit_pu / tot if tot else 0.0,
                pl=hit_pl / tot if tot else 0.0, nan_pop=nan_pop, nan_price=nan_price, nan_bond=nan_bond, excl_bond_nan=excl_bond_nan, seeds=len(nat))


def eval_div(test, refs, label):
    """test の統計が refs（list of stats）に対して発散かどうか。[(bool, 文)]"""
    res = []
    allref = [v for r in refs for v in r['armval'].values()]
    gmax = max(allref) if allref else float('nan')
    hits = []
    new_only = 0
    for n, v in test['armval'].items():
        rv = [r['armval'][n] for r in refs if n in r['armval']]
        if rv:
            lim = 10 * max(rv)
        else:
            lim = 10 * gmax
            new_only += 1
        if v > lim:
            hits.append((n, v, lim))
    res.append((bool(hits), f'populationK：腕の値が基準の 10 倍超の国 {len(hits)} 件（基準に居ない国 {new_only} か国は A 系の全ての国の最大の 10 倍＝{10 * gmax:.0f} と比較・3 種未満で除いた国 {test["dropped"]}）'
                + ''.join(f'　{n}={v:.0f}>{lim:.0f}' for n, v, lim in hits[:8])))
    for key, nm, floor in (('bond', '金利が上限(≥0.2999)', 0.01), ('pu', '値段が天井(≥3.999)', 0.01), ('pl', '値段が底(≤0.4001)', 0.0)):
        rs = max(r[key] for r in refs)
        lim = max(10 * rs, floor)
        res.append((test[key] >= lim and test[key] > 0, f'{nm}の張り付き率 {test[key] * 100:.3f}%（基準の最大 {rs * 100:.3f}%・線 {lim * 100:.3f}%）'))
    nanhit = test['nan_pop'] + test['nan_price'] + test['nan_bond']
    res.append((nanhit > 0, f'NaN／無限大（populationK {test["nan_pop"]}・priceLevel {test["nan_price"]}・bondMonths>0 の金利 {test["nan_bond"]}）／除外した計器の NaN（bondMonths=0 の金利）{test["excl_bond_nan"]} 行'))
    return res


def sec_div(arms):
    p('=== 発散 ①\'（追記4 の（2））[止] ===')
    st = {k: div_stats(a) for k, a in arms.items()}
    for k, s in st.items():
        p(f'  {k}：種 {s["seeds"]}・国×四半期(人口100以上) {s["tot"]}・金利上限 {s["bond"] * 100:.3f}%・値段天井 {s["pu"] * 100:.3f}%・値段底 {s["pl"] * 100:.3f}%・'
          f'NaN(pop {s["nan_pop"]}・price {s["nan_price"]}・bond>0 {s["nan_bond"]})・除外した金利NaN {s["excl_bond_nan"]}・3種以上居る国 {len(s["armval"])}・除いた国 {s["dropped"]}')
    order = [k for k in ('A', 'N1', 'N2', 'B') if k in st]
    # ヌル腕の確かめ
    for t in ('N1', 'N2', 'B'):
        if t not in st:
            continue
        refs = [st[k] for k in ('A', 'N1', 'N2') if k in st and k != t]
        if not refs:
            continue
        p(f'  --- 試験：{t} を {"+".join(k for k in ("A", "N1", "N2") if k in st and k != t)} と比べる ---')
        for hit, msg in eval_div(st[t], refs, t):
            p(f'    {"★当たり" if hit else "　外れ  "}　{msg}')
    nullhit = False
    for t in ('N1', 'N2'):
        if t in st:
            refs = [st[k] for k in ('A', 'N1', 'N2') if k in st and k != t]
            if refs and any(h for h, _ in eval_div(st[t], refs, t)):
                nullhit = True
    if 'B' in st:
        refs = [st[k] for k in ('A', 'N1', 'N2') if k in st]
        bh = [m for h, m in eval_div(st['B'], refs, 'B') if h]
        if bh and nullhit:
            p('  判定：B が当たったがヌル腕も当たった＝この線は採否に使わず記録（追記4）')
        elif bh:
            p('  判定：★B が発散の線に当たり、ヌル腕は当たらない＝止める線（B1／B2 で切り分け）')
        else:
            p('  判定：B は発散の線に当たらない（ヌル腕の当たり＝' + ('あり' if nullhit else 'なし') + '）')


# ------------------------------------------------------------------ 補給崩壊（記録）
def sec_sup(arms):
    p('=== 補給崩壊（人口100以上・supplyFillShare<0.3 の国×四半期の件数・種ごと）[記] ===')
    for k, a in arms.items():
        nat = a.table('nation', NAT_COLS)
        per = {}
        for s, rows in nat.items():
            per[s] = sum(1 for x in rows if fl(x['populationK']) >= 100 and fl(x['supplyFillShare']) < 0.3)
        v = [per[s] for s in sorted(per)]
        p(f'  {k}：合計 {sum(v)}・千件以上の種 {sum(1 for x in v if x >= 1000)}/{len(v)}・種ごと {v}')


# ------------------------------------------------------------------ R1 税÷GDP
def sec_r1(arms):
    p('=== R1 税÷GDP（窓 1850〜1914）英と清 [記・ずれ] ===')
    for k, a in arms.items():
        nat = a.table('nation', NAT_COLS)
        rs, xs, skipped = [], collections.defaultdict(list), 0
        for s, rows in nat.items():
            m = {}
            for nm in ('イギリス', '清'):
                v = []
                for x in rows:
                    y = fl(x['year'])
                    g = fl(x['nominalGdpPerYear'])
                    if x['nation'] == nm and 1850 <= y <= 1914 and g > 0:
                        v.append((fl(x['directTaxNetMeanPerMonth']) + fl(x['indirectRevenueMeanPerMonth'])) * 12 / g)
                m[nm] = v
            if len(m['イギリス']) < 130 or len(m['清']) < 130:
                skipped += 1
                continue
            e, c = sum(m['イギリス']) / len(m['イギリス']), sum(m['清']) / len(m['清'])
            xs['イギリス'].append(e)
            xs['清'].append(c)
            if c > 0:
                rs.append(e / c)
        p(f'  {k}：英 {median(xs["イギリス"]):.4f}・清 {median(xs["清"]):.4f}・英÷清 の種の中央値 {median(rs):.3f}（種 {len(rs)}・窓の行が半分未満で除いた種 {skipped}）')


# ------------------------------------------------------------------ R2・R3 年
def first_year(rows, nation, col, thr):
    for x in rows:
        if x['nation'] == nation and fl(x[col]) >= thr:
            return fl(x['year'])
    return YEAR_END


def sec_r23(arms, which):
    if which == 'r2':
        p('=== R2 専門官僚制の採用の年（種の中央値）[記・ずれ]（普は bureaucracy=2・英と米は ≧1）===')
        items = [('プロイセン', 'bureaucracy', 2), ('イギリス', 'bureaucracy', 1), ('アメリカ', 'bureaucracy', 1)]
    else:
        p('=== R3 教育法が Public(2) 以上に届く年（種の中央値）[記・ずれ] ===')
        items = [(n, 'educationLaw', 2) for n in ('オスマン', 'ロシア', '清', '日本')]
    for k, a in arms.items():
        sca = a.table('scalars', SCA_COLS)
        out = []
        for nm, col, thr in items:
            ys = [first_year(rows, nm, col, thr) for rows in sca.values()]
            out.append(f'{nm}={median(ys):.0f}（到達しない種 {sum(1 for y in ys if y >= YEAR_END)}/{len(ys)}）')
        p(f'  {k}：' + '　'.join(out))


# ------------------------------------------------------------------ R4 と張り付き率
def sec_r4(arms):
    p('=== R4 法の浸透（ChildLaborLegal・WorkHoursValue の種の中央）[記] と退化の確かめ ===')
    for k, a in arms.items():
        sca = a.table('scalars', SCA_COLS)
        tot = hi = eff1 = 0
        for rows in sca.values():
            for x in rows:
                y = fl(x['year'])
                if 1850 <= y <= 1914 and fl(x['popK']) >= 100:
                    tot += 1
                    if fl(x['adminPowerSmoothed']) >= 0.5:
                        hi += 1
                    if fl(x['adminEfficiency']) >= 1 - 1e-9:
                        eff1 += 1
        p(f'  {k}：窓1850〜1914・人口100以上 {tot} 行・adminPowerSmoothed≥0.5 {hi / tot * 100 if tot else float("nan"):.1f}%・adminEfficiency≥1−1e-9 {eff1 / tot * 100 if tot else float("nan"):.1f}%')
        for nm in ('イギリス', 'フランス', 'プロイセン', '清', '日本'):
            row = []
            for yr in (1850, 1880, 1914, 1950):
                cl, wh = [], []
                for rows in sca.values():
                    last = None
                    for x in rows:
                        if x['nation'] == nm and fl(x['year']) == yr:
                            last = x
                    if last is not None:
                        cl.append(fl(last['childLaborLegal']))
                        wh.append(fl(last['workHoursValue']))
                row.append(f'{yr}:児童労働{median(cl):.2f}/労働時間{median(wh):.2f}')
            p(f'    {nm}　' + '　'.join(row))
    # 労働時間 1914 の外れ
    if 'B' in arms:
        out = []
        def wh1914(a, nm):
            v = []
            for rows in a.table('scalars', SCA_COLS).values():
                last = None
                for x in rows:
                    if x['nation'] == nm and fl(x['year']) == 1914:
                        last = x
                if last is not None:
                    v.append(fl(last['workHoursValue']))
            return median(v)
        cnt = 0
        for nm in ('イギリス', 'フランス', 'プロイセン', '清', '日本'):
            ref = [wh1914(arms[k], nm) for k in ('A', 'N1', 'N2') if k in arms]
            ref = [x for x in ref if fin(x)]
            b = wh1914(arms['B'], nm)
            if ref and fin(b) and not (min(ref) <= b <= max(ref)):
                cnt += 1
                out.append(f'{nm}(B={b:.2f}・A系 {min(ref):.2f}〜{max(ref):.2f})')
        p(f'  年1914 の労働時間の中央が B で A 系の幅の外の国 {cnt}：' + '　'.join(out))


# ------------------------------------------------------------------ G-asia
def sec_asia(arms):
    p('=== G-asia：techLevelIndex の窓 1850〜1950 の種ごとの最大（国の中央値の上位8）[記] ===')
    for k, a in arms.items():
        st = a.table('stock', ['year', 'nation', 'techLevelIndex'])
        mx = collections.defaultdict(dict)
        for s, rows in st.items():
            for x in rows:
                y, v = fl(x['year']), fl(x['techLevelIndex'])
                if 1850 <= y <= 1950 and fin(v):
                    d = mx[x['nation']]
                    if s not in d or v > d[s]:
                        d[s] = v
        med = sorted(((median(list(d.values())), n, len(d)) for n, d in mx.items() if len(d) >= 3), reverse=True)
        p(f'  {k}：' + '　'.join(f'{n}={v:.2f}({c}種)' for v, n, c in med[:8]))


# ------------------------------------------------------------------ 群 F・錨
def sec_fz(arms):
    p('=== 群 F（凍った錨：born=0 かつ anchorOld＞anchorRegions×(1+1e-6) かつ popK≥100 の行の補給崩壊）[記] ===')
    for k, a in arms.items():
        try:
            anc = a.table('anchor', ['quarter', 'year', 'nation', 'born', 'popK', 'anchorOld', 'anchorRegions', 'flag'])
        except Exception as e:
            p(f'  {k}：anchor_s 無し（{e}）')
            continue
        nat = a.table('nation', NAT_COLS)
        rows_f = cols_f = 0
        for s, rows in anc.items():
            sup = {}
            for x in nat.get(s, []):
                sup[(x['quarter'], x['nation'])] = fl(x['supplyFillShare'])
            for x in rows:
                if x['born'] == '0' and fl(x['popK']) >= 100 and fl(x['anchorOld']) > fl(x['anchorRegions']) * (1 + 1e-6):
                    rows_f += 1
                    if sup.get((x['quarter'], x['nation']), 1.0) < 0.3:
                        cols_f += 1
        p(f'  {k}：群 F の行 {rows_f}・うち補給崩壊 {cols_f}（anchor_s は挙げた国が四半期・他は年末）')


# ------------------------------------------------------------------ 人口の倍率と距離 D・exact／near（報告）
POP_OK = {'フランス': (1.25, 1.7), 'オーストリア': (1.8, 2.6), 'イギリス': (2.6, 4.3), 'スペイン': (1.6, 2.2), 'ポルトガル': (1.5, 2.0), '日本': (1.7, 2.4), '清': (1.1, 1.9)}


def pop_growth(path):
    for line in open(path, encoding='utf-8', errors='replace'):
        if line.strip().startswith('人口: 成長倍率の一致'):
            return {n: float(v) for n, v in re.findall(r'([^\s×]+)×([0-9.]+)', line)}
    return {}


def exact_near(path):
    for line in open(path, encoding='utf-8', errors='replace'):
        m = re.search(r'史実\(exact\)\s*([0-9.]+)\s*%\s*/\s*近い\(near\)\s*([0-9.]+)\s*%', line)
        if m:
            return float(m.group(1)), float(m.group(2))
    return None


def sec_pop(reports):
    p('=== 人口の倍率（1783→1914・10種の平均）と距離 D・exact／near [記・ずれ] ===')
    ds = {}
    for k, path in reports.items():
        g = pop_growth(path)
        d = 0.0
        for n, (lo, hi) in POP_OK.items():
            if n in g:
                v = g[n]
                if v < lo:
                    d += abs(math.log(v / lo))
                elif v > hi:
                    d += abs(math.log(v / hi))
        ds[k] = d
        en = exact_near(path)
        p(f'  {k}：D={d:.3f}　' + '　'.join(f'{n}×{g[n]:.2f}' for n in POP_OK if n in g) + (f'　exact {en[0]}・near {en[1]}' if en else ''))
    ref = [ds[k] for k in ('A', 'N1', 'N2') if k in ds]
    if 'B' in ds and ref:
        margin = abs(ds.get('N1', 0) - ds.get('N2', 0)) if 'N1' in ds and 'N2' in ds else 0
        p(f'  B の D={ds["B"]:.3f}・A系の最大 {max(ref):.3f}＋ヌルの開き {margin:.3f}＝{max(ref) + margin:.3f}（ずれの記録：{"あり" if ds["B"] > max(ref) + margin else "なし"}）')


def main():
    arms, reports, secs = load_arms(sys.argv[1:])
    want = lambda s: secs is None or s in secs
    if want('div') and arms:
        sec_div(arms)
    if want('sup') and arms:
        sec_sup(arms)
    if want('r1') and arms:
        sec_r1(arms)
    if want('r2') and arms:
        sec_r23(arms, 'r2')
    if want('r3') and arms:
        sec_r23(arms, 'r3')
    if want('r4') and arms:
        sec_r4(arms)
    if want('asia') and arms:
        sec_asia(arms)
    if want('fz') and arms:
        sec_fz(arms)
    if want('pop') and reports:
        sec_pop(reports)


if __name__ == '__main__':
    main()
