# (938) 腕ごとの〈植民地格・産出 0・人口 100 千人以上〉の州×四半期の数と、そこからの建国の数。読むだけ。
import csv, glob, os, sys
sys.stdout.reconfigure(encoding='utf-8')
def num(s):
    try: return float(s)
    except ValueError: return float('nan')
for arg in sys.argv[1:]:
    arm, d = arg.split('=', 1)
    zq = 0; zreg = 0; colq = 0
    for f in glob.glob(os.path.join(d, 'region_s*.tsv')):
        with open(f, encoding='utf-8', newline='') as fh:
            r = csv.reader(fh, delimiter='\t'); h = next(r); ix = {c: i for i, c in enumerate(h)}
            seen = set()
            for x in r:
                if x[ix['tier']] != 'Colonial' or not (num(x[ix['populationK']]) >= 100): continue
                colq += 1
                o = num(x[ix['factoryOutputRefValuePerMonth']]) + num(x[ix['extractionOutputRefValuePerMonth']])
                if not (o > 0):
                    zq += 1; seen.add(x[ix['region']])
            zreg += len(seen)
    print(f'{arm}\t植民地格の州×四半期(人口≧100千) {colq}\t産出0 {zq}\t産出0になった州(種ごとの和) {zreg}', flush=True)
