"""(814) 型 A の 10 件の窓（ピークの年〜1/4 未満の年＝r809 と同じ）で稼ぎの成り立ちと紙の見積もりを出す（読むだけ・エンジンの関数は呼ばない）。
使い方: python -I r814_decomp.py <ev_type.tsv> <出力.tsv> <ラベル>=<ディレクトリ> ...
列：
  frozenShare＝窓の四半期のうち工場と採取の産出がどちらも 0 の割合／earnPCmed＝1 人あたりの稼ぎの中央／earnRatioMed＝持ち主の国の植民地格でない州の 1 人あたりに対する比の中央
  decayPerMonth＝稼ぎ÷人口が 1e-6 未満の連続の区間で log を四半期に最小二乗した月あたりの減り（区間 8 四半期以上のときだけ）
  empMed・wPremMed・landF1Share（LandFactor＝1 の割合＝耕地÷農村≥1）・arablePerRuralMed
  fd の列（計器の国の州だけ）：fillMed・subOverNeedMed・desiredOverNeedMed・boughtOverDesiredMed・fillBelowGateShare（fillNew<0.8）
  紙：paperFullShare＝min(1,(sub+desired)/need)≥0.8 の割合（欲しい量を全て買えた上限）／
      rCash＝同じ州×種で 1 人あたりの稼ぎ≥0.1 の四半期の bought/desired の中央（現金があった時の買えた割合）／paperCashShare＝(sub+rCash×desired)/need≥0.8 の割合
      paperLandShare＝0.7×holding（必要量で切る）≥0.8 の割合（r809 paperA と同じ根 A の紙）"""
import sys, os, collections, math
def fl(x):
    try: return float(x)
    except: return float('nan')
