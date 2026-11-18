# (1203) 問い① 2 つの生の nation_s を (四半期, 国) の行で突き合わせ、種ごとに最初に違う四半期と、その四半期に違う国・列を出す
# 使い方: python -X utf8 -I firstdiv.py <生A> <生B> <種0> <種1> <出力>
import sys
a, b, s0, s1, outp = sys.argv[1], sys.argv[2], int(sys.argv[3]), int(sys.argv[4]), sys.argv[5]
def load(p):
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); d = {}
        for l in f:
            c = l.rstrip('\n').split('\t'); d[(int(c[0]), c[3])] = c
    return h, d
o = open(outp, 'w', encoding='utf-8', newline='\n')
o.write('seed\tfirst_div_quarter\tyear\tnations_diff_count\tnations\tcols_first_nation\n')
for s in range(s0, s1 + 1):
    ha, da = load(f'{a}/nation_s{s}.tsv'); hb, db = load(f'{b}/nation_s{s}.tsv')
    common = [c for c in ha if c in hb]; ia = [ha.index(c) for c in common]; ib = [hb.index(c) for c in common]
    keys = sorted(set(da) | set(db)); fq = None; diffs = []
    for k in keys:
        if fq is not None and k[0] > fq: break
        ra, rb = da.get(k), db.get(k)
        if ra is None or rb is None: same = False; cols = ['(行の有無)']
        else:
            cols = [common[j] for j in range(len(common)) if ra[ia[j]] != rb[ib[j]]]; same = not cols
        if not same:
            fq = k[0]; diffs.append((k[1], cols))
    if fq is None: o.write(f'{s}\t-\t-\t0\t\t\n'); continue
    yr = (da.get((fq, diffs[0][0])) or db.get((fq, diffs[0][0])))[1]
    o.write(f"{s}\t{fq}\t{yr}\t{len(diffs)}\t{'・'.join(n for n, _ in diffs[:6])}\t{','.join(diffs[0][1][:6])}\n")
