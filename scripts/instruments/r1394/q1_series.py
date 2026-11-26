# (1394) rv：一国の四半期の並びを腕どうしで並べる（読むだけ・列は列名で引く）
# 使い方: python q1_series.py <RAW_A> <RAW_B> <seed> <国名> <出力.tsv>
import sys, os, collections
A, B, seed, NAT, out = sys.argv[1], sys.argv[2], int(sys.argv[3]), sys.argv[4], sys.argv[5]
def fl(x):
    try: return float(x)
    except: return float('nan')
def rows(raw, t):
    p = os.path.join(raw, f'{t}_s{seed}.tsv')
    if not os.path.exists(p): return
    with open(p, encoding='utf-8') as fh:
        h = fh.readline().rstrip('\n').split('\t')
        for ln in fh: yield dict(zip(h, ln.rstrip('\n').split('\t')))
NCOL = ['totalPopulationK','populationK','soldierK','nominalGdpPerYear','priceLevel','atWarMonths','civilWarMonths','coreRegions','integratedRegions','colonialRegions','nonCoreRegions','polityCode','govDebtStock','adminSpendableAtBudgetPerMonth','revenueBaseMeanPerMonth','payRateMeanShare']
SCOL = ['totalMoneyStock','treasuryStock','gdpRealPerYear']
def load(raw):
    d = collections.OrderedDict()
    for v in rows(raw, 'nation'):
        if v['nation'] != NAT: continue
        d[int(v['quarter'])] = {c: v.get(c, '') for c in NCOL} | {'year': v['year']}
    st = {}
    for v in rows(raw, 'stock'):
        if v['nation'] != NAT: continue
        st[v['time']] = v
    # stock の time は四半期の番号か月か不明＝year 列と並べるため最後の行を年で持つ
    sty = {}
    for v in st.values(): sty.setdefault(v['year'], []).append(v)
    nreg = collections.Counter()
    for v in rows(raw, 'region'):
        if v['nation'] == NAT and v.get('activeFlag', '1') not in ('0',): nreg[int(v['quarter'])] += 1
    return d, sty, nreg
da, sa, ra = load(A); db, sb, rb = load(B)
qs = sorted(set(da) | set(db))
with open(out, 'w', encoding='utf-8', newline='\n') as fo:
    fo.write('q\tyear\t' + '\t'.join(f'{c}_A\t{c}_B' for c in NCOL + ['nreg']) + '\n')
    for q in qs:
        a = da.get(q, {}); b = db.get(q, {})
        fo.write(f"{q}\t{a.get('year', b.get('year',''))}\t" + '\t'.join(f"{a.get(c,'')}\t{b.get(c,'')}" for c in NCOL) + f"\t{ra.get(q,'')}\t{rb.get(q,'')}\n")
print('rows', len(qs), 'A', len(da), 'B', len(db))
