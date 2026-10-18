#!/usr/bin/env python
"""(408) 行政費の返り：止める線 ⑥①〜④ と記録の線 R1・R2・R5'・R7 の読み手（事前登録 measurements/ar_20261018_prereg.txt [4][5][11]）。
使い方：python stop.py <A の ins> <N1 の ins> <N2 の ins> <B の ins>   （各 <出力>/ins・計器つき 10 種の生）
  ⑥① 同じ国が毎回壊れる：国（nationIndex）×腕で〈人口か州の数〔nation_s の core＋integrated＋colonial〕が走る最大の半分を割る or 消える〉種の数。B が 8 以上かつ A・N1・N2 の最大が 2 以下の国があれば鳴る。
  ⑥② 一国の発散：年末（1850・1900・1950）の首位の国の世界の実質GDP の取り分。B の中央値が A・N1・N2 の最大を（A・N1・N2 の幅の 2 倍）超えて超え、かつ 8 以上の種で同じ国なら鳴る。
  ⑥③ 国庫の枯渇：fiscal_s（挙げた 8 国・月）の treasuryEndStock ≤ 0 の行数。B が A・N1・N2 の最大の 2 倍以上、かつ差が全行の 0.5% 以上なら鳴る（ヌルの最大が 0 のときも差の床で鳴る）。
  ⑥④ 歳入の崩れ：開幕〜1800 の税÷GDP（nation_s の revenueBaseMeanPerMonth×12÷nominalGdpPerYear）の中央値（種×四半期）が A に対して 0.7 倍を下回る国が 3 国以上なら鳴る
       （⚠形の作りの上で届け率の比は 0.77 を下回れない＝この線が鳴らなかったことは形の良し悪しの証拠にしない）。
  出力は標準出力のテキスト。採否の機械的な判定ではなく、数字を全部出す（判定行は〈鳴る〉〈鳴らない〉）。
"""
import csv, functools, glob, os, statistics, sys
from collections import defaultdict

NAT8 = ['清', 'ロシア', 'オスマン', 'フランス', 'イギリス', 'プロイセン', 'オーストリア', '日本']
ROMA = {'清': 'QI', 'ロシア': 'RU', 'オスマン': 'OT', 'フランス': 'FR', 'イギリス': 'UK', 'プロイセン': 'PR', 'オーストリア': 'AT', '日本': 'JP'}


def rd(path):
    with open(path, encoding='utf-8', newline='') as f:
        return list(csv.DictReader(f, delimiter='\t'))


def fl(x):
    try:
        v = float(x)
        return v
    except Exception:
        return float('nan')


def seeds_of(d):
    return sorted(int(os.path.basename(p)[len('nation_s'):-4]) for p in glob.glob(os.path.join(d, 'nation_s*.tsv')))


@functools.lru_cache(maxsize=None)
def parse_nation(d, s):
    out = []
    for r in rd(os.path.join(d, f'nation_s{s}.tsv')):
        out.append((int(r['nationIndex']), r['nation'], int(r['quarter']), int(r['year']), fl(r['populationK']), fl(r['nominalGdpPerYear']), fl(r['revenueBaseMeanPerMonth']),
                    fl(r['realGdpPerYear']), fl(r['govDebtStock']), fl(r['sovereignDefaultCumulativeCount']),
                    fl(r['coreRegions']) + fl(r['integratedRegions']) + fl(r['colonialRegions'])))
    return out


@functools.lru_cache(maxsize=None)
def load_nation(d, s):
    """名前をキーに（変名で系列が分かれる＝年別の読み・挙げた 8 国の読み用）。"""
    out = defaultdict(list)
    for idx, nm, *rest in parse_nation(d, s):
        out[nm].append(tuple(rest))
    return out


@functools.lru_cache(maxsize=None)
def load_idx(d, s):
    """nationIndex をキーに（変名・統一をまたいで同じ国の系列・⑥① 用）。値＝(最後の名前, 行の並び)"""
    out = {}
    for idx, nm, *rest in parse_nation(d, s):
        e = out.setdefault(idx, [nm, []])
        e[0] = nm; e[1].append(tuple(rest))
    return out


@functools.lru_cache(maxsize=None)
def load_prov(d, s):
    out = defaultdict(dict)
    p = os.path.join(d, f'admrule_s{s}.tsv')
    if os.path.exists(p):
        for r in rd(p):
            out[r['nation']][int(r['quarter'])] = fl(r['provincesCount'])
    return out


