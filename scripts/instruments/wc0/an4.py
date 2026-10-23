import sys, glob, csv, math, io, collections
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')
d = sys.argv[1]
def num(x):
    try: return float(x)
    except: return float('nan')
pops = []; byN = collections.Counter(); byG = collections.Counter(); regs = collections.defaultdict(lambda: [0, 0.0, 0.0, 1e18])
tot_rows = 0
for f in sorted(glob.glob(d + '/wcapreg_s*.tsv')):
    with open(f, encoding='utf-8') as fh:
        for x in csv.DictReader(fh, delimiter='\t'):
            if x['group'] not in ('labor','farmlabor','advlabor'): continue
            c90 = num(x['c90Months'])
            if c90 <= 0: continue
            p = num(x['popAvg']); pops.append(p)
            byN[x['nation']] += int(c90); byG[x['group']] += int(c90)
            r = regs[(x['nation'], x['region'])]; r[0] += int(c90); r[1] += p * c90; r[2] = max(r[2], p); r[3] = min(r[3], p)
pops.sort()
print("labor-group c90 rows:", len(pops), " popAvg quantiles: p10 %.0f p50 %.0f p90 %.0f max %.0f" % (pops[len(pops)//10], pops[len(pops)//2], pops[len(pops)*9//10], pops[-1]))
print("by nation (c90 months):", dict(byN.most_common(12)))
print("by group:", dict(byG))
print("regions:", len(regs))
for k, r in sorted(regs.items(), key=lambda t: -t[1][0])[:12]:
    print(k, "c90mo=%d avgPop=%.0f maxPop=%.0f minPop=%.0f" % (r[0], r[1]/r[0], r[2], r[3]))
