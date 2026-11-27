# (1402) 読む線：バーミンガムの吸い込みの始まりを読む。
#  (1) region_s（四半期）：workerSinkShareEnd（(1392) の定義＝主な工場労働者の POP に乗る生産者の LaborWeight×Scale のうち投入の充足<0.05 の割合）・
#      主食の輸入の割合 stapleImport÷(stapleImport+stapleProduced)・食の充足・人口・国の中の取り分 regionShareEnd・世界の産出の取り分 gdpOutputShareEnd（末尾 7 列が無い生は空）。
#  (2) sellr_s（10 年ごとの標本）：q3_sink と同じ定義の吸い込み（作られていない財＝lastProduced<0.02×max(lastDemand,1) に乗る laborWeight の割合・労働の財を除く）を
#      最初の行だけ（q3_sink と同じ）と同じ鍵の全ての行の和の 2 通り。上位の財。
#  (3) prodcap_s（10 年ごと・有れば）：楽器・防水布・チョコレートの capacity・output・lastInputFill。
#  (4) trrail_s（年）：railTrackEnd・procureGotUnitsPerYear・railFreightPaidGoldPerYear。
#  python -X utf8 -I bham.py <タグ> <生> <種の起点> <出力フォルダ>
import sys, os, csv, collections
tag, D, SB, O = sys.argv[1], sys.argv[2], int(sys.argv[3]), sys.argv[4]
B = 'バーミンガム'; LAB = ('労働力', '農業労働力', '高度労働力'); G3 = ('楽器', '防水布', 'チョコレート')
def f(x):
    try: return float(x)
    except ValueError: return float('nan')
def rd(p):
    with open(p, encoding='utf-8') as fh:
        r = csv.reader(fh, delimiter='\t'); h = next(r); ix = {k: j for j, k in enumerate(h)}
        for x in r: yield ix, x
