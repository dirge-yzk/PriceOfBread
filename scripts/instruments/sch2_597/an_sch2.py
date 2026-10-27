#!/usr/bin/env python
# (597) 奨学金の法（教育費の軸の最上段だけ）の読み手：事前登録 measurements/sch2_597_20261027_prereg.txt の [3]（J2・K1・K2・記録）と [8]（R1〜R6・D1）を、計器つき 10 種の ed_s から読む。
# 使い方：PYTHONUTF8=1 python an_sch2.py <B の ins フォルダ> [<B0 の ins フォルダ>|-] [種の数=10] [対照の ins フォルダ...]
#   ed_s の末尾 7 列（higherTuitionPerMonth・higherSubsidyPerMonth・higherSubsidyExpectedPerMonth・higherCoverShare・higherFillShare・eduStage・eduStageMismatchShare）を読む。無ければ即終了。
#   K1    入口が開いたか＝higherTuitionPerMonth＞0 の国×四半期がある種の数（≧8/10 で通る。外れたら J2 の腕の中の観測は blocked）。種のファイルが欠けても分母は種の数のまま（欠け＝不合格に倒す）。
#   K2    eduStage＝3 かつ higherTuitionPerMonth＞0 の国×四半期がある種の数（≧5/10 なら J2 の正の半分と D1 を判定する。足りなければ blocked）。
#   J2零  eduStageMismatchShare＝0 かつ eduStage≦2 の国×四半期で higherSubsidyPerMonth と higherCoverShare は厳密に 0（違反 0 で通る）。
#   J2正  eduStageMismatchShare＝0 かつ eduStage＝3・授業料＞0 の国×四半期で（補助＞0 ⇔ 期待値＞0）かつ、割合＞0 なら補助と期待値が 1e-6 の相対で一致（違反 0 で通る）。
#   NaN   eduStage・授業料・補助・期待値・割合・食い違いが NaN の行は fail-closed（違反に数える）。
#   食い違い（eduStageMismatchShare＞0）の行は違反に数えず R-mismatch に件数と、補助か割合が 0 でない件数を記録（全体の 2% を超えたら J2 は「弱い」と書く）。
#   D1    B0 を渡したとき：種ごとに fB（B の eduStage＝3・授業料＞0・割合＞0 の行の higherFillShare の中央値）と fB0（B0 の eduStage＝3・授業料＞0 の行の中央値）。
#         fB0≧0.98 の種は飽和で除く。両腕とも該当行が 5 件以上ある種だけ評価。評価できる種が 5 未満なら blocked。通る＝fB≧fB0 が 8 割以上かつ fB＞fB0 が 5 割以上。
#   D1b   記録：B の最初の補助が出る四半期 q0 より前の行が B0 と（補助系 3 列を除いて）一致する種で、q0 から 4 四半期の補助が出た国の充足を B と B0 で並べる。
#   記録  R-stage・R-fill・R-sub・R-cap（補助が出る行のうち割合≧0.94 の割合）・R-flip（国×種ごとの段 3 への入りと出）・R-jump（入った行の割合）・R-mismatch。
#   対照の ins フォルダ（基線 m592_ins・N1・N2 など）は higherFillShare・授業料の合計だけを読む（入口が閉じていれば全て 0）＝RULE36 の国ごとの数の並べ方の対照。
import csv, sys, statistics as st

args = sys.argv[1:]
B = args[0].rstrip('/') + '/'
B0 = None
if len(args) > 1 and args[1] != '-': B0 = args[1].rstrip('/') + '/'
NS = int(args[2]) if len(args) > 2 else 10
CTRL = [a.rstrip('/') + '/' for a in args[3:]]
NEW = ['higherTuitionPerMonth', 'higherSubsidyPerMonth', 'higherSubsidyExpectedPerMonth', 'higherCoverShare', 'higherFillShare', 'eduStage', 'eduStageMismatchShare']
SUBSYS = ('higherSubsidyPerMonth', 'higherSubsidyExpectedPerMonth', 'higherCoverShare')


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


def load(folder, s):
    ed = list(rows(f'{folder}ed_s{s}.tsv'))
    if ed and any(c not in ed[0] for c in NEW):
        sys.exit(f'{folder}ed_s{s}.tsv に eduStage 系の列が無い（旧の木）')
    return ed


def vals(r):
    return {c: num(r[c]) for c in NEW}


tot = dict(seeds=0, missing=0, k1=0, k2=0, zero_rows=0, zero_viol=0, pos_rows=0, pos_viol=0, iff_viol=0, nan_rows=0,
           mm_rows=0, mm_nonzero=0, rows=0, cap_rows=0, sub_rows=0)
