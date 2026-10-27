#!/usr/bin/env python
# (590) 奨学金の法の読み手：事前登録 measurements/sch_590_20261027_prereg.txt の [3] と追記 [8] の J4・記録を、計器つき 10 種（B）の ed_s と reform_s から読む。
# 使い方：PYTHONUTF8=1 python an_sch.py <B の ins フォルダ> [種の数=10]
#   ed_s の末尾 5 列（higherTuitionPerMonth・higherSubsidyPerMonth・higherSubsidyExpectedPerMonth・higherCoverShare・higherFillShare）を読む。列が無い（旧の木の ed_s）なら即座に止める。
#   J4(a)  入口が開いたか（高等教育の授業料＞0 の国×四半期がある種の数）＝点火の確かめ。sch の判定ではない。補助＞0 の国×四半期がある種の数も出す。
#   J4(b)  授業料＞0 の国×四半期で higherSubsidyPerMonth と higherSubsidyExpectedPerMonth が 1e-6 の相対で一致（NaN・欠けは違反に数える＝fail-closed）。
#   J4(c)  higherCoverShare＝0 の国×四半期で higherSubsidyPerMonth は厳密に 0。
#   記録  R-cap（higherCoverShare≧0.94 の国×四半期の数）・R-jump（教育法の符号が変わる前後の higherCoverShare）・R-fill（higherFillShare の国×十年の中央値）・R-sub（補助÷授業料の中央値）。
import csv, sys, statistics as st, math

B = sys.argv[1].rstrip('/') + '/'
NS = int(sys.argv[2]) if len(sys.argv) > 2 else 10
NEW = ['higherTuitionPerMonth', 'higherSubsidyPerMonth', 'higherSubsidyExpectedPerMonth', 'higherCoverShare', 'higherFillShare']


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


def bad(x):
    return not (x == x)   # NaN


def decade(year):
    return int(year) // 10 * 10


tot = dict(seeds_seen=0, entrance=0, subpos=0, b_rows=0, b_viol=0, c_rows=0, c_viol=0, cap=0, nan_rows=0)
fill = {}
subshare = {}
jump = []
for s in range(1, NS + 1):
    ed = list(rows(f'{B}ed_s{s}.tsv'))
    if not ed:
        print(f'seed {s}: ed_s{s}.tsv が無い（欠け＝不合格に倒す）')
        continue
    if any(c not in ed[0] for c in NEW):
        sys.exit('ed_s に奨学金の列が無い（旧の木）')
    tot['seeds_seen'] += 1
    ent = False; sp = False
    law = {}
    for r in rows(f'{B}reform_s{s}.tsv'):
        law[(r['quarter'], r['nation'])] = r['educationLawCode']
    prev = {}
    for r in ed:
        tu, su, ex, cv, fl = (num(r[c]) for c in NEW)
        key = (r['quarter'], r['nation'])
        if any(bad(x) for x in (tu, su, ex, cv)):
            tot['nan_rows'] += 1
            continue
        if tu > 0: ent = True
        if su > 0: sp = True
        if tu > 0:
            tot['b_rows'] += 1
            if abs(su - ex) > 1e-6 * max(abs(su), abs(ex), 1e-12): tot['b_viol'] += 1
            subshare.setdefault(decade(r['year']), []).append(su / tu)
        if cv == 0:
            tot['c_rows'] += 1
            if su != 0: tot['c_viol'] += 1
        if cv >= 0.94: tot['cap'] += 1
        if not bad(fl): fill.setdefault((r['nation'], decade(r['year'])), []).append(fl)
        lc = law.get(key)
        pl = prev.get(r['nation'])
        if pl is not None and pl[0] is not None and lc is not None and lc != pl[0]:
            jump.append((s, r['nation'], r['year'], pl[0], lc, pl[1], cv))
        prev[r['nation']] = (lc, cv)
    tot['entrance'] += ent; tot['subpos'] += sp
    print(f'seed {s}: 入口(授業料>0)={ent} 補助>0={sp}')
print('---')
print(f"J4(a) 入口が開いた種 {tot['entrance']}/{tot['seeds_seen']}（点火の確かめ）・補助>0 の種 {tot['subpos']}/{tot['seeds_seen']}")
print(f"J4(b) 授業料>0 の行 {tot['b_rows']} 件中 違反 {tot['b_viol']}（NaN の行 {tot['nan_rows']} 件は別に数える）")
print(f"J4(c) 割合=0 の行 {tot['c_rows']} 件中 補助が 0 でない {tot['c_viol']}")
print(f"R-cap 割合≧0.94 の国×四半期 {tot['cap']}")
if jump:
    print(f'R-jump 教育法の符号が変わった前後の割合（{len(jump)} 件・先頭 10 件）：種・国・年・前の法・後の法・前の割合・後の割合')
    for j in jump[:10]: print('  ', j)
else:
    print('R-jump なし')
print('R-sub 補助÷授業料の中央値（十年ごと）：', {d: round(st.median(v), 4) for d, v in sorted(subshare.items())})
print('R-fill higherFillShare の国×十年の中央値：')
NAT9 = ['清', 'ロシア', 'オスマン', 'フランス', 'イギリス', 'プロイセン', 'オーストリア', '日本', 'アメリカ']
for (n, d), v in sorted(fill.items()):
    if n not in NAT9: continue
    print(f'  {n} {d}s {st.median(v):.5f}')
