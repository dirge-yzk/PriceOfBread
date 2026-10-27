#!/usr/bin/env python
# (597) 奨学金の法（教育費の軸の最上段だけ）の読み手：事前登録 measurements/sch2_597_20261027_prereg.txt の [3] の J2 の腕の中の観測・K1・K2・記録を、計器つき 10 種（B）の ed_s から読む。
# 使い方：PYTHONUTF8=1 python an_sch2.py <B の ins フォルダ> [種の数=10]
#   ed_s の末尾 6 列（higherTuitionPerMonth・higherSubsidyPerMonth・higherSubsidyExpectedPerMonth・higherCoverShare・higherFillShare・eduStage）を読む。eduStage が無い（旧の木の ed_s）なら即終了。
#   K1   入口が開いたか＝higherTuitionPerMonth＞0 の国×四半期がある種の数（≧8/10 で通る。外れたら J2 の腕の中の観測は blocked）。
#   K2   eduStage＝3 かつ higherTuitionPerMonth＞0 の国×四半期がある種の数（≧5/10 なら J2 の正の半分を判定する。足りなければ正の半分は blocked）。
#   J2零 eduStage≦2 の国×四半期で higherSubsidyPerMonth と higherCoverShare は厳密に 0（違反 0 で通る）。eduStage が NaN の行は違反に数える（fail-closed）。
#   J2正 eduStage＝3・higherTuitionPerMonth＞0・higherCoverShare＞0 の国×四半期で higherSubsidyPerMonth＞0 かつ higherSubsidyExpectedPerMonth と 1e-6 の相対で一致（違反 0 で通る）。
#   記録 R-stage（eduStage の分布〔国×四半期の割合・十年ごと〕と段 3 の国の数）・R-fill（段 3 とそれ以外の higherFillShare の十年ごとの中央値）・R-sub（段 3 の補助÷授業料の中央値・十年ごと）・R-cap（higherCoverShare≧0.94）。
#   国ごとの数は基線・N1・N2 と並べる（RULE36）。この読み手は基線の ed_s には eduStage が無いので、基線の側は higherFillShare だけ別に読む。
import csv, sys, statistics as st

B = sys.argv[1].rstrip('/') + '/'
NS = int(sys.argv[2]) if len(sys.argv) > 2 else 10
NEW = ['higherTuitionPerMonth', 'higherSubsidyPerMonth', 'higherSubsidyExpectedPerMonth', 'higherCoverShare', 'higherFillShare', 'eduStage']


def rows(path):
    try:
        with open(path, encoding='utf-8') as f:
            yield from csv.DictReader(f, delimiter='\t')
    except FileNotFoundError:
        return


def num(x):
    try:
        return float(x)
    except Exception:
        return float('nan')


def nan(x):
    return not (x == x)


def decade(year):
    return int(year) // 10 * 10


tot = dict(seeds=0, k1=0, k2=0, zero_rows=0, zero_viol=0, pos_rows=0, pos_viol=0, nan_rows=0, cap=0)
stage_cnt = {}          # decade -> [n0, n1, n2, n3]
stage3_nations = {}     # decade -> set(nation)
fill_s3 = {}; fill_other = {}; subshare = {}
for s in range(1, NS + 1):
    ed = list(rows(f'{B}ed_s{s}.tsv'))
    if not ed:
        print(f'seed {s}: ed_s{s}.tsv が無い（欠け＝不合格に倒す）')
        continue
    if any(c not in ed[0] for c in NEW):
        sys.exit('ed_s に eduStage の列が無い（旧の木）')
    tot['seeds'] += 1
    k1 = False; k2 = False
    for r in ed:
        tu, su, ex, cv, fl, es = (num(r[c]) for c in NEW)
        if nan(es) or nan(tu) or nan(su) or nan(cv):
            tot['nan_rows'] += 1
            continue
        d = decade(r['year'])
        stage = int(round(es))
        stage_cnt.setdefault(d, [0, 0, 0, 0])[min(max(stage, 0), 3)] += 1
        if tu > 0: k1 = True
        if stage <= 2:
            tot['zero_rows'] += 1
            if su != 0 or cv != 0: tot['zero_viol'] += 1
        else:
            stage3_nations.setdefault(d, set()).add(r['nation'])
            if tu > 0: k2 = True
            if tu > 0 and cv > 0:
                tot['pos_rows'] += 1
                if not (su > 0) or abs(su - ex) > 1e-6 * max(abs(su), abs(ex), 1e-12): tot['pos_viol'] += 1
                subshare.setdefault(d, []).append(su / tu)
        if cv >= 0.94: tot['cap'] += 1
        if not nan(fl): (fill_s3 if stage >= 3 else fill_other).setdefault(d, []).append(fl)
    tot['k1'] += k1; tot['k2'] += k2
    print(f'seed {s}: K1 入口(授業料>0)={k1} K2 段3かつ授業料>0={k2}')
print('---')
print(f"K1 入口が開いた種 {tot['k1']}/{tot['seeds']}（≧8/10 で通る）")
print(f"K2 段3かつ授業料>0 の国×四半期がある種 {tot['k2']}/{tot['seeds']}（≧5/10 で正の半分を判定）")
print(f"J2零 段≦2 の行 {tot['zero_rows']} 件中 補助か割合が 0 でない {tot['zero_viol']}（NaN の行 {tot['nan_rows']} 件は別に数える）")
print(f"J2正 段3・授業料>0・割合>0 の行 {tot['pos_rows']} 件中 違反 {tot['pos_viol']}")
print(f"R-cap 割合≧0.94 の国×四半期 {tot['cap']}")
print('R-stage 段 0/1/2/3 の国×四半期の件数（十年ごと）と段 3 の国の数：')
for d, c in sorted(stage_cnt.items()):
    print(f'  {d}s {c}  段3の国 {len(stage3_nations.get(d, ()))}')
print('R-sub 段 3 の補助÷授業料の中央値（十年ごと）：', {d: round(st.median(v), 4) for d, v in sorted(subshare.items())})
print('R-fill higherFillShare の中央値（十年ごと・段3 / それ以外）：')
for d in sorted(set(fill_s3) | set(fill_other)):
    a = st.median(fill_s3[d]) if d in fill_s3 else float('nan')
    b = st.median(fill_other[d]) if d in fill_other else float('nan')
    print(f'  {d}s {a:.5f} / {b:.5f}')