def collapsed(series, last_q):
    """series＝[(quarter, value)]。走る最大の半分を割った or 最後の四半期より前に途切れた（消えた）。"""
    if not series:
        return False
    if series[-1][0] < last_q - 1:
        return True
    mx = 0.0
    for _, v in series:
        if v != v:
            continue
        mx = max(mx, v)
        if mx > 0 and v < 0.5 * mx:
            return True
    return False


def line1(arms):
    names = ['A', 'N1', 'N2', 'B']
    cnt = {a: defaultdict(int) for a in names}
    label = {}
    for a, d in zip(names, arms):
        for s in seeds_of(d):
            nat = load_idx(d, s)
            last_q = max(q for nm, v in nat.values() for q, *_ in v)
            for idx, (nm, rows) in nat.items():
                peak = max((r[2] for r in rows if r[2] == r[2]), default=0)
                if peak < 500:  # 人口のピークが 50 万人未満の国は数えない
                    continue
                label[idx] = nm
                popser = [(r[0], r[2]) for r in rows]
                provser = [(r[0], r[8]) for r in rows]   # 州の数＝core＋integrated＋colonial（nation_s）
                if collapsed(popser, last_q) or collapsed(provser, last_q):
                    cnt[a][idx] += 1
    hit = []
    print('⑥① 同じ国が毎回壊れる（人口か州の数が走る最大の半分を割る or 消える・種の数）')
    allnames = sorted(set(n for a in names for n in cnt[a]), key=lambda n: -cnt['B'][n])
    for n in allnames[:15]:
        print('   #%-3d %-14s A=%d N1=%d N2=%d B=%d' % (n, label.get(n, '?'), cnt['A'][n], cnt['N1'][n], cnt['N2'][n], cnt['B'][n]))
    for n in allnames:
        if cnt['B'][n] >= 8 and max(cnt['A'][n], cnt['N1'][n], cnt['N2'][n]) <= 2:
            hit.append(label.get(n, str(n)))
    print('   → %s' % ('鳴る：' + ','.join(hit) if hit else '鳴らない'))
    return bool(hit)


def top_share(d, s, year):
    nat = load_nation(d, s)
    best = None; tot = 0.0
    vals = {}
    for n, rows in nat.items():
        yr = [r for r in rows if r[1] == year]
        if not yr:
            continue
        v = yr[-1][5]
        if v == v:
            vals[n] = v; tot += v
    if not vals or tot <= 0:
        return None, float('nan')
    n = max(vals, key=vals.get)
    return n, vals[n] / tot


def line2(arms):
    names = ['A', 'N1', 'N2', 'B']
    print('⑥② 一国の発散（年末の首位の国の世界の実質GDP の取り分・種の中央値）')
    fired = False
    for year in (1850, 1900, 1950):
        med = {}; same = {}
        for a, d in zip(names, arms):
            sh = []; who = defaultdict(int)
            for s in seeds_of(d):
                n, v = top_share(d, s, year)
                if n is not None:
                    sh.append(v); who[n] += 1
            med[a] = statistics.median(sh) if sh else float('nan')
            same[a] = max(who.values()) if who else 0
        nulls = [med['A'], med['N1'], med['N2']]
        width = max(nulls) - min(nulls)
        f = med['B'] > max(nulls) + 2 * width and same['B'] >= 8
        print('   %d  A=%.3f N1=%.3f N2=%.3f B=%.3f  幅=%.3f  同じ首位の種(B)=%d  → %s' % (year, med['A'], med['N1'], med['N2'], med['B'], width, same['B'], '鳴る' if f else '鳴らない'))
        fired = fired or f
    return fired


def line3(arms):
    names = ['A', 'N1', 'N2', 'B']
    cnt = {}; tot = {}
    for a, d in zip(names, arms):
        c = 0; t = 0
        for p in glob.glob(os.path.join(d, 'fiscal_s*.tsv')):
            for r in rd(p):
                if r['nation'] not in NAT8:
                    continue
                v = fl(r['treasuryEndStock'])
                if v != v:
                    continue
                t += 1
                if v <= 0:
                    c += 1
        cnt[a] = c; tot[a] = t
    nullmax = max(cnt['A'], cnt['N1'], cnt['N2'])
    f = cnt['B'] >= 2 * nullmax and cnt['B'] - nullmax >= 0.005 * tot['B']
    print('⑥③ 国庫の枯渇（fiscal_s の挙げた 8 国・月で treasuryEndStock ≤ 0 の行数）')
    print('   A=%d N1=%d N2=%d B=%d（全行 %d）  → %s' % (cnt['A'], cnt['N1'], cnt['N2'], cnt['B'], tot['B'], '鳴る' if f else '鳴らない'))
    return f


