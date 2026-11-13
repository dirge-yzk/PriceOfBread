# (1093) c104al 問い1・問い2 の走査（読むだけ・腕 0）。1 本の腕のフォルダを読み、行の写しと要約を書く。
# 使い方: python -X utf8 -I q1scan.py <腕のフォルダ> <出力の接頭辞> [種の一覧 例 1-10]
# 定義は prereg.txt の D1〜D8（結果の前に固定）。
import sys, os, math
from collections import defaultdict

D = sys.argv[1]; OUT = sys.argv[2]
SEEDS = list(range(1, 11))
MODE = sys.argv[4] if len(sys.argv) > 4 else ''   # (1093) mutation on paper: M8 = drop -PrevImport, MF = sign flip (row flags stay the instrument sellRBoth)
if len(sys.argv) > 3 and sys.argv[3]:
    a, b = sys.argv[3].split('-'); SEEDS = list(range(int(a), int(b) + 1))
ROWYEARS = {'1850', '1870', '1913'}
SERVICE = {"労働力", "教育", "医療", "行政", "兵力", "高度労働力", "農業労働力", "上下水道", "電化インフラ", "家事奉仕", "高等教育"}
THETAS = (0.7, 0.9, 0.99)
INFRA = {'上下水道', '電化インフラ'}   # service goods made by the worker POP (Simulation.cs:1380/1384/1387 workerIdx) = in the group mean, not in the stats
RISE = 0.01


def rd(p):
    f = open(p, encoding='utf-8', newline='')
    h = f.readline().rstrip('\r\n').split('\t')
    return f, {k: i for i, k in enumerate(h)}


def lpc(isrgo, recipe):
    if isrgo == '1' or recipe == '家内工業': return 'P'
    if recipe == '職人': return 'A'
    return 'W'


rows_out = open(OUT + '_rows.tsv', 'w', encoding='utf-8', newline='\n')
rows_out.write('seed\tyear\tregion\tnation\tgood\trecipe\tocc\tlpc\tcls\tcap\tout\tpx\tlw\tsOld\tsBoth\trho\tkappa\tkappa2\n')
sum_out = open(OUT + '_sum.tsv', 'w', encoding='utf-8', newline='\n')
cols = ['seed', 'year', 'nRows', 'nExcl', 'nNoPx', 'vAll', 'nRise', 'vRise', 'nFall', 'vFall']
for t in THETAS:
    cols += ['vCB%g' % t, 'nCB%g' % t, 'nCBrise%g' % t, 'vCBrise%g' % t, 'vCBk%g' % t, 'vCBrisek%g' % t]
cols += ['vRisek', 'vAllk', 'vKgt105', 'vKlt095', 'vK2gt105', 'vK2lt095', 'vAbsK05', 'nRise0', 'vRise0', 'nCBrise0_0.9', 'vCBrise0_0.9']
sum_out.write('\t'.join(cols) + '\n')
log = open(OUT + '_log.txt', 'w', encoding='utf-8', newline='\n')

