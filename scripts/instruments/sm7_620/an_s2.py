# (588) 線 sm6：S2（差し引きの増えが δ_N×N を越える）の出所の分解と、開幕の sumN の読み（読むだけ）。事前登録 measurements/sm6_588_20261027_prereg.txt [4] ①②。
# 使い方：python -X utf8 an_s2.py <計器の出力フォルダ（slot_s<seed>.tsv を持つ）> [<seed>...]
# 出す物：
#   S2 全体＝国×四半期の (added − wornN) ÷ (deltaN × max(nStart, sumN) × 3) が (1+δ)^2 を超えた行の数・最大（全ての国）。
#   S2 私的＝(addedPriv − wornN) ÷ (…) の同じ数え。巨大建造物＝addedMega の寄与で超えた行（addedMega を除くと超えない行）。国家（addedState）は書き手が消えた＝常に 0 のはず。
#   超えた行の上位（国・四半期・倍率・出所の内訳）。
import csv, glob, os, sys, math

def rows(p):
    if not os.path.exists(p): return
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r, None)
        if h is None: return
        for x in r:
            if len(x) == len(h): yield dict(zip(h, x))

def fl(x):
    try: return float(x)
    except Exception: return float('nan')

d = sys.argv[1]
seeds = [int(x) for x in sys.argv[2:]] or sorted(int(os.path.basename(p)[6:-4]) for p in glob.glob(os.path.join(d, 'slot_s*.tsv')))
tot = bad_all = bad_priv = bad_mega_only = 0
worst = []
for s in seeds:
    for r in rows(os.path.join(d, f'slot_s{s}.tsv')):
        n0 = fl(r['nStart']); dl = fl(r['deltaN']); w = fl(r['wornN'])
        if not (n0 > 0 and dl > 0): continue
        nE = fl(r['sumN']); base = dl * max(n0, nE if nE == nE else 0.0) * 3.0; tol = (1.0 + dl) ** 2   # 分母は四半期の始めと終わりの大きい方（巨大建造物の足しが四半期の中で N を増やした月の損耗・増えを受けるため）
        a = fl(r['added']); ap = fl(r['addedPriv']); am = fl(r['addedMega']); ast = fl(r['addedState'])
        tot += 1
        ratio_all = (a - w) / base; ratio_priv = (ap - w) / base
        if ratio_all > tol: bad_all += 1
        if ratio_priv > tol: bad_priv += 1
        if ratio_all > tol and ratio_priv <= tol: bad_mega_only += 1
        if ratio_all > tol: worst.append((ratio_all, ratio_priv, s, r['nation'], int(r['quarter']), n0, ap, am, ast))
print(f'国×四半期の行 {tot}・S2 全体の超過 {bad_all}・私的だけの超過 {bad_priv}・巨大建造物の寄与で初めて超えた行 {bad_mega_only}')
worst.sort(reverse=True)
for x in worst[:25]:
    print(f'  全体 {x[0]:.2f}倍 私的 {x[1]:.2f}倍 種{x[2]} {x[3]} Q{x[4]} nStart={x[5]:.1f} addedPriv={x[6]:.3f} addedMega={x[7]:.3f} addedState={x[8]:.3f}')

print()

# ---- 判定の表（事前登録 追記 3）：門＝S2 私的（配線の確認・① が無くても式から通る）・S2m 桁（総量）。② は種 1〜10 では記録・種 11〜20 でだけ門
gate_open = all(s >= 11 for s in seeds)
print(f'[S2 私的＝門（配線の確認・全 61 国）] 超過行 {bad_priv}/{tot}  [{"PASS" if bad_priv == 0 and tot > 0 else "FAIL"}]')
print(f'[R-mega＝記録→段3 の前提] 総量の超過行 {bad_all}/{tot}（巨大建造物の足しを含む）')
# S2m 桁：国×四半期で ΣN の四半期末÷四半期始 が 10 倍を超える（nStart≧1 枠の国）。巨大建造物を含む総量・発散の定義（桁）
m10 = 0; mx = 0.0; wst = None; rows_m = 0
for s in seeds:
    for r in rows(os.path.join(d, f'slot_s{s}.tsv')):
        n0 = fl(r['nStart']); nE = fl(r['sumN'])
        if not (n0 >= 1.0) or nE != nE: continue
        rows_m += 1; v = nE / n0
        if v > mx: mx = v; wst = (s, r['nation'], int(r['quarter']), n0, nE, fl(r['addedMega']))
        if v > 10.0: m10 += 1
print(f'[S2m＝門（総量の桁）] 国×四半期 {rows_m}・ΣN 末÷始 の最大 {mx:.2f} {wst}・10 倍超 {m10}  [{"PASS" if rows_m > 0 and m10 == 0 else "FAIL"}]')
# R-fac：工場ごとの私的な純増の国の最大（配りの上限が無い形で、工場が δ_N×N の何倍まで受け取るか）
fm = []; 
for s in seeds:
    for r in rows(os.path.join(d, f'slot_s{s}.tsv')):
        v = fl(r.get('facMaxNet', 'nan'))
        if v == v: fm.append(v)
if fm:
    fm.sort(); print(f'[R-fac＝記録] 工場ごとの私的な純増 (addedPriv−worn)÷(δ_N×nStart×3) の国×四半期の最大: 最大 {fm[-1]:.2f}・99.9% 点 {fm[int(len(fm)*0.999)]:.2f}・中央値 {fm[len(fm)//2]:.3f}（行 {len(fm)}）')
# ② 開幕の ΣN÷headsSum（61 国×種）≦ 1.25
mx = 0.0; nb = 0; nn = 0; wst = None; sl = []
for s in seeds:
    for r in rows(os.path.join(d, f'slotopen_s{s}.tsv')):
        h = fl(r.get('headsSum', 'nan')); n = fl(r['sumN'])
        v2 = fl(r.get('sleepNShare', 'nan'))
        if v2 == v2: sl.append(v2)
        if not (h > 0): continue
        nn += 1; v = n / h
        if v > mx: mx = v; wst = (s, r['nation'])
        if v > 1.25: nb += 1
lab = 'PASS' if nn > 0 and nb == 0 else ('NA' if nn == 0 else 'FAIL')
print(f'[② 開幕の ΣN÷headsSum（61 国×種）] 国×種 {nn}・最大 {mx:.3f} {wst}・1.25 超 {nb}  [{lab}]（{"門" if gate_open else "記録＝種 1〜10 は canary を見てから置いた線"}）')
if sl: sl.sort(); print(f'[R-A0＝記録] 開幕の N のうち頭数 0 の個体（眠り）の割合 sleepNShare: 中央値 {sl[len(sl)//2]:.4f}・最大 {sl[-1]:.4f}・国×種 {len(sl)}（A0 の分子に眠りの損耗が入る割合）')
