# (1210) gread.py の直し（元＝origin/c120t2:scripts/instruments/c120t2/gread.py・g は元のまま・px を同じ州の系列へ・gdp と selftest を足した）。読むだけの台本。
# 使い方：
#   python -X utf8 -I gread.py g <A0 の生（depmin_s）> <B の生（depmin_s）> <種の起点> <種の終点>
#   python -X utf8 -I gread.py px <生（mintreg_s）> <種の起点> <種の終点>
#       ＝S2b：(国, 州) の系列ごとに、続いた四半期（q と q−1）だけを比べる＝非有限と 1 四半期 100 倍以上（上がりと下がり）。
#       元の px は国×四半期の最初の行を読んだ＝州が入れ替わると別の州の値段どうしを比べた（(1202) v の低〈同じ州の系列では 0 件〉）。
#       参考に、元の読み方（国の最初の行）の当たりの数も 1 行で名乗る（old=…）。
#   python -X utf8 -I gread.py gdp <生（nation_s）> <種の起点> <種の終点>
#       ＝名目 GDP（nominalGdpPerYear）の 1 四半期 100 倍以上の行。各行に〈政府の州の数 前→後〉〈政府の人口 前→後〉と、
#       州が 1 四半期で欠けた（州の数が減った）か・人口が 1/10 未満か 10 倍以上になったか（★(702)・㊳）を名乗らせる。系列の最初の行から 4 四半期以内は〈建国の直後〉と名乗る。
#   python -X utf8 -I gread.py selftest
#       ＝人工の表で px と gdp の読み方を確かめる（元の読み方で当たり・新しい読み方で 0／州の欠けを名乗る）。
import sys, os, csv, math, statistics, collections, tempfile
csv.field_size_limit(1 << 30)

def rows(p):
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for row in r: yield ix, row

def med(v):
    v = sorted(x for x in v if x == x)
    return statistics.median(v) if v else float('nan')

def fnum(s):
    try: return float(s)
    except ValueError: return float('nan')

def g(A0, B, s0, s1):
    YEARS = (1850, 1880, 1913)
    kap = collections.defaultdict(list); n_pop = collections.Counter()
    site = collections.defaultdict(dict)
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

def ratio_hits(series, label, s, bad, stat):
    # series: q -> (Ag, Au)・続いた四半期だけを比べる
    prev_q = None; prev = None
    for q in sorted(series):
        vals = series[q]
        for j, nm in ((0, 'Ag'), (1, 'Au')):
            if not math.isfinite(vals[j]): bad.append('種 %d %s q%d price%s 非有限' % (s, label, q, nm))
        if prev is not None and q == prev_q + 1:
            for j, nm in ((0, 'Ag'), (1, 'Au')):
                a, b = prev[j], vals[j]
                if a > 0 and b > 0 and math.isfinite(a) and math.isfinite(b):
                    rr = max(b / a, a / b); stat['max'] = max(stat['max'], rr); stat['n'] += 1
                    if rr >= 100: bad.append('種 %d %s q%d price%s 1 四半期 %.1f 倍' % (s, label, q, nm, rr))
        elif prev is not None:
            stat['gap'] += 1
        prev_q, prev = q, vals

def px(D, s0, s1, quiet=False):
    bad = []; stat = {'max': 0.0, 'n': 0, 'gap': 0}
    old_bad = []; old_stat = {'max': 0.0, 'n': 0, 'gap': 0}
    for s in range(s0, s1 + 1):
        ser = collections.defaultdict(dict)       # (国, 州) -> q -> (Ag, Au)
        first = collections.defaultdict(dict)     # 国 -> q -> 最初の行（元の読み方・参考）
        for ix, r in rows(os.path.join(D, 'mintreg_s%d.tsv' % s)):
            nat, reg, q = r[ix['nation']], r[ix['region']], int(r[ix['q']])
            v = (fnum(r[ix['priceAg']]), fnum(r[ix['priceAu']]))
            ser[(nat, reg)][q] = v
            if q not in first[nat]: first[nat][q] = v
        for (nat, reg), qs in ser.items(): ratio_hits(qs, nat + '/' + reg, s, bad, stat)
        for nat, qs in first.items(): ratio_hits(qs, nat + '（国の最初の行）', s, old_bad, old_stat)
    if not quiet:
        print('== S2b 銀と金の値段（mintreg_s・(国, 州) の系列・続いた四半期）：比べた続きの数 %d・1 四半期の比の最大 %.3f・飛んだ四半期 %d・当たり %d 件 ==' % (stat['n'], stat['max'], stat['gap'], len(bad)))
        for x in bad[:40]: print(x)
        print('old=国の最初の行の読み方（元の gread.py の型・参考）：比べた %d・最大 %.3f・当たり %d 件' % (old_stat['n'], old_stat['max'], len(old_bad)))
        for x in old_bad[:10]: print('  old ' + x)
    return bad, old_bad

