#!/usr/bin/env python
# (328) 流れG ① の静的な表：同じ財に並ぶ別レシピの 生産性÷基準レシピの生産性（比）と、gate=1 のときの自己費用の前後。
# 読むだけ（ソースを字面で読む。シムは回さない）。出所の独立：比は goods.json の productivity と Simulation.cs の字面から作る
# （C# の実装 Simulation.ApplyRecipeCostFollowsProductivity の計算をここで写していない＝番人の期待値とは別の出所）。
# 使い方：python ratio_table.py <repo の根> > 表.tsv
import json, re, sys
root = sys.argv[1] if len(sys.argv) > 1 else '.'
sim = open(root + '/PriceOfBread.SimCore/Simulation.cs', encoding='utf-8-sig').read()
cat = open(root + '/PriceOfBread.SimCore/Catalog.cs', encoding='utf-8-sig').read()
coef = open(root + '/PriceOfBread.SimCore/Coefficients.cs', encoding='utf-8-sig').read()
goods = json.load(open(root + '/PriceOfBread.SimCore/goods.json', encoding='utf-8-sig'))
byid = {g['id']: g for g in goods}
ids = {m.group(1): int(m.group(2)) for m in re.finditer(r'public const int (\w+)Id = (\d+);', cat)}
FCPS = float(re.search(r'FullChainProductivityScale = ([0-9.]+);', sim).group(1))
FACTORY_VA = float(re.search(r'FactoryVa = ([0-9.]+);', cat).group(1))
def cdef(name):
    m = re.search(r'public double ' + name + r' = ([0-9.]+);', coef)
    return float(m.group(1))
rows = []
seen = set()
for m in re.finditer(r'new Producer\s*\{', sim):
    blk = sim[m.end(): m.end() + 900]
    head = blk[:130]   # GoodId と RecipeName は同じ new Producer の頭（次の Producer へはみ出さない）
    g = re.search(r'GoodId = (?:Catalog\.(\w+)Id|gid)', head)
    rn = re.search(r'RecipeName = "([^"]+)"', head)
    if not rn or not g or not g.group(1): continue
    name = rn.group(1)
    if name in ('職人',) : pass
    pe = re.search(r'Productivity = ([^;]*?),\s*(?:Output|Scale|MarginalCost)', blk, re.S)
    mc = re.search(r'MarginalCost = ([^,]+),', blk)
    if not pe or not mc: continue
    gid = ids.get(g.group(1))
    key = (gid, name)
    if key in seen: continue
    seen.add(key)
    e = pe.group(1).strip()
    e = e.replace('FullChainProductivityScale', repr(FCPS)).replace('LaborProductivity', '1.0')
    e = re.sub(r'coef\.(\w+)', lambda mm: repr(cdef(mm.group(1))), e)
    base = byid[gid]['productivity'] * FCPS
    e = re.sub(r'\w+\.Productivity', repr(base), e)   # 〈既定のレシピ×係数〉の形（leblanc／rifleDefault ほか）＝基準はその財の既定のレシピ
    try:
        prod = eval(e)
    except Exception as ex:
        print('FAIL', name, repr(e), file=sys.stderr); raise
    mcs = mc.group(1).strip()
    mcv = FACTORY_VA if 'FactoryVa' in mcs else float(mcs)
    rows.append((byid[gid]['name'], name, prod, base, prod / base, mcv, mcv / (prod / base), byid[gid]['tier']))
# 職人（AddArtisanProducers）：生産性＝その財の既定×0.6、自己費用 0.35
for gname in ('ClothingId', 'FurnitureId', 'LeatherGoodsId', 'PorcelainId'):
    gid = ids[gname[:-2]]
    base = byid[gid]['productivity'] * FCPS
    rows.append((byid[gid]['name'], '職人', base * 0.6, base, 0.6, 0.35, 0.35 / 0.6, byid[gid]['tier']))
print('\t'.join(['財', 'レシピ', '生産性', '基準レシピの生産性', '比', '自己費用(播種)', '自己費用(gate=1)', '財のtier', '基準レシピの有無']))
for r in sorted(rows, key=lambda x: (x[0], x[1])):
    # 基準レシピ（RecipeName=""）が構築で足されるのは役務でない財（CatalogEconomy）と、労働力（AddServiceProducers の人力）だけ
    has_base = (r[7] != 'Service') or r[0] == '労働力'
    print('\t'.join([r[0], r[1], '%.4g' % r[2], '%.4g' % r[3], '%.4f' % r[4], '%.4f' % r[5], ('%.4f' % r[6]) if has_base else '-（基準なし＝割らない）', r[7], '有' if has_base else '無']))
