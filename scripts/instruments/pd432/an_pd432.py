#!/usr/bin/env python
"""(432) 線 pd 後半：人口の死の側の判定の読み手（読むだけ）。事前登録 measurements/pd_20261019_prereg.txt の追記3 [8'] と同じ規則。
使い方：PYTHONUTF8=1 python an_pd432.py <腕 B の計器の出力> [--base <基線 m428_ins の計器の出力>]
・種ごとに T1〜T4・T6・T8・T9 を判定し、10 種のうち何種で満たしたかを数える（7 以上＝当たり・4〜6＝部分・3 以下＝外れ）。
・期間：1783〜99（Q13 以後＝開幕の補償死 Q0〜Q12 を除く）・1850〜99・1900〜49（1950 年を含めない）。各期間の値は 種ごとの四半期の中央値。
・T1 英の mu0（pop_s scope=N の muFrom0PerYear）が 3 期間で単調に下がり、1900〜49÷1783〜99 ≦ 0.85。
・T2 英の 15 歳の余命（popc_s の lifeExpectancy15Years＝シミュが結婚の入力に使う量そのもの）が単調に伸び、1900〜49−1783〜99 ≧ +4 年。
・T3 英の mu0 の比（1900〜49÷1783〜99）＜ mu4 の同じ比。
・T4 1900〜49 に 英の e15 ＞ 清の e15 + 3 年（記録寄り＝Q の差を含む構造）。
・T6 英の η（popc_s の knowledgeEfficacyEta）の 1949〜50 年の中央 ≧ 0.75 かつ 世界で最初の細菌説の発見（disc_s kind=E row=細菌説）が 1950 年までに起きる。
・T8 region_s：主食の充足 foodFillShare＜0.6 が 12 四半期続いた州の run のうち、run の終わりの人口が始めより小さい割合 ≧ 0.8（腕の全種の合算）・
       人口÷耕地（populationK÷arableLand、arableLand＞0 の活きた州の行）の p99 が 基線 m428 の 3 倍以内（--base があるとき）。
・T9（医療だけを分けて見る止める判定）英の popc_s medMulBand0（層 0 の医療の掛け目）の 1900〜49 の中央 ÷ 1783〜99 の中央 ≦ 0.85。
       〈入力が平ら〉の注記：英の doctorCareQ の 1900〜49 の中央 ＜ 1783〜99 の中央 × 0.5（注記だけ＝採否の規則は変えない）。
・記録だけ：T5 の代わりの食い違い、栄養と飢えの重なり、栄養→初婚の年齢の相関、英の粗死亡、国の表。
"""
import csv, glob, math, os, statistics, sys, collections
import os as _os, sys as _sys
_sys.path.insert(0, _os.path.join(_os.path.dirname(_os.path.abspath(__file__)), '..', 'pop'))
import age_guard as _age_guard
_age_guard.require_bands8_argv()   # (490) 8 層（10 歳刻み）の pop_s だけを前提にする＝11 層の生なら止める

if hasattr(sys.stdout, 'reconfigure'):
    sys.stdout.reconfigure(encoding='utf-8')

UK, QING, FR = 'イギリス', '清', 'フランス'
MAJOR = ['イギリス', 'フランス', 'プロイセン', 'オーストリア', 'ロシア', '日本', '清', 'ネーデルラント']
ERA = {'A': (1783, 1800), 'B': (1850, 1900), 'C': (1900, 1950)}   # 年の半開区間
QMIN = 13


def num(v):
    try:
        return float(v)
    except Exception:
        return float('nan')


def med(xs):
    xs = [x for x in xs if x == x and not math.isinf(x)]
    return statistics.median(xs) if xs else float('nan')


def rows(path):
    with open(path, encoding='utf-8') as fh:
        for r in csv.DictReader(fh, delimiter='\t'):
            yield r


def seeds_of(d, name):
    out = {}
    for f in sorted(glob.glob(os.path.join(d, name + '_s*.tsv'))):
        digits = ''.join(ch for ch in os.path.basename(f).split('_s')[-1] if ch.isdigit())
        out[int(digits)] = f
    return out


def era_series(series, col):
    """series: [(quarter, year, dict)] -> {era: median of col}（Q13 以後）。"""
    out = {}
    for k, (lo, hi) in ERA.items():
        out[k] = med([d.get(col, float('nan')) for q, y, d in series if q >= QMIN and lo <= y < hi])
    return out


def mono_down(e):
    return e['A'] == e['A'] and e['B'] == e['B'] and e['C'] == e['C'] and e['A'] > e['B'] > e['C']


def mono_up(e):
    return e['A'] == e['A'] and e['B'] == e['B'] and e['C'] == e['C'] and e['A'] < e['B'] < e['C']


def verdict(n, tot):
    return '当たり' if n >= 7 else ('部分' if n >= 4 else '外れ')


