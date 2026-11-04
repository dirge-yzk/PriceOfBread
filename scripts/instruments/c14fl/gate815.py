# (815) c14fl 門の台本（読むだけ・事前登録 measurements/m815_20261104_prereg_fl.txt [4]）
# 使い方：python gate815.py <腕の ins> <旗 0 の ins（cpworld_s のある物＝ident の計器つきの腕）> <旗 0 の ins（divhost_s・nation_s＝keep_813/base_ins）> <種 例 1-10> [--short]
#   --short：突然変異の短い腕（G1・G2 だけを読む・G3・G4 は 1949 年が要る）
import sys, csv, os, collections
arm, base_cp, base_dh, spec = sys.argv[1:5]
short = '--short' in sys.argv
seeds = list(range(int(spec.split('-')[0]), int(spec.split('-')[1]) + 1)) if '-' in spec else [int(x) for x in spec.split(',')]
def rows(path):
    with open(path, encoding='utf-8') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for row in r: yield ix, row
def q(a, p):
    a = sorted(a)
    return a[min(len(a) - 1, int(p * len(a)))] if a else float('nan')
# ---- G1 事象ごと
g1n = 0; g1out = 0; g1floor = 0; g1floorOut = 0; g1lo = float('inf'); g1hi = -float('inf'); g1sumA = 0.0; g1sumE = 0.0
for s in seeds:
    p = os.path.join(arm, f'cpwd_s{s}.tsv')
    if not os.path.exists(p): continue
    for ix, row in rows(p):
        we = float(row[ix['woExpectedIndex']]); wa = float(row[ix['woActualIndex']]); e = float(row[ix['fxRate']])
        f1 = float(row[ix['fciAtCoupon']]); f2 = float(row[ix['fciAfterWriteDown']])
        if we > 1e-9 and not (we > 1e-9 * f1):   # ★追記 1：浮動小数の床の下＝台帳の値で確かめる
            g1floor += 1
            if abs(f2 - (f1 - we)) > 1e-12 * f1: g1floorOut += 1; g1out += 1
            continue
        if we > 1e-9:
            g1n += 1; r = wa / we; g1lo = min(g1lo, r); g1hi = max(g1hi, r); g1sumA += wa * e; g1sumE += we * e
            if not (0.999 <= r <= 1.001): g1out += 1
        elif abs(wa) > 1e-9:
            g1n += 1; g1out += 1   # 落とすはずの無い事象で落ちた
print(f'G1 事象 {g1n}・床の下の事象 {g1floor}（台帳の外れ {g1floorOut}）・外れ {g1out}・比の最小 {g1lo:.6g}・最大 {g1hi:.6g}・額の比（金建て）{g1sumA / g1sumE if g1sumE else float("nan"):.6g}＝{"通る" if g1n > 0 and g1out == 0 else "落ちる"}')
# ---- G2 呼び出しごと
def g2(d):
    n = 0; out = 0; skip = 0; clip = 0; woAll = 0.0; woSC = 0.0; worst = 0.0; flAll = 0.0; flSC = 0.0
    for s in seeds:
        p = os.path.join(d, f'cpworld_s{s}.tsv')
        if not os.path.exists(p): continue
        for ix, row in rows(p):
            od = float(row[ix['ownerDropGold']]); hf = float(row[ix['hostFlightDropGold']]); hw = float(row[ix['hostWoDropGold']])
            sk = int(row[ix['skip']]); cl = int(row[ix['clipNations']])
            n += 1; woAll += hw; flAll += hf
            if sk or cl:
                skip += sk; clip += 1 if cl else 0; woSC += hw; flSC += hf; continue
            h = hf + hw
            tol = 1e-9 * max(od, h, 1e-6) + 1e-12 * float(row[ix['ownerBeforeGold']])   # ★追記 1：和の丸めの桁
            dev = abs(od - h)
            worst = max(worst, dev / max(od, h, 1e-6))
            if dev > tol: out += 1
    return n, out, skip, clip, woAll, woSC, worst, flAll, flSC