def taxgdp(d, upto):
    out = defaultdict(list)
    for s in seeds_of(d):
        for n, rows in load_nation(d, s).items():
            if n not in NAT8:
                continue
            for q, y, pk, gdp, rev, *_ in rows:
                if y <= upto and gdp > 0 and rev == rev:
                    out[n].append(rev * 12 / gdp)
    return out


def line4(arms):
    A = taxgdp(arms[0], 1800); B = taxgdp(arms[3], 1800)
    print('⑥④ 歳入の崩れ（開幕〜1800 の税÷GDP の中央値・B÷A）')
    low = 0
    for n in NAT8:
        a = statistics.median(A[n]) if A[n] else float('nan'); b = statistics.median(B[n]) if B[n] else float('nan')
        r = b / a if a else float('nan')
        if r == r and r < 0.7:
            low += 1
        print('   %-8s A=%.4f B=%.4f  比=%.3f' % (ROMA[n], a, b, r))
    print('   → 0.7 倍を下回る国 %d  → %s' % (low, '鳴る' if low >= 3 else '鳴らない'))
    return low >= 3


def rec_r1(arms):
    names = ['A', 'N1', 'N2', 'B']
    print('R1 税÷GDP の年別（種×四半期の中央値）')
    for yr in (1784, 1790, 1800, 1820, 1850, 1900):
        row = []
        for n in NAT8:
            vs = []
            for a, d in zip(names, arms):
                v = []
                for s in seeds_of(d):
                    for q, y, pk, gdp, rev, *_ in load_nation(d, s).get(n, []):
                        if y == yr and gdp > 0 and rev == rev:
                            v.append(rev * 12 / gdp)
                vs.append(statistics.median(v) if v else float('nan'))
            row.append('%s %.3f/%.3f/%.3f/%.3f' % (ROMA[n], *vs))
        print('   %d  ' % yr + ' | '.join(row))
    print('   （各国 A/N1/N2/B）')


def rec_r2(arms):
    """admrule_s：B÷旧の届き（deliveryShare÷deliveryOldShare）と法の浸透（旧→新）・充足・領主の居ない州の人口割合（種の中央値・年末の行）"""
    print('R2 届きの内訳（B の admrule_s・種の中央値）')
    d = arms[3]
    yrs = (1783, 1790, 1800, 1820, 1850, 1900)
    x = defaultdict(lambda: defaultdict(list))
    for p in sorted(glob.glob(os.path.join(d, 'admrule_s*.tsv'))):
        last = {}
        for r in rd(p):
            if r['nation'] in NAT8 and int(r['year']) in yrs:
                last[(r['nation'], int(r['year']))] = r
        for k, r in last.items():
            x[k]['f'].append(fl(r['adminFillShare']))
            x[k]['dl'].append(fl(r['deliveryShare']) / fl(r['deliveryOldShare']) if fl(r['deliveryOldShare']) > 0 else float('nan'))
            x[k]['law'].append(fl(r['stateReachShare']) / fl(r['stateReachOldShare']) if fl(r['stateReachOldShare']) > 0 else float('nan'))
            x[k]['ll'].append(fl(r['lordlessPopShare']))
    def m(k, f):
        v = [t for t in x[k][f] if t == t]
        return statistics.median(v) if v else float('nan')
    for yr in yrs:
        print('   %d  ' % yr + ' | '.join('%s f%.2f dlv×%.2f law×%.2f nolord%.2f' % (ROMA[n], m((n, yr), 'f'), m((n, yr), 'dl'), m((n, yr), 'law'), m((n, yr), 'll')) for n in NAT8))