def med(v):
    v = sorted(x for x in v if math.isfinite(x))
    return v[len(v)//2] if v else float('nan')
evp, out = sys.argv[1], sys.argv[2]
sets = dict(a.split('=', 1) for a in sys.argv[3:])
ev = []
with open(evp, encoding='utf-8') as fh:
    h = fh.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
    for line in fh:
        x = line.rstrip('\n').split('\t')
        if x[ix['cause']] == '飢え': ev.append((x[ix['set']], int(x[ix['seed']]), x[ix['region']], int(x[ix['peakYear']]), int(x[ix['dropYear']])))
cols = ['set','seed','region','y0','y1','nQ','frozenShare','earnPCmed','earnRatioMed','decayPerMonth','decayFromYear','empMed','wPremMed','landF1Share','arablePerRuralMed',
        'fdQ','fillMed','subOverNeedMed','desiredOverNeedMed','boughtOverDesiredMed','fillBelowGateShare','paperFullShare','rCash','paperCashShare','paperLandShare']
o = open(out, 'w', encoding='utf-8', newline='\n'); o.write('\t'.join(cols) + '\n')
for lab, sd, reg, y0, y1 in ev:
    d = sets[lab]; R = {}; main = collections.defaultdict(lambda: [0.0, 0.0])
    with open(os.path.join(d, 'region_s%d.tsv' % sd), encoding='utf-8') as fh:
        h = fh.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in fh:
            a = line.rstrip('\n').split('\t')
            q = int(a[ix['quarter']]); nat = a[ix['nation']]; pop = fl(a[ix['populationK']]); e = fl(a[ix['earnedPerMonth']])
            if a[ix['tier']] != 'Colonial' and math.isfinite(pop) and math.isfinite(e): m = main[(q, nat)]; m[0] += e; m[1] += pop
            if a[ix['region']] == reg:
                R[q] = dict(y=int(a[ix['year']]), nat=nat, pop=pop, rural=fl(a[ix['ruralPopulationK']]), ar=fl(a[ix['arableLand']]), lf=fl(a[ix['landFactor']]), hold=fl(a[ix['holdingFactor']]),
                            wp=fl(a[ix['wagePremium']]), emp=fl(a[ix['employmentShare']]), e=e, fo=fl(a[ix['factoryOutputRefValuePerMonth']]), eo=fl(a[ix['extractionOutputRefValuePerMonth']]))
    F = {}
    fp = os.path.join(d, 'fd_s%d.tsv' % sd)
    if os.path.exists(fp):
        with open(fp, encoding='utf-8') as fh:
            h = fh.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
            for line in fh:
                a = line.rstrip('\n').split('\t')
                if a[ix['region']] == reg:
                    F[int(a[ix['quarter']])] = dict(need=fl(a[ix['stapleNeedPerMonth']]), des=fl(a[ix['desiredPerMonth']]), dem=fl(a[ix['demandPerMonth']]), sub=fl(a[ix['subPerMonth']]), fill=fl(a[ix['fillNew']]))
    def epc(r): return r['e'] / r['pop'] if r['pop'] > 0 else float('nan')
    def er(q, r):
        m = main.get((q, r['nat']), [0, 0]); mpc = m[0] / m[1] if m[1] > 0 else float('nan')
        return epc(r) / mpc if mpc > 0 else float('nan')
    W = [q for q in sorted(R) if y0 <= R[q]['y'] <= y1]
    fro = sum(1 for q in W if R[q]['fo'] == 0 and R[q]['eo'] == 0) / max(1, len(W))
    # 減りの速さ：1e-6 未満の最長の連続区間
    runs = []; cur = []
    for q in sorted(R):
        if R[q]['pop'] >= 1 and math.isfinite(R[q]['e']) and R[q]['e'] > 0 and epc(R[q]) < 1e-6: cur.append(q)
        else:
            if cur: runs.append(cur); cur = []
    if cur: runs.append(cur)
    dec = float('nan'); dfy = ''
    if runs:
        rr = max(runs, key=len)
        if len(rr) >= 8:
            xs = rr; ys = [math.log(epc(R[q])) for q in rr]
            mx = sum(xs) / len(xs); my = sum(ys) / len(ys)
            b = sum((x - mx) * (y - my) for x, y in zip(xs, ys)) / sum((x - mx) ** 2 for x in xs)
            dec = 1 - math.exp(b / 3); dfy = str(R[rr[0]]['y'])
    fq = [q for q in W if q in F and F[q]['need'] > 0]
    rc = med([F[q]['dem'] / F[q]['des'] for q in F if q in R and epc(R[q]) >= 0.1 and F[q]['des'] > 0 and q > 0])
    def share(pred, qs): return sum(1 for q in qs if pred(q)) / len(qs) if qs else float('nan')
    row = [lab, sd, reg, y0, y1, len(W), fro, med([epc(R[q]) for q in W]), med([er(q, R[q]) for q in W]), dec, dfy,
           med([R[q]['emp'] for q in W]), med([R[q]['wp'] for q in W]), share(lambda q: R[q]['lf'] >= 1.0, W), med([R[q]['ar'] / max(1.0, R[q]['rural']) for q in W]),
           len(fq), med([F[q]['fill'] for q in fq]), med([F[q]['sub'] / F[q]['need'] for q in fq]), med([F[q]['des'] / F[q]['need'] for q in fq]),
           med([F[q]['dem'] / F[q]['des'] for q in fq if F[q]['des'] > 0]), share(lambda q: F[q]['fill'] < 0.8, fq),
           share(lambda q: min(1, (F[q]['sub'] + F[q]['des']) / F[q]['need']) >= 0.8, fq), rc,
           share(lambda q: min(1, (F[q]['sub'] + (rc if math.isfinite(rc) else 0) * F[q]['des']) / F[q]['need']) >= 0.8, fq),
           share(lambda q: min(1.0, 0.7 * R[q]['hold']) >= 0.8, W)]
    f = lambda v: format(v, '.3g') if isinstance(v, float) else str(v)
    o.write('\t'.join(f(v) for v in row) + '\n')
o.close(); print('書いた', out)
