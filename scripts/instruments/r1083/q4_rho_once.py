# (1083) 問い 4：★(1078) ③（F_f に ρ を 1 回・海軍は足さない）と同点の割り方で、開幕の影の目標がどう変わるか（紙・keep の列から再計算）。
# 使い方: python -X utf8 -I q4_rho_once.py open_rows.tsv > q4_out.txt
# 式の写し（名乗る）：RulePowerClasses（InstrumentsRulePower.cs:45〜57）を Python へ写した＝持ち主の側＝F_S＋κF_I／在地＝(1−κ)F_I＋F_R／外国＝ΣF_f／無統治＝F_U、最大の区分（同点は若い番号）。
# 版：V0 今（重み w に ρ・F_f＝配り×ρ＝ρ が 2 回）／Va 重みだけに ρ（F_f＝配り前の値 fForeignRaw＝表から厳密）／
#     Vb 力だけに ρ（重みから ρ を抜く＝国 n の配りを 1/ρ_n(r) で重み直す）＝F_f,b(r)＝F_f,0(r)×ρ̄_n÷ρ_n(r)、ρ̄_n＝O_n÷Σ_r(配り_n(r)÷ρ_n(r))（配りで重みを付けた調和平均）。
#     ρ_n(r)＝fForeignSum÷fForeignRaw は 1 国だけから受ける行でだけ分かる＝ρ̄_n はその行だけから求めた近似（覆う割合を出す）・複数の国から受ける行は Vb を上下の幅で出す。
#   tol：持ち主と無統治の比べ合いに相対の許容差 1e-9（同点は持ち主）を置いた版。海軍：今の表は海軍を F_f に入れていない（NavalAlloc の列だけ）＝推奨〈足さない〉は今と同じ。比べに〈足したら〉の版 Vn（F_f＋NavalAlloc×1）を記録で出す。
import csv, sys, collections
import sys as _s; _s.stdout.reconfigure(newline='\n')  # 出力は LF
rows = list(csv.DictReader(open(sys.argv[1], encoding='utf-8'), delimiter='\t'))
def f(x):
    try: return float(x)
    except: return float('nan')
def classes(fs, fi, fr, fsum, fu, k, tol=0.0):
    k = 1 if k > 1 else k if k > 0 else 0
    fsum = fsum if fsum > 0 else 0
    tot = fs + fi + fr + fsum + fu
    if not (tot > 0): return 'none'
    o, l, fo, u = (fs + k * fi) / tot, ((1 - k) * fi + fr) / tot, fsum / tot, fu / tot
    best, bv = 'owner', o
    if l > bv * (1 + tol): best, bv = 'local', l
    if fo > bv * (1 + tol): best, bv = 'foreign', fo
    if u > bv * (1 + tol): best = 'ungov'
    return best
# 自己検査：V0 の写しが表の bestClass と一致するか
mis = 0
for x in rows:
    b = classes(f(x['fStateIndex']), f(x['fInterIndex']), f(x['fResistIndex']), f(x['fForeignSumIndex']), f(x['fUngovIndex']), f(x['kappaIndex']))
    if b != x['bestClass']: mis += 1
print('自己検査：写した式（G10 の値から）と表の bestClass の不一致', mis, '/', len(rows), '（不一致は G10 の丸めで同点が逆に割れた行）')
# ρ̄_n（種×国）
alloc = collections.defaultdict(float); inv = collections.defaultdict(float); O = {}
single = {}
for x in rows:
    O[(x['seed'], x['nation'])] = f(x['oNationIndex'])
    s, t, r = f(x['fForeignSumIndex']), f(x['fForeignTopIndex']), f(x['fForeignRawIndex'])
    if s > 0:
        one = t >= s * (1 - 1e-9); single[(x['seed'], x['region'])] = one
        if one and r > 0:
            k = (x['seed'], x['foreignTopKey']); rho = s / r
            alloc[k] += r; inv[k] += r / rho
