# (1103) 2 つの生（ins）の共通の表をバイトで比べる（読むだけ・アブレーションの腕と線の腕 B の刈った生）。
# 使い方: python -X utf8 -I cmpraw1103.py <ins A> <ins B>
import sys, os, hashlib
a, b = sys.argv[1], sys.argv[2]
fa, fb = set(os.listdir(a)), set(os.listdir(b))
com = sorted(fa & fb); bad = []
def h(p):
    x = hashlib.sha1()
    with open(p, 'rb') as f:
        for c in iter(lambda: f.read(1 << 22), b''): x.update(c)
    return x.hexdigest()
for f in com:
    if os.path.getsize(os.path.join(a, f)) != os.path.getsize(os.path.join(b, f)) or h(os.path.join(a, f)) != h(os.path.join(b, f)): bad.append(f)
print(f'共通の表 {len(com)}・バイト一致 {len(com) - len(bad)}・違う {len(bad)}・A だけ {len(fa - fb)}・B だけ {len(fb - fa)}')
for f in bad[:20]: print('  違う', f)
print('A だけ（頭）', sorted(fa - fb)[:12])