for s in SEEDS:
    ps = os.path.join(D, 'sellr_s%d.tsv' % s); pp = os.path.join(D, 'prodcap_s%d.tsv' % s); pn = os.path.join(D, 'nationgood_s%d.tsv' % s)
    if not (os.path.exists(ps) and os.path.exists(pp) and os.path.exists(pn)):
        log.write('seed %d missing\n' % s); continue
    px = {}
    f, ix = rd(pn)
    for ln in f:
        v = ln.rstrip('\r\n').split('\t')
        try: p = float(v[ix['priceMeanGold']])
        except ValueError: continue
        if p > 0: px[(v[0], v[1], v[2])] = p
    f.close()
    fs, ixs = rd(ps); fp, ixp = rd(pp)
    # 年ごとに塊で読む（sellr は 1784 を持たない＝prodcap の 1784 を飛ばす）
    def blocks(f, ix):
        cur = None; buf = []
        for ln in f:
            v = ln.rstrip('\r\n').split('\t')
            if v[0] != cur:
                if buf: yield cur, buf
                cur = v[0]; buf = []
            buf.append(v)
        if buf: yield cur, buf
    gp = blocks(fp, ixp)
    for ys, bs in blocks(fs, ixs):
        yp, bp = next(gp)
        while yp != ys:
            yp, bp = next(gp)
        if len(bp) != len(bs):
            log.write('seed %d year %s count mismatch %d %d\n' % (s, ys, len(bs), len(bp))); continue
        bad = 0
        recs = []
        occ = defaultdict(int)
        for a, b in zip(bs, bp):
            if a[ixs['region']] != b[ixp['region']] or a[ixs['ownerNation']] != b[ixp['ownerNation']] or a[ixs['good']] != b[ixp['good']]:
                bad += 1; continue
            g = a[ixs['good']]; reg = a[ixs['region']]; nat = a[ixs['ownerNation']]; rec = b[ixp['recipe']]
            k = (reg, g, rec); o = occ[k]; occ[k] += 1
            recs.append((reg, nat, g, rec, o, lpc(b[ixp['isRgo']], rec), b[ixp['class']], float(b[ixp['capacity']]), float(b[ixp['output']]),
                         float(a[ixs['laborWeight']]), float(a[ixs['sellROld']]), float(a[ixs['sellRBoth']]), float(b[ixp['productivity']]), float(a[ixs['lastDemand']]), float(a[ixs['prevExportAll']]), float(a[ixs['prevImportAll']])))
        if bad:
            log.write('seed %d year %s key mismatch %d\n' % (s, ys, bad))
        # 組の平均の ρ
        num = defaultdict(float); den = defaultdict(float); num2 = defaultdict(float); den2 = defaultdict(float)
        st = defaultdict(float)
        nExcl = 0
        keep = []
        for i, r in enumerate(recs):
            reg, nat, g, rec, o, lp, cls, cap, out, lw, so, sb, prd, dD, dE, dI = r
            if g in SERVICE and g not in INFRA: nExcl += 1; continue
            sbm = sb if MODE == '' else (max(dD + dE, so) if MODE == 'M8' else max(dD - dE + dI, so if so > dD else 0.0))
            rho = sbm / so if so > 0 else 1.0
            keep.append((r, rho))
            if lw > 0:
                num[(reg, lp)] += lw * rho; den[(reg, lp)] += lw
            if cap > 0 and prd > 0:
                num2[(reg, lp)] += cap / prd * rho; den2[(reg, lp)] += cap / prd
        st['nExcl'] = nExcl
        for r, rho in keep:
            reg, nat, g, rec, o, lp, cls, cap, out, lw, so, sb, prd, dD, dE, dI = r
            if g in INFRA: st['nInfra'] += 1; continue
            rb = num[(reg, lp)] / den[(reg, lp)] if den[(reg, lp)] > 0 else 1.0
            rb2 = num2[(reg, lp)] / den2[(reg, lp)] if den2[(reg, lp)] > 0 else 1.0
            kap = rho / rb if rb > 0 else 1.0
            kap2 = rho / rb2 if rb2 > 0 else 1.0
            p = px.get((ys, nat, g))
            st['nRows'] += 1
            if ys in ROWYEARS:
                rows_out.write('%d\t%s\t%s\t%s\t%s\t%s\t%d\t%s\t%s\t%.6g\t%.6g\t%s\t%.6g\t%.6g\t%.6g\t%.6g\t%.6g\t%.6g\n' % (
                    s, ys, reg, nat, g, rec, o, lp, cls, cap, out, ('%.6g' % p) if p else '', lw, so, sb, rho, kap, kap2))
            if not p: st['nNoPx'] += 1; continue
            v = out * p
            if not (v > 0): continue
            st['vAll'] += v; st['vAllk'] += v * kap
            rise = so > 0 and (sb - so) / so > RISE
            fall = sb < so * (1 - RISE)
            if rise: st['nRise'] += 1; st['vRise'] += v; st['vRisek'] += v * kap
            if sb > so: st['nRise0'] += 1; st['vRise0'] += v
            if sb > so and cap > 0 and out / cap >= 0.9: st['nCBrise0_0.9'] += 1; st['vCBrise0_0.9'] += v
            if fall: st['nFall'] += 1; st['vFall'] += v
            if kap > 1.05: st['vKgt105'] += v
            if kap < 0.95: st['vKlt095'] += v
            if kap2 > 1.05: st['vK2gt105'] += v
            if kap2 < 0.95: st['vK2lt095'] += v
            if abs(kap - 1) > 0.05: st['vAbsK05'] += v
            if cap > 0:
                rat = out / cap
                for t in THETAS:
                    if rat >= t:
                        st['vCB%g' % t] += v; st['nCB%g' % t] += 1; st['vCBk%g' % t] += v * kap
                        if rise: st['nCBrise%g' % t] += 1; st['vCBrise%g' % t] += v; st['vCBrisek%g' % t] += v * kap
        sum_out.write('\t'.join([str(s), ys] + ['%.10g' % st[c] for c in cols[2:]]) + '\n')
    fs.close(); fp.close()
    log.write('seed %d done\n' % s); log.flush()
rows_out.close(); sum_out.close(); log.close()
