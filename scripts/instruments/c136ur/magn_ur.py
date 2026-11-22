# (1312) c136ur 史実の桁の線（c134f1/magn_f1.py の写し＋①b 植民地格の農業の外の割合＝この線が一番動かす量）（読むだけ）
#   使い方: python -X utf8 -I magn_ur.py <腕の ins（D:/…）> <基準の ins（D:/…）> <種の起点> <基準の種の起点>
#   ① 国ごとの都市化率＝1−Σ農村の人口÷Σ州の人口（region_s・1850 年と 1913 年の第 1 四半期・種の中央値）
#      門＝〈農業の外の人口の割合〉の史実〈推〉と比べる（比が 10 倍以上か 1/10 以下なら線に当たる）・記録＝〈町に住む人の割合〉の史実〈推〉との比
#      （§0-2 ★(1294) ①＝作者 2026-11-20「全て推奨で」で推奨を採った：門は農業の外・町は記録）
#   ② 世界の飢えの死（popclose_s の deathsFamineK の全期間の和・種の中央値）＝史実 約 1.5 億人〈推〉（門）
#   ③ 記録：州の間の移民の出÷人口（region_s・‰・四半期あたり・年代別・全ての国）・urxpd_s の famineGated・sumFlowPos・sumFlowNeg と
#      (1296) の列 famineBackK・famineRegainK・famineRegainRegionMonths・oppSignRegionMonths（列が無い生は nan）
import sys, csv, os, statistics as st, collections
TOWN = {  # 国: (1850, 1913) 町に住む人の割合〈推〉（c132fg/magn_fg.py と同じ値）
    'イギリス': (0.40, 0.70), 'フランス': (0.19, 0.38), 'プロイセン': (0.15, 0.49), 'アメリカ': (0.15, 0.46),
    '日本': (0.10, 0.15), '清': (0.06, 0.06), 'ロシア': (0.07, 0.14), 'オスマン': (0.15, 0.17), 'オーストリア': (0.07, 0.23),
}
NONAG = {  # 国: (1850, 1913) 農業の外の人口の割合〈推〉（材料 E-1 の列・Mitchell ほか）
    'イギリス': (0.78, 0.91), 'フランス': (0.48, 0.59), 'プロイセン': (0.44, 0.65), 'アメリカ': (0.45, 0.69),
    '日本': (0.20, 0.40), '清': (0.20, 0.22), 'ロシア': (0.12, 0.25), 'オスマン': (0.20, 0.22), 'オーストリア': (0.28, 0.42),
}
FAMINE_HIST_K = 150000.0
COL_NONAG = {1850: 0.10, 1913: 0.10}   # 植民地格の農業の外の人口の割合〈推〉（下書き §5：史実 0.05〜0.2 の対数の中ほど）
Q = {1850: (1850 - 1783) * 4, 1913: (1913 - 1783) * 4}
WIN = [(1783, 1799), (1800, 1849), (1850, 1913), (1914, 1950)]
PDK = ('famineGated', 'sumFlowPos', 'sumFlowNeg', 'famineBackK', 'famineRegainK', 'famineRegainRegionMonths', 'oppSignRegionMonths', 'regionMonths')

def seeds(d, sb):
    return [s for s in range(sb, sb + 10) if os.path.exists(os.path.join(d, f'region_s{s}.tsv'))]

