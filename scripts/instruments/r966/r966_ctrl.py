# (966) c86ef 読むだけ：対照＝全ての国×種で、配当の重さと純の流出を、F のある国と無い国で並べる（E が 3/4 の国にある＝E だけでは型を分けない、の確かめ）。
#   定義（数える前に固定）：
#     lev＝外資の負債÷名目 GDP（年）の中央（divhost の fci・nation_s の nominalGdpPerYear・両方正の四半期）
#     Eの割合＝配当の請求 ＞0 の四半期のうち 払い ＜請求の 1/2 の割合
#     drain8＝8 四半期の純資本（outCapital−inOther）の和÷その窓の頭の貯蓄（cearn・国の州の和）の最大（頭の貯蓄が正の窓だけ）
#     imp8＝同じ窓の純輸入（outImport−inExport）の和÷頭の貯蓄（drain8 が最大の窓の値）
#     paid8＝同じ窓の配当の払いの和÷頭の貯蓄
#   F＝priceLevel ≦0.4＋1e-6 が 4 四半期以上続く区間がある国。分かれた国＝cederule_s の Seceded・Colonial の receiver。
#   助走の行は (国,quarter) ごとに call が最大の行だけ。エンジンの関数を呼ばない。
#   python -X utf8 -I r966_ctrl.py <出力> <名>=<ins>:<種の始め>:<種の終わり> ...
import sys, os, statistics as st
FLOOR = 0.4 + 1e-6
def fl(x):
    try: return float(x)
    except ValueError: return float('nan')
def rd(p):
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in f: yield ix, line.rstrip('\n').split('\t')
def hasrun(qs, k=4):
    qs = sorted(set(qs)); c = 0; pv = None
    for q in qs:
        c = c + 1 if pv is not None and q == pv + 1 else 1; pv = q
        if c >= k: return True
    return False
out = sys.argv[1]
fo = open(out, 'w', encoding='utf-8', newline='')
fo.write('\t'.join(['盤面', '種', '国', '分', 'F', '人口の最大', 'lev', 'Eの割合', 'drain8', 'imp8', 'paid8', 'drain8の窓の頭']) + '\n')
for arg in sys.argv[2:]:
    name, rest = arg.split('=', 1); d, a0, a1 = rest.rsplit(':', 2)
    for s in range(int(a0), int(a1) + 1):
        pn = os.path.join(d, f'nation_s{s}.tsv')
        if not os.path.exists(pn): continue
        sec = set()
        for ix, x in rd(os.path.join(d, f'cederule_s{s}.tsv')):
            if x[ix['how']] == 'Seceded' and x[ix['tierBefore']] == 'Colonial': sec.add(x[ix['receiver']])
        N = {}
        for ix, x in rd(pn):
            N.setdefault(x[ix['nation']], {})[int(x[ix['quarter']])] = (fl(x[ix['populationK']]), fl(x[ix['nominalGdpPerYear']]), fl(x[ix['priceLevel']]))
        D = {}; cl = {}
        for ix, x in rd(os.path.join(d, f'divhost_s{s}.tsv')):
            k = (x[ix['nation']], int(x[ix['quarter']])); c = int(x[ix['call']])
            if cl.get(k, -1) > c: continue
            cl[k] = c; D[k] = (fl(x[ix['fci']]), fl(x[ix['divReqIndex']]), fl(x[ix['paidIndex']]))
        C = {}
        for ix, x in rd(os.path.join(d, f'cearn_s{s}.tsv')):
            k = (x[ix['nation']], int(x[ix['quarter']])); a = C.setdefault(k, [0.0, 0.0, 0.0])
            for j, cn in enumerate(('savings', 'outCapitalPerQuarter', 'inOtherPerQuarter', 'outImportPerQuarter', 'inExportPerQuarter')):
                v = fl(x[ix[cn]]); v = v if v == v else 0.0
                if j == 0: a[0] += v
                elif j == 1: a[1] += v
                elif j == 2: a[1] -= v
                elif j == 3: a[2] += v
                else: a[2] -= v
        for n, ser in N.items():
            qs = sorted(ser)
            F = hasrun([q for q in qs if ser[q][2] <= FLOOR])
            lv = [D[(n, q)][0] / ser[q][1] for q in qs if (n, q) in D and D[(n, q)][0] > 0 and ser[q][1] > 0]
            rq = [D[(n, q)] for q in qs if (n, q) in D and D[(n, q)][1] > 0]
            es = sum(1 for v in rq if not (v[2] >= 0.5 * v[1])) / len(rq) if rq else float('nan')
            best = (float('-inf'), float('nan'), float('nan'), '')
            for i, q in enumerate(qs):
                if (n, q) not in C or not (C[(n, q)][0] > 0): continue
                w = [t for t in range(q, q + 8) if (n, t) in C]
                if len(w) < 8: continue
                s0 = C[(n, q)][0]
                dr = sum(C[(n, t)][1] for t in w) / s0
                if dr > best[0]:
                    best = (dr, sum(C[(n, t)][2] for t in w) / s0, sum(D[(n, t)][2] for t in w if (n, t) in D) / s0, q)
            pk = max(v[0] for v in ser.values())
            g = lambda v: format(v, '.3g') if v == v and abs(v) != float('inf') else ''
            fo.write('\t'.join(map(str, [name, s, n, '分' if n in sec else '-', 'F' if F else '-', round(pk), g(st.median(lv)) if lv else '', g(es), g(best[0]), g(best[1]), g(best[2]), best[3]])) + '\n')
        fo.flush()
