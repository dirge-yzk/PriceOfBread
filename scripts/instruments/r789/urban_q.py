# r789: 州の都市の人口（人口−農村）と総人口の〈ピーク以後の最小÷ピーク〉の分布（腕ごと・種ごとの p10・中央値）。都市のピーク≧150 千人の州。
import sys, glob, os, collections, statistics
sys.stdout.reconfigure(encoding='utf-8')
root, arms = sys.argv[1], sys.argv[2].split(',')
PK = 150
for arm in arms:
    allu, allt, perseed = [], [], []
    for f in sorted(glob.glob(f'{root}/{arm}/region_s*.tsv')):
        ser = collections.defaultdict(list)
        with open(f, encoding='utf-8') as fh:
            h = fh.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
            for l in fh:
                c = l.rstrip('\n').split('\t')
                if int(c[0]) % 4: continue
                p = float(c[ix['populationK']]); ser[c[ix['region']]].append((p - float(c[ix['ruralPopulationK']]), p))
        us = []
        for reg, s in ser.items():
            iu = max(range(len(s)), key=lambda i: s[i][0])
            if s[iu][0] < PK: continue
            ru = min(x[0] for x in s[iu:]) / s[iu][0]
            it = max(range(len(s)), key=lambda i: s[i][1])
            rt = min(x[1] for x in s[it:]) / s[it][1]
            us.append(ru); allu.append(ru); allt.append(rt)
        us.sort(); perseed.append((us[len(us)//10], statistics.median(us), sum(1 for x in us if x < 0.25)))
    allu.sort(); allt.sort()
    print(f'{arm}: 州×種 {len(allu)}・都市 p10 {allu[len(allu)//10]:.3f} 中央値 {statistics.median(allu):.3f}・総人口 p10 {allt[len(allt)//10]:.3f} 中央値 {statistics.median(allt):.3f}・種ごとの都市<1/4 の件数 {[x[2] for x in perseed]}')
