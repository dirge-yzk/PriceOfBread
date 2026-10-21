#!/usr/bin/env python
"""(361) 人口の作り替え 段A 計器＋段B 影の器の読み手：pop_s<seed>.tsv（SimCore/InstrumentsPop.cs）を国×時代で畳む。
使い方：PYTHONUTF8=1 python an_pop.py <計器の出力フォルダ（pop_s*.tsv のあるところ）> [-n 国名,国名,...] [--all]
・N 行（国×四半期〔挙げた国は毎四半期・他は年末〕）を、時代（1783-1799・1800-1849・1850-1899・1900-1950）ごとに、(種×四半期) の中央値で出す。
・回復期の窓（回復期の月〔平常の死が 0 の月〕が窓の月の数の 25% を超える窓）は死の率・余命・子の価値の影が壊れる（余命が数百〜数千年）＝既定では除いて中央値を取り、除いた後の行数を『行（平常/全）』の列に出す（--all-rows で除かない）。
・値の列は pop_s の見出しのとおり。NaN の割合を各表の最後の列に出す（計器が自分で申告する限界＝NaN の行は中央値に入らない）。
・⚠ 史実の数は設計 doc §4-1 の【推】（出典の頁は見ていない）。比は目安。
"""
import csv, glob, os, sys, math, collections, statistics
import os as _os, sys as _sys
_sys.path.insert(0, _os.path.join(_os.path.dirname(_os.path.abspath(__file__)), '..', 'pop'))
import age_guard as _age_guard
_age_guard.require_bands8_argv()   # (490) 8 層（10 歳刻み）の pop_s だけを前提にする＝11 層の生なら止める

ERAS = [(1783, 1800), (1800, 1850), (1850, 1900), (1900, 1951)]
DEFAULT = ['イギリス', 'フランス', 'ドイツ', 'プロイセン', 'オーストリア', 'ロシア', '日本', '清', 'アメリカ', 'インド', 'ネーデルラント', 'ポルトガル', 'スペイン']
# 1850 年前後の粗出生／粗死亡（‰）【推】設計 doc §4-1・§1-3
REF1850 = {'イギリス': (34, 22), 'フランス': (27, 24), 'ロシア': (50, 35)}


def num(v):
    try:
        return float(v)
    except Exception:
        return float('nan')


def med(xs):
    xs = [x for x in xs if not math.isnan(x) and not math.isinf(x)]
    return statistics.median(xs) if xs else float('nan')


def f(x, n=2):
    return '   nan' if x != x else (f'{x:6.{n}f}')


def load(d):
    rows = []
    for p in sorted(glob.glob(os.path.join(d, 'pop_s*.tsv'))):
        with open(p, encoding='utf-8') as fh:
            rd = csv.reader(fh, delimiter='\t')
            h = next(rd)
            ix = {k: i for i, k in enumerate(h)}
            seed = os.path.basename(p)[5:-4]
            for r in rd:
                if len(r) == len(h):
                    rows.append((seed, ix, r))
    return rows


def pick(N, nm, y0, y1, keep_rec):
    sel = [(ix, r) for _, ix, r in N if r[ix['nation']] == nm and y0 <= int(r[ix['year']]) < y1]
    tot = len(sel)
    if not keep_rec:
        sel = [(ix, r) for ix, r in sel if num(r[ix['recoveringMonths']]) <= 0.25 * num(r[ix['monthsCount']])]
    return sel, tot


