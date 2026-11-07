# (947) 合流：合わせた木の腕の生（ins）と線の腕の生をファイルごとにバイトで比べ、成績表を名乗り行（SOURCE-HASH）以外で比べる（読むだけ）。
#   python -X utf8 -I cmp947.py <腕の ins> <線の ins> <腕の成績表> <線の成績表>
import sys, os, hashlib
a, b, ba, bb = sys.argv[1:5]

def h(p):
    m = hashlib.sha256()
    with open(p, 'rb') as f:
        for c in iter(lambda: f.read(1 << 22), b''):
            m.update(c)
    return m.hexdigest()

fa = set(os.listdir(a)); fb = set(os.listdir(b))
print('files', len(fa), len(fb), 'onlyA', sorted(fa - fb)[:10], 'onlyB', sorted(fb - fa)[:10])
diff = [f for f in sorted(fa & fb) if os.path.getsize(os.path.join(a, f)) != os.path.getsize(os.path.join(b, f)) or h(os.path.join(a, f)) != h(os.path.join(b, f))]
print('common', len(fa & fb), 'differ', len(diff), diff[:20])

def rows(p):
    return [l for l in open(p, 'rb').read().replace(b'\r\n', b'\n').split(b'\n') if not l.startswith(b'SOURCE-HASH')]
ra, rb = rows(ba), rows(bb)
nd = sum(1 for x, y in zip(ra, rb) if x != y) + abs(len(ra) - len(rb))
print('band lines', len(ra), len(rb), 'differ', nd)
for x, y in zip(ra, rb):
    if x != y:
        print(' A:', x.decode('utf-8', 'replace')[:160]); print(' B:', y.decode('utf-8', 'replace')[:160]); break
