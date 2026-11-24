# 問い2：ある表のある国のある時刻の行を基準と腕で並べ、違う欄だけ出す（読むだけ）
# 使い方: python q2_row_diff.py <基準> <腕> <表> <種> <国名> <時刻>
import sys, csv, os
A, B, t, s, nat, tm = sys.argv[1:7]
def get(d):
    with open(os.path.join(d, f"{t}_s{s}.tsv"), encoding='utf-8') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ni = h.index('nation')
        for row in r:
            if row[ni] == nat and row[0] == tm: return h, row
h, a = get(A); _, b = get(B)
print(f"# {t}_s{s}.tsv 国={nat} 時刻={tm}：欄\t基準\t腕")
for k, x, y in zip(h, a, b):
    if x != y: print(f"{k}\t{x}\t{y}")
