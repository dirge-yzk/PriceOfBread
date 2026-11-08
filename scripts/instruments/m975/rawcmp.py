# (975) 2 つの計器の生のフォルダを表ごとにバイトで比べる（読むだけ）。python -I rawcmp.py <A> <B>
import os, sys, hashlib, collections
A, B = sys.argv[1], sys.argv[2]
def h(p):
    m = hashlib.sha256()
    with open(p, 'rb') as f:
        for c in iter(lambda: f.read(1 << 20), b''): m.update(c)
    return m.hexdigest()
fa = set(os.listdir(A)); fb = set(os.listdir(B))
same = 0; diff = collections.Counter(); only = collections.Counter()
for f in sorted(fa & fb):
    if h(os.path.join(A, f)) == h(os.path.join(B, f)): same += 1
    else: diff[f.split('_s')[0]] += 1
for f in fa - fb: only['A:' + f.split('_s')[0]] += 1
for f in fb - fa: only['B:' + f.split('_s')[0]] += 1
print(f'A={A}\nB={B}\n共通 {len(fa & fb)} 本・バイト一致 {same}・違い {sum(diff.values())}')
for k, v in sorted(diff.items()): print('  違い', k, v)
for k, v in sorted(only.items()): print('  片方だけ', k, v)