stage_cnt = {}; stage3_nations = {}; fill_s3 = {}; fill_other = {}; subshare = {}
flip_in = []; flip_out = []; jump = []
perseed = {}
for s in range(1, NS + 1):
    ed = load(B, s)
    if not ed:
        tot['missing'] += 1
        print(f'seed {s}: ed_s{s}.tsv が無い（欠け＝不合格に倒す）')
        continue
    tot['seeds'] += 1
    k1 = False; k2 = False
    prev = {}; nin = nout = 0
    fb = []; s3n = set()
    for r in ed:
        v = vals(r)
        tu, su, ex, cv, fl, es, mm = (v[c] for c in NEW)
        tot['rows'] += 1
        if nan(es) or nan(tu) or nan(su) or nan(ex) or nan(cv) or nan(mm):
            tot['nan_rows'] += 1
            continue
        d = decade(r['year'])
        stage = int(round(es))
        stage_cnt.setdefault(d, [0, 0, 0, 0])[min(max(stage, 0), 3)] += 1
        if tu > 0: k1 = True
        key = r['nation']
        was3 = prev.get(key, False)
        if stage >= 3 and not was3: nin += 1; jump.append(cv)
        if stage < 3 and was3: nout += 1
        prev[key] = stage >= 3
        if mm > 0:
            tot['mm_rows'] += 1
            if su != 0 or cv != 0: tot['mm_nonzero'] += 1
        else:
            if stage <= 2:
                tot['zero_rows'] += 1
                if su != 0 or cv != 0: tot['zero_viol'] += 1
            elif tu > 0:
                tot['pos_rows'] += 1
                if (su > 0) != (ex > 0): tot['iff_viol'] += 1
                if cv > 0 and (not (su > 0) or abs(su - ex) > 1e-6 * max(abs(su), abs(ex), 1e-12)): tot['pos_viol'] += 1
        if stage >= 3:
            stage3_nations.setdefault(d, set()).add(key); s3n.add(key)
            if tu > 0:
                k2 = True
                if su > 0: subshare.setdefault(d, []).append(su / tu)
        if su > 0:
            tot['sub_rows'] += 1
            if cv >= 0.94: tot['cap_rows'] += 1
        if not nan(fl): (fill_s3 if stage >= 3 else fill_other).setdefault(d, []).append(fl)
        if stage == 3 and tu > 0 and cv > 0 and not nan(fl): fb.append(fl)
    flip_in.append(nin); flip_out.append(nout)
    tot['k1'] += k1; tot['k2'] += k2
    perseed[s] = dict(fB=(st.median(fb) if len(fb) >= 5 else None), nB=len(fb), s3n=len(s3n), nin=nin, nout=nout)
    print(f'seed {s}: K1 入口(授業料>0)={k1} K2 段3かつ授業料>0={k2} 段3の国={len(s3n)} 入り={nin} 出={nout} 段3&授業料&割合の行={len(fb)}')
print('---')
N = NS
print(f"K1 入口が開いた種 {tot['k1']}/{N}（≧8/10 で通る・欠け {tot['missing']} 種）")
print(f"K2 段3かつ授業料>0 の国×四半期がある種 {tot['k2']}/{N}（≧5/10 で正の半分と D1 を判定）")
ok_k1 = tot['k1'] >= 8 and tot['missing'] == 0
ok_k2 = tot['k2'] >= 5
print(f"J2零 食い違い無しの段≦2 の行 {tot['zero_rows']} 件中 補助か割合が 0 でない {tot['zero_viol']}（違反 0 で通る）")
print(f"J2正 食い違い無しの段3・授業料>0 の行 {tot['pos_rows']} 件中 (補助>0)≠(期待値>0) {tot['iff_viol']}・割合>0 で補助が 0 か期待値と不一致 {tot['pos_viol']}（違反 0 で通る）")
print(f"NaN の行（fail-closed＝違反に数える）{tot['nan_rows']} 件")
mmr = tot['mm_rows'] / max(tot['rows'], 1)
print(f"R-mismatch 食い違い（州の写しの段≠国の段）の行 {tot['mm_rows']}/{tot['rows']}（{mmr:.4%}）・うち補助か割合が 0 でない {tot['mm_nonzero']}"
      + ('・2% を超えた＝J2 の判定は弱い' if mmr > 0.02 else ''))
j2_zero = tot['zero_viol'] == 0 and tot['nan_rows'] == 0
j2_pos = (tot['pos_viol'] == 0 and tot['iff_viol'] == 0 and tot['nan_rows'] == 0) if ok_k2 else None
print(f"R-cap 補助が出る行 {tot['sub_rows']} 件のうち割合≧0.94 の割合 " + (f"{tot['cap_rows'] / tot['sub_rows']:.3f}" if tot['sub_rows'] else 'n/a'))
print(f"R-flip 国×種ごとの段 3 への入り（種ごとの合計）{flip_in}・出 {flip_out}")
if jump: print(f"R-jump 段 3 に入った行の割合の中央値 {st.median(jump):.4f}・最大 {max(jump):.4f}（n={len(jump)}）")
print('R-stage 段 0/1/2/3 の国×四半期の件数（十年ごと）と段 3 の国の数：')
for d, c in sorted(stage_cnt.items()):
    print(f'  {d}s {c}  段3の国 {len(stage3_nations.get(d, ()))}')
