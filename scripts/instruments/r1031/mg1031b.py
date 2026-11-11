# (1031) c97mg F1 の門 W1〜W4 の母集合への効き（読むだけ）
# merch1021.py（exit の式）と同じ母集合（merchantK>1e-9・nan を除く）で、Eps の帯（流量が Eps 以下になる微小な商人）を分けて数える。
# 使い方：python -X utf8 -I mg1031b.py <ins フォルダ> <種 例 1-10> <出力>
import sys, os
from collections import defaultdict
d = sys.argv[1]; lo, hi = (int(x) for x in sys.argv[2].split('-')); outp = sys.argv[3]
EPS = 1e-9; RF = 0.03; DR = 0.01   # MiddleFallRatePerQuarter・ArtisanDeclineRatePerQuarter（Coefficients.cs:722・817）
def rd(path):
    with open(path, 'rb') as f:
        h = f.readline().decode('utf-8').rstrip('\r\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for ln in f: yield ix, ln.decode('utf-8').rstrip('\r\n').split('\t')
def fl(v):
    try: return float(v)
    except ValueError: return float('nan')
K = lambda t: 'Colonial' if t == 'Colonial' else 'Home'
C = defaultdict(int); runs = {m: defaultdict(list) for m in ('all', 'f1flow')}
mkq = defaultdict(list)
for s in range(lo, hi + 1):
    ser = defaultdict(list)
    for ix, x in rd(os.path.join(d, f'merch_s{s}.tsv')):
        ser[(x[ix['nation']], x[ix['region']])].append((int(x[ix['quarter']]), K(x[ix['tier']]), fl(x[ix['merchantK']]), fl(x[ix['merchEarnedPerCap']]),
            fl(x[ix['workerIncomePerCap']]), fl(x[ix['promotionPremium']]), x[ix['merchFallGateIn']], fl(x[ix['merchFallOut']]),
            fl(x[ix['merchPettyIn']]), fl(x[ix['merchCapRiseOut']])))
    for key, L in ser.items():
        L.sort()
        run = {'all': 0, 'f1flow': 0}; prevq = {'all': None, 'f1flow': None}; kk = None
        def close(m, k):
            if run[m]: runs[m][k].append(run[m])
            run[m] = 0
        for (q, k, mk, me, wi, pr, gate, fo, pin, cro) in L:
            kk = k
            live = mk > EPS and me == me and wi == wi and pr == pr
            if not live:
                close('all', k); close('f1flow', k); continue
            hit = wi > me * pr
            attract = max(0.0, wi) / max(EPS, max(0.0, me) * pr)
            f1 = DR * mk * min(attract - 1.0, 1.0) if attract > 1.0 else 0.0   # F1 の素案の流量（merchantK は社会移動の前の頭数＝近似）
            dust = RF * (mk + pin - cro) <= EPS                                  # :345 の流量が Eps 以下になる帯（逆向きの行の区分 c と同じ判定）
            f1dust = hit and not (f1 > EPS)
            C[(k, 'live')] += 1; C[(k, 'hit')] += hit
            C[(k, 'liveDust')] += dust; C[(k, 'hitDust')] += hit and dust; C[(k, 'hitF1flowLeEps')] += f1dust
            if gate == '1':
                C[(k, 'gate1')] += 1; C[(k, 'hitGate1')] += hit
                if not (fo > EPS): C[(k, 'gate1Rev')] += 1; C[(k, 'hitGate1Rev')] += hit
                if dust: C[(k, 'gate1Dust')] += 1
            if hit: mkq[k].append(mk)
            for m, ok in (('all', hit), ('f1flow', hit and f1 > EPS)):
                if ok:
                    run[m] = run[m] + 1 if (run[m] and prevq[m] == q - 1) else (close(m, k) or 1)
                    prevq[m] = q
                else:
                    close(m, k)
        if kk: close('all', kk); close('f1flow', kk)
with open(outp, 'w', encoding='utf-8') as o:
    P = lambda *a: print(*a, file=o)
    P(f'(1031) mg1031b 生={d} 種={lo}-{hi}')
    for k in ('Colonial', 'Home'):
        g = lambda n: C[(k, n)]
        P(f'B1 {k} 空でない行={g("live")} exit に当たる={g("hit")} ({g("hit") / max(1, g("live")):.4f})・Eps の帯（:345 の流量≦Eps）={g("liveDust")}・exit かつ帯={g("hitDust")}・exit かつ F1 の流量≦Eps={g("hitF1flowLeEps")}')
        P(f'B2 {k} 門1={g("gate1")}・うち逆向き={g("gate1Rev")}・うち帯={g("gate1Dust")}・exit かつ門1={g("hitGate1")} ({g("hitGate1") / max(1, g("hit")):.4f})・exit かつ門1 かつ逆向き={g("hitGate1Rev")}')
        for m in ('all', 'f1flow'):
            R = sorted(runs[m][k])
            if R:
                tot = sum(R)
                P(f'B3 {k} 続き[{m}] 数={len(R)} 行={tot} 中央={R[len(R) // 2]} 90%={R[int(0.9 * (len(R) - 1))]} 最大={R[-1]} ≧20 に入る行の割合={sum(r for r in R if r >= 20) / tot:.4f}')
        M = sorted(mkq[k])
        if M:
            P(f'B4 {k} exit の行の merchantK（千人）分位：1% {M[int(.01 * len(M))]:.3g}・10% {M[int(.1 * len(M))]:.3g}・50% {M[len(M) // 2]:.3g}・≦1e-6 の行={sum(v <= 1e-6 for v in M)}・≦1e-3 の行={sum(v <= 1e-3 for v in M)}')
print('done')