def read(d, sb):
    urb = collections.defaultdict(list); fam = []; px = collections.defaultdict(list); mig = collections.defaultdict(list)
    for s in seeds(d, sb):
        acc = collections.defaultdict(lambda: [0.0, 0.0]); m = collections.defaultdict(lambda: [0.0, 0.0])
        with open(os.path.join(d, f'region_s{s}.tsv'), encoding='utf-8') as fh:
            for r in csv.DictReader(fh, delimiter='\t'):
                q = int(r['quarter']); y = int(r['year'])
                for yy, qq in Q.items():
                    if q == qq and r['nation'] in TOWN:
                        a = acc[(r['nation'], yy)]; a[0] += float(r['populationK']); a[1] += float(r['ruralPopulationK'])
                    if q == qq and r.get('tier') == 'Colonial':
                        a = acc[('_col', yy)]; a[0] += float(r['populationK']); a[1] += float(r['ruralPopulationK'])
                w = next((f'{a0}-{b0}' for a0, b0 in WIN if a0 <= y <= b0), None)
                if w: mm = m[w]; mm[0] += float(r['migrationOutKPerQuarter']); mm[1] += float(r['populationK'])
        for k, (p, rr) in acc.items():
            if p > 0: urb[k].append(1 - rr / p)
        for w, (o, p) in m.items():
            if p > 0: mig[w].append(1000.0 * o / p)
        p = os.path.join(d, f'popclose_s{s}.tsv')
        if os.path.exists(p):
            t = 0.0
            with open(p, encoding='utf-8') as fh:
                for r in csv.DictReader(fh, delimiter='\t'): t += float(r['deathsFamineK'])
            fam.append(t)
        p = os.path.join(d, f'urxpd_s{s}.tsv')
        if os.path.exists(p):
            v = collections.defaultdict(float); has = set()
            with open(p, encoding='utf-8') as fh:
                for r in csv.DictReader(fh, delimiter='\t'):
                    for k in PDK:
                        if k in r and r[k] not in (None, ''): v[k] += float(r[k]); has.add(k)
            for k in PDK: px[k].append(v[k] if k in has else float('nan'))
    return urb, fam, px, mig

def med(x):
    x = [v for v in x if v == v]
    return st.median(x) if x else float('nan')

A, B = sys.argv[1], sys.argv[2]; sa, sbb = int(sys.argv[3]), int(sys.argv[4])
ua, fa, pa, ma = read(A, sa); ub, fb, pb, mb = read(B, sbb)
hit = 0
print('① 都市化率（種の中央値）  国\t年\t腕\t基準\t腕÷基準\t史実 農業の外〈推〉\t腕÷農業の外（門）\t線\t史実 町〈推〉\t腕÷町（記録）\t基準÷町（記録）')
for n in TOWN:
    for i, y in enumerate((1850, 1913)):
        a = med(ua.get((n, y), [])); b = med(ub.get((n, y), []))
        hn = NONAG[n][i]; ht = TOWN[n][i]
        ra = a / hn if hn > 0 else float('nan')
        bad = (ra >= 10 or ra <= 0.1) if a == a else False
        hit += bad
        print(f'{n}\t{y}\t{a:.4f}\t{b:.4f}\t{(a / b if b > 0 else float("nan")):.3f}\t{hn:.2f}\t{ra:.3f}\t{"当たる" if bad else "-"}\t{ht:.2f}\t{a / ht:.3f}\t{b / ht:.3f}')
for y in (1850, 1913):
    a = med(ua.get(('_col', y), [])); b = med(ub.get(('_col', y), [])); hn = COL_NONAG[y]
    ra = a / hn if a == a else float('nan')
    bad = (ra >= 10 or ra <= 0.1) if a == a else False
    hit += bad
    print(f'①b 植民地格（全ての国の和）	{y}	腕 {a:.4f}	基準 {b:.4f}	腕÷基準 {(a / b if b > 0 else float("nan")):.3f}	史実〈推〉 {hn:.2f}	腕÷史実（門） {ra:.3f}	{"当たる" if bad else "-"}')
fma = med(fa); fmb = med(fb)
r2 = fma / FAMINE_HIST_K if fma == fma else float('nan')
bad2 = (r2 >= 10 or r2 <= 0.1) if r2 == r2 else False
hit += bad2
print(f'② 世界の飢えの死 千人（全期間・種の中央値）\t腕 {fma:.0f}\t基準 {fmb:.0f}\t腕÷基準 {(fma / fmb if fmb > 0 else float("nan")):.3f}\t史実〈推〉 {FAMINE_HIST_K:.0f}\t腕÷史実 {r2:.3f}\t{"当たる" if bad2 else "-"}\t種 {len(fa)}')
for w0, w1 in WIN:
    w = f'{w0}-{w1}'; a = med(ma.get(w, [])); b = med(mb.get(w, []))
    print(f'③ 記録 州の間の移民の出 ‰／四半期 {w}\t腕 {a:.4f}\t基準 {b:.4f}\t腕÷基準 {(a / b if b > 0 else float("nan")):.3f}')
for k in PDK:
    a = med(pa.get(k, [])); b = med(pb.get(k, []))
    print(f'③ 記録 urxpd_s {k}（計器の 9 国・全期間の和・種の中央値）\t腕 {a:.1f}\t基準 {b:.1f}\t腕÷基準 {(a / b if b > 0 else float("nan")):.3f}')
print(f'史実の桁の線に当たった数 {hit}')
