import sys, glob, csv, math, io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')
picked = "清,ロシア,オスマン,フランス,イギリス,プロイセン,オーストリア,日本,アメリカ".split(',')
def num(x):
    try: return float(x)
    except: return float('nan')
def load(d, seeds):
    rows = []
    for f in sorted(glob.glob(d + '/wcap_s*.tsv')):
        s = f.split('_s')[-1].split('.')[0]
        if s not in seeds: continue
        with open(f, encoding='utf-8') as fh:
            for x in csv.DictReader(fh, delimiter='\t'): rows.append(x)
    return rows
seeds = set(sys.argv[3].split(','))
A = load(sys.argv[1], seeds); B = load(sys.argv[2], seeds)
def era(x): return (int(x['year']) - 1780) // 30 * 30 + 1780
def mean(xs, f):
    v = [f(x) for x in xs]; v = [t for t in v if not math.isnan(t)]
    return sum(v)/len(v) if v else float('nan')
lnr = lambda x: num(x['sMeanCeilSide']) * math.log(num(x['ceilMult']))   # mean over live region-months of max(0,ln(P/wrp))
print("rows base", len(A), "abl", len(B))
print("group era | base: meanPosLnRatio c70 meanFlow maxRatio | wc30: meanPosLnRatio c70(>=10.8x) c90(>=21x) meanFlow maxRatio")
for g in ['labor','advlabor','farmlabor','education','healthcare','highered','other']:
    for e in range(1780, 1960, 30):
        a = [x for x in A if x['group']==g and era(x)==e and x['nation'] in picked]
        b = [x for x in B if x['group']==g and era(x)==e and x['nation'] in picked]
        if not a or not b: continue
        print("%s %d | %.3f %.3f %.3f %.2f | %.3f %.3f %.3f %.3f %.2f" % (g, e, mean(a,lnr), mean(a,lambda x:num(x['c70Share'])), mean(a,lambda x:num(x['meanFlow'])), max(num(x['maxRatio']) for x in a),
            mean(b,lnr), mean(b,lambda x:num(x['c70Share'])), mean(b,lambda x:num(x['c90Share'])), mean(b,lambda x:num(x['meanFlow'])), max(num(x['maxRatio']) for x in b)))
