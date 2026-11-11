# (1015) c94rh 案 B の向きの紙：財ごとに、world.json で賦存>1 の州（本国と植民地と抽象の標的・Sites の行を含む）と 1783 の持ち主の一覧。
# 使い方: python -X utf8 -I rh_suppliers.py <world.json> <goods.json> <Catalog.cs> <財,財,...> <出力のファイル>
import sys, re, json
WJ, GJ, CAT, GL, OUT = sys.argv[1:6]
w = json.load(open(WJ, encoding='utf-8-sig')); goods = json.load(open(GJ, encoding='utf-8-sig'))
gid = {g['name']: g['id'] for g in goods}
src = open(CAT, encoding='utf-8-sig').read()
IDS = {m.group(1): int(m.group(2)) for m in re.finditer(r'public const int (\w+Id)\s*=\s*(\d+);', src)}
blk = src[src.index('public static readonly SiteRow[] Sites'):]; blk = blk[:blk.index('];')]
SITES = [(m.group(2), IDS[m.group(3)], m.group(4)) for m in re.finditer(r'new\("([^"]+)",\s*"([^"]+)",\s*(\w+Id),\s*(\d+)', blk)]
places = []
for n in w['nations']:
    for p in n['provinces']: places.append((p['city'], set(p['resources']), n['name'], 'P'))
    for c in n.get('initialColonies', []): places.append((c['name'], set(c['resources']), n['name'], 'C'))
for a in w.get('abstractTargets', []):
    for c in a.get('colonies', []): places.append((c['name'], set(c['resources']), a['name'], 'A'))
o = open(OUT, 'w', encoding='utf-8', newline='\n')
o.write(f'# rh_suppliers world={WJ}（P＝本国の州・C＝植民地・A＝抽象の標的・S＝Catalog.Sites の行〔年代〕）\ngood\tcount\tplaces(州@1783の持ち主/種類)\n')
for g in GL.split(','):
    i = gid[g]
    lst = [f'{pl}@{nn}/{k}' for pl, rs, nn, k in places if i in rs]
    lst += [f'{pl}/S{d}' for pl, gg, d in SITES if gg == i and not any(pl == x[0] and i in x[1] for x in places)]
    o.write(f'{g}\t{len(lst)}\t' + ' '.join(lst) + '\n')
o.close(); print('ok', OUT)
