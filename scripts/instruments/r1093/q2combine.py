# (1093) c104al 問い1・問い2 のまとめ（読むだけ）。q1scan.py の出力（<腕>_sum.tsv・<腕>_rows.tsv）を読む。
# 使い方: python -X utf8 -I q2combine.py <出力> base n1 n2   （接頭辞は同じフォルダの名）
import sys, os, math, statistics as st
from collections import defaultdict
OUT = sys.argv[1]; ARMS = sys.argv[2:]
YEARS = ['1850', '1870', '1913']
o = open(OUT, 'w', encoding='utf-8', newline='\n')


def med(xs):
    xs = [x for x in xs if math.isfinite(x)]
    return st.median(xs) if xs else float('nan')


def r(a, b):
    return a / b if b > 0 else float('nan')


# ---- 要約（全ての年） ----
S = {}
for a in ARMS:
    f = open(a + '_sum.tsv', encoding='utf-8'); h = f.readline().rstrip('\n').split('\t')
    for ln in f:
        v = ln.rstrip('\n').split('\t'); d = dict(zip(h, v))
        S[(a, d['year'], int(d['seed']))] = {k: float(x) for k, x in d.items() if k not in ('year', 'seed')}
    f.close()
years_all = sorted({k[1] for k in S})


def stat(a, y, fn):
    return med([fn(S[k]) for k in S if k[0] == a and k[1] == y])


o.write('[Q1a〜Q1c] 種の中央（10 種）。CB＝output÷capacity≧θ。値は価額の割合（output×priceMeanGold）\n')
o.write('arm\tyear\tnSeedRows\tCBshare0.7\tCBshare0.9\tCBshare0.99\triseCnt|CB0.9\triseVal|CB0.9\triseVal|CB0.7\triseVal|all\tfallVal|all\t|k-1|>.05\tk>1.05\tk<.95\tk2>1.05\tk2<.95\tnoPxRows\n')
for a in ARMS:
    for y in years_all:
        row = [a, y, '%d' % stat(a, y, lambda d: d['nRows'])]
        row += ['%.4f' % stat(a, y, lambda d, t=t: r(d['vCB%s' % t], d['vAll'])) for t in ('0.7', '0.9', '0.99')]
        row += ['%.4f' % stat(a, y, lambda d: r(d['nCBrise0.9'], d['nCB0.9'])), '%.4f' % stat(a, y, lambda d: r(d['vCBrise0.9'], d['vCB0.9'])),
                '%.4f' % stat(a, y, lambda d: r(d['vCBrise0.7'], d['vCB0.7'])),
                '%.4f' % stat(a, y, lambda d: r(d['vRise'], d['vAll'])), '%.4f' % stat(a, y, lambda d: r(d['vFall'], d['vAll'])),
                '%.4f' % stat(a, y, lambda d: r(d['vAbsK05'], d['vAll'])), '%.4f' % stat(a, y, lambda d: r(d['vKgt105'], d['vAll'])),
                '%.4f' % stat(a, y, lambda d: r(d['vKlt095'], d['vAll'])), '%.4f' % stat(a, y, lambda d: r(d['vK2gt105'], d['vAll'])),
                '%.4f' % stat(a, y, lambda d: r(d['vK2lt095'], d['vAll'])), '%d' % stat(a, y, lambda d: d['nNoPx'])]
        o.write('\t'.join(row) + '\n')

# ---- G-b（反証 中-1 (b) の書き方）：M＝CB の中で売れる量が上がった行の価額の割合・M′＝一次（κ を掛けた） ----
o.write('\n[G-b] M＝CB(θ) の中の上がった行の価額の割合（旗 0）・M1＝一次の旗 1（v×κ）。幅＝3 本のヌル腕の M の最大−最小（種の中央）\n')
o.write('theta\tyear\t' + '\t'.join('M_%s\tM1_%s\tdM_%s' % (a, a, a) for a in ARMS) + '\twidth\tdM/width_min\tfire(dM>2w)\tLOO_falsefire\n')
for t in ('0.7', '0.9', 'all'):
    for y in years_all:
        Ms = []; M1s = []
        for a in ARMS:
            if t == 'all':
                M = stat(a, y, lambda d: r(d['vRise'], d['vAll'])); M1 = stat(a, y, lambda d: r(d['vRisek'], d['vAllk']))
            else:
                M = stat(a, y, lambda d, t=t: r(d['vCBrise' + t], d['vCB' + t])); M1 = stat(a, y, lambda d, t=t: r(d['vCBrisek' + t], d['vCBk' + t]))
            Ms.append(M); M1s.append(M1)
        w = max(Ms) - min(Ms)
        dMs = [b - a for a, b in zip(Ms, M1s)]
        fire = all(d > 2 * w for d in dMs)
        # 一つ抜き：ヌル腕の 1 本を腕と見なし、残り 2 本の最大を幅（残り 2 本の差）だけ越えたら空振り
        ff = 0
        for i in range(3):
            oth = [Ms[j] for j in range(3) if j != i]; wo = max(oth) - min(oth)
            if Ms[i] - max(oth) > wo: ff += 1
        o.write('%s\t%s\t' % (t, y) + '\t'.join('%.5f\t%.5f\t%.5f' % (a, b, c) for a, b, c in zip(Ms, M1s, dMs))
                + '\t%.5f\t%.2f\t%s\t%d/3\n' % (w, min(dMs) / w if w > 0 else float('inf'), fire, ff))

