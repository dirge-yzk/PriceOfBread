# (1215) 史実の桁の線 H1：英の価格水準の 1793〜1830 年の最大 ÷ 1792 年の最後の四半期の値（nation_s の priceLevel）。種ごと・最大。
# 史実：英の物価は兌換の停止の期間（1797〜1821）に 1793 年比 約 1.8 倍（Gayer-Rostow-Schwartz の指数の大きさ・推）＝10 倍の線は 18。
# 使い方: python -X utf8 -I ukprice.py <生> <種0> <種1>
import sys, csv
raw, s0, s1 = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
out = []
for s in range(s0, s1 + 1):
    b = None; mx = None
    for r in csv.DictReader(open(f'{raw}/nation_s{s}.tsv', encoding='utf-8'), delimiter='\t'):
        if r['nation'] != 'イギリス': continue
        y = int(r['year']); p = float(r['priceLevel'])
        if y == 1792: b = p
        elif 1793 <= y <= 1830 and b: mx = p / b if mx is None else max(mx, p / b)
    out.append((s, mx)); print(f'seed {s}\tmax/1792 {mx:.3f}' if mx is not None else f'seed {s}\tnone')
v = [m for _, m in out if m is not None]
print(f'MAX {max(v):.3f}\tH1 {"赤" if max(v) >= 18 else "緑"}（線 18）' if v else 'MAX none')
