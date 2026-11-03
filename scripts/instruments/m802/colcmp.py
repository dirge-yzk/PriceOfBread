# (802) 列を名指しで落として表を突き合わせる（読むだけ）。python -I colcmp.py <腕 ins> <比べる側 ins> <表の頭> <腕から落とす列,...|-> <比べる側から落とす列,...|->
#   (797) portchcmp.py の一般化。合わせた木 M（score_s の旧 3 列なし・cpeace_s の repMovedIndex あり）と c11fl の腕 B（その逆）を突き合わせる。
import os, sys, glob
sys.stdout.reconfigure(encoding='utf-8')
a, b, pre = sys.argv[1], sys.argv[2], sys.argv[3]
da = [x.encode() for x in sys.argv[4].split(',') if x and x != '-']; db = [x.encode() for x in sys.argv[5].split(',') if x and x != '-']
def drop(L, cols):
    h = L[0].split(b'\t'); idx = {h.index(c) for c in cols if c in h}
    if len(idx) != len(cols): print('  ⚠落とす列が見出しに無い', [c.decode() for c in cols if c not in h])
    return [b'\t'.join(c for k, c in enumerate(l.split(b'\t')) if k not in idx) if l else l for l in L]
ok = bad = 0
for fa in sorted(glob.glob(os.path.join(a, pre + '*.tsv'))):
    f = os.path.basename(fa); fb = os.path.join(b, f)
    if not os.path.exists(fb): bad += 1; print('比べる側に無い', f); continue
    A = drop(open(fa, 'rb').read().split(b'\n'), da); B = drop(open(fb, 'rb').read().split(b'\n'), db)
    if A == B: ok += 1
    else: bad += 1; print('違う', f)
print(f'{pre}：腕から {sys.argv[4]}・比べる側から {sys.argv[5]} を落として一致 {ok}・違う {bad}')
