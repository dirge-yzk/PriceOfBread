# (1398) c149sd 紙の台本と腕 B の記録：満足度の読み手を不満度へ（旗 SatReadersToDiscontent）。読むだけ。列は列名で引く。
#   使い方: python -I -X utf8 paper_sd.py <名前>=<ins フォルダ> [<名前>=<ins フォルダ> ...] [--seeds 1-10]
#   各フォルダごとに：
#   [1] 不穏の分布（国×四半期・dcont_s）
#       旧＝max(0, 0.62−satMeanShare)（AI の旧の式・SelfPlay の UnrestSatisfactionThreshold）
#       D 列＝discontentMeanShare（その木の旗で Politics.Discontent を大衆の層で平均した値＝旗 1 の木では D̄ そのもの・旗 0 の木では声の項を含む今の D）
#       下限＝0.8×max(0, livingDropMeanIndex)（旗 1 の D̄ の下限＝Pos は凸・Clamp01 の上の端は 0.8×落差 ≤1 の盤面で効かない）
#   [2] AI の判断の向き（modsig_s の stance＝0 近代化・1 工業化・2 福祉・3 財政・4 軍備 の割合）と社会保障の目標の平均（nation_s socialSecurityTargetEndShare）
#   [3] 件数：蜂起（dcont_s factionRevoltsCum の国ごとの最大の和）・内戦（nation_s civilWarMonths が 0 から正になった回数）・内戦の終わり（cwend_s の行）
#       ・政体の変わり（nation_s polityCode が前の行と違う回数・国の最初の行は数えない）・政変の門（coupgate_s passSplit＝1 の行）
import csv, glob, os, sys, statistics as st

def num(s):
    try: return float(s)
    except (ValueError, TypeError): return float('nan')