def main():
    a = sys.argv[1:]
    if not a:
        print(__doc__); return
    d = a[0]; names = None; allm = '--all' in a; keep_rec = '--all-rows' in a
    if '-n' in a:
        names = a[a.index('-n') + 1].split(',')
    rows = load(d)
    if not rows:
        print('pop_s*.tsv が無い', d); return
    seeds = sorted({s for s, _, _ in rows})
    N = [(s, ix, r) for s, ix, r in rows if r[ix['scope']] == 'N']
    C = [(s, ix, r) for s, ix, r in rows if r[ix['scope']] == 'C']
    print(f'# pop_s 読み手：{d}  種 {len(seeds)}（{",".join(seeds)}）  N 行 {len(N)}  C 行 {len(C)}')
    allnames = collections.Counter(r[ix['nation']] for _, ix, r in N)
    if names is None:
        names = [n for n in DEFAULT if n in allnames]
        if allm:
            names = sorted(allnames)
    print(f'# 国 {len(allnames)} 本（行数の多い順に先頭 8）：' + ' '.join(f'{k}:{v}' for k, v in allnames.most_common(8)))

    # ---- 表1：流れ（粗出生・粗死亡・合計出生率の代理・層0の死の率・10歳までの生存・60歳の余命）
    cols1 = ['cbrPerMille', 'cdrPerMille', 'tfrProxy', 'muFrom0PerYear', 's10Share', 'l60Years', 'share60plus', 'recoveringMonths']
    print('\n## 表1 流れ（時代ごとの (種×四半期) の中央値）')
    print('国\t時代\t行（平常/全）\t' + '\t'.join(c.replace('PerMille', '‰').replace('PerYear', '/年') for c in cols1) + '\t余命NaN率')
    for nm in names:
        for (y0, y1) in ERAS:
            sel, tot = pick(N, nm, y0, y1, keep_rec)
            if not sel:
                continue
            vals = [med([num(r[ix[c]]) for ix, r in sel]) for c in cols1]
            nanl = sum(1 for ix, r in sel if math.isnan(num(r[ix['l60Years']]))) / len(sel)
            print(f'{nm}\t{y0}-{y1 - 1}\t{len(sel)}/{tot}\t' + '\t'.join(f(v, 3) for v in vals) + f'\t{nanl:.2f}')

    # ---- 表2：子の価値の影と m・c（ψ=0）
    cols2 = ['yAdultPerMonth', 'basketPerMonth', 'childEarningsE', 'childCostC', 'oldAgeO', 'survivingChildrenNs', 'pensionPiShare', 'vNoUtilityIndex', 'psiBreakevenIndex', 'gOfVShare', 'marriedMNewShare', 'cKnownShare', 'tfrShadow']
    print('\n## 表2 影（段B・ψ=0・K＝識字）')
    print('国\t時代\t' + '\t'.join(c for c in cols2) + '\tvNaN率')
    for nm in names:
        for (y0, y1) in ERAS:
            sel, tot = pick(N, nm, y0, y1, keep_rec)
            if not sel:
                continue
            vals = [med([num(r[ix[c]]) for ix, r in sel]) for c in cols2]
            nanv = sum(1 for ix, r in sel if math.isnan(num(r[ix['vNoUtilityIndex']]))) / len(sel)
            print(f'{nm}\t{y0}-{y1 - 1}\t' + '\t'.join(f(v, 3) for v in vals) + f'\t{nanv:.2f}')

    # ---- 表3：影の出生（式 (b)）と実際の出生・合計出生率の影と代理
    print('\n## 表3 影の出生÷実際の出生（年あたり・中央値）と合計出生率（影／代理）')
    print('国\t時代\t影÷実際\t合計出生率の影\t合計出生率の代理')
    for nm in names:
        for (y0, y1) in ERAS:
            sel, tot = pick(N, nm, y0, y1, keep_rec)
            if not sel:
                continue
            rat = []
            for ix, r in sel:
                mo = num(r[ix['monthsCount']]); b = num(r[ix['birthsK']]); sb = num(r[ix['shadowBirthsPerYearK']])
                if mo > 0 and b > 0 and sb == sb:
                    rat.append(sb / (b * 12.0 / mo))
            print(f'{nm}\t{y0}-{y1 - 1}\t{f(med(rat), 3)}\t{f(med([num(r[ix["tfrShadow"]]) for ix, r in sel]), 3)}\t{f(med([num(r[ix["tfrProxy"]]) for ix, r in sel]), 3)}')

    # ---- 表4：1850 年前後（1846-1855）の粗出生／粗死亡と史実の目安
    print('\n## 表4 1846-1855 年の粗出生／粗死亡（‰・中央値）と史実の目安【推】')
    for nm in names:
        sel = [(ix, r) for _, ix, r in N if r[ix['nation']] == nm and 1846 <= int(r[ix['year']]) <= 1855]
        if not sel:
            continue
        cb = med([num(r[ix['cbrPerMille']]) for ix, r in sel]); cd = med([num(r[ix['cdrPerMille']]) for ix, r in sel])
        ref = REF1850.get(nm)
        extra = f'  史実 {ref[0]}/{ref[1]}  比 {cb / ref[0]:.2f}/{cd / ref[1]:.2f}' if ref else ''
        print(f'{nm}\t{cb:.1f}/{cd:.1f}{extra}')

    # ---- 計器の自己申告：全ての行の NaN の割合
    print('\n## 計器の限界（全ての N 行）')
    tot = len(N)
    for c in ['l60Years', 'vNoUtilityIndex', 'tfrProxy', 'marriedMNewShare', 'basketPerMonth', 's10Share']:
        n = sum(1 for _, ix, r in N if math.isnan(num(r[ix[c]])))
        print(f'{c}\tNaN {n}/{tot} = {n / tot:.3f}')
    rec = sum(num(r[ix['recoveringMonths']]) for _, ix, r in N if not math.isnan(num(r[ix['recoveringMonths']])))
    mon = sum(num(r[ix['monthsCount']]) for _, ix, r in N if not math.isnan(num(r[ix['monthsCount']])))
    print(f'回復期の月（平常の死が 0 の月）の割合\t{rec / mon:.3f}' if mon else '')


if __name__ == '__main__':
    main()