def pearson(xs, ys):
    p = [(x, y) for x, y in zip(xs, ys) if x == x and y == y]
    if len(p) < 3:
        return float('nan')
    mx = sum(x for x, _ in p) / len(p); my = sum(y for _, y in p) / len(p)
    sx = math.sqrt(sum((x - mx) ** 2 for x, _ in p)); sy = math.sqrt(sum((y - my) ** 2 for _, y in p))
    if sx == 0 or sy == 0:
        return float('nan')
    return sum((x - mx) * (y - my) for x, y in p) / (sx * sy)


def load_nation(d, seed, nation):
    """pop_s（scope=N）・popc_s の指定国の四半期の系列。"""
    pf = seeds_of(d, 'pop').get(seed); cf = seeds_of(d, 'popc').get(seed)
    P = {}
    if pf:
        for r in rows(pf):
            if r['scope'] != 'N' or r['nation'] != nation:
                continue
            q = int(r['quarter'])
            P[q] = dict(year=int(r['year']), mu0=num(r['muFrom0PerYear']), mu4=num(r['muFrom40PerYear']), cdr=num(r['cdrPerMille']), cbr=num(r['cbrPerMille']),
                        dnorm=num(r['deathsNormalK']), dfam=num(r['deathsFamineK']), popK=num(r['popK']))
    if cf:
        for r in rows(cf):
            if r['nation'] != nation:
                continue
            q = int(r['quarter'])
            P.setdefault(q, dict(year=int(r['year']))).update(
                e15=num(r['lifeExpectancy15Years']), eta=num(r['knowledgeEfficacyEta']), Q=num(r['doctorCareQ']), nut=num(r['nutritionMulIndex']),
                med0=num(r['medMulBand0']), med4=num(r['medMulBand4']), age=num(r['marriageAgeYears']), lvl=num(r['deathLevelIndex']))
    return sorted((q, d_['year'], d_) for q, d_ in P.items())


def first_bacteria(d, seed):
    f = seeds_of(d, 'disc').get(seed)
    if not f:
        return None
    best = None
    for r in rows(f):
        if r['kind'] == 'E' and r['row'] == '細菌説':
            y = int(r['year'])
            best = y if best is None else min(best, y)
    return best


def region_runs(d, seed):
    """region_s：(runs, shrink, p99 材料)。run＝foodFillShare＜0.6 が 12 四半期以上続く 州の連続区間。"""
    f = seeds_of(d, 'region').get(seed)
    if not f:
        return None
    by = collections.defaultdict(list)
    dens = []
    for r in rows(f):
        if r['activeFlag'] not in ('1', 'True', 'true'):
            continue
        key = (r['nationIndex'], r['nation'], r['region'])
        q = int(r['quarter']); pop = num(r['populationK']); ff = num(r['foodFillShare']); al = num(r['arableLand'])
        by[key].append((q, pop, ff))
        if al == al and al > 0 and pop == pop:
            dens.append(pop / al)
    runs = shrink = 0
    for key, ser in by.items():
        ser.sort()
        cur = []
        for q, pop, ff in ser + [(-99, 0, 1.0)]:
            if ff == ff and ff < 0.6 and (not cur or q == cur[-1][0] + 1):
                cur.append((q, pop, ff))
            else:
                if len(cur) >= 12:
                    runs += 1
                    if cur[-1][1] < cur[0][1]:
                        shrink += 1
                cur = [(q, pop, ff)] if (ff == ff and ff < 0.6) else []
    return runs, shrink, dens


def p99(xs):
    xs = sorted(x for x in xs if x == x)
    return xs[int(0.99 * (len(xs) - 1))] if xs else float('nan')


