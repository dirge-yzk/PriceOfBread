# (1202) c120t2 記録の先頭 G-能・G-産（門にしない・事前登録 §5）と S2b（銀と金の値段の発散＝止める線）。
# 使い方：
#   python -X utf8 -I gread.py g <A0 の生（depmin_s）> <B の生（depmin_s）> <種の起点> <種の終点>
#   python -X utf8 -I gread.py px <生（mintreg_s）> <種の起点> <種の終点>      ＝S2b：国×四半期の priceAg・priceAu の非有限と 1 四半期 100 倍以上（上がりと下がり）
# G-能：ふつうの鉱物の生産者（siteRow<0・heads>0・capacity>0）で A0 の州の品位（regionGrade）≦0.2。
#   κ＝[capacity÷(heads×productivity)]_B ÷ [同]_A0 × regionGrade_A0（種・年・州・財・レシピで突き合わせ・1850・1880・1913 の最後の q）。設計どおりなら κ≒1・州の品位を掛けたままなら ≒0.15。
# G-産：産地の生産者（B・siteRow≥0・siteFound=1）の開いた年からの品位（計器の grade）と 1 人あたりの能力（capacity÷(heads×productivity)）の +1・+5・+30・+60 年。
#   (ii)＝(+30 の 1 人あたり)÷(+1 の 1 人あたり)。v の訂正 8：能力が律速なら設計の +30 年の品位は約 0.654・閾値 0.758 との余裕は 1.16 倍しかない（記録の限界）。
import sys, os, csv, math, statistics, collections
csv.field_size_limit(1 << 30)

def rows(p):
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for row in r: yield ix, row

def med(v):
    v = sorted(x for x in v if x == x)
    return statistics.median(v) if v else float('nan')

def g(A0, B, s0, s1):
    YEARS = (1850, 1880, 1913)
    kap = collections.defaultdict(list); n_pop = collections.Counter()
    site = collections.defaultdict(dict)   # (seed, region, good) -> year -> (grade, perHead, found)
    for s in range(s0, s1 + 1):
        def load(d, keep_sites):
            out = {}; lastq = {}
            for ix, r in rows(os.path.join(d, 'depmin_s%d.tsv' % s)):
                y = int(r[ix['year']]); q = int(r[ix['q']])
                sr = int(r[ix['siteRow']])
                cap = float(r[ix['capacity']]); hd = float(r[ix['heads']]); pr = float(r[ix['productivity']])
                ph = cap / (hd * pr) if hd > 0 and pr > 0 and cap > 0 else float('nan')
                if keep_sites and sr >= 0:
                    site[(s, r[ix['region']], r[ix['good']])][y] = (float(r[ix['grade']]), ph, r[ix['siteFound']] == '1', float(r[ix['depInit']]))
                if y in YEARS and sr < 0:
                    lastq[y] = max(lastq.get(y, -1), q)
                    out[(y, q, r[ix['region']], r[ix['good']], r[ix['recipe']])] = (ph, float(r[ix['regionGrade']]))
            return {k: v for k, v in out.items() if k[1] == lastq[k[0]]}
        a = load(A0, False); b = load(B, True)
        for k, (pha, gra) in a.items():
            if not (gra <= 0.2) or not (pha == pha): continue
            n_pop[k[0]] += 1
            if k in b and b[k][0] == b[k][0]: kap[k[0]].append(b[k][0] / pha * gra)
    print('== G-能（記録）κ の中央・件数（母集合＝A0 で州の品位≦0.2・突き合わせた件数） ==')
    for y in YEARS:
        v = kap[y]; print('%d\tκ 中央 %.4f\t四分位 %s\t母集合 %d\t突き合わせ %d' % (y, med(v), ' '.join('%.3f' % x for x in (statistics.quantiles(v, n=4) if len(v) >= 4 else [])), n_pop[y], len(v)))
    print('== G-産（記録）産地の生産者：開いた年・品位と 1 人あたりの能力の +1・+5・+30・+60 年・(ii)＝+30÷+1 ==')
    r2 = []
    for key, ys in sorted(site.items()):
        op = min((y for y, v in ys.items() if v[2]), default=None)
        if op is None: continue
        cells = []
        for d in (1, 5, 30, 60):
            v = ys.get(op + d); cells.append('+%d %s' % (d, ('品位 %.3f・1人 %.3g' % (v[0], v[1])) if v else '—'))
        a1 = ys.get(op + 1); a30 = ys.get(op + 30)
        ii = a30[1] / a1[1] if a1 and a30 and a1[1] > 0 else float('nan')
        if ii == ii: r2.append(ii)
        print('種 %d\t%s %s\t開いた %d\t%s\t(ii) %.3f' % (key[0], key[1], key[2], op, '\t'.join(cells), ii))
    print('(ii) の中央 %.3f（件数 %d）' % (med(r2), len(r2)))

def px(D, s0, s1):
    bad = []; nmax = 0.0; nrow = 0
    for s in range(s0, s1 + 1):
        ser = collections.defaultdict(dict)
        for ix, r in rows(os.path.join(D, 'mintreg_s%d.tsv' % s)):
            k = (r[ix['nation']], int(r[ix['q']]))
            if k[1] in ser[k[0]]: continue
            ser[k[0]][k[1]] = (r[ix['priceAg']], r[ix['priceAu']])
        for nat, qs in ser.items():
            prev = None
            for q in sorted(qs):
                vals = []
                for j, nm in ((0, 'Ag'), (1, 'Au')):
                    try: x = float(qs[q][j])
                    except ValueError: x = float('nan')
                    vals.append(x)
                    if not math.isfinite(x): bad.append('種 %d %s q%d price%s 非有限 %s' % (s, nat, q, nm, qs[q][j]))
                if prev:
                    for j, nm in ((0, 'Ag'), (1, 'Au')):
                        a, b = prev[j], vals[j]
                        if a > 0 and b > 0 and math.isfinite(a) and math.isfinite(b):
                            rr = max(b / a, a / b); nmax = max(nmax, rr); nrow += 1
                            if rr >= 100: bad.append('種 %d %s q%d price%s 1 四半期 %.1f 倍' % (s, nat, q, nm, rr))
                prev = vals
    print('== S2b 銀と金の値段（mintreg_s・国×四半期）：比べた続きの数 %d・1 四半期の比の最大 %.3f・当たり %d 件 ==' % (nrow, nmax, len(bad)))
    for x in bad[:40]: print(x)

if sys.argv[1] == 'g': g(sys.argv[2], sys.argv[3], int(sys.argv[4]), int(sys.argv[5]))
else: px(sys.argv[2], int(sys.argv[3]), int(sys.argv[4]))
