# (657) 線 igi3 前半：段0 の読み（読むだけ・新しい走りはゼロ）。入力＝基線 m655 の計器つき 10 種とヌル腕 N1・N2 の生（D:/PoB_work/keep_655）。
# 使い方：PYTHONUTF8=1 python s0.py <keep_655 のパス>   （出力は標準出力＝measurements/igi3_657_20261030_s0.txt に写した）
#  ① urxpd_s の capBound÷regionMonths（PopDecision の都市の天井が目標を切った地域×月の割合・種ごと／年代別／国別）
#  ② region_s の u＝1−ruralPopulationK/populationK の最大と 0.95・0.9 以上の件数（active な州×四半期・基線／N1／N2）
#  M3 の閾値の材料（k_urb_delta.py を基線に対し N1・N2 で）は別に scripts/instruments/igi/k_urb_delta.py を呼ぶ。
import sys, csv, statistics, collections
root = sys.argv[1] if len(sys.argv) > 1 else "D:/PoB_work/keep_655"
def era(y): return '1783-1829' if y < 1830 else '1830-1869' if y < 1870 else '1870-1909' if y < 1910 else '1910-1950'
per = []; agg = collections.defaultdict(lambda: [0.0, 0.0])
for s in range(1, 11):
    a = b = 0.0
    with open(f"{root}/m655_ins/urxpd_s{s}.tsv", encoding='utf-8') as f:
        for row in csv.DictReader(f, delimiter='\t'):
            rm = float(row['regionMonths']); cb = float(row['capBound']); y = int(row['year'])
            a += rm; b += cb
            for k in (era(y), row['nation']): agg[k][0] += rm; agg[k][1] += cb
    per.append(b / a if a else 0.0)
print("capBound_share_per_seed", " ".join(f"{x:.3f}" for x in per), "median", f"{statistics.median(per):.3f}")
for k, v in sorted(agg.items()): print("capBound_share", k, f"{v[1]/v[0]:.3f}", "regionMonths", int(v[0]))
for name in ('m655_ins', 'n1', 'n2'):
    mx = 0.0; c9 = c95 = n = 0
    for s in range(1, 11):
        with open(f"{root}/{name}/region_s{s}.tsv", encoding='utf-8') as f:
            r = csv.reader(f, delimiter='\t'); h = next(r)
            iac, ip, ir = [h.index(x) for x in ('activeFlag', 'populationK', 'ruralPopulationK')]
            for row in r:
                if row[iac] != '1': continue
                try: pp = float(row[ip]); u = 1 - float(row[ir]) / pp if pp > 0 else None
                except Exception: continue
                if u is None: continue
                n += 1; mx = max(mx, u); c9 += u >= 0.9; c95 += u >= 0.95
    print("urban_share", name, "rows", n, "max_u", f"{mx:.4f}", ">=0.9", c9, ">=0.95", c95)