rbar = {k: (alloc[k] / inv[k] if inv[k] > 0 else float('nan')) for k in alloc}
cov = {k: alloc[k] / O[k] for k in alloc if O.get(k, 0) > 0}
print('ρ̄_n の近似の覆う割合（1 国だけの行の配り÷O_n）：国ごとの種の中央')
byn = collections.defaultdict(list)
for k, v in cov.items(): byn[k[1]].append((v, rbar[k]))
import statistics as st
for n, vs in sorted(byn.items(), key=lambda kv: -st.median(O[(s, kv[0])] for s in [str(i) for i in range(1, 11)] if (s, kv[0]) in O)):
    print(f'  {n}\t覆う {st.median(v[0] for v in vs):.3f}\tρ̄ {st.median(v[1] for v in vs):.3f}')
rmin = {s: min(v for k, v in rbar.items() if k[0] == s and v == v) for s in {k[0] for k in rbar}}
res = collections.Counter(); moved = collections.Counter(); undet = 0
for x in rows:
    fs, fi, fr, fu, kap = f(x['fStateIndex']), f(x['fInterIndex']), f(x['fResistIndex']), f(x['fUngovIndex']), f(x['kappaIndex'])
    s0, raw = f(x['fForeignSumIndex']), f(x['fForeignRawIndex']); nav = f(x['navalAllocIndex'])
    seed = x['seed']
    if s0 > 0 and single.get((seed, x['region'])):
        rb = rbar.get((seed, x['foreignTopKey']), float('nan')); sb_lo = sb_hi = raw * rb
    elif s0 > 0:
        sb_lo, sb_hi = raw * rmin[seed], raw   # 幅：ρ̄ は (0,1]・国ごとの最小の ρ̄ を下に
    else: sb_lo = sb_hi = 0.0
    v = {
        'V0': classes(fs, fi, fr, s0, fu, kap),
        'V0+tol': classes(fs, fi, fr, s0, fu, kap, 1e-9),
        'Va': classes(fs, fi, fr, raw, fu, kap),
        'Va+tol': classes(fs, fi, fr, raw, fu, kap, 1e-9),
        'Vb_lo+tol': classes(fs, fi, fr, sb_lo, fu, kap, 1e-9),
        'Vb_hi+tol': classes(fs, fi, fr, sb_hi, fu, kap, 1e-9),
        'Vn(海軍を足す・記録)': classes(fs, fi, fr, s0 + (nav if nav > 0 else 0), fu, kap),
    }
    if v['Vb_lo+tol'] != v['Vb_hi+tol']: undet += 1
    for k, b in v.items(): res[(k, b)] += 1
    if x['bestClass'] == 'foreign':
        for k, b in v.items(): moved[(k, b)] += 1
names = ['V0', 'V0+tol', 'Va', 'Va+tol', 'Vb_lo+tol', 'Vb_hi+tol', 'Vn(海軍を足す・記録)']
print('\n開幕 3080 行の影の最大の区分（版ごと）')
for k in names: print(f'  {k}\t' + '\t'.join(f'{b} {res[(k, b)]}' for b in ('owner', 'local', 'foreign', 'ungov', 'none')) + f'\t持ち主でない {3080 - res[(k, "owner")]}')
print('Vb で上下の幅が区分を割る行', undet)
print('\n今（V0）外国の行（表の bestClass＝foreign・C の 0 行を含む全て）が各版でどこへ行くか')
for k in names: print(f'  {k}\t' + '\t'.join(f'{b} {moved[(k, b)]}' for b in ('owner', 'local', 'foreign', 'ungov')))
# 外国の行の O の大きさへの感度：F_f を c 倍したら（V0+tol）何行が外国のまま
print('\nO の大きさの感度（V0+tol の F_f を c 倍）：開幕の外国の行の数')
for c in (1.0, 0.5, 0.25, 0.1, 0.05):
    n = sum(1 for x in rows if classes(f(x['fStateIndex']), f(x['fInterIndex']), f(x['fResistIndex']), c * f(x['fForeignSumIndex']), f(x['fUngovIndex']), f(x['kappaIndex']), 1e-9) == 'foreign')
    print(f'  c {c}\t外国 {n}')
