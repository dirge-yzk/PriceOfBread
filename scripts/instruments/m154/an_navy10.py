"""(154) 国どうしの比を10年の窓で読む（(151) の反証役の指摘＝1年の断面ではなく README の「10年程度」の窓・海軍は海軍力と艦隊の両方）。
python an_navy10.py <計器の出力ディレクトリ>   （stock 表の年末の行・生きている国だけ＝(245) 旧の inj_navy の Y 行を標準の計器へ畳んだ）
窓＝1815-24・1855-64・1904-13（史実の値は an_navy.py の 1815・1860・1914 の表＝記憶の概数【推】）。
種ごとに窓の年の平均（国）÷窓の年の平均（英）＝比 → 10種の中央値。判定＝シミュ÷max(史実, 床 0.02)（両方が床の下なら可）が 10 以上か 0.1 以下で NG・3〜10 倍は記録。
艦隊の比＝艦隊のゲージ（fleet＝艦の量・乗員を見ない）。海軍力＝艦と乗員の弱い方（naval）。"""
import sys, os, statistics as st
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'))
from instr import rows, num, year_end, seeds as seeds_of
D = sys.argv[1]
HIST_NAVAL = {1815: {'フランス': 0.40, 'プロイセン': 0.0, 'ロシア': 0.35, 'アメリカ': 0.08, '日本': 0.0, '清': 0.0},
              1860: {'フランス': 0.65, 'プロイセン': 0.02, 'ロシア': 0.25, 'アメリカ': 0.15, '日本': 0.01, '清': 0.02},
              1914: {'フランス': 0.33, 'プロイセン': 0.48, 'ロシア': 0.25, 'アメリカ': 0.36, '日本': 0.26, '清': 0.02}}
HIST_LAND = {1815: {'フランス': 1.6, 'プロイセン': 0.9, 'ロシア': 5.3, 'アメリカ': 0.07, '日本': 2.7, '清': 5.3},
             1860: {'フランス': 1.8, 'プロイセン': 0.9, 'ロシア': 4.1, 'アメリカ': 0.07, '日本': 1.8, '清': 3.6},
             1914: {'フランス': 3.5, 'プロイセン': 3.2, 'ロシア': 5.2, 'アメリカ': 0.4, '日本': 1.0, '清': 2.0}}
WIN = {1815: (1815, 1824), 1860: (1855, 1864), 1914: (1904, 1913)}
FLOOR = 0.02
NAT = ['フランス', 'プロイセン', 'ロシア', 'アメリカ', '日本', '清']
Y = {}; seeds = seeds_of(D, 'stock')
for sd, r in rows(D, 'stock'):
    if not year_end(r) or not (num(r['totalPopulationK']) > 0) or num(r['landPowerIndex']) != num(r['landPowerIndex']): continue   # 生きている国（人口＞0）で国力のある国だけ
    Y[(sd, int(r['year']), r['nation'])] = {'naval': num(r['navalPowerIndex']), 'fleet': num(r['fleetGaugeIndex']), 'land': num(r['landPowerIndex'])}
def med(xs):
    xs = [x for x in xs if x == x]
    return st.median(xs) if xs else float('nan')
def wmean(s, n, key, a, b):
    xs = [Y[(s, y, n)][key] for y in range(a, b + 1) if (s, y, n) in Y and Y[(s, y, n)][key] == Y[(s, y, n)][key]]
    return sum(xs) / len(xs) if xs else float('nan')
print(f'######## {D}（種 {len(seeds)}・10年の窓・英＝1・種の中央値・［史実］・シミュ÷史実〔床 {FLOOR}〕）')
ng = {'海軍力': 0, '艦隊': 0, '陸軍力': 0}
for yr, (a, b) in WIN.items():
    for kind, key, H in (('海軍力', 'naval', HIST_NAVAL), ('艦隊', 'fleet', HIST_NAVAL), ('陸軍力', 'land', HIST_LAND)):
        cells = []
        for n in NAT:
            rs = []
            for s in seeds:
                x = wmean(s, n, key, a, b); u = wmean(s, 'イギリス', key, a, b)
                if x == x and u == u and u > 0: rs.append(x / u)
            r = med(rs); h = H[yr].get(n, float('nan'))
            if not (r == r): cells.append(f'{n} —'); continue
            ratio = 1.0 if (r < FLOOR and h < FLOOR) else r / max(h, FLOOR)   # ★(172) 反証役 vd3#4＝旧 max(シミュ,床)÷max(史実,床)＝シミュの側にも床があり README の定義（シミュ÷max(史実,床)）と違った
            flag = 'NG' if (ratio >= 10 or ratio <= 0.1) else ('記録' if (ratio >= 3 or ratio <= 1 / 3) else '')
            if flag == 'NG': ng[kind] += 1
            cells.append(f'{n} {r:.3g}［{h:g}］{ratio:.3g}{flag}')
        print(f'{a}-{b} {kind}: ' + '・'.join(cells))
print('NG のセル（6国×3窓）＝' + '・'.join(f'{k} {v}' for k, v in ng.items()))
