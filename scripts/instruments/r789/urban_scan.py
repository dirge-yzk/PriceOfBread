# r789: 州の都市の人口（人口−農村）のピーク以後の最小÷ピーク。割譲型（ピークから最小までに持ち主が替わった・または開幕の持ち主と違う）とそれ以外（チャールストン型）に分ける。
# 引数：<keep のフォルダ> <腕,...> [都市のピークの下限 千人=150] [比の閾値=0.25]
import sys, glob, os, collections
sys.stdout.reconfigure(encoding='utf-8')
root, arms = sys.argv[1], sys.argv[2].split(',')
PK = float(sys.argv[3]) if len(sys.argv) > 3 else 150; TH = float(sys.argv[4]) if len(sys.argv) > 4 else 0.25
for arm in arms:
    cnt = collections.Counter(); lst = []
    files = sorted(glob.glob(f'{root}/{arm}/region_s*.tsv'))
    for f in files:
        sd = os.path.basename(f)[8:-4]
        ser = collections.defaultdict(list)
        with open(f, encoding='utf-8') as fh:
            h = fh.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
            for l in fh:
                c = l.rstrip('\n').split('\t')
                q = int(c[0])
                if q % 4: continue
                pop = float(c[ix['populationK']]); u = pop - float(c[ix['ruralPopulationK']])
                ser[c[ix['region']]].append((int(c[1]), u, c[ix['nation']], c[ix['tier']]))
        for reg, s in ser.items():
            pk = max(s, key=lambda r: r[1])
            if pk[1] < PK: continue
            after = [r for r in s if r[0] >= pk[0]]
            mn = min(after, key=lambda r: r[1]); ratio = mn[1] / pk[1]
            owners = {r[2] for r in s if r[0] <= mn[0]}
            ceded = len(owners) > 1 or s[0][3] in ('Integrated', 'Colonial')
            if ratio < TH:
                cnt['ceded' if ceded else 'noCede'] += 1
                lst.append((ratio, sd, reg, 'ceded' if ceded else 'noCede', pk[0], round(pk[1]), mn[0]))
    lst.sort()
    print(f'{arm}: 種 {len(files)}・都市のピーク≧{PK:.0f} 千人で最小÷ピーク<{TH}：割譲型 {cnt["ceded"]}・それ以外 {cnt["noCede"]}・最小の比 {lst[0][0] if lst else float("nan"):.3f}')
    for x in lst[:12]: print('   ', f'{x[0]:.3f}', 's' + x[1], x[2], x[3], f'pk {x[5]}@{x[4]} min@{x[6]}')
