# (1333) c139sd 設計の材料の紙（読むだけ・エンジンの関数を呼ばない・腕を回さない）
# 使い方: python -X utf8 -I paper1333.py <base の ins（D:/ の形）> <ssB の ins> <出力のフォルダ>
# 読む表：subst_s（s2・s3 の列・ssB は 3-1 の列も）・class_s。種 1〜10。
#  問い 1：ln x̄＝ln σ0＋ln(能力÷人数 の比)＋ln(売れ残りの割合の比)＋ln(焼いた月から最初の行までのずれ)＋ln(T÷T0)＋ln f̄ に分ける（人口の重み subPopK）。
#          σraw＝ρ×U×T÷(必要量×N)・ρ＝σ0×必要量×N0÷(U0×T0)（EconomyStep.cs:122）⇒ σraw＝σ0×[(U/N)÷(U0/N0)]×(T÷T0)。
#  問い 2：〈売れ残った労働〉の基準 C（能力＝U＝能力−売れた・今の式 EconomyStep.cs:91）と P（実際の産出＝農業労働の市場の産出−売れた）で
#          σraw を最初の行に合わせて置き直し、κ_E（ΣE÷ΣQ）・農民の失業（押し出し）を 1913 年で並べる。f̄ は財ごとの f の代わり（限界）。
import sys, os, math, statistics
from collections import defaultdict
base, ssb, out = sys.argv[1], sys.argv[2], sys.argv[3]
SEEDS = range(1, 11)
NINE = ['イギリス', 'フランス', 'プロイセン', 'アメリカ', 'ロシア', '日本', '清', 'オスマン', 'オーストリア']
NONFARM = {'イギリス': 0.9, 'フランス': 0.6, 'プロイセン': 0.65, 'アメリカ': 0.7, 'ロシア': 0.25, '日本': 0.4, '清': 0.2, 'オスマン': 0.2, 'オーストリア': 0.45}
KAPPA = {'ロシア': 0.15, '清': 0.3}
BINS = [(1783, 1799), (1800, 1824), (1825, 1849), (1850, 1874), (1875, 1899), (1900, 1913), (1914, 1950)]
def binof(y):
    for b in BINS:
        if b[0] <= y <= b[1]: return b
    return None
def fnum(x):
    try:
        v = float(x); return v if math.isfinite(v) else None
    except Exception: return None
