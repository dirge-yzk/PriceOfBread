# 問い2：開幕（stock の open・warm・q0〜、nation と scalars の q0〜）で、ある国の欄を基準と腕で並べる（読むだけ）
# 使い方: python q2_open_compare.py <基準> <腕> <種> <国名> <表> <時刻,時刻,...> <欄,欄,...>
import sys, os, csv
A, B, seed, nat, t, times, cols = sys.argv[1:8]
times = times.split(','); cols = cols.split(',')
def get(d):
    out = {}
    with open(os.path.join(d, f"{t}_s{seed}.tsv"), encoding='utf-8') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for row in r:
            if row[ix['nation']] == nat and row[0] in times: out[row[0]] = {c: row[ix[c]] for c in cols if c in ix}
    return out
a, b = get(A), get(B)
print(f"# {t}_s{seed}.tsv 国={nat}：欄\t時刻\t基準\t腕\t腕÷基準")
for c in cols:
    for tm in times:
        x = a.get(tm, {}).get(c); y = b.get(tm, {}).get(c)
        try: rr = f"{float(y)/float(x):.3g}" if float(x) != 0 else '-'
        except: rr = '-'
        print(f"{c}\t{tm}\t{x}\t{y}\t{rr}")
