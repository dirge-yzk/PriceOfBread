# (1285) 問い3 の紙の台本：案〈格の順＝本国格の札の人口が無い国だけ植民地格で数える〉を今の生に当てる（同じ盤面・エンジンを呼ばない）。
# 数える：空の札の国×四半期（今の規則 new／案 fb）・主流の文化≠首都の札（H1）・主流が今の規則と違う国×四半期
# 使い方: python -X utf8 -I q3_paper.py <生> <種の起点> <種の数> <world.json> <colony_labels.tsv>
import sys, csv, json, collections
raw, sb, ns, wpath, lpath = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4], sys.argv[5]
csv.field_size_limit(10**8)
w = json.load(open(wpath, encoding='utf-8-sig'))
prov = {p['city']: (p.get('culture') or '', p.get('religion') or '') for n in w['nations'] for p in n['provinces']}
lab = {r['name']: (r['culture'], r['religion']) for r in csv.DictReader(open(lpath, encoding='utf-8'), delimiter='\t')}
def labof(reg): return prov[reg] if reg in prov else lab.get(reg, ('', ''))
def best(t):
    b, bv = '', 0.0
    for k, v in t.items():
        if v > bv: bv, b = v, k
    return b
o = collections.Counter()
for s in range(sb, sb + ns):
    byq = collections.defaultdict(dict)
    with open(f"{raw}/region_s{s}.tsv", encoding='utf-8') as f:
        rd = csv.reader(f, delimiter='\t'); h = next(rd); ix = {k: i for i, k in enumerate(h)}
        for c in rd:
            if len(c) < len(h) or c[ix['activeFlag']] != '1': continue
            byq[int(c[ix['quarter']])].setdefault(c[ix['nation']], []).append((c[ix['region']], c[ix['tier']], float(c[ix['populationK']])))
    for q in byq:
        for nat, regs in byq[q].items():
            for j in (0, 1):  # 0 文化・1 宗教
                ch, cc = {}, {}
                for (reg, t, pk) in regs:
                    if not (pk > 0): continue
                    v = labof(reg)[j]
                    if not v: continue
                    d = cc if t == 'Colonial' else ch
                    d[v] = d.get(v, 0) + pk
                new = best(ch); fb = new if ch else best(cc)
                k = 'cult' if j == 0 else 'rel'
                o[f'{k}_nq'] += 1
                if new == '': o[f'{k}_empty_new'] += 1
                if fb == '': o[f'{k}_empty_fb'] += 1
                if fb != new: o[f'{k}_changed'] += 1
                if j == 0:
                    capc = labof(regs[0][0])[0]
                    if new != capc: o['H1_new'] += 1
                    if fb != capc: o['H1_fb'] += 1
print(f"{raw} 種 {sb}..{sb+ns-1}: " + ' '.join(f"{k}={v}" for k, v in sorted(o.items())))
