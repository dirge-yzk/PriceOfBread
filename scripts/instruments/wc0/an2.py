import sys, glob, csv, collections, math, io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')
d = sys.argv[1]; seeds = set(sys.argv[2].split(',')) if len(sys.argv) > 2 else None
picked = "清,ロシア,オスマン,フランス,イギリス,プロイセン,オーストリア,日本,アメリカ".split(',')
def num(x):
    try: return float(x)
    except: return float('nan')
rows = []
for f in sorted(glob.glob(d + '/wcap_s*.tsv')):
    s = f.split('_s')[-1].split('.')[0]
    if seeds and s not in seeds: continue
    with open(f, encoding='utf-8') as fh:
        for x in csv.DictReader(fh, delimiter='\t'): rows.append(x)
print("rows", len(rows))
def era(x):
    y = int(x['year']); return (y - 1780) // 30 * 30 + 1780
def mean(xs, c):
    v = [num(x[c]) for x in xs if not math.isnan(num(x[c]))]
    return sum(v)/len(v) if v else float('nan')
G = ['labor','advlabor','farmlabor','education','healthcare','highered','other']
for label, sel in (("picked9", lambda x: x['nation'] in picked), ("all-nations-yearend", lambda x: x['nation'] not in picked)):
    print("==", label, " (mean over nation-quarter rows, 10 seeds)")
    print("group\tera\tn\tc70\tc90\tc95\tover\twantUp90\tc70Pop\tf70\tf90\tmeanFlow\tdampC70\tmaxS\tmaxRatio")
    for g in G:
        for e in range(1780, 1960, 30):
            xs = [x for x in rows if x['group'] == g and era(x) == e and sel(x)]
            if not xs: continue
            print("%s\t%d\t%d\t%.4f\t%.4f\t%.4f\t%.4f\t%.4f\t%.4f\t%.4f\t%.4f\t%.3f\t%.3f\t%.3f\t%.2f" % (g, e, len(xs), mean(xs,'c70Share'), mean(xs,'c90Share'), mean(xs,'c95Share'), mean(xs,'overShare'), mean(xs,'wantUp90Share'), mean(xs,'c70PopShare'), mean(xs,'f70Share'), mean(xs,'f90Share'), mean(xs,'meanFlow'), mean(xs,'dampMeanC70'), max(num(x['maxS']) for x in xs), max(num(x['maxRatio']) for x in xs)))
# per nation, groups edu/health/other, c70 by era
print("== per-nation c70Share / meanFlow for education,healthcare,other (picked) by era")
for n in picked:
    for g in ('education','healthcare','other'):
        out = []
        for e in range(1780, 1960, 30):
            xs = [x for x in rows if x['nation'] == n and x['group'] == g and era(x) == e]
            out.append("%.2f/%.2f" % (mean(xs,'c70Share'), mean(xs,'meanFlow')) if xs else "-")
        print(n, g, " ".join(out))
# UK 1913
print("== イギリス 1913 (all seeds): group c70 c90 meanFlow maxS maxRatio ceilMult")
for g in G:
    xs = [x for x in rows if x['nation'] == 'イギリス' and x['group'] == g and x['year'] == '1913']
    if xs: print(g, len(xs), "%.3f %.4f %.3f %.3f %.3f %.2f" % (mean(xs,'c70Share'), mean(xs,'c90Share'), mean(xs,'meanFlow'), max(num(x['maxS']) for x in xs), max(num(x['maxRatio']) for x in xs), mean(xs,'ceilMult')))
# regions
reg = []
for f in sorted(glob.glob(d + '/wcapreg_s*.tsv')):
    s = f.split('_s')[-1].split('.')[0]
    if seeds and s not in seeds: continue
    with open(f, encoding='utf-8') as fh:
        for x in csv.DictReader(fh, delimiter='\t'): reg.append(x)
print("regrows", len(reg))
agg = collections.defaultdict(lambda: [0,0,0.0,0])
for x in reg:
    k = (x['group'], x['nation'], x['region'])
    a = agg[k]; a[0] += int(num(x['c70Months'])); a[1] += int(num(x['c90Months'])); a[2] = max(a[2], num(x['maxS'])); a[3] += 1
print("== top regions by c90 months (labor groups only), seeds pooled")
for k, a in sorted(((k, a) for k, a in agg.items() if k[0] in ('labor','advlabor','farmlabor')), key=lambda t: -t[1][1])[:15]:
    print(k, "c70mo=%d c90mo=%d maxS=%.3f qrows=%d" % tuple(a))
print("== top regions by c90 months (all groups), seeds pooled")
for k, a in sorted(agg.items(), key=lambda t: -t[1][1])[:15]:
    print(k, "c70mo=%d c90mo=%d maxS=%.3f qrows=%d" % tuple(a))
# events with s>=0.9 total by group
tot = collections.Counter(); 
for x in reg: tot[x['group']] += int(num(x['c90Months']))
print("c90 region-months by group:", dict(tot))
tot70 = collections.Counter()
for x in reg: tot70[x['group']] += int(num(x['c70Months']))
print("c70 region-months by group:", dict(tot70))
# c90 months by year bucket for labor groups
yb = collections.Counter()
for x in reg:
    if x['group'] in ('labor','advlabor','farmlabor'): yb[int(x['year'])//10*10] += int(num(x['c90Months']))
print("labor-groups c90 region-months by decade:", dict(sorted(yb.items())))