summ = open(os.path.join(O, f'bham_sum_{tag}.tsv'), 'w', encoding='utf-8', newline='\n')
summ.write('arm\tseed\tsinkQ>0.2\tfirstQ>0.2\tfirstYr>0.2\tmaxSinkQ\tmaxSinkYr\tmaxSink\timpShMax1830_45\timpShMaxYr\tfirstQhalf\tfirstYrHalf\tpopPeakYr\tminOverPeak\tsellrYrs>0.2\tsellrYrs>0.2(sum)\n')
sell = open(os.path.join(O, f'bham_sellr_{tag}.tsv'), 'w', encoding='utf-8', newline='\n')
sell.write('arm\tseed\tyear\tsinkFirst\tsinkSum\ttop3\tbreadImpSh\t楽器lw\t楽器prod\t防水布lw\t防水布prod\tチョコlw\tチョコprod\n')
pc = open(os.path.join(O, f'bham_prodcap_{tag}.tsv'), 'w', encoding='utf-8', newline='\n')
pc.write('arm\tseed\tyear\tgood\tcapacity\toutput\tlastInputFill\n')
tr = open(os.path.join(O, f'bham_trrail_{tag}.tsv'), 'w', encoding='utf-8', newline='\n')
tr.write('arm\tseed\tyear\trailTrackEnd\tprocureGotUnitsPerYear\trailFreightPaidGoldPerYear\tneedOutShareEnd\n')
for s in range(SB, SB + 10):
    Q = {}
    for ix, x in rd(os.path.join(D, f'region_s{s}.tsv')):
        if x[ix['region']] != B: continue
        q = int(x[ix['quarter']])
        if q in Q: continue
        g = lambda c: f(x[ix[c]]) if c in ix else float('nan')
        im, pr = g('stapleImportPerMonth'), g('stapleProducedPerMonth')
        Q[q] = dict(y=int(x[ix['year']]), pop=g('populationK'), food=g('foodFillShare'), sink=g('workerSinkShareEnd'), imp=im, prod=pr,
                    impSh=im / (im + pr) if im + pr > 0 else float('nan'), rsh=g('regionShareEnd'), gsh=g('gdpOutputShareEnd'), cap=g('transportCapEnd'))
    with open(os.path.join(O, f'bham_q_{tag}_s{s}.tsv'), 'w', encoding='utf-8', newline='\n') as fo:
        fo.write('quarter\tyear\tpopK\tfoodFill\tsink\tstapleImp\tstapleProd\timpSh\tregionShare\tgdpOutShare\ttransportCap\n')
        for q in sorted(Q):
            v = Q[q]
            if 1820 <= v['y'] <= 1880: fo.write('\t'.join([str(q), str(v['y'])] + [f'{v[k]:.4g}' for k in ('pop', 'food', 'sink', 'imp', 'prod', 'impSh', 'rsh', 'gsh', 'cap')]) + '\n')
    qs = sorted(Q); hi = [q for q in qs if Q[q]['sink'] > 0.2]
    mxq = max((q for q in qs if Q[q]['sink'] == Q[q]['sink']), key=lambda q: Q[q]['sink'], default=None)
    w = [q for q in qs if 1830 <= Q[q]['y'] <= 1845 and Q[q]['impSh'] == Q[q]['impSh']]
    im = max(w, key=lambda q: Q[q]['impSh'], default=None)
    half = next((q for q in qs if im is not None and q > im and Q[q]['impSh'] < 0.5 * Q[im]['impSh']), None) if im is not None and Q[im]['impSh'] > 0.05 else None
    pk = max(qs, key=lambda q: Q[q]['pop']); mop = min(Q[q]['pop'] / Q[pk]['pop'] for q in qs if q >= pk)
    # sellr
    T = collections.defaultdict(float); S1 = collections.defaultdict(float); TS = collections.defaultdict(float); SS = collections.defaultdict(float)
    GG = collections.defaultdict(lambda: collections.defaultdict(float)); seen = set(); X = {}
    for ix, x in rd(os.path.join(D, f'sellr_s{s}.tsv')):
        if x[ix['region']] != B: continue
        gd = x[ix['good']]; y = int(x[ix['year']])
        if gd == 'パン' and (y, gd) not in X: X[(y, gd)] = (f(x[ix['prevImportAll']]), f(x[ix['lastDemand']]))
        if gd in LAB: continue
        lw = max(0.0, f(x[ix['laborWeight']])); dead = f(x[ix['lastProduced']]) < 0.02 * max(f(x[ix['lastDemand']]), 1.0)
        TS[y] += lw
        if dead: SS[y] += lw; GG[y][gd] += lw
        if gd in G3: X[(y, gd)] = (X.get((y, gd), (0, 0))[0] + lw, f(x[ix['lastProduced']]))
        if (y, gd) in seen: continue
        seen.add((y, gd)); T[y] += lw
        if dead: S1[y] += lw
    ny1 = ny2 = 0
    for y in sorted(T):
        a1 = S1[y] / T[y] if T[y] > 0 else float('nan'); a2 = SS[y] / TS[y] if TS[y] > 0 else float('nan')
        ny1 += a1 > 0.2; ny2 += a2 > 0.2
        top = ','.join(f'{g}:{v:.0f}' for g, v in sorted(GG[y].items(), key=lambda t: -t[1])[:3])
        bi = X.get((y, 'パン')); bs = bi[0] / max(1.0, bi[1]) if bi else float('nan')
        sell.write('\t'.join([tag, str(s), str(y), f'{a1:.3f}', f'{a2:.3f}', top, f'{bs:.3f}'] + [f'{X.get((y, g), (float("nan"),) * 2)[j]:.0f}' for g in G3 for j in (0, 1)]) + '\n')
    p = os.path.join(D, f'prodcap_s{s}.tsv')
    if os.path.exists(p):
        for ix, x in rd(p):
            if x[ix['region']] == B and x[ix['good']] in G3:
                pc.write('\t'.join([tag, str(s), x[ix['year']], x[ix['good']], x[ix['capacity']], x[ix['output']], x[ix['lastInputFill']]]) + '\n')
    for ix, x in rd(os.path.join(D, f'trrail_s{s}.tsv')):
        if x[ix['region']] == B and 1825 <= int(x[ix['year']]) <= 1870:
            tr.write('\t'.join([tag, str(s), x[ix['year']]] + [x[ix[c]] if c in ix else '' for c in ('railTrackEnd', 'procureGotUnitsPerYear', 'railFreightPaidGoldPerYear', 'needOutShareEnd')]) + '\n')
    Y = lambda q: Q[q]['y'] if q is not None else ''
    summ.write('\t'.join(str(v) for v in [tag, s, len(hi), hi[0] if hi else '', Y(hi[0]) if hi else '', mxq if mxq is not None else '', Y(mxq), f'{Q[mxq]["sink"]:.3f}' if mxq is not None else 'nan',
               f'{Q[im]["impSh"]:.3f}' if im is not None else 'nan', Y(im), half if half is not None else '', Y(half), Q[pk]['y'], f'{mop:.3f}', ny1, ny2]) + '\n')
print('done', tag)
