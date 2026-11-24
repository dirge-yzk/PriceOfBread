# 問い2：助走の終わり（stock の warm 行）で、全ての国の欄を基準と腕で比べる（腕÷基準の分布と英の値）（読むだけ）
# 使い方: python q2_warm_all.py <基準> <腕> <種> <欄,欄,...>
import sys, os, csv, statistics
A, B, s, cols = sys.argv[1], sys.argv[2], sys.argv[3], sys.argv[4].split(',')
def get(d):
    out = {}
    with open(os.path.join(d, f"stock_s{s}.tsv"), encoding='utf-8') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for row in r:
            if row[0] == 'warm': out[row[ix['nation']]] = {c: float(row[ix[c]]) for c in cols}
    return out
a, b = get(A), get(B)
print(f"# stock_s{s} warm：欄\t国の数\t腕÷基準の中央\t最小\t最大\t英の基準\t英の腕\t比が0.7未満の国")
for c in cols:
    rs = []; low = []
    for n in a:
        x, y = a[n][c], b.get(n, {}).get(c)
        if y is None or not (x > 0): continue
        rs.append(y / x)
        if y / x < 0.7: low.append(f"{n}:{y/x:.2f}")
    uk = (a.get('イギリス', {}).get(c), b.get('イギリス', {}).get(c))
    print(f"{c}\t{len(rs)}\t{statistics.median(rs):.3f}\t{min(rs):.3f}\t{max(rs):.3f}\t{uk[0]:.4g}\t{uk[1]:.4g}\t{' '.join(low[:12])}")
