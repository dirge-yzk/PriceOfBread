# (1081) c103dm 記録：紙 §3-3 の小国の 1800 年の国庫÷歳入（stock_s の treasuryStock ÷ nation_s の revenueBaseMeanPerMonth×12・年のラベル 1800 の最後の行・10 種の中央）。読むだけ。
#   python -X utf8 -I rec_treas.py B=<ins> M=<ins> N1=<ins> N2=<ins>
import sys, os, csv, statistics
csv.field_size_limit(1 << 30)
arms = dict(a.split('=', 1) for a in sys.argv[1:])
NAT = ['アラビア', 'カンボジア', 'ヒヴァ', 'コンゴ王国', 'ハワイ王国', 'オマーン', 'デリー', 'ボルヌ帝国', 'ラヘジ', 'イエメン', '西山朝', '両シチリア']
def last(path, col):
    out = {}
    if not os.path.exists(path): return out
    with open(path, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); iy, i_n, ic = h.index('year'), h.index('nation'), h.index(col)
        for row in r:
            if row[iy] == '1800': out[row[i_n]] = row[ic]
    return out
def num(v):
    try: return float(v)
    except: return float('nan')
res = {}
for nm, d in arms.items():
    per = {n: [] for n in NAT}
    for s in range(1, 11):
        t = last(os.path.join(d, f'stock_s{s}.tsv'), 'treasuryStock'); rv = last(os.path.join(d, f'nation_s{s}.tsv'), 'revenueBaseMeanPerMonth')
        for n in NAT:
            if n in t and n in rv and num(rv[n]) > 0: per[n].append(num(t[n]) / (12 * num(rv[n])))
    res[nm] = per
print('== 小国の 1800 年の国庫÷年の歳入の基準（10 種の中央・n＝種の数）')
for n in NAT:
    print(f'  {n}：' + '・'.join(f'{nm} {statistics.median(res[nm][n]):.3g}(n{len(res[nm][n])})' if res[nm][n] else f'{nm} なし' for nm in arms))
