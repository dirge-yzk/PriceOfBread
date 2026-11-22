# (1299) 問い4：world.json の本国の州（provinces）と開幕の植民地（initialColonies）で、文化・宗教の札が空の州を数える。
# 植民地の札は places.json（ColonyLabelsFromData 1）から取る。places.json に同じ名の札があるかも並べる（データで埋められるかの材料）。
# 使い方: python -X utf8 -I q4_world.py <SimCore のフォルダ> <出力>
import sys, json, os
src, out = sys.argv[1], sys.argv[2]
w = json.load(open(os.path.join(src, 'world.json'), encoding='utf-8'))
pl = json.load(open(os.path.join(src, 'places.json'), encoding='utf-8'))
places = {}
if isinstance(pl, dict):
    items = pl.get('places', pl)
    if isinstance(items, dict): items = [dict(name=k, **v) if isinstance(v, dict) else dict(name=k) for k, v in items.items()]
else: items = pl
for p in items:
    if isinstance(p, dict) and 'name' in p: places[p['name']] = p
lines = []; cntP = cntPR = 0; cntC = cntCR = 0; nProv = nCol = 0
natNoCol = []
for n in w['nations']:
    provs = n.get('provinces', []); cols = n.get('initialColonies', [])
    emptyP = [p for p in provs if not p.get('culture')]
    emptyPR = [p for p in provs if not p.get('religion')]
    nProv += len(provs); nCol += len(cols)
    for p in provs:
        c, r = p.get('culture', ''), p.get('religion', '')
        if not c or not r:
            name = p.get('city') or p.get('name')
            pp = places.get(name, {})
            lines.append(f"本国\t{n['name']}\t{name}\tculture={c!r}\treligion={r!r}\tpopK={p.get('popK', '')}\tplaces.culture={pp.get('culture')!r}\tplaces.religion={pp.get('religion')!r}\tplaces.kind={pp.get('kind')!r}\t国の州の数={len(provs)}\t札の空の州={len(emptyP)}\t開幕の植民地={len(cols)}")
            cntP += (not c); cntPR += (not r)
    for col in cols:
        name = col.get('name'); pp = places.get(name, {})
        if not pp.get('culture') or not pp.get('religion'):
            lines.append(f"植民地\t{n['name']}\t{name}\tplaces.culture={pp.get('culture')!r}\tplaces.religion={pp.get('religion')!r}\tplaces.kind={pp.get('kind')!r}")
            cntC += (not pp.get('culture')); cntCR += (not pp.get('religion'))
    if not cols: natNoCol.append(n['name'])
head = [f'本国の州 {nProv}・文化の札が空 {cntP}・宗教の札が空 {cntPR}',
        f'開幕の植民地 {nCol}・places.json の文化の札が空 {cntC}・宗教の札が空 {cntCR}',
        f'開幕に植民地を持たない国 {len(natNoCol)} / {len(w["nations"])}']
with open(out, 'w', encoding='utf-8', newline='\n') as f:
    f.write('\n'.join(head) + '\n\n' + '\n'.join(lines) + '\n')
print('\n'.join(head))