def rec_r5p(arms):
    """R5'：行政費の費用÷返り（B の fiscal_s と rule_s と admrule_s から）。返り＝歳入×(deliveryShare−d×(R0+R1))÷deliveryShare（充足 0 の届け率との差）。費用＝claimAdminPerMonth。"""
    d = arms[3]
    yrs = (1784, 1790, 1800, 1820, 1850, 1900)
    res = defaultdict(list)   # (nation, year) -> [ratio per seed]
    for s in seeds_of(d):
        fp = os.path.join(d, f'fiscal_s{s}.tsv'); ap = os.path.join(d, f'admrule_s{s}.tsv'); rp = os.path.join(d, f'rule_s{s}.tsv')
        if not (os.path.exists(fp) and os.path.exists(ap) and os.path.exists(rp)):
            continue
        cost = {}
        for r in rd(fp):
            if r['nation'] in NAT8 and int(r['year']) in yrs:
                cost[(r['nation'], int(r['year']))] = fl(r['claimAdminPerMonth'])   # 年内の最後の月の行
        adm = {}
        for r in rd(ap):
            if r['nation'] in NAT8 and int(r['year']) in yrs:
                adm[(r['nation'], int(r['year']))] = r
        rev = defaultdict(float); lastq = {}
        for r in rd(rp):
            k = (r['nation'], int(r['year']))
            if r['nation'] in NAT8 and int(r['year']) in yrs:
                q = int(r['quarter'])
                if q > lastq.get(k, -1):
                    lastq[k] = q; rev[k] = 0.0
                if q == lastq[k]:
                    rev[k] += fl(r['revenuePerMonth'])
        for k, a in adm.items():
            if k not in cost or k not in rev:
                continue
            dlv = fl(a['deliveryShare']); dd = 0.7 * (fl(a['ruleStateShare']) + fl(a['ruleLocalShare']))
            ret = rev[k] * (dlv - dd) / dlv if dlv > 0 else float('nan')
            if ret > 0:
                res[k].append(cost[k] / ret)
    print("R5' 行政費の費用÷返り（B・種の中央値・年末）  ⚠上下水道の支払いも claimAdmin に入る＝費用は大きめ")
    for yr in yrs:
        print('   %d  ' % yr + ' | '.join(('%s %.2f' % (ROMA[n], statistics.median(res[(n, yr)])) if res[(n, yr)] else '%s -' % ROMA[n]) for n in NAT8))
    print('   （費用÷返り＞1 は行政費が税の元を取らない。⚠d＝0.7 は PrivilegeTaxDiscount 0.3 の既定の読み。史実では行政は純費用だった＝確証なし・Q8）')


def rec_r7(arms):
    names = ['A', 'N1', 'N2', 'B']
    print('R7 債務と不履行（挙げた 8 国・種の中央値）：1850・1900 の債務÷名目GDP と 1950 の不履行の累計')
    for a, d in zip(names, arms):
        row = []
        for n in NAT8:
            dg = defaultdict(list); df = []
            for s in seeds_of(d):
                rows = load_nation(d, s).get(n, [])
                for q, y, pk, gdp, rev, rg, debt, dfl, prv in rows:
                    if y in (1850, 1900) and gdp > 0:
                        dg[y].append(debt / gdp)
                if rows:
                    df.append(rows[-1][7])
            m = lambda v: statistics.median(v) if v else float('nan')
            row.append('%s %.2f/%.2f/%.1f' % (ROMA[n], m(dg[1850]), m(dg[1900]), m(df)))
        print('   %-2s ' % a + ' | '.join(row))


ERAS = [('1783-1800', 1783, 1800), ('1801-1850', 1801, 1850), ('1851-1950', 1851, 1950)]


def rec_r6(arms):
    """R6 帰還の輪：予算が効いている国×月（買える割合の上限 capped÷need が 1 未満で、充足がその上限の 0.02 以内）の割合（挙げた 8 国・時代別）。"""
    names = ['A', 'B']
    print('R6 予算が効いている国×月の割合（fiscal_s の挙げた 8 国・時代別）と充足の平均')
    for a, d in zip(names, (arms[0], arms[3])):
        tot = defaultdict(int); bind = defaultdict(int); fsum = defaultdict(float)
        for p in glob.glob(os.path.join(d, 'fiscal_s*.tsv')):
            for r in rd(p):
                if r['nation'] not in NAT8:
                    continue
                y = int(r['year']); need = fl(r['adminNeedAtBudgetPerMonth']); cap = fl(r['adminClaimCappedAtBudgetPerMonth']); f = fl(r['adminFillShare'])
                if not (need > 0) or f != f:
                    continue
                for lab, lo, hi in ERAS:
                    if lo <= y <= hi:
                        tot[lab] += 1; fsum[lab] += f
                        s = cap / need
                        if s < 1.0 and f >= s - 0.02:
                            bind[lab] += 1
        print('   %s  ' % a + '  '.join('%s: 効く%.1f%% 充足平均%.2f (n=%d)' % (lab, 100.0 * bind[lab] / max(1, tot[lab]), fsum[lab] / max(1, tot[lab]), tot[lab]) for lab, _, _ in ERAS))