def main():
    a = sys.argv[1:]
    if not a:
        print(__doc__); return
    d = a[0]; base = None
    if '--base' in a:
        base = a[a.index('--base') + 1]
    seeds = sorted(set(seeds_of(d, 'popc')))
    print(f'# 出力 {d}  popc_s の種 {seeds}  基線 {base}')
    T = collections.OrderedDict((k, []) for k in ['T1', 'T2', 'T3', 'T4', 'T6', 'T9'])
    detail = []
    rec = collections.defaultdict(list)
    for s in seeds:
        uk = load_nation(d, s, UK); qg = load_nation(d, s, QING)
        if not uk:
            print(f'# seed{s}: 英の系列が無い'); continue
        m0 = era_series(uk, 'mu0'); m4 = era_series(uk, 'mu4'); e15 = era_series(uk, 'e15'); e15q = era_series(qg, 'e15')
        q9 = era_series(uk, 'med0'); qq = era_series(uk, 'Q'); eta = era_series(uk, 'eta')
        eta_end = med([x['eta'] for q, y, x in uk if 1949 <= y <= 1950 and 'eta' in x])
        rA = m0['C'] / m0['A'] if m0['A'] == m0['A'] and m0['A'] > 0 else float('nan')
        r4 = m4['C'] / m4['A'] if m4['A'] == m4['A'] and m4['A'] > 0 else float('nan')
        t1 = mono_down(m0) and rA <= 0.85
        t2 = mono_up(e15) and (e15['C'] - e15['A']) >= 4.0
        t3 = rA == rA and r4 == r4 and rA < r4
        t4 = (e15['C'] == e15['C'] and e15q['C'] == e15q['C'] and e15['C'] > e15q['C'] + 3.0)
        fb = first_bacteria(d, s)
        t6 = (eta_end == eta_end and eta_end >= 0.75 and fb is not None and fb <= 1950)
        r9 = q9['C'] / q9['A'] if q9['A'] == q9['A'] and q9['A'] > 0 else float('nan')
        t9 = r9 == r9 and r9 <= 0.85
        flat = (qq['C'] == qq['C'] and qq['A'] == qq['A'] and qq['C'] < 0.5 * qq['A'])
        for k, v in zip(T, [t1, t2, t3, t4, t6, t9]):
            T[k].append(bool(v))
        detail.append(f'seed{s}\tmu0 {m0["A"]:.4f}/{m0["B"]:.4f}/{m0["C"]:.4f} 比{rA:.3f}\tmu4比{r4:.3f}\te15 {e15["A"]:.1f}/{e15["B"]:.1f}/{e15["C"]:.1f} (+{e15["C"]-e15["A"]:.1f})\t清e15C {e15q["C"]:.1f}\t'
                      f'med0 {q9["A"]:.3f}/{q9["B"]:.3f}/{q9["C"]:.3f} 比{r9:.3f}\tQ {qq["A"]:.3f}/{qq["B"]:.3f}/{qq["C"]:.3f}{"（平ら）" if flat else ""}\tη末 {eta_end:.2f}\t細菌説 {fb}\t'
                      f'T1{int(t1)} T2{int(t2)} T3{int(t3)} T4{int(t4)} T6{int(t6)} T9{int(t9)}')
        # 記録：粗死亡・栄養→初婚の年齢の相関・栄養と飢えの重なり
        for k in 'ABC':
            rec[f'英の粗死亡‰ {k}'].append(med([x.get('cdr', float('nan')) for q, y, x in uk if q >= QMIN and ERA[k][0] <= y < ERA[k][1]]))
        sel = [x for q, y, x in uk if q >= QMIN and 'nut' in x]
        rec['栄養の掛け目と初婚の年齢の相関（英・Q13 以後）'].append(pearson([x['nut'] for x in sel], [x['age'] for x in sel]))
        for nat in MAJOR:
            ser = load_nation(d, s, nat) if nat not in (UK, QING) else (uk if nat == UK else qg)
            ch = med([(x['nut'] - 1) / x['nut'] for q, y, x in ser if q >= QMIN and x.get('nut', 0) == x.get('nut', 0) and x.get('nut', 0) > 0])
            fam = med([x['dfam'] / (x['dfam'] + x['dnorm']) for q, y, x in ser if q >= QMIN and x.get('dnorm', 0) == x.get('dnorm', 0) and (x.get('dfam', 0) + x.get('dnorm', 0)) > 0])
            rec[f'{nat}：平常の死のうち栄養の項が占める割合（中央）'].append(ch)
            rec[f'{nat}：全死亡のうち飢えの死の割合（中央）'].append(fam)
    print('\n== 種ごと（英：期間 A=1783〜99・B=1850〜99・C=1900〜49 の中央）==')
    for x in detail:
        print(x)
    print('\n== 判定（10 種のうち）==')
    for k in T:
        n = sum(T[k]); tot = len(T[k])
        print(f'{k}\t{n}/{tot}\t{verdict(n, tot)}')
    # T8
    runs = shrink = 0; dens_b = []
    for s in seeds:
        r = region_runs(d, s)
        if r:
            runs += r[0]; shrink += r[1]; dens_b += r[2]
    share = shrink / runs if runs else float('nan')
    p99b = p99(dens_b)
    print(f'\nT8\t飢えの run（充足＜0.6 が 12 四半期以上）{runs} 本のうち縮んだ {shrink} 本＝{share:.3f}（≧0.8 で満たす）\t人口÷耕地の p99 {p99b:.3f}')
    if base:
        runs0 = shrink0 = 0; dens0 = []
        for s in sorted(seeds_of(base, 'region')):
            r = region_runs(base, s)
            if r:
                runs0 += r[0]; shrink0 += r[1]; dens0 += r[2]
        p99a = p99(dens0)
        print(f'T8 基線\trun {runs0}・縮んだ {shrink0}＝{(shrink0 / runs0 if runs0 else float("nan")):.3f}\t人口÷耕地の p99 {p99a:.3f}\tB÷基線 {p99b / p99a if p99a else float("nan"):.3f}（≦3 で満たす）')
    print('\n== 記録（採否に使わない）==')
    for k, v in rec.items():
        print(f'{k}\t種の中央 {med(v):.4f}\t{" ".join(f"{x:.3f}" for x in v)}')
    print('\n# T5（州の充足が低いほど平常の死が高い）は判定から外した（追記3）：栄養の掛け目は式そのもので、州の死の系列は計器に無い。番人 M1・M5 が担う。')


if __name__ == '__main__':
    main()
