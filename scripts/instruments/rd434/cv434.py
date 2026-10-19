# ★(434) 潜在量の安定（事前登録 T3）：計器 port_s の港の州の tradePotentialWeight の四半期間の変動係数（州×種ごと）の中央値。
# 使い方：PYTHONUTF8=1 python cv434.py <計器の生のフォルダ> [国をカンマ区切り]
import csv, sys, statistics, collections, math
d = sys.argv[1]; NAT = sys.argv[2].split(',') if len(sys.argv) > 2 else ['清', 'ロシア', 'イギリス']
cv = collections.defaultdict(list)
for s in range(1, 11):
    ser = collections.defaultdict(list)
    try: f = open(f'{d}/port_s{s}.tsv', encoding='utf-8')
    except OSError: continue
    with f:
        for r in csv.DictReader(f, delimiter='\t'):
            if r['nation'] in NAT: ser[(r['nation'], r['region'])].append(float(r['tradePotentialWeight']))
    for (n, rg), v in ser.items():
        if len(v) < 40: continue
        # 四半期間の変動：差分の絶対値の平均÷平均（トレンドを除く）
        m = sum(v) / len(v)
        if m <= 1e-9: continue
        dd = [abs(v[i] - v[i - 1]) for i in range(1, len(v))]
        cv[n].append(sum(dd) / len(dd) / m)
allv = [x for v in cv.values() for x in v]
print('国ごとの中央値（四半期間の差分の絶対値の平均÷平均・州×種）:', {n: round(statistics.median(v), 4) for n, v in cv.items()}, '全体', round(statistics.median(allv), 4), '件数', len(allv))
