"""(311) 線 n5 の記録の列（毎回同じ・読むだけ・採否の理由にしない）。事前登録 measurements/n5_20261014_prereg.txt の [5]。
python an_n5_rec.py 腕名=<ins> [腕名=<ins>…]   （英を POB_INSTRUMENTS_NATIONS に挙げた走行の nation_s・fiscal_s）
出す物（英）：兌換停止のある種の数〔fiscal_s の onGold 1→0・monthIndex 窓〕（Q1〜Q40・Q80・Q160・Q240・全期）／不履行のある種の数〔nation_s の sovereignDefaultCumulativeCount＞0〕（同）／
  払えない月の数（shortfallMonths）と満額借りても足りない月の数（payRateBelowOneMonths）の Q1〜Q40 の和の種の中央〔最小〜最大〕／歳入÷年GDP の Q5〜Q40 の平均の種の中央／Q40 末の債務÷年GDP。
限界の自己申告：shortfallMonths・payRateBelowOneMonths の定義は Instruments.cs の列名どおりに読む（月数）。窓の和→種の中央。NaN は数えない。Q80＝q79 まで、Q160＝q159 まで、Q240＝q239 まで。"""
import sys, os, glob, re, statistics, collections
ARMS = collections.OrderedDict(a.split('=', 1) for a in sys.argv[1:])
def fl(x):
    try: return float(x)
    except Exception: return float('nan')
def cell(xs, nd=3):
    ys = [x for x in xs if x == x]
    return '—' if not ys else f'{statistics.median(ys):.{nd}f} ({min(ys):.{nd}f}〜{max(ys):.{nd}f})'
def table(D, kind):
    out = {}
    for p in glob.glob(os.path.join(D, f'{kind}_s*.tsv')):
        sd = int(re.search(rf'{kind}_s(\d+)\.tsv$', p).group(1))
        with open(p, encoding='utf-8') as fh:
            h = fh.readline().rstrip('\n').split('\t'); ix = {x: i for i, x in enumerate(h)}
            out[sd] = [dict((k, r[i]) for k, i in ix.items()) for r in (ln.rstrip('\n').split('\t') for ln in fh)]
    return out
WINQ = [('Q1〜Q40', 39), ('Q80', 79), ('Q160', 159), ('Q240', 239), ('全期', 10 ** 6)]
for arm, D in ARMS.items():
    nat = table(D, 'nation'); fis = table(D, 'fiscal')
    seeds = sorted(set(nat) & set(fis))
    print(f'######## {arm}（{D}・種 {len(seeds)}）')
    sus = {w: 0 for w, _ in WINQ}; dfl = {w: 0 for w, _ in WINQ}
    for sd in seeds:
        uk = [r for r in nat[sd] if r['nation'] == 'イギリス']
        f = [r for r in fis[sd] if r['nation'] == 'イギリス'] if fis[sd] and 'nation' in fis[sd][0] else fis[sd]
        for w, qmax in WINQ:
            prev = None; hit = False
            for r in f:
                og = fl(r['onGold']); mi = int(r['monthIndex'])
                if prev == 1 and og == 0 and mi < 3 * (qmax + 1): hit = True; break
                prev = og
            sus[w] += 1 if hit else 0
            last = [fl(r['sovereignDefaultCumulativeCount']) for r in uk if int(r['quarter']) <= qmax]
            dfl[w] += 1 if last and last[-1] == last[-1] and last[-1] > 0 else 0
    print('英の兌換停止のある種の数\t' + '\t'.join(f'{w} {sus[w]}/{len(seeds)}' for w, _ in WINQ))
    print('英の不履行のある種の数\t' + '\t'.join(f'{w} {dfl[w]}/{len(seeds)}' for w, _ in WINQ))
    sc, pb, rv, dbt = [], [], [], []
    for sd in seeds:
        uk = [r for r in nat[sd] if r['nation'] == 'イギリス']
        q40 = [r for r in uk if int(r['quarter']) <= 39]
        sc.append(sum(fl(r['shortfallMonths']) for r in q40 if fl(r['shortfallMonths']) == fl(r['shortfallMonths'])))
        pb.append(sum(fl(r['payRateBelowOneMonths']) for r in q40 if fl(r['payRateBelowOneMonths']) == fl(r['payRateBelowOneMonths'])))
        g = [(fl(r['revenueBaseMeanPerMonth']) * 12 / fl(r['nominalGdpPerYear'])) for r in uk if 4 <= int(r['quarter']) <= 39 and fl(r['nominalGdpPerYear']) > 0]
        rv.append(sum(g) / len(g) if g else float('nan'))
        e = [r for r in uk if int(r['quarter']) == 39]
        dbt.append(fl(e[0]['govDebtStock']) / fl(e[0]['nominalGdpPerYear']) if e and fl(e[0]['nominalGdpPerYear']) > 0 else float('nan'))
    print('英の払えない月（shortfallMonths の Q1〜Q40 の和）\t' + cell(sc, 0))
    print('英の満額借りても足りない月（payRateBelowOneMonths の Q1〜Q40 の和）\t' + cell(pb, 0))
    print('英の歳入÷年GDP（Q5〜Q40 の四半期の平均）\t' + cell(rv, 4))
    print('英の Q40 末の債務÷年GDP\t' + cell(dbt, 3))
    print()
