"""(153) 兵士÷人口の史実比較（stock 表の年末の行・その年末に交戦していない年だけ＝平時の近似）。python an_sold.py <計器の出力ディレクトリ>（(245) 旧の inj_wpn の M 行を標準の計器へ畳んだ）
史実と窓と判定は sol/an_hist.py と同じ（10年の窓・種ごとの窓の平均の中央・比 10 倍以上か 1/10 以下で NG・3〜10 倍は記録・床 0.1%）。
⚠ an_hist.py は四半期ごと（戦争と内戦の月が0の四半期）＝この読み手は年末の断面だけ＝近似。"""
import sys, os, statistics as st
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'))
from instr import rows, num, year_end, seeds as seeds_of
D = sys.argv[1]
WIN = [(1784, 1793), (1815, 1824), (1855, 1864), (1904, 1913)]
HS = {'イギリス': [0.5, 1.0, 1.0, 0.9], 'フランス': [0.6, 0.6, 1.6, 2.2], 'プロイセン': [3.3, 1.3, 1.1, 1.2], 'ロシア': [1.4, 1.8, 1.2, 0.8],
      '清': [0.3, 0.3, 0.25, 0.25], '日本': [1.5, 1.5, 1.5, 0.5], 'アメリカ': [0.02, 0.2, 0.05, 0.17]}
FLOOR = 0.1
M = {}; seeds = seeds_of(D, 'stock')
for sd, r in rows(D, 'stock'):
    if year_end(r): M[(sd, r['nation'], int(r['year']))] = (num(r['soldierK']), num(r['populationK']), num(r['atWarFlag']))
print(f'# 兵士÷人口%（年末の断面・交戦していない年・種 {len(seeds)}）')
ng = []
for n, hs in HS.items():
    out = []
    for wi, (y0, y1) in enumerate(WIN):
        per = []
        for sd in seeds:
            v = [100 * M[(sd, n, y)][0] / M[(sd, n, y)][1] for y in range(y0, y1 + 1) if (sd, n, y) in M and M[(sd, n, y)][1] > 0 and M[(sd, n, y)][2] == 0]
            if v: per.append(st.mean(v))
        s = st.median(per) if per else float('nan'); h = hs[wi]
        if s != s: out.append(f'{y0}-{y1}: —'); continue
        if s < FLOOR and h < FLOOR: out.append(f'{y0}-{y1}: {s:.2f}［{h}］床の下'); continue
        r = max(s, FLOOR) / max(h, FLOOR)
        tag = 'NG' if (r >= 10 or r <= 0.1) else ('記録' if (r >= 3 or r <= 1 / 3) else '可')
        if tag == 'NG': ng.append(f'{n} {y0}-{y1} {r:.1f}倍')
        out.append(f'{y0}-{y1}: {s:.2f}［{h}］{r:.2f}倍 {tag}')
    print(n, ' ／ '.join(out))
print('NG:', ng if ng else 'なし')
