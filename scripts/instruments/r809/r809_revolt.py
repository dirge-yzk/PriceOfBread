"""(809) c13hl：州一揆の鎮圧（Politics.CheckRegionalRevolt）が同じ州を毎四半期に削っているかを読む（読むだけ）。
使い方: python -I r809_revolt.py <出力.tsv> <ラベル>|<ディレクトリ>|<種>|<国>|<州> ...
 ①secession_s（州ごと・四半期）の militancyMeanIndex（＝州の平均の闘争度＝RegionRevoltHeat と同じ量か近い量）と crowdK（州の平民＝RegionCrowd と同じ量か近い量）から、
   その国の四半期ごとに 熱×群衆 が最大の州（SelectRevoltRegion の鍵の再現・計器の量で近似）と、その熱が門 0.50 を超えたかを数える。
   ⚠近似＝計器の列がエンジンの関数と同じ定義かは列の名前から読んだだけ（PendingRebelRegions の除外・首都の蜂起の四半期の除外は再現しない）。
 ②pop_s の N 行の warDeadK（暴力の死＝戦死・鎮圧・粛清・戦場の民間人）から casualty_s の deadK（兵の戦死）を引いた〈戦場の外の暴力の死〉を 10 年ごとに。
 ③その州が最大で門を超えた四半期の数×0.002×群衆（RevoltSuppressDeadFrac＝鎮圧の死の上限の見積り＝鎮圧でなく譲歩なら死は無い）。"""
import sys, os, collections, math
def fl(x):
    try: return float(x)
    except: return float('nan')
out = sys.argv[1]
o = open(out, 'w', encoding='utf-8', newline='\n')
o.write('set\tseed\tnation\tregion\tdecade\tquartersArgmax\tquartersArgmaxOverGate\tmeanHeatRegion\tcrowdK\tsuppressDeadUpperK\tnationViolentNonBattleK\tnationBattleK\tnationWarDeadK\n')
for a in sys.argv[2:]:
    lab, d, sd, nat, reg = a.split('|', 4); sd = int(sd)
    Q = collections.defaultdict(list)   # q -> [(region, heat, crowd)]
    with open(os.path.join(d, 'secession_s%d.tsv' % sd), encoding='utf-8') as fh:
        h = fh.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in fh:
            x = line.rstrip('\n').split('\t')
            if x[ix['nation']] != nat: continue
            Q[(int(x[ix['quarter']]), int(x[ix['year']]))].append((x[ix['region']], fl(x[ix['militancyMeanIndex']]), fl(x[ix['crowdK']])))
    dec = collections.defaultdict(lambda: [0, 0, 0.0, 0, 0.0, 0.0])
    for (q, y), lst in Q.items():
        best = max(lst, key=lambda t: (t[1] * t[2]) if math.isfinite(t[1] * t[2]) else -1)
        dd = dec[y // 10 * 10]
        me = [t for t in lst if t[0] == reg]
        if me: dd[2] += me[0][1]; dd[3] += 1; dd[4] += me[0][2]
        if best[0] == reg:
            dd[0] += 1
            if best[1] > 0.5: dd[1] += 1; dd[5] += 0.002 * best[2]
    W = collections.defaultdict(float); B = collections.defaultdict(float)
    with open(os.path.join(d, 'pop_s%d.tsv' % sd), encoding='utf-8') as fh:
        h = fh.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in fh:
            x = line.rstrip('\n').split('\t')
            if x[0] == 'N' and x[ix['nation']] == nat: W[int(x[ix['year']]) // 10 * 10] += fl(x[ix['warDeadK']])
    with open(os.path.join(d, 'casualty_s%d.tsv' % sd), encoding='utf-8') as fh:
        h = fh.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in fh:
            x = line.rstrip('\n').split('\t')
            if x[ix['nation']] == nat: B[int(x[ix['year']]) // 10 * 10] += fl(x[ix['deadK']])
    for y in sorted(set(dec) | set(W)):
        dd = dec.get(y, [0, 0, 0.0, 0, 0.0, 0.0])
        o.write(f'{lab}\t{sd}\t{nat}\t{reg}\t{y}\t{dd[0]}\t{dd[1]}\t{(dd[2] / dd[3]) if dd[3] else float("nan"):.3f}\t{(dd[4] / dd[3]) if dd[3] else float("nan"):.4g}\t{dd[5]:.4g}\t{W.get(y, 0) - B.get(y, 0):.4g}\t{B.get(y, 0):.4g}\t{W.get(y, 0):.4g}\n')
o.close(); print('書いた', out)