def gdp(D, s0, s1, quiet=False):
    hits = []; nmax = 0.0; ncmp = 0
    for s in range(s0, s1 + 1):
        ser = collections.defaultdict(dict)
        for ix, r in rows(os.path.join(D, 'nation_s%d.tsv' % s)):
            nat, q = r[ix['nation']], int(r[ix['quarter']])
            # nonCoreRegions は州の名の並び（数ではない）＝数えない。州の数＝本国格・統合・植民地（数の列だけ）。
            nreg = tuple(int(fnum(r[ix[c]])) for c in ('coreRegions', 'integratedRegions', 'colonialRegions'))
            ser[nat][q] = (fnum(r[ix['nominalGdpPerYear']]), nreg, fnum(r[ix['populationK']]))
        for nat, qs in ser.items():
            q0 = min(qs); pq = None
            for q in sorted(qs):
                if pq is not None and q == pq + 1:
                    (a, ra, pa), (b, rb, pb) = qs[pq], qs[q]
                    if a > 0 and b > 0 and math.isfinite(a) and math.isfinite(b):
                        rr = max(b / a, a / b); nmax = max(nmax, rr); ncmp += 1
                        if rr >= 100:
                            tags = []
                            nm3 = ('本国格', '統合', '植民地')
                            lost = ['%s %d→%d' % (nm3[i], ra[i], rb[i]) for i in range(3) if rb[i] < ra[i]]
                            gain = ['%s %d→%d' % (nm3[i], ra[i], rb[i]) for i in range(3) if rb[i] > ra[i]]
                            if lost: tags.append('政府の州が 1 四半期で欠けた（' + '・'.join(lost) + '）')
                            if gain: tags.append('州が増えた（' + '・'.join(gain) + '）')
                            if pa > 0 and pb > 0 and (pb / pa < 0.1 or pb / pa >= 10): tags.append('政府の人口 %.4g→%.4g 千人（1/10 未満か 10 倍以上）' % (pa, pb))
                            if q - q0 <= 4: tags.append('建国の直後（系列の最初の行から %d 四半期）' % (q - q0))
                            if not tags: tags.append('州と人口の跳びなし')
                            hits.append('種 %d %s q%d 名目 GDP %.4g→%.4g（%.1f 倍）・%s' % (s, nat, q, a, b, rr, '・'.join(tags)))
                pq = q
    if not quiet:
        print('== 名目 GDP の 1 四半期の比（nation_s・続いた四半期）：比べた %d・最大 %.3f・100 倍以上 %d 件 ==' % (ncmp, nmax, len(hits)))
        for x in hits[:60]: print(x)
    return hits

