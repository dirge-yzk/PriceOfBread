# (832) 列が末尾に増えた表（silverwt_s ほか）を、古い生の列（見出しの名前）だけで新しい生と行ごとに比べる（読むだけ）。
#   python -X utf8 -I cmpprefix826.py <新しい生の ins> <古い生の ins> <表の名前の頭,...> [種の起点] [種の数]
#   出力：表×種ごとに〈行の数・古い列の全てが文字列で一致した行の数〉と、最初に違う行。古い見出しの列が新しい見出しに無ければ〈列が無い〉。
import sys, os
new, old, stems = sys.argv[1], sys.argv[2], sys.argv[3].split(',')
s0 = int(sys.argv[4]) if len(sys.argv) > 4 else 1
n = int(sys.argv[5]) if len(sys.argv) > 5 else 10
bad = 0
for stem in stems:
    for s in range(s0, s0 + n):
        pa, pb = os.path.join(new, f'{stem}{s}.tsv'), os.path.join(old, f'{stem}{s}.tsv')
        if not (os.path.exists(pa) and os.path.exists(pb)):
            print(f'{stem}{s}: 片方に無い（新 {os.path.exists(pa)}・古 {os.path.exists(pb)}）'); bad += 1; continue
        la = open(pa, 'rb').read().split(b'\n'); lb = open(pb, 'rb').read().split(b'\n')
        ha = la[0].rstrip(b'\r').split(b'\t'); hb = lb[0].rstrip(b'\r').split(b'\t')
        miss = [c for c in hb if c not in ha]
        if miss: print(f'{stem}{s}: 古い列が新に無い {miss[:5]}'); bad += 1; continue
        ix = [ha.index(c) for c in hb]
        ra = [r for r in la[1:] if r.strip()]; rb = [r for r in lb[1:] if r.strip()]
        same = 0; first = None
        for i, (x, y) in enumerate(zip(ra, rb)):
            xa = x.rstrip(b'\r').split(b'\t'); yb = y.rstrip(b'\r').split(b'\t')
            if [xa[k] for k in ix] == yb: same += 1
            elif first is None: first = i + 2
        ok = same == len(ra) == len(rb)
        if not ok: bad += 1
        print(f'{stem}{s}: 行 新 {len(ra)}・古 {len(rb)}・古い {len(hb)} 列で一致 {same}{"" if ok else f"・最初に違う行 {first}"}')
print('判定:', '一致' if bad == 0 else f'不一致 {bad}')
