# (1283) c132fg 史実の桁の線（線が一番動かす量）と記録の量を 1 枚に（読むだけ）
#   使い方: python magn_fg.py <腕の ins（D:/…）> <基準の ins（D:/…）> <種の起点> <基準の種の起点>
#   ① 国ごとの都市化率＝1−Σ農村の人口÷Σ州の人口（region_s・1850 年と 1913 年の第 1 四半期・種の中央値）
#      史実（Bairoch 1988 の 5 千人以上の町の割合と各国の国勢調査の都市の割合の間の値〈推〉）と比べる＝比が 10 倍以上か 1/10 以下なら線に当たる
#   ② 世界の飢えの死（popclose_s の deathsFamineK の全期間の和・種の中央値）＝史実の 1783〜1950 年の飢饉の死 約 1.5 億人〈推〉（Ó Gráda 2009 の 1870 年以後 約 1 億＋それ以前）
#   ③ 記録：urxpd_s の famineGated（州×月）・都市への流れ sumFlowPos・農村への流れ sumFlowNeg（計器の 9 国・全期間の和・種の中央値）
import sys, csv, glob, os, statistics as st, collections
HIST = {  # 国: (1850, 1913) 〈推〉
    'イギリス': (0.40, 0.70), 'フランス': (0.19, 0.38), 'プロイセン': (0.15, 0.49), 'アメリカ': (0.15, 0.46),
    '日本': (0.10, 0.15), '清': (0.06, 0.06), 'ロシア': (0.07, 0.14), 'オスマン': (0.15, 0.17), 'オーストリア': (0.07, 0.23),
}
FAMINE_HIST_K = 150000.0
Q = {1850: (1850 - 1783) * 4, 1913: (1913 - 1783) * 4}

def seeds(d, sb):
    return [s for s in range(sb, sb + 10) if os.path.exists(os.path.join(d, f'region_s{s}.tsv'))]

def read(d, sb):
    urb = collections.defaultdict(list); fam = []; px = collections.defaultdict(list)
    for s in seeds(d, sb):
        acc = collections.defaultdict(lambda: [0.0, 0.0])
        with open(os.path.join(d, f'region_s{s}.tsv'), encoding='utf-8') as fh:
            for r in csv.DictReader(fh, delimiter='\t'):
                q = int(r['quarter'])
                for y, qq in Q.items():
                    if q == qq and r['nation'] in HIST:
                        a = acc[(r['nation'], y)]; a[0] += float(r['populationK']); a[1] += float(r['ruralPopulationK'])
        for k, (p, rr) in acc.items():
            if p > 0: urb[k].append(1 - rr / p)
        p = os.path.join(d, f'popclose_s{s}.tsv')
        if os.path.exists(p):
            t = 0.0
            with open(p, encoding='utf-8') as fh:
                for r in csv.DictReader(fh, delimiter='\t'): t += float(r['deathsFamineK'])
            fam.append(t)
        p = os.path.join(d, f'urxpd_s{s}.tsv')
        if os.path.exists(p):
            g = fp = fn = 0.0
            with open(p, encoding='utf-8') as fh:
                for r in csv.DictReader(fh, delimiter='\t'):
                    g += int(r['famineGated']); fp += float(r['sumFlowPos']); fn += float(r['sumFlowNeg'])
            px['famineGated'].append(g); px['flowPosK'].append(fp); px['flowNegK'].append(fn)
    return urb, fam, px

A, B = sys.argv[1], sys.argv[2]; sa, sbb = int(sys.argv[3]), int(sys.argv[4])
ua, fa, pa = read(A, sa); ub, fb, pb = read(B, sbb)
hit = 0
print('① 都市化率（種の中央値）  国\t年\t腕\t基準\t腕÷基準\t史実〈推〉\t腕÷史実\t線')
for n, (h50, h13) in HIST.items():
    for y, h in ((1850, h50), (1913, h13)):
        a = st.median(ua[(n, y)]) if ua.get((n, y)) else float('nan')
        b = st.median(ub[(n, y)]) if ub.get((n, y)) else float('nan')
        ra = a / h if h > 0 else float('nan')
        bad = (ra >= 10 or ra <= 0.1) if a == a else False
        hit += bad
        print(f'{n}\t{y}\t{a:.4f}\t{b:.4f}\t{(a / b if b > 0 else float("nan")):.3f}\t{h:.2f}\t{ra:.3f}\t{"当たる" if bad else "-"}')
fma = st.median(fa) if fa else float('nan'); fmb = st.median(fb) if fb else float('nan')
r2 = fma / FAMINE_HIST_K if fma == fma else float('nan')
bad2 = (r2 >= 10 or r2 <= 0.1) if r2 == r2 else False
hit += bad2
print(f'② 世界の飢えの死 千人（全期間・種の中央値）\t腕 {fma:.0f}\t基準 {fmb:.0f}\t腕÷基準 {(fma / fmb if fmb > 0 else float("nan")):.3f}\t史実〈推〉 {FAMINE_HIST_K:.0f}\t腕÷史実 {r2:.3f}\t{"当たる" if bad2 else "-"}\t種 {len(fa)}')
for k in ('famineGated', 'flowPosK', 'flowNegK'):
    a = st.median(pa[k]) if pa.get(k) else float('nan'); b = st.median(pb[k]) if pb.get(k) else float('nan')
    print(f'③ 記録 {k}（計器の 9 国・全期間の和・種の中央値）\t腕 {a:.1f}\t基準 {b:.1f}\t腕÷基準 {(a / b if b > 0 else float("nan")):.3f}')
print(f'史実の桁の線に当たった数 {hit}')
