# (848) cearn_s の経路の 3 列（outTop1Route・outTop2Route・inTop1Route）だけを、基準の木の行番号を測る木へ difflib で写して比べる（読むだけ・列が末尾に増えた腕でも読む）。
#   python -I -X utf8 routecheck848.py <基準の ins> <腕の ins> <リポジトリ> <基準のコミット> <腕のコミット> [種の数=10]
import sys, os, difflib, subprocess
from collections import Counter
base, arm, repo, c0, c1 = sys.argv[1:6]; ns = int(sys.argv[6]) if len(sys.argv) > 6 else 10
files = subprocess.run(['git', '-C', repo, 'diff', '--name-only', c0, c1, '--', '*.cs'], capture_output=True, text=True, encoding='utf-8').stdout.split()
maps = {}
for f in files:
    if not f.startswith('PriceOfBread.SimCore/'): continue
    a = subprocess.run(['git', '-C', repo, 'show', f'{c0}:{f}'], capture_output=True, text=True, encoding='utf-8').stdout.split('\n')
    b = subprocess.run(['git', '-C', repo, 'show', f'{c1}:{f}'], capture_output=True, text=True, encoding='utf-8').stdout.split('\n')
    m = {}
    for blk in difflib.SequenceMatcher(None, a, b, autojunk=False).get_matching_blocks():
        for k in range(blk.size): m[blk.a + k + 1] = blk.b + k + 1
    maps[os.path.basename(f)] = m
def remap(v):
    p = v.split(':')
    if len(p) != 4 or p[0] not in maps: return v
    n = maps[p[0]].get(int(p[1]))
    return v if n is None else f'{p[0]}:{n}:{p[2]}:{p[3]}'
cols = ['outTop1Route', 'outTop2Route', 'inTop1Route']
bad = Counter(); ex = []; rows = 0; other = 0
for s in range(1, ns + 1):
    fa, fb = os.path.join(base, f'cearn_s{s}.tsv'), os.path.join(arm, f'cearn_s{s}.tsv')
    with open(fa, encoding='utf-8') as A, open(fb, encoding='utf-8') as B:
        ha = A.readline().rstrip('\n').split('\t'); hb = B.readline().rstrip('\n').split('\t')
        ia = [ha.index(c) for c in cols]; ib = [hb.index(c) for c in cols]; na = len(ha)
        for la, lb in zip(A, B):
            rows += 1
            xa = la.rstrip('\n').split('\t'); xb = lb.rstrip('\n').split('\t')
            for c, i, j in zip(cols, ia, ib):
                if remap(xa[i]) != xb[j]:
                    bad[(c, xa[i], xb[j])] += 1
                    if len(ex) < 8: ex.append((s, xa[2], xa[3], xa[0], c, xa[i], remap(xa[i]), xb[j]))
            ka = [v for k, v in enumerate(xa) if k not in ia]; kb = [v for k, v in enumerate(xb[:na]) if k not in ib]
            if ka != kb: other += 1
print(f'写した .cs：{sorted(maps)}・行 {rows}・経路の列の違い {sum(bad.values())}・経路の外の列（共通の列）の違い {other}')
for k, v in bad.most_common(12): print(f'  {v}\t{k}')
for e in ex: print('  例', e)
print('判定：' + ('一致（経路の列は写して一致・共通の列は一致）' if not bad and not other else '違いあり'))
