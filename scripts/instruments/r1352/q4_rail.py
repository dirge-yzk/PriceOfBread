# (1352) 州 X の鉄道の線路（trrail.railTrackEnd）と必要（needEnd）を腕×種で年ごとに並べる
import sys, csv, os, glob
sys.stdout.reconfigure(encoding='utf-8')
reg = sys.argv[1]; yrs = [int(y) for y in sys.argv[2].split(',')]
print('arm\tseed\t' + '\t'.join('trk%d' % y for y in yrs) + '\tbought_sum\tdecay_sum\twar_sum')
for a in sys.argv[3:]:
    name, d = a.split('=', 1)
    for p in sorted(glob.glob(os.path.join(d, 'trrail_s*.tsv')), key=lambda x: int(os.path.basename(x).split('_s')[-1].split('.')[0])):
        sd = os.path.basename(p).split('_s')[-1].split('.')[0]
        trk = {}; b = dc = wl = 0.0
        with open(p, encoding='utf-8') as f:
            for r in csv.DictReader(f, delimiter='\t'):
                if r['region'] != reg: continue
                y = int(r['year'])
                if y in yrs: trk[y] = float(r['railTrackEnd'])
                b += float(r['boughtCapPerYear'] or 0); dc += float(r['decayLossPerYear'] or 0); wl += float(r['warLossPerYear'] or 0)
        print('\t'.join([name, sd] + [('%.0f' % trk[y]) if y in trk else '-' for y in yrs] + ['%.0f' % b, '%.0f' % dc, '%.0f' % wl]))
