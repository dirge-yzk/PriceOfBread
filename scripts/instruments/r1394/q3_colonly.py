# (1394) rv：本国格の州を持たない国（core 0・integrated 0）の国×四半期を数え、名目 GDP の 1 四半期の比の最大と 1 人あたりの最小を出す（読むだけ）
# 使い方: python q3_colonly.py <名札> <RAW> [種...]
import sys, os, glob, re, collections
tag, raw = sys.argv[1], sys.argv[2]
def fl(x):
    try: return float(x)
    except: return float('nan')
seeds = [int(s) for s in sys.argv[3:]] or sorted(int(re.search(r'_s(\d+)\.tsv', p).group(1)) for p in glob.glob(os.path.join(raw, 'nation_s*.tsv')))
tot = 0; cnt = collections.Counter(); out = []
for s in seeds:
    p = os.path.join(raw, f'nation_s{s}.tsv')
    with open(p, encoding='utf-8') as fh:
        h = fh.readline().rstrip('\n').split('\t')
        ser = collections.defaultdict(list)
        for ln in fh:
            v = dict(zip(h, ln.rstrip('\n').split('\t')))
            ser[v['nation']].append(v)
    for nat, rs in ser.items():
        first = int(rs[0]['quarter'])
        prev = None; nq = 0; mx = (0, None); mn = (float('inf'), None)
        for v in rs:
            q = int(v['quarter']); pop = fl(v['totalPopulationK']); g = fl(v['nominalGdpPerYear'])
            col_only = fl(v['coreRegions']) == 0 and fl(v['integratedRegions']) == 0 and fl(v['colonialRegions']) > 0
            if col_only and pop >= 100:
                nq += 1
                if pop > 0 and g / pop < mn[0]: mn = (g / pop, v['year'])
                if prev is not None and prev > 0 and g / prev > mx[0]: mx = (g / prev, v['year'], q - first)
            prev = g
        if nq:
            cnt['nations'] += 1; tot += nq
            out.append((s, nat, nq, first, round(mx[0], 1), mx[1:], round(mn[0], 4), mn[1]))
print(f'{tag}\t種 {len(seeds)}\t植民地格だけの国（人口 100 千以上）の国 {cnt["nations"]}\t国×四半期 {tot}')
for o in sorted(out, key=lambda t: -t[4])[:12]:
    print('   種 %s %s 四半期 %d 最初の行 q%d 1 四半期の比の最大 %s %s 1 人あたりの最小 %s（%s 年）' % o)
