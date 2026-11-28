# (1436) c154ha：腕 B と腕 B2（計器だけ直した木）の成績表と計器の表が全行バイト一致かを数える（名乗り行 SOURCE-HASH を除く）。
#   使い方: python same_arm.py <成績表 B> <成績表 B2> <ins B> <ins B2>
import sys, os, hashlib
cb, cb2, ib, ib2 = sys.argv[1:5]
def card(p): return [l for l in open(p, 'rb').read().split(b'\n') if b'SOURCE-HASH' not in l]
a, b = card(cb), card(cb2)
diff = sum(1 for x, y in zip(a, b) if x != y) + abs(len(a) - len(b))
print(f'成績表：行 {len(a)}・{len(b)}・違う行 {diff}')
def h(p):
    m = hashlib.sha256()
    with open(p, 'rb') as f:
        for ch in iter(lambda: f.read(1 << 20), b''): m.update(ch)
    return m.hexdigest()
fa = sorted(os.listdir(ib)); fb = sorted(os.listdir(ib2))
only = sorted(set(fa) ^ set(fb)); same = 0; differ = []
for n in sorted(set(fa) & set(fb)):
    if h(os.path.join(ib, n)) == h(os.path.join(ib2, n)): same += 1
    else: differ.append(n)
print(f'計器の表：B {len(fa)}・B2 {len(fb)}・片方だけ {len(only)}・バイト一致 {same}・違う {len(differ)}')
for n in differ[:20]: print('  違う', n)
for n in only[:20]: print('  片方だけ', n)
sys.exit(0 if diff == 0 and not differ and not only else 1)