# ---- G-d（§7 W2(d) の書き方）：腕 A で上がった行のうち、産出が腕 B より高い行の割合（同じ鍵の行どうし） ----
def load(a):
    R = {}
    f = open(a + '_rows.tsv', encoding='utf-8'); h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
    for ln in f:
        v = ln.rstrip('\n').split('\t')
        if v[ix['year']] not in ('1870', '1913'): continue
        so = float(v[ix['sOld']]); sb = float(v[ix['sBoth']])
        flag = 1 if so > 0 and (sb - so) / so > 0.01 else (-1 if sb < so * 0.99 else 0)
        key = (v[ix['seed']], v[ix['year']], v[ix['region']], v[ix['good']], v[ix['recipe']], v[ix['occ']])
        px = float(v[ix['px']]) if v[ix['px']] else 0.0
        R[key] = (float(v[ix['out']]), float(v[ix['kappa']]), flag, px)
    f.close()
    return R


Rs = {a: load(a) for a in ARMS}
o.write('\n[G-d] 腕 A で上がった行（flag＋1）のうち out_A＞out_B の割合（件数と A の価額）／下がった行（flag−1）のうち out_A＜out_B の割合。pred＝out_A×κ_A で同じ数え\n')
o.write('year\tA\tB\tnUp\tupCnt\tupCntPred\tupVal\tupValPred\tnDn\tdnCnt\tdnCntPred\n')
res = defaultdict(list)
for y in ('1870', '1913'):
    for A in ARMS:
        for B in ARMS:
            if A == B: continue
            RA, RB = Rs[A], Rs[B]
            c = defaultdict(float)
            for k, (oa, ka, fl, px) in RA.items():
                if k[1] != y or fl == 0: continue
                c['all'] += 1
                rb = RB.get(k)
                if rb is None: continue
                c['hit'] += 1
                ob = rb[0]
                if not (oa > 0 or ob > 0): continue
                if fl > 0:
                    c['nUp'] += 1; v = oa * px
                    c['vUp'] += v
                    if oa > ob: c['up'] += 1; c['upV'] += v
                    if oa * ka > ob: c['upP'] += 1; c['upPV'] += oa * ka * px
                    c['vUpP'] += oa * ka * px
                else:
                    c['nDn'] += 1
                    if oa < ob: c['dn'] += 1
                    if oa * ka < ob: c['dnP'] += 1
            row = (r(c['up'], c['nUp']), r(c['upP'], c['nUp']), r(c['upV'], c['vUp']), r(c['upPV'], c['vUpP']), r(c['dn'], c['nDn']), r(c['dnP'], c['nDn']))
            res[y].append(row)
            o.write('%s\t%s\t%s\t%d\t%.4f\t%.4f\t%.4f\t%.4f\t%d\t%.4f\t%.4f\n' % ((y, A, B, c['nUp']) + row[:4] + (c['nDn'],) + row[4:]))
            o.write('    key match %s %s->%s: %d/%d = %.4f\n' % (y, A, B, c['hit'], c['all'], r(c['hit'], c['all'])))
    rows = res[y]
    for nm, i0, i1 in (('upCnt', 0, 1), ('upVal', 2, 3), ('dnCnt', 4, 5)):
        nul = [x[i0] for x in rows]; pr = [x[i1] for x in rows]
        w = max(nul) - min(nul)
        o.write('  %s %s：ヌル 6 向きの幅 %.4f（%.4f〜%.4f）・一次の効き（pred−null 同じ向き）の最小 %.4f・最大 %.4f・最小÷幅 %.2f\n' % (
            y, nm, w, min(nul), max(nul), min(p - n for p, n in zip(pr, nul)), max(p - n for p, n in zip(pr, nul)), (min(p - n for p, n in zip(pr, nul)) / w) if w > 0 else float('inf')))
o.close()