print('R-sub 段 3 の補助÷授業料の中央値（十年ごと）：', {d: round(st.median(v), 4) for d, v in sorted(subshare.items())})
print('R-fill higherFillShare の中央値（十年ごと・段3 / それ以外）：')
for d in sorted(set(fill_s3) | set(fill_other)):
    a = st.median(fill_s3[d]) if d in fill_s3 else float('nan')
    b = st.median(fill_other[d]) if d in fill_other else float('nan')
    print(f'  {d}s {a:.5f} / {b:.5f}')

d1 = None
if B0:
    ev = 0; ge = 0; gt = 0; sat = 0
    print('D1 B と B0（段3・授業料>0。B は割合>0 も）の higherFillShare の中央値（種ごと）：')
    for s in range(1, NS + 1):
        e0 = load(B0, s)
        p = perseed.get(s)
        if not e0 or p is None:
            print(f'  seed {s}: ファイル欠け'); continue
        f0 = [num(r['higherFillShare']) for r in e0 if num(r['eduStage']) == 3 and num(r['higherTuitionPerMonth']) > 0 and not nan(num(r['higherFillShare']))]
        fB0 = st.median(f0) if len(f0) >= 5 else None
        fB = p['fB']
        if fB0 is not None and fB0 >= 0.98:
            sat += 1; print(f'  seed {s}: B0 が飽和（{fB0:.4f}）で除く'); continue
        if fB is None or fB0 is None:
            print(f'  seed {s}: 該当行が足りない（B {p["nB"]}・B0 {len(f0)}）'); continue
        ev += 1; ge += fB >= fB0; gt += fB > fB0
        print(f'  seed {s}: fB={fB:.5f} fB0={fB0:.5f} ({"B>B0" if fB > fB0 else "B=B0" if fB == fB0 else "B<B0"})')
    if ev < 5:
        d1 = None; print(f'D1 blocked（評価できる種 {ev}<5・飽和で除いた種 {sat}）')
    else:
        d1 = ge >= 0.8 * ev and gt >= 0.5 * ev
        print(f'D1 評価できる種 {ev}（飽和除外 {sat}）：fB≧fB0 {ge}・fB＞fB0 {gt} → {"通る" if d1 else "外れ（採らない）"}')
    # D1b：最初の補助より前の行が一致する種
    print('D1b（記録）最初の補助の四半期 q0 より前の行が B0 と一致する種：')
    for s in range(1, NS + 1):
        eb = load(B, s); e0 = load(B0, s)
        if not eb or not e0: continue
        q0 = None; n0 = set()
        for r in eb:
            if num(r['higherSubsidyPerMonth']) > 0:
                q = int(r['quarter'])
                if q0 is None or q < q0: q0 = q; n0 = {r['nation']}
                elif q == q0: n0.add(r['nation'])
        if q0 is None: print(f'  seed {s}: 補助が出ていない'); continue
        pre_b = [r for r in eb if int(r['quarter']) < q0]; pre_0 = [r for r in e0 if int(r['quarter']) < q0]
        same = len(pre_b) == len(pre_0) and all(all(a[c] == b[c] for c in a if c not in SUBSYS) for a, b in zip(pre_b, pre_0))
        if not same: print(f'  seed {s}: q0={q0} より前から軌道が分かれている（比べない）'); continue
        def fills(rs): return [(int(r['quarter']), r['nation'], num(r['higherFillShare'])) for r in rs if q0 <= int(r['quarter']) <= q0 + 4 and r['nation'] in n0]
        print(f'  seed {s}: q0={q0} 国={sorted(n0)}\n    B  {fills(eb)}\n    B0 {fills(e0)}')

if CTRL:
    print('対照（基線・N1・N2 など。入口が閉じていれば授業料と充足は 0）：')
    for c in CTRL:
        tu = 0.0; fl = 0.0; n = 0
        for s in range(1, NS + 1):
            for r in rows(f'{c}ed_s{s}.tsv'):
                n += 1; tu += max(0.0, num(r.get('higherTuitionPerMonth', 'nan')) or 0.0); fl += max(0.0, num(r.get('higherFillShare', 'nan')) or 0.0)
        print(f'  {c}: 行 {n}・授業料の合計 {tu:.6g}・充足の合計 {fl:.6g}')

print('---')
print(f"判定（機構）：K1 {'通る' if ok_k1 else '外れ'}・J2零 {'通る' if j2_zero else '外れ'}・J2正 {'blocked' if j2_pos is None else ('通る' if j2_pos else '外れ')}・D1 {'blocked/なし' if d1 is None else ('通る' if d1 else '外れ')}")
