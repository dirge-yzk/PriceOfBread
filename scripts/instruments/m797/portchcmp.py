# (797) portch_s の突き合わせ（読むだけ）：比べる側（列 portNew あり）から portNew を除いた表が、腕（列なし）とバイト一致するか。python -I portchcmp.py <腕 ins> <比べる側 ins>
import os, sys, glob
sys.stdout.reconfigure(encoding='utf-8')
a, b = sys.argv[1], sys.argv[2]; ok = bad = 0
for fa in sorted(glob.glob(os.path.join(a, 'portch_s*.tsv'))):
    f = os.path.basename(fa); A = open(fa, 'rb').read().split(b'\n'); B = open(os.path.join(b, f), 'rb').read().split(b'\n')
    h = B[0].split(b'\t'); i = h.index(b'portNew') if b'portNew' in h else -1
    if i >= 0: B = [b'\t'.join(c for k, c in enumerate(l.split(b'\t')) if k != i) if l else l for l in B]
    if A == B: ok += 1
    else: bad += 1; print('違う', f)
print(f'portch_s：比べる側から portNew を除いて一致 {ok}・違う {bad}')
