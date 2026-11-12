# (1058) c100pq 問い3 の上界（読むだけ・近似）
# sellr_s の行は「生産者のある（州・財）」だけ（InstrumentsSellr.cs:47〜48）＝首都州に生産者の無い調達の財（例：ロンドンのキニーネ）は U1b に元から入らない。
# 国家の調達は全て首都州（World.Region＝Regions[0]）＝sellr の国ごとの最初の州（w.Regions の順に書く）〈推〉。
# capE＝首都州の調達の財（石炭・原油・キニーネ・缶詰・帆船・汽船）の行の prevExportAll×px の和（清算の輸出も含む＝調達の上界）。
# U1b_cap＝(Ea − capE)÷P2（直しの上界の側）。q23.py の U1b_adj＝(Ea − procF)÷P2 と並べ、真の直しは min(capE, procF) 以下。
import sys, os, math, statistics as st
from collections import defaultdict
MAJ = ['イギリス', 'フランス', 'プロイセン', 'ロシア', 'アメリカ', '清']
PROC = {'石炭', '原油', 'キニーネ', '缶詰', '帆船', '汽船'}
YEARS = ['1870', '1913', '1950']
OUT = sys.argv[1]; ARMS = [a.split('=', 1) for a in sys.argv[2:]]


def one(d, s):
    px = {}
    with open(os.path.join(d, 'nationgood_s%d.tsv' % s), encoding='utf-8') as f:
        ix = {k: i for i, k in enumerate(f.readline().rstrip('\n').split('\t'))}
        for ln in f:
            v = ln.rstrip('\n').split('\t')
            if v[0] in YEARS:
                p = float(v[ix['priceMeanGold']])
                if p > 0: px[(v[0], v[1], v[2])] = p
    seen = set(); cap = {}; Ea = defaultdict(float); P2 = defaultdict(float); capE = defaultdict(float); capRows = defaultdict(list)
    with open(os.path.join(d, 'sellr_s%d.tsv' % s), encoding='utf-8') as f:
        ix = {k: i for i, k in enumerate(f.readline().rstrip('\n').split('\t'))}
        for ln in f:
            v = ln.rstrip('\n').split('\t')
            y = v[ix['year']]
            if y not in YEARS: continue
            nat = v[ix['ownerNation']]; reg = v[ix['region']]; g = v[ix['good']]
            if (y, nat) not in cap: cap[(y, nat)] = reg
            key = (y, reg, g)
            if key in seen or nat not in MAJ: continue
            seen.add(key)
            p = px.get((y, nat, g))
            if not p: continue
            e = float(v[ix['prevExportAll']]) * p
            Ea[(y, nat)] += e; P2[(y, nat)] += float(v[ix['lastProduced']]) * p
            if reg == cap[(y, nat)] and g in PROC:
                capE[(y, nat)] += e
                dem = float(v[ix['lastDemand']])
                capRows[(y, nat)].append((g, float(v[ix['prevExportAll']]), dem))
    return Ea, P2, capE, cap, capRows


def med(xs):
    xs = [x for x in xs if math.isfinite(x)]
    return st.median(xs) if xs else float('nan')


out = []; agg = {}
for nm, d in ARMS:
    ss = [s for s in range(1, 11) if os.path.exists(os.path.join(d, 'sellr_s%d.tsv' % s))]
    per = [one(d, s) for s in ss]
    out.append('== %s %s 種 %s' % (nm, d, ss))
    for y in YEARS:
        u = []; uc = []; sh = []
        for Ea, P2, capE, cap, rows in per:
            E = sum(Ea[(y, n)] for n in MAJ); P = sum(P2[(y, n)] for n in MAJ); C = sum(capE[(y, n)] for n in MAJ)
            if P > 0: u.append(E / P); uc.append((E - C) / P)
            if E > 0: sh.append(C / E)
        agg[(nm, y)] = (med(u), med(uc))
        out.append('  %s 6大国 U1b %.6f U1b_cap %.6f 首都州の調達の財の輸出÷州の外の全て（上界）%.4f' % (y, med(u), med(uc), med(sh)))
        for n in MAJ:
            caps = sorted({p[3].get((y, n), '?') for p in per})
            r = [ (p[4][(y, n)]) for p in per]
            exs = [sum(e for _, e, _ in rr) for rr in r]
            dems = [sum(dd for _, _, dd in rr) for rr in r]
            out.append('    %s 首都 %s 調達の財の行の 輸出の和 中央 %.4g 需要の和 中央 %.4g 輸出÷需要 中央 %.3f' % (n, '/'.join(caps)[:30], med(exs), med(dems), med([e / dd for e, dd in zip(exs, dems) if dd > 0])))
for prefix in ('', '53'):
    b, n1, n2 = 'base' + prefix, 'n1' + prefix, 'n2' + prefix
    if (b, '1913') not in agg: continue
    for arm in [a for a, _ in ARMS if a not in ('base', 'n1', 'n2', 'base53', 'n153', 'n253')]:
        for y in YEARS:
            for k, key in ((0, 'U1b'), (1, 'U1b_cap')):
                vb, v1, v2, va = agg[(b, y)][k], agg[(n1, y)][k], agg[(n2, y)][k], agg[(arm, y)][k]
                top = max(v1 / vb, v2 / vb, vb / v1, vb / v2)
                out.append('問い3 %s 対 %s %s %-7s 腕 %.6f base %.6f n1 %.6f n2 %.6f 新÷旧 %.4f 幅の上端 %.4f %s' % (arm, b, y, key, va, vb, v1, v2, va / vb, top, '緑' if va / vb > top else '赤'))
with open(OUT, 'w', encoding='utf-8') as f:
    f.write('# (1058) q3b.py %s\n' % ' '.join(sys.argv[2:]))
    f.write('\n'.join(out) + '\n')
print(len(out))