n, out, sk, cl, woAll, woSC, worst, flAll, flSC = g2(arm)
share = woSC / woAll if woAll > 0 else 0.0
print(f'G2 呼び出し {n}・保存の外れ {out}（相対の最大 {worst:.3g}）・skip {sk}・clip の呼び出し {cl}・skip か clip の落としの割合 {share:.3g}＝{"通る" if n > 0 and out == 0 and share <= 1e-3 else "落ちる"}（母集合が空なら落ちる）')
if os.path.isdir(base_cp):
    bn, bout, bsk, bcl, _, _, bworst, bfl, bflSC = g2(base_cp)
    print(f'   旗 0（{os.path.basename(base_cp.rstrip("/"))}）：呼び出し {bn}・保存の外れ {bout}（相対の最大 {bworst:.3g}）・skip {bsk}・clip の呼び出し {bcl}・skip か clip の逃避の割合（記録）{bflSC / bfl if bfl > 0 else 0:.3g}')
print(f'   記録：腕の skip か clip の逃避の割合 {flSC / flAll if flAll > 0 else 0:.3g}')
if short: sys.exit(0)
# ---- G3 世界の外資の負債の和（1949 年の 4 四半期の和）
def wsum(d):
    m = {}
    for s in seeds:
        p = os.path.join(d, f'cpworld_s{s}.tsv'); t = 0.0; last = {}
        for ix, row in rows(p):
            if int(row[ix['year']]) == 1949: last[row[ix['quarter']]] = float(row[ix['fciSumGold']])
        m[s] = sum(last.values())
    return m
A = wsum(arm); B = wsum(base_cp)
rs = [A[s] / B[s] for s in seeds if B.get(s, 0) > 0]
print(f'G3 1949 の世界の外資の負債の和 腕÷旗 0 の種の中央 {q(rs, 0.5):.4g}（種ごと {", ".join(f"{x:.3f}" for x in rs)}）＝{"通る" if q(rs, 0.5) >= 0.2 else "落ちる"}（≧0.2）')
# ---- G4 外資の負債÷年 GDP（fci>0 の国・1949 の 4 四半期の延べ）の中央の比
def fg(d):
    out = collections.defaultdict(list)
    for s in seeds:
        gdp = {}
        for ix, row in rows(os.path.join(d, f'nation_s{s}.tsv')): gdp[(row[ix['quarter']], row[ix['nation']])] = float(row[ix['nominalGdpPerYear']])
        last = {}
        for ix, row in rows(os.path.join(d, f'divhost_s{s}.tsv')):
            y = int(row[ix['year']])
            if y in (1850, 1900, 1949): last[(row[ix['quarter']], row[ix['nation']])] = (y, float(row[ix['fci']]))
        for (qq, nat), (y, f) in last.items():
            g = gdp.get((qq, nat))
            if g and g > 0 and f > 0: out[y].append(f / g)
    return out
FA = fg(arm); FB = fg(base_dh)
r4 = q(FA[1949], 0.5) / q(FB[1949], 0.5)
print(f'G4 外資の負債÷年 GDP の 1949 の中央 腕 {q(FA[1949], 0.5):.4g}・旗 0 {q(FB[1949], 0.5):.4g}・比 {r4:.4g}＝{"通る" if 0.1 <= r4 <= 0.8 else "落ちる"}（[0.1, 0.8]）')
for y in (1850, 1900, 1949):
    print(f'   記録 {y}：腕 中央 {q(FA[y], 0.5):.4g} p90 {q(FA[y], 0.9):.4g} n {len(FA[y])}／旗 0 中央 {q(FB[y], 0.5):.4g} p90 {q(FB[y], 0.9):.4g} n {len(FB[y])}')
# ---- 記録：未払いの額の割合
cl = 0.0; un = 0.0
for s in seeds:
    for ix, row in rows(os.path.join(arm, f'cpworld_s{s}.tsv')): cl += float(row[ix['claimGold']]); un += float(row[ix['unpaidGold']])
print(f'記録 未払いの額の割合（腕）{un / cl if cl else float("nan"):.4f}（旗 0 の紙の台本 0.5733）')
