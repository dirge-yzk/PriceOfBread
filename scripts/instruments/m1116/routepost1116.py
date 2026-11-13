# (1116) 合流：合わせた木で、直した実物の ident_check._cearn_remap を使い、基準の生の経路の名（cearn_s・cpop_s）が全て写せるかを数える。
#   python -I -X utf8 routepost1116.py <リポジトリ> <基準のコミット> <測る木> <基準の ins>
import sys, os, re, csv, collections, importlib.util
repo, base, tree, raw = sys.argv[1:5]
csv.field_size_limit(1 << 30)
sp = importlib.util.spec_from_file_location('ic', os.path.join(tree, 'scripts', 'instruments', 'ident', 'ident_check.py'))
ic = importlib.util.module_from_spec(sp); sp.loader.exec_module(ic)
ic.CEARN.update(root=repo, base=base, tree=tree, arm_commit=None, maps={}, paths=None)
RE = re.compile(r'^([A-Za-z0-9_]+\.cs):(\d+)(.*)$', re.S)
names = collections.Counter()
for f in sorted(os.listdir(raw)):
    cols = ic.CEARN_ROUTE_COLS if f.startswith('cearn_s') else ic.CPOP_ROUTE_COLS if f.startswith('cpop_s') else None
    if not cols: continue
    with open(os.path.join(raw, f), encoding='utf-8', newline='') as fh:
        rd = csv.reader(fh, delimiter='\t'); h = next(rd); ix = [i for i, c in enumerate(h) if c in cols]
        for row in rd:
            for i in ix:
                if i < len(row):
                    mo = RE.match(row[i])
                    if mo: names[f'{mo.group(1)}:{mo.group(2)}'] += 1
st = collections.Counter(); bad = []
for n, c in sorted(names.items()):
    r = ic._cearn_remap(n + ':x')
    k = 'unmapped' if r.endswith('?') else ('same' if r == n + ':x' else 'moved')
    st[k] += c
    if k == 'unmapped': bad.append((n, c))
print('distinct', len(names), dict(st)); print('unmapped distinct', len(bad))
for b in bad: print('UNMAPPED', *b)
