# (1002) c92gf：gates997.py の写し・G-L3 の床だけ isfinite を足した（他は 1 字も変えない）。元の説明：
# (997) c91sl 門の台本（読むだけ）：gates991.py の写し・G-L3 だけ事前登録 measurements/m997_20261109_prereg_sl.txt [5] の形（相対 1e-9 か、行の |実際−写し| ≦ 1e-12 の両側）。他の門は 1 字も変えない。
# 使い方: python gates991.py <生のフォルダ（landsale_s<種>.tsv）> [種の数=10] [種の起点=1]
# 足した列が無い生（旗 0・基線の生）は実際の側を 0 と読む＝G-L1 の下限・G-L2b・G-L3 の母集合が空で赤（旗 0 で落ちることの確かめ）。
import sys, os, math, statistics as st
d = sys.argv[1]; ns = int(sys.argv[2]) if len(sys.argv) > 2 else 10; s0 = int(sys.argv[3]) if len(sys.argv) > 3 else 1
L1_LO = 0.9; TOL = 1e-9; SLOT_TOL = 1e-12; L2_LO = 0.9; L4_HI = 0.05; L3_ABS = 1e-12   # L3_ABS＝Model.cs の Pop.Take の額の床（旗 TakeFloorPositive 0 の 1e-12）と同じ桁＝和の順の丸めだけを許す（保有の床 1e-9 は写しの側＝c91lc）
def f(x):
    try: return float(x)
    except: return float('nan')
viol = {'L1_row': 0, 'L3': 0, 'L2a': 0}
seed_l1, seed_l2, seed_l4, seed_e4 = [], [], [], []
nat = {}   # nation -> {seed: [shareNetSum, netSum]}
nat_exp = {}
ev_total = 0; buyers = 0; buyers_noninv = 0; rows = 0; maxrel = 0.0; maxslot = 0.0; has_cols = True
for s in range(s0, s0 + ns):
    p = os.path.join(d, f'landsale_s{s}.tsv')
    act = exp = drop = phi = shn = nsum = esn = enn = 0.0
    with open(p, encoding='utf-8') as fh:
        h = fh.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        if 'saleActualPaidPerQuarter' not in ix: has_cols = False
        g = lambda v, k: f(v[ix[k]]) if k in ix else 0.0
        for ln in fh:
            v = ln.rstrip('\n').split('\t'); rows += 1
            a = g(v, 'saleActualPaidPerQuarter'); e = g(v, 'saleIncludedUnpaidPerQuarter')
            if not (a <= e * (1 + TOL) + 1e-12): viol['L1_row'] += 1
            if g(v, 'saleLawOpenFlag') > 0: act += a; exp += e
            r3 = g(v, 'salePaidCopyMaxRelDiff')
            a3 = g(v, 'saleCopyPaidPerQuarter')
            if not (math.isfinite(r3) and (r3 <= TOL or abs(a - a3) <= L3_ABS)): viol['L3'] += 1   # (1002) 番兵（+inf・nan）を通さない
            maxrel = max(maxrel, r3) if r3 == r3 else float('nan')
            sd = g(v, 'saleSlotSumMaxAbsDev')
            if not (sd <= SLOT_TOL): viol['L2a'] += 1
            maxslot = max(maxslot, sd) if sd == sd else float('nan')
            drop += g(v, 'saleHeldDropSum'); phi += g(v, 'salePhiSum')
            a4 = g(v, 'saleActualShareNetSum'); n4 = g(v, 'saleActualNetSum'); shn += a4; nsum += n4
            nn = v[ix['nation']]; x = nat.setdefault(nn, {}).setdefault(s, [0.0, 0.0]); x[0] += a4; x[1] += n4
            ne = g(v, 'saleNetRentPerQuarter'); se = g(v, 'saleExpectedSharePerYear')
            y = nat_exp.setdefault(nn, {}).setdefault(s, [0.0, 0.0])
            if ne > 0 and se == se: y[0] += se * ne; y[1] += ne; esn += se * ne; enn += ne
            ev_total += g(v, 'saleEventCount'); buyers += g(v, 'saleBuyerCount'); buyers_noninv += g(v, 'saleBuyerNonInvestorCount')
    seed_l1.append(act / exp if exp > 0 else float('nan'))
    seed_l2.append(drop / phi if phi > 0 else float('nan'))
    seed_l4.append(shn / nsum if nsum > 0 else float('nan'))
    seed_e4.append(esn / enn if enn > 0 else float('nan'))
print(f'# gates997 {d}・種 {s0}〜{s0+ns-1}・行 {rows}・足した列 {"あり" if has_cols else "なし（実際の側を 0 と読む）"}・売りの件数 {ev_total:.0f}')
ok = lambda b: '通る' if b else '赤'
g1lo = all(x == x and x >= L1_LO for x in seed_l1)
print(f'G-L1 上側（行ごと 実際 ≤ 期待×(1+1e-9)）違反 {viol["L1_row"]} 行 → {ok(viol["L1_row"] == 0)}')
print(f'G-L1 下側（種ごと 法が開いた行の Σ実際÷Σ期待 ≥ {L1_LO}）{["%.6f" % x for x in seed_l1]} 最小 {min(seed_l1) if seed_l1 else float("nan"):.6f} → {ok(g1lo)}')
g2b = all(x == x and L2_LO <= x <= 1 + TOL for x in seed_l2)
print(f'G-L2a（RGO ごとの枠の和の動き ≤ {SLOT_TOL}）違反 {viol["L2a"]} 行・最大 {maxslot:.3g} → {ok(viol["L2a"] == 0)}')
print(f'G-L2b（種ごと Σ手放した割合÷Σφ ∈ [{L2_LO}, 1+1e-9]）{["%.6f" % x for x in seed_l2]} → {ok(g2b)}')
g3 = viol['L3'] == 0 and ev_total > 0
print(f'G-L3（値段の写しと払いの相対差 ≤ 1e-9 か 行の |実際−写し| ≤ {L3_ABS:g}・売りの件数 > 0）違反 {viol["L3"]} 行・最大 {maxrel:.3g}・件数 {ev_total:.0f} → {ok(g3)}')
g4 = all(x == x and x <= L4_HI for x in seed_l4) and len(seed_l4) > 0
print(f'G-L4（種ごと 世界でまとめた年に手放す割合 Σ12φ純÷Σ純 ≤ {L4_HI}）{["%.6f" % x for x in seed_l4]} → {ok(g4)}')
print(f'G-L4 の参照（記録）：期待の側でまとめた年の割合（983 の列）{["%.6f" % x for x in seed_e4]}')
print(f'G-L6（記録）：買い手の件数 {buyers:.0f}・うち投資家（資本家）でない {buyers_noninv:.0f}')
print('# 記録：国ごとの年に手放す割合（実際）の種の中央・期待の種の中央（期待は 983 の列 saleExpectedSharePerYear の純の地代の重み）')
for nn in sorted(nat, key=lambda k: -sum(x[1] for x in nat[k].values())):
    acts = [x[0] / x[1] for x in nat[nn].values() if x[1] > 0]
    exps = [x[0] / x[1] for x in nat_exp.get(nn, {}).values() if x[1] > 0]
    if not acts and not exps: continue
    print(f'  {nn}\t実際 {st.median(acts) if acts else 0:.6g}（{len(acts)} 種）\t期待 {st.median(exps) if exps else 0:.6g}（{len(exps)} 種）')
allg = viol['L1_row'] == 0 and g1lo and viol['L2a'] == 0 and g2b and g3 and g4
print(f'判定: {"全ての門が通る" if allg else "赤あり"}')
