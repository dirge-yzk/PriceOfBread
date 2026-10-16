import sys, glob, csv, collections

def load(d):
    rows = []
    for f in sorted(glob.glob(d + '/scalars_s*.tsv')):
        with open(f, encoding='utf-8') as fh:
            r = csv.DictReader(fh, delimiter='\t')
            for x in r:
                rows.append(x)
    return rows

def fl(x, k):
    try:
        return float(x[k])
    except Exception:
        return float('nan')

tag = sys.argv[1]
rows = load(sys.argv[2])
print(tag, 'rows', len(rows))
if not rows:
    sys.exit(0)
print('flags', {k: sorted(set(x[k] for x in rows)) for k in ('flagD', 'flagC', 'flagW')})
print('nations', sorted(set(x['nation'] for x in rows)))
n = len(rows)
c = collections.OrderedDict()
c['decay>0.05'] = sum(1 for x in rows if fl(x, 'decay') > 0.05)
c['decayCeiling>0'] = sum(1 for x in rows if fl(x, 'decayCeiling') > 0)
c['atWar&warExh>0.05'] = sum(1 for x in rows if x['atWar'] == '1' and fl(x, 'warExhaustion') > 0.05)
c['atWar&warExh>0.05&milWarMean>0'] = sum(1 for x in rows if x['atWar'] == '1' and fl(x, 'warExhaustion') > 0.05 and fl(x, 'milWarMean') > 0)
c['milWarMean>0'] = sum(1 for x in rows if fl(x, 'milWarMean') > 0)
c['dmWar<0'] = sum(1 for x in rows if fl(x, 'dmWar') < 0)
c['dmGraft<0'] = sum(1 for x in rows if fl(x, 'dmGraft') < 0)
c['corrErosion>0'] = sum(1 for x in rows if fl(x, 'corrErosion') > 0)
c['effCorruption>0'] = sum(1 for x in rows if fl(x, 'effCorruption') > 0)
for k, v in c.items():
    print(f'  {k}: {v} / {n}')
by = collections.defaultdict(list)
for x in rows:
    by[x['nation']].append(x)
print('nation  rows  decay(first,max)  ceiling(max)  effCorr(mean)  dynMandate(first,last)  legit(mean)  warExh(max)  milWarMean(max)  dmGraft(min)')
for nm, xs in by.items():
    d = [fl(x, 'decay') for x in xs]
    print(nm, len(xs), round(d[0], 3), round(max(d), 3), round(max(fl(x, 'decayCeiling') for x in xs), 3),
          round(sum(fl(x, 'effCorruption') for x in xs) / len(xs), 4),
          round(fl(xs[0], 'dynMandate'), 3), round(fl(xs[-1], 'dynMandate'), 3),
          round(sum(fl(x, 'legitimacy') for x in xs) / len(xs), 3),
          round(max(fl(x, 'warExhaustion') for x in xs), 3), round(max(fl(x, 'milWarMean') for x in xs), 4),
          round(min(fl(x, 'dmGraft') for x in xs), 4))
# polity transitions
print('polity transitions (nation: [(year,code)...])')
for nm in ('フランス', 'ロシア', 'オーストリア', 'プロイセン', 'オスマン', '清', '日本', 'イギリス'):
    xs = by.get(nm, [])
    xs = sorted(xs, key=lambda x: int(x['quarter']))
    tr = []
    prev = None
    for x in xs:
        if prev is not None and x['polityCode'] != prev:
            tr.append((x['year'], prev + '>' + x['polityCode']))
        prev = x['polityCode']
    print(' ', nm, tr[:8])
