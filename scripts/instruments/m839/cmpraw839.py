# (839) 2 つの計器つきの生のフォルダの表をバイトで比べる（読むだけ・(832) の cmpraw の型）。
#   python -X utf8 -I cmpraw839.py <新の ins> <旧の ins> [外す表の頭,...]
#   出力：両方にある表の数・同じ・違う（違う表の名前）・新にだけ・旧にだけ。外した頭の表は数えない（呼び手が別の読み手で比べる）。
import sys, os, hashlib
new, old = sys.argv[1], sys.argv[2]
skip = tuple(x for x in (sys.argv[3].split(',') if len(sys.argv) > 3 else []) if x)
def h(p):
    d = hashlib.sha256()
    with open(p, 'rb') as f:
        for c in iter(lambda: f.read(1 << 20), b''): d.update(c)
    return d.hexdigest()
A = {n for n in os.listdir(new) if n.endswith('.tsv') and not n.startswith(skip)}
B = {n for n in os.listdir(old) if n.endswith('.tsv') and not n.startswith(skip)}
both = sorted(A & B); same = []; diff = []
for n in both:
    pa, pb = os.path.join(new, n), os.path.join(old, n)
    (same if os.path.getsize(pa) == os.path.getsize(pb) and h(pa) == h(pb) else diff).append(n)
print(f'両方にある表 {len(both)}（外した頭 {",".join(skip) or "なし"}）・同じ {len(same)}・違う {len(diff)}・新にだけ {len(A - B)}・旧にだけ {len(B - A)}')
for n in diff: print('  違う', n)
for n in sorted(A - B): print('  新にだけ', n)
for n in sorted(B - A): print('  旧にだけ', n)
