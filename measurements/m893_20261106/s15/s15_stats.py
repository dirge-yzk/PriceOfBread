# (893) c76lg 読むだけ：S15 の門を、既にある生（計器つき 10 種）で数える。新しい腕なし。
# 使い方: python -X utf8 s15_stats.py <生のフォルダ> [<名>]  （例 D:/PoB_work/keep_892/base_ins R1'）
import sys, os, csv, collections, math
d = sys.argv[1]; name = sys.argv[2] if len(sys.argv) > 2 else d
seeds = range(1, 11) if not d.rstrip('/').endswith('a11') else range(11, 21)
def rows(t, s):
    p = os.path.join(d, f'{t}_s{s}.tsv')
    if not os.path.exists(p): return None
    with open(p, 'rb') as f:
        txt = f.read().decode('utf-8')
    lines = txt.split('\n')
    hdr = lines[0].rstrip('\r').split('\t')
    out = []
    for l in lines[1:]:
        if not l.strip(): continue
        v = l.rstrip('\r').split('\t')
        out.append(dict(zip(hdr, v)))
    return out
def f(x):
    try: return float(x)
    except: return float('nan')
P = ['絶対', '専制', '幕府', '立憲', '共和', 'ブル独', 'プロ独', '軍政', '無政府']
print(f'# 生 {name}＝{d}')
# 1. coupgate の経路
route = collections.Counter(); route_seed = collections.defaultdict(set); r4_pol = collections.Counter(); r5_att = collections.Counter(); r3_att = collections.Counter()
passOld = passSplit = n_cg = 0; nan_rows = 0
for s in seeds:
    R = rows('coupgate', s)
    if R is None: print(f'coupgate_s{s} 無い'); continue
    for r in R:
        n_cg += 1; rt = r['route']; route[rt] += 1; route_seed[rt].add(s)
        passOld += r['passOld'] == '1'; passSplit += r['passSplit'] == '1'
        if any(math.isnan(f(r[k])) for k in ('challenge', 'kappa', 'a')): nan_rows += 1
        if rt == '4': r4_pol[(P[int(r['polity'])], P[int(r['attractor'])])] += 1
        if rt == '5': r5_att[P[int(r['attractor'])]] += 1
        if rt == '3': r3_att[P[int(r['attractor'])]] += 1
print(f'coupgate 行 {n_cg}（第 1 の門を通った国×四半期）・経路', dict(sorted(route.items())), '・種の数', {k: len(v) for k, v in sorted(route_seed.items())})
print(f'  passOld=1 {passOld}・passSplit=1 {passSplit}・challenge/kappa/a が NaN の行 {nan_rows}')
print('  経路 4（下賜）の現政体→引力点', dict(r4_pol.most_common(8)))
print('  経路 5（暴力の即時）の引力点', dict(r5_att.most_common(9)))
print('  経路 3（首都の蜂起）の引力点', dict(r3_att.most_common(9)))
# 2. scalars：正統性・天命・敗戦ショック・政体の変化
leg = []; dm = []; ds_pos = 0; n_sc = 0; leg0 = leg1 = 0; dm_hi = 0; dm_lo = 0; cm_crown_dmhi = 0
trans = collections.Counter(); trans_seed = collections.Counter()
legmin_by = collections.defaultdict(lambda: 9.0)
for s in seeds:
    R = rows('scalars', s)
    if R is None: continue
    last = {}
    for r in R:
        n_sc += 1; L = f(r['legitimacy']); D = f(r['dynMandate']); leg.append(L); dm.append(D)
        if f(r['defeatShock']) > 0: ds_pos += 1
        if L <= 1e-12: leg0 += 1
        if L >= 1 - 1e-12: leg1 += 1
        if D >= 0.6: dm_hi += 1
        if D <= 0.4: dm_lo += 1
        if r['polityCode'] == '3' and r['crown'] not in ('0', 'None') and D >= 0.6: cm_crown_dmhi += 1
        k = r['nation']; pc = r['polityCode']
        if k in last and last[k] != pc: trans[(P[int(last[k])], P[int(pc)])] += 1; trans_seed[s] += 1
        last[k] = pc
def q(a, p):
    a = sorted(x for x in a if not math.isnan(x)); return a[int(p * (len(a) - 1))] if a else float('nan')
print(f'scalars 行 {n_sc}・正統性 p05 {q(leg,.05):.3f} p50 {q(leg,.5):.3f} p95 {q(leg,.95):.3f}・0 の行 {leg0}・1 の行 {leg1}・敗戦ショック＞0 の行 {ds_pos}')
print(f'  天命 p05 {q(dm,.05):.3f} p50 {q(dm,.5):.3f} p95 {q(dm,.95):.3f}・≥0.6 の行 {dm_hi}・≤0.4 の行 {dm_lo}・立憲×王冠×天命≥0.6 の行 {cm_crown_dmhi}（信任帝政の床の 3 条件・国民意識≥0.5 は列が無い）')
print(f'  政体の変化（行の間で polityCode が変わった・挙げた国は四半期・他は年末）計 {sum(trans.values())}・種ごと', dict(sorted(trans_seed.items())))
print('  上位', dict(trans.most_common(14)))
# 3. reform：政治資本
n_rf = 0; wall = wallL = wallS = 0; blkL = blkS = blkF = 0; wlb = 0; stock_pos = 0; spent_full = 0; mx = []
for s in seeds:
    R = rows('reform', s)
    if R is None: continue
    for r in R:
        n_rf += 1
        wall += int(f(r['wallPassCount'])); wallL += int(f(r['wallPassLawCount'])); wallS += int(f(r['wallPassSliderCount']))
        blkL += int(f(r['blockedLawCount'])); blkS += int(f(r['blockedSliderCount'])); blkF += int(f(r['blockedWhileFrozenCount']))
        wlb += int(f(r['wallLegitBlockedCount']))
        S = f(r['mandateStockIndex']); B = f(r['mandateMaxIndex']); mx.append(B)
        if S > 0: stock_pos += 1
        if B > 0 and S >= B - 1e-12: spent_full += 1
print(f'reform 行 {n_rf}・目減り S＞0 の行 {stock_pos}・S≧b（使い切り）の行 {spent_full}・b p50 {q(mx,.5):.4f} p95 {q(mx,.95):.4f}')
print(f'  在庫の門で止まった一歩 法 {blkL}・スライダー {blkS}（うち S＞b の凍結 {blkF}）・壁を通った一歩 {wall}（法 {wallL}・スライダー {wallS}）・満タンで正統性の側で止まった試み {wlb}')
# 4. nation：内戦の月
cw = 0; cwn = set()
for s in seeds:
    R = rows('nation', s)
    if R is None: continue
    for r in R:
        if f(r['civilWarMonths']) > 0: cw += 1; cwn.add((s, r['nation']))
print(f'nation 内戦の月＞0 の国×四半期 {cw}・(種,国) {len(cwn)}')