def adm_rows(d):
    out = []
    for p in sorted(glob.glob(os.path.join(d, 'admrule_s*.tsv'))):
        out.append([r for r in rd(p) if r['nation'] in NAT8])
    return out


def rec_r4(arms):
    """R4 徴兵の母数と復員：admrule_s の新しい列（挙げた 8 国・四半期）。A と B を並べる。"""
    print('R4 定員と復員（挙げた 8 国×四半期・時代別）：人口の側で定員が決まる割合／平時の復員の対象 excessSoldierK の和／兵士 soldierK の中央値／人口の側の定員 manLimitK の B÷旧')
    for a, d in zip(['A', 'B'], (arms[0], arms[3])):
        pop = defaultdict(list); exc = defaultdict(float); sold = defaultdict(list); ml = defaultdict(list); n = defaultdict(int)
        for rows in adm_rows(d):
            for r in rows:
                y = int(r['year'])
                for lab, lo, hi in ERAS:
                    if lo <= y <= hi:
                        n[lab] += 1; pop[lab].append(fl(r['popSideDecidesFlag']))
                        if fl(r['atWarFlag']) == 0:
                            exc[lab] += fl(r['excessSoldierK'])
                        sold[lab].append(fl(r['soldierK']))
                        if fl(r['manLimitOldK']) > 0:
                            ml[lab].append(fl(r['manLimitK']) / fl(r['manLimitOldK']))
        print('   %s  ' % a + '  '.join('%s: 人口側%.0f%% 復員対象和%.0fK 兵中央%.1fK 定員比%.3f' % (lab, 100.0 * statistics.mean(pop[lab]) if pop[lab] else float('nan'), exc[lab], statistics.median(sold[lab]) if sold[lab] else float('nan'), statistics.median(ml[lab]) if ml[lab] else float('nan')) for lab, _, _ in ERAS))


def rec_r2b(arms):
    """R2' 仲介者の取り分の行き先と領主の所得（A と B・時代別・挙げた 8 国の種×四半期）"""
    print("R2' 領主の居ない州の人口割合の中央値／仲介者の取り分の率（全体→領主の居る州）の中央値／貴族の一人あたり所得の最大の中央値と最大値")
    for a, d in zip(['A', 'B'], (arms[0], arms[3])):
        ll = defaultdict(list); ca = defaultdict(list); cl = defaultdict(list); nm = defaultdict(list)
        for rows in adm_rows(d):
            for r in rows:
                y = int(r['year'])
                for lab, lo, hi in ERAS:
                    if lo <= y <= hi:
                        ll[lab].append(fl(r['lordlessPopShare'])); ca[lab].append(fl(r['ruleCutRateAllShare'])); cl[lab].append(fl(r['ruleCutRateLordedShare'])); nm[lab].append(fl(r['nobleIncomeMaxPerCapPerMonth']))
        md = lambda v: statistics.median([t for t in v if t == t]) if [t for t in v if t == t] else float('nan')
        print('   %s  ' % a + '  '.join('%s: 領主なし%.2f 取り分率%.4f→%.4f 貴族所得max中央%.3g 最大%.3g' % (lab, md(ll[lab]), md(ca[lab]), md(cl[lab]), md(nm[lab]), max([t for t in nm[lab] if t == t] or [float('nan')])) for lab, _, _ in ERAS))


if __name__ == '__main__':
    arms = sys.argv[1:5]
    assert len(arms) == 4, __doc__
    for fn in (line1, line2, line3, line4):
        r = fn(arms)
        print()
    for fn in (rec_r1, rec_r2, rec_r2b, rec_r4, rec_r5p, rec_r6, rec_r7):
        fn(arms); print()