def selftest():
    d = tempfile.mkdtemp(prefix='gread_selftest_', dir=os.environ.get('TMP') or None)
    H = 'year\tq\tnation\tregion\tpriceAg\tpriceAu\n'
    with open(os.path.join(d, 'mintreg_s1.tsv'), 'w', encoding='utf-8', newline='\n') as f:
        f.write(H)
        # 国 X：q0 は州 a（値段 1）が最初の行・q1 は州 a が消え州 b（値段 200）が最初の行＝元の読み方は 200 倍で当たる・同じ州の系列では 0 件
        f.write('1783\t0\tX\ta\t1\t1\n1783\t0\tX\tb\t200\t200\n1783\t1\tX\tb\t201\t201\n1783\t2\tX\ta\t1.01\t1.01\n')
        # 国 Y：同じ州が本当に 150 倍＝新しい読み方でも当たる
        f.write('1783\t0\tY\tc\t1\t1\n1783\t1\tY\tc\t150\t1\n')
        # 国 G：州 g が q1 に欠けて q2 に 300 で戻る＝続いた四半期ではない＝1 四半期の比に数えない（飛んだ四半期に数える）
        f.write('1783\t0\tG\tg\t1\t1\n1783\t1\tG\th\t1\t1\n1783\t2\tG\tg\t300\t300\n')
    with open(os.path.join(d, 'nation_s1.tsv'), 'w', encoding='utf-8', newline='\n') as f:
        f.write('quarter\tnation\tnominalGdpPerYear\tcoreRegions\tintegratedRegions\tcolonialRegions\tnonCoreRegions\tpopulationK\n')
        # nonCoreRegions は実物と同じく名の並び（C:州;）を入れる＝数として足すと読み違える（(1210) で一度踏んだ）
        for q in range(0, 8): f.write('%d\tZ\t%s\t3\t0\t0\t%s\t1000\n' % (q, 1000 if q < 7 else 5, '' if q < 7 else 'C:p;'))
        for q in range(0, 8): f.write('%d\tW\t%s\t%d\t0\t1\tC:ア;\t%s\n' % (q, 1000 if q < 7 else 2, 3 if q < 7 else 1, 1000 if q < 7 else 50))
        for q in range(0, 8): f.write('%d\tV\t%s\t1\t0\t0\t\t10\n' % (q, 1 if q < 6 else 500))
        # U：本国格 1→0（州が入れ替わり植民地の名が残る）・人口 1/5＝人口の印は付かず州の欠けだけを名乗る（実物の種 3 ライン諸邦 q216 の型）
        for q in range(0, 8): f.write('%d\tU\t%s\t%d\t0\t1\t%s\t%s\n' % (q, 1000 if q != 6 else 2, 1 if q != 6 else 0, 'C:イ;', 3000 if q != 6 else 600))
    bad, old = px(d, 1, 1, quiet=True)
    ok1 = len([x for x in old if 'X' in x and '倍' in x]) >= 1
    ok2 = not any('X/' in x for x in bad)
    ok3 = any('Y/c' in x and '150.0 倍' in x for x in bad)
    ok3b = not any('G/g' in x for x in bad)
    h = gdp(d, 1, 1, quiet=True)
    ok4 = any(' Z ' in x and '州と人口の跳びなし' in x for x in h)
    ok5 = any(' W ' in x and '政府の州が 1 四半期で欠けた（本国格 3→1）' in x and '1/10 未満' in x for x in h)
    ok5b = any(' U q6 ' in x and '政府の州が 1 四半期で欠けた（本国格 1→0）' in x and '人口' not in x for x in h) and any(' U q7 ' in x and '州が増えた（本国格 0→1）' in x for x in h)
    ok6 = any(' V ' in x and '州と人口の跳びなし' in x for x in h) and not any(' V ' in x and '建国の直後' in x for x in h)
    res = [('元の読み方で国 X が当たる（州の入れ替わりの跳び）', ok1), ('同じ州の系列で国 X は 0 件', ok2), ('同じ州の本当の 150 倍は当たる', ok3), ('欠けた四半期をまたぐ 300 倍は 1 四半期の比に数えない', ok3b),
           ('州も人口も跳ばない GDP の 200 倍は〈跳びなし〉と名乗る', ok4), ('州が 3→1・人口 1/20 の GDP の 500 倍は州の欠けと人口を名乗る', ok5), ('本国格 1→0→1 の跳びは欠けと戻りを名乗る（名の並びの列を数えない）', ok5b),('系列の 6 四半期目は建国の直後と名乗らない', ok6)]
    for nm, ok in res: print(('PASS ' if ok else 'FAIL ') + nm)
    for fn in os.listdir(d): os.remove(os.path.join(d, fn))
    os.rmdir(d)
    print('selftest %s' % ('PASS' if all(o for _, o in res) else 'FAIL'))
    return all(o for _, o in res)

if __name__ == '__main__':
    m = sys.argv[1]
    if m == 'g': g(sys.argv[2], sys.argv[3], int(sys.argv[4]), int(sys.argv[5]))
    elif m == 'px': px(sys.argv[2], int(sys.argv[3]), int(sys.argv[4]))
    elif m == 'gdp': gdp(sys.argv[2], int(sys.argv[3]), int(sys.argv[4]))
    elif m == 'selftest': sys.exit(0 if selftest() else 1)
    else: raise SystemExit('mode: g | px | gdp | selftest')