def rows(path):
    with open(path, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t')
        h = next(r, None)
        if h is None: return
        ix = {k: i for i, k in enumerate(h)}
        for row in r:
            yield ix, row

def q(xs, p):
    if not xs: return float('nan')
    xs = sorted(xs); k = (len(xs) - 1) * p
    lo = int(k); hi = min(lo + 1, len(xs) - 1)
    return xs[lo] + (xs[hi] - xs[lo]) * (k - lo)

def seeds_of(d, lo, hi):
    out = []
    for s in range(lo, hi + 1):
        if os.path.exists(os.path.join(d, f'dcont_s{s}.tsv')): out.append(s)
    return out

def one(name, d, lo, hi):
    ss = seeds_of(d, lo, hi)
    old, dcol, lb = [], [], []
    stance = [0] * 5; stn = 0; sst = []
    rev = civ = cwe = pol = coup = 0
    per = []
    for s in ss:
        o_s, d_s = [], []
        rmax = {}
        for ix, r in rows(os.path.join(d, f'dcont_s{s}.tsv')):
            sat = num(r[ix['satMeanShare']]); dd = num(r[ix['discontentMeanShare']]); dr = num(r[ix['livingDropMeanIndex']])
            mk = num(r[ix['massPopK']])
            if not (mk > 0): continue
            old.append(max(0.0, 0.62 - sat)); dcol.append(dd); lb.append(0.8 * max(0.0, dr))
            o_s.append(max(0.0, 0.62 - sat)); d_s.append(dd)
            nat = r[ix['nation']]; rv = num(r[ix['factionRevoltsCum']])
            if rv == rv: rmax[nat] = max(rmax.get(nat, 0.0), rv)
        rs = sum(rmax.values()); rev += rs
        p = os.path.join(d, f'modsig_s{s}.tsv')
        if os.path.exists(p):
            for ix, r in rows(p):
                k = int(num(r[ix['stance']])) if num(r[ix['stance']]) == num(r[ix['stance']]) else -1
                if 0 <= k < 5: stance[k] += 1; stn += 1
        cs = ps = 0; prevcw = {}; prevpol = {}
        for ix, r in rows(os.path.join(d, f'nation_s{s}.tsv')):
            nat = r[ix['nation']]; cw = num(r[ix['civilWarMonths']]); pc = r[ix['polityCode']]
            v = num(r[ix['socialSecurityTargetEndShare']])
            if v == v: sst.append(v)
            if nat in prevcw and not (prevcw[nat] > 0) and cw > 0: cs += 1
            prevcw[nat] = cw
            if nat in prevpol and prevpol[nat] != pc: ps += 1
            prevpol[nat] = pc
        civ += cs; pol += ps
        p = os.path.join(d, f'cwend_s{s}.tsv')
        ce = sum(1 for _ in rows(p)) if os.path.exists(p) else 0; cwe += ce
        p = os.path.join(d, f'coupgate_s{s}.tsv')
        cp = sum(1 for ix, r in rows(p) if r[ix['passSplit']] == '1') if os.path.exists(p) else 0; coup += cp
        per.append((s, st.median(o_s) if o_s else float('nan'), st.median(d_s) if d_s else float('nan'), rs, cs, ce, ps, cp))
    print(f'## {name}  {d}  種 {ss}')
    print(f'[1] 不穏の分布（国×四半期 {len(old)} 行）  p10 p25 p50 p75 p90 p99 平均')
    for lab, xs in (('旧 max(0,0.62−満足)', old), ('D 列（この木の旗）', dcol), ('D̄ の下限 0.8×Pos(落差の平均)', lb)):
        print(f'  {lab}\t' + '\t'.join(f'{q(xs, p):.4f}' for p in (.1, .25, .5, .75, .9, .99)) + f'\t{(sum(xs)/len(xs) if xs else float("nan")):.4f}')
    if old:
        z = sum(1 for x in dcol if x <= 1e-12) / len(dcol)
        print(f'  D 列が 0 の行の割合 {z:.3f}・旧が 0 の行の割合 {sum(1 for x in old if x <= 1e-12)/len(old):.3f}')
        print(f'  平均の比 D 列÷旧 {(sum(dcol)/max(1e-12, sum(old))):.3f}・福祉の効用の項 2.5×不穏 の平均 旧 {2.5*sum(old)/len(old):.4f}・D 列 {2.5*sum(dcol)/len(dcol):.4f}')
    tot = max(1, stn)
    print(f'[2] 姿勢の割合（{stn} 行）近代化 {stance[0]/tot:.3f} 工業化 {stance[1]/tot:.3f} 福祉 {stance[2]/tot:.3f} 財政 {stance[3]/tot:.3f} 軍備 {stance[4]/tot:.3f}・社会保障の目標の平均 {(sum(sst)/len(sst) if sst else float("nan")):.4f}')
    print(f'[3] 件数の和（{len(ss)} 種）蜂起 {rev:.0f}・内戦の始まり {civ}・内戦の終わり {cwe}・政体の変わり {pol}・政変の門 passSplit {coup}')
    print('  種\t旧の中央\tD 列の中央\t蜂起\t内戦の始まり\t内戦の終わり\t政体の変わり\t政変の門')
    for t in per: print('  ' + '\t'.join(f'{x:.4f}' if isinstance(x, float) else str(x) for x in t))
    return dict(rev=rev, civ=civ, cwe=cwe, pol=pol, coup=coup)

def main(a):
    lo, hi = 1, 10; pairs = []
    i = 0
    while i < len(a):
        if a[i] == '--seeds': lo, hi = (int(x) for x in a[i + 1].split('-')); i += 2; continue
        n, d = a[i].split('=', 1); pairs.append((n, d)); i += 1
    res = {n: one(n, d, lo, hi) for n, d in pairs}
    if len(pairs) >= 2:
        b = pairs[0][0]
        print(f'## 比（÷{b}）＝史実の桁の門：蜂起と内戦の始まりが 10 倍を超えない')
        for n, _ in pairs[1:]:
            for k in ('rev', 'civ', 'cwe', 'pol', 'coup'):
                x, y = res[n][k], res[b][k]
                print(f'  {n}÷{b} {k} {x}/{y} = {(x / y if y else float("inf") if x else 1.0):.3f}')

main(sys.argv[1:])