TERMS = ['s0', 'cap', 'us', 'res', 'T', 'f']
# 集計の器
dec = defaultdict(lambda: defaultdict(float))     # (群, 期) -> 和
y13 = defaultdict(lambda: defaultdict(list))      # 国 -> 量 -> [種ごと]
xs13 = defaultdict(list)                          # 国 -> [(x̄, 重み)]
skipped = defaultdict(int)
chk_max = 0.0
for sd in SEEDS:
    p = os.path.join(base, f'subst_s{sd}.tsv')
    ref = {}
    acc13 = defaultdict(lambda: defaultdict(float))
    with open(p, 'r', encoding='utf-8', newline='') as f:
        h = f.readline().rstrip('\r\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        g = lambda c, k: fnum(c[ix[k]])
        for line in f:
            c = line.rstrip('\r\n').split('\t')
            if c[ix['s2BasisKind']] != '2': continue
            y = int(c[ix['year']]); nat = c[ix['nation']]; reg = c[ix['region']]
            N, need, rho = g(c, 'subPopK'), g(c, 'stapleNeedPerCapita'), g(c, 's2RhoInst')
            U, cap, T, T0, s0, fb, sr = g(c, 's2UEnd'), g(c, 's2CapEnd'), g(c, 's2TechEnd'), g(c, 's2Tech0Inst'), g(c, 's2D0Inst'), g(c, 's3HarvestInst'), g(c, 's3SrawInst')
            E, Q = g(c, 's3EInst'), g(c, 's3QInst')
            prod, soldF = g(c, 'farmProducedPerMonth'), g(c, 'farmSoldPerMonth')
            if None in (N, need, rho, U, cap, T, T0, s0, fb, sr) or not (N > 0 and cap > 0 and rho > 0 and T0 > 0 and s0 > 0):
                skipped['欠け'] += 1; continue
            if not (U > 0 and fb > 0): skipped['U か f̄ が 0'] += 1; continue
            uR = U * rho * T0 / (N * s0 * need)
            if reg not in ref:
                up0 = max(0.0, (prod or 0.0) - (soldF or 0.0)) / N
                ref[reg] = (cap / N, U / cap, uR, sr, T, up0)
            capN0, us0, uR0, sr0, Tf, up0 = ref[reg]
            t = {'s0': math.log(s0), 'cap': math.log((cap / N) / capN0), 'us': math.log((U / cap) / us0), 'res': math.log(uR0) if uR0 > 0 else 0.0,
                 'T': math.log(T / T0), 'f': math.log(fb)}
            lx = sum(t.values())
            chk_max = max(chk_max, abs(lx - math.log(sr * fb)) if sr > 0 else 0.0)
            # x̄ の分解（問い 1）
            b = binof(y)
            x = sr * fb
            for grp in ('全て', nat if nat in NINE else None):
                if grp is None or b is None: continue
                d = dec[(grp, b)]
                d['w'] += N
                for k in TERMS: d[k] += N * t[k]
                d['x>1'] += N if x > 1 else 0.0
                d['sr>1'] += N if sr > 1 else 0.0
                d['E'] += E or 0.0; d['Q'] += Q or 0.0
                d['sold/cap'] += N * (cap - U) / cap
            # 問い 2：基準 C と P
            if y == 1913:
                a = acc13[nat]
                a['w'] += N
                a['E_sh'] += E or 0.0; a['Q_sh'] += Q or 0.0
                a['E_C'] += N * need * max(0.0, x - 1.0); a['Q_C'] += N * need * x
                a['u_C'] += N * (U / cap) * max(0.0, 1.0 - 1.0 / x)
                lneed_f = U / x   # Lneed_f＝必要量×N÷(ρ×T×f̄)＝U÷(σraw×f̄)
                a['kL'] += N * ((cap - U) / ((cap - U) + min(U, lneed_f)) if (cap - U) + min(U, lneed_f) > 0 else 0.0)
                if up0 > 0 and prod and prod > 0:
                    upn = max(0.0, prod - (soldF or 0.0)) / N
                    srP = sr0 * (upn / up0) * (T / Tf)
                    xP = srP * fb
                    a['wP'] += N
                    a['E_P'] += N * need * max(0.0, xP - 1.0); a['Q_P'] += N * need * xP
                    a['u_P'] += N * (max(0.0, prod - (soldF or 0.0)) / prod) * max(0.0, 1.0 - 1.0 / xP if xP > 0 else 0.0)
                    a['xP>1'] += N if xP > 1 else 0.0
                else:
                    a['P_なし'] += N
                a['x>1'] += N if x > 1 else 0.0
                if nat in KAPPA: xs13[nat].append((x, N))
    for nat, a in acc13.items():
        if nat not in NINE: continue
        r = y13[nat]
        r['κ影'].append(a['E_sh'] / a['Q_sh'] if a['Q_sh'] > 0 else 0.0)
        r['κC'].append(a['E_C'] / a['Q_C'] if a['Q_C'] > 0 else 0.0)
        r['κP'].append(a['E_P'] / a['Q_P'] if a['Q_P'] > 0 else float('nan'))
        r['u_C'].append(a['u_C'] / a['w'] if a['w'] > 0 else 0.0)
        r['u_P'].append(a['u_P'] / a['wP'] if a['wP'] > 0 else float('nan'))
        r['κL'].append(a['kL'] / a['w'] if a['w'] > 0 else 0.0)
        r['x>1'].append(a['x>1'] / a['w'] if a['w'] > 0 else 0.0)
        r['xP>1'].append(a['xP>1'] / a['wP'] if a['wP'] > 0 else float('nan'))
        r['P なしの人口'].append(a['P_なし'] / a['w'] if a['w'] > 0 else 0.0)
    # 農業の外の割合（ss_gate.py の H2 と同じ定義）
    pc = os.path.join(base, f'class_s{sd}.tsv')
    cs = defaultdict(lambda: [0.0, 0.0])
    with open(pc, 'r', encoding='utf-8', newline='') as f:
        h = f.readline().rstrip('\r\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            c = line.rstrip('\r\n').split('\t')
            if c[ix['time']] == 'open' or c[ix['year']] != '1913': continue
            cl = c[ix['className']]
            if cl == '兵士': continue
            v = fnum(c[ix['sizeK']]) or 0.0
            a = cs[c[ix['nation']]]; a[1] += v
            if cl == '農民': a[0] += v
    for nat, a in cs.items():
        if nat in NINE and a[1] > 0: y13[nat]['農業の外'].append(1.0 - a[0] / a[1])
# ssB（m1321＋旗 1）：実際の側と、失業の新旧（E＝0 の行でも f̄ で変わる経路）
ssr = defaultdict(float); ssk = defaultdict(lambda: defaultdict(list))
for sd in SEEDS:
    p = os.path.join(ssb, f'subst_s{sd}.tsv')
    if not os.path.exists(p): ssr['欠け'] += 1; continue
    acc = defaultdict(lambda: [0.0, 0.0, 0.0])
    with open(p, 'r', encoding='utf-8', newline='') as f:
        h = f.readline().rstrip('\r\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        g = lambda c, k: fnum(c[ix[k]])
        for line in f:
            c = line.rstrip('\r\n').split('\t')
            if c[ix['s2BasisKind']] != '2': continue
            N = g(c, 'subPopK') or 0.0
            off, so, pa, q = (g(c, k) or 0.0 for k in ('s3EOfferedEngine', 's3ESoldEngine', 's3RPaidEngine', 's3QEngine'))
            ssr['出した'] += off; ssr['売れた'] += so; ssr['払った'] += pa; ssr['Q'] += q
            un, uo, fb = g(c, 's3UnempNewInst'), g(c, 's2UnempB'), g(c, 's3HarvestInst')
            if un is not None and uo is not None:
                ssr['行'] += 1; ssr['重み'] += N
                if off <= 0:
                    ssr['E=0 の行'] += 1
                    if abs(un - uo) > 1e-12: ssr['E=0 で失業が新旧で違う行'] += 1; ssr['その人口'] += N
                    ssr['E=0 の新−旧（人口加重の和）'] += N * (un - uo)
                    if fb is not None and fb > 1 and un > uo + 1e-12: ssr['f̄>1 で新が上'] += 1
                    if fb is not None and fb < 1 and un < uo - 1e-12: ssr['f̄<1 で新が下'] += 1
            if int(c[ix['year']]) == 1913:
                a = acc[c[ix['nation']]]; a[0] += so; a[1] += q; a[2] += off
    for nat in KAPPA:
        a = acc.get(nat, [0.0, 0.0, 0.0])
        ssk[nat]['κ実際'].append(a[0] / a[1] if a[1] > 0 else 0.0)
        ssk[nat]['κ出した'].append(a[2] / a[1] if a[1] > 0 else 0.0)
def med(v):
    v = [x for x in v if x == x]
    return statistics.median(v) if v else float('nan')
def mean(v):
    v = [x for x in v if x == x]
    return sum(v) / len(v) if v else float('nan')
os.makedirs(out, exist_ok=True)
with open(os.path.join(out, 'paper1333.txt'), 'w', encoding='utf-8', newline='\n') as o:
    P = lambda s='': o.write(s + '\n')
    P(f'# (1333) 紙 base={base} ssB={ssb} 種 1〜10・s2BasisKind＝2 の行・重み subPopK')
    P(f'飛ばした行 {dict(skipped)}・分解の検算（|Σ項−ln(σraw×f̄)| の最大）{chk_max:.3g}')
    P('== 問い 1：ln x̄ の分解（人口加重の平均・x̄＝σraw×f̄・x̄＞1 で余りが立つ）')
    P('群\t期\tln σ0\tln 能力/人\tln 売れ残り割合\tln 焼きのずれ\tln T/T0\tln f̄\t=ln x̄\tx̄\tx̄>1 の人口\tσraw>1 の人口\t売れた/能力\tκ影(ΣE/ΣQ)')
    for grp in ['全て'] + NINE:
        for b in BINS:
            d = dec.get((grp, b))
            if not d or d['w'] <= 0: continue
            w = d['w']; m = {k: d[k] / w for k in TERMS}; lx = sum(m.values())
            P(f'{grp}\t{b[0]}-{b[1]}\t' + '\t'.join(f'{m[k]:+.3f}' for k in TERMS) + f'\t{lx:+.3f}\t{math.exp(lx):.3f}\t{d["x>1"]/w:.4f}\t{d["sr>1"]/w:.4f}\t{d["sold/cap"]/w:.3f}\t{(d["E"]/d["Q"] if d["Q"]>0 else 0):.2e}')
    P('== 問い 1：1913 年の x̄ の分位（人口加重・全ての種を合わせた）')
    for nat, v in xs13.items():
        v.sort(); tw = sum(w for _, w in v); acc_w = 0; qs = {}
        for x, w in v:
            acc_w += w
            for qn in (0.5, 0.9, 0.99):
                if qn not in qs and acc_w >= qn * tw: qs[qn] = x
        P(f'{nat}\t中央 {qs.get(0.5, float("nan")):.3f}\tp90 {qs.get(0.9, float("nan")):.3f}\tp99 {qs.get(0.99, float("nan")):.3f}\t最大 {v[-1][0]:.3f}\t行 {len(v)}')
    P('== 問い 2：1913 年・基準 C（能力）と P（実際の産出）・種の中央（κ は平均も）・錨〈推〉')
    P('国\tκ影(エンジンの式)\tκC(f̄ 近似)\tκP\tκP の平均\tκ の錨\tx̄>1 の人口 C\tP\t失業 C\t失業 P\tP の基準が無い人口\tκL(売った労働の割合)\t農業の外(今)\t錨')
    for nat in NINE:
        r = y13[nat]
        P(f'{nat}\t{med(r["κ影"]):.2e}\t{med(r["κC"]):.2e}\t{med(r["κP"]):.3g}\t{mean(r["κP"]):.3g}\t{KAPPA.get(nat, "-")}\t{med(r["x>1"]):.4f}\t{med(r["xP>1"]):.3f}\t{med(r["u_C"]):.4f}\t{med(r["u_P"]):.3f}\t{med(r["P なしの人口"]):.3f}\t{med(r["κL"]):.3f}\t{med(r["農業の外"]):.3f}\t{NONFARM[nat]}')
    P('== ssB（m1321＋旗 SubsistenceSellSurplus 1・計器つき 10 種）：実際の側')
    P('  ' + '・'.join(f'{k} {v:.6g}' for k, v in ssr.items()))
    for nat in KAPPA:
        P(f'  {nat} 1913 κ実際（売れた÷Q）中央 {med(ssk[nat]["κ実際"]):.3g} 平均 {mean(ssk[nat]["κ実際"]):.3g}・出した÷Q 中央 {med(ssk[nat]["κ出した"]):.3g} 平均 {mean(ssk[nat]["κ出した"]):.3g}・錨〈推〉{KAPPA[nat]}・平均÷錨 {mean(ssk[nat]["κ実際"])/KAPPA[nat]:.3g}')
