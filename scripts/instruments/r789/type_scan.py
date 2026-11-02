# r789: keep_784 の 40 種から〈州の人口がピークの 1/4 未満・所得の崩壊が人口の減りより先〉の型を探す
import sys, glob, os, collections
sys.stdout.reconfigure(encoding='utf-8')
root = sys.argv[1]; thr = float(sys.argv[2]) if len(sys.argv) > 2 else 0.25
for arm in ['base_ins', 'n1', 'n2', 'a11']:
    for f in sorted(glob.glob(f'{root}/{arm}/region_s*.tsv')):
        sd = os.path.basename(f)[8:-4]
        ser = collections.defaultdict(list)
        with open(f, encoding='utf-8') as fh:
            h = fh.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
            for l in fh:
                c = l.rstrip('\n').split('\t')
                if c[ix['activeFlag']] != '1': pass
                q = int(c[0]); 
                if q % 4: continue
                pop = float(c[ix['populationK']]); rur = float(c[ix['ruralPopulationK']])
                ern = float(c[ix['earnedPerMonth']])
                ser[c[ix['region']]].append((q, int(c[1]), pop, rur, ern, c[ix['nation']], c[ix['tier']]))
        for reg, s in ser.items():
            pk = max(s, key=lambda r: r[2])
            if pk[2] < 300: continue
            after = [r for r in s if r[0] >= pk[0]]
            mn = min(after, key=lambda r: r[2])
            if mn[2] / pk[2] >= thr: continue
            # income per capita peak near pop peak (within 10y before), then year when ipc falls below 1/2 of it
            ipc = [(r[1], r[4] / r[2] if r[2] > 0 else 0) for r in s]
            pre = [v for y, v in ipc if pk[1] - 10 <= y <= pk[1]]
            ref = max(pre) if pre else 0
            yhalf = next((y for y, v in ipc if y > pk[1] - 10 and v < ref / 2), None)
            y90 = next((r[1] for r in after if r[2] < 0.9 * pk[2]), None)
            print(f'{arm}\ts{sd}\t{reg}\t{pk[5]}/{pk[6]}\tpk {pk[2]:.0f}@{pk[1]}\turb {pk[2]-pk[3]:.0f}\tmin {mn[2]/pk[2]:.3f}@{mn[1]}\tipcHalf@{yhalf}\tpop-10%@{y90}\towner_end {s[-1][5]}')
