#!/usr/bin/env python3
# (764) 計器の生の 2 つのフォルダを表ごとにバイトで突き合わせる（読むだけ）。python -I rawcmp.py <腕> <比べる側> [除く表の頭,...]
# 既知の揺れ＝burscan_s の seconds・clock_s の kind=C の行（ident.cfg の KNOWN と同じ）＝その列・行を除いて比べる。
import os, sys
sys.stdout.reconfigure(encoding='utf-8')
a, b = sys.argv[1], sys.argv[2]; skip = tuple(x for x in (sys.argv[3].split(',') if len(sys.argv) > 3 else []) if x)
fa = {f for f in os.listdir(a) if f.endswith('.tsv')}; fb = {f for f in os.listdir(b) if f.endswith('.tsv')}
def norm(p, f):
    L = open(p, 'rb').read().split(b'\n')
    if f.startswith('burscan_s'):
        h = L[0].split(b'\t'); i = h.index(b'seconds') if b'seconds' in h else -1
        return [b'\t'.join(c for k, c in enumerate(l.split(b'\t')) if k != i) for l in L]
    if f.startswith('clock_s'):
        h = L[0].split(b'\t'); k = h.index(b'kind') if b'kind' in h else -1
        return [l for l in L if not (k >= 0 and len(l.split(b'\t')) > k and l.split(b'\t')[k] == b'C')]
    return L
same = known = 0; diff = []
for f in sorted(fa & fb):
    if f.startswith(skip): continue
    ra, rb = open(os.path.join(a, f), 'rb').read(), open(os.path.join(b, f), 'rb').read()
    if ra == rb: same += 1
    elif norm(os.path.join(a, f), f) == norm(os.path.join(b, f), f): known += 1
    else: diff.append(f)
onlya = sorted(f for f in fa - fb if not f.startswith(skip)); onlyb = sorted(f for f in fb - fa if not f.startswith(skip))
skipped = sorted(f for f in fb | fa if f.startswith(skip))
print(f'バイト一致 {same}・既知の揺れ {known}・違う {len(diff)}・腕だけ {len(onlya)}・比べる側だけ {len(onlyb)}・除いた表 {len(skipped)}（{",".join(skip)}）')
for f in diff[:30]: print('  違う', f)
for f in onlya[:30]: print('  腕だけ', f)
for f in onlyb[:30]: print('  比べる側だけ', f)
print('判定:', '一致' if not diff and not onlya and not onlyb else '不一致')
