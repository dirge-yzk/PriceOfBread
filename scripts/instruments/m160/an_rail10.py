"""(160) 鉄道の容量の国どうしの比を10年の窓で史実と照合（(159) 反証役の指摘 vb#1＝鉄道の容量の比は測っていない）。読むだけ。
python an_rail10.py <計器の出力ディレクトリ> [ラベル]   （stock 表の年末の行の railCapacityStock）（(245) 旧の注入の行を標準の計器 SimCore/Instruments.cs の表へ畳んだ）
史実＝線路の長さ（km）の記憶の概数【推】（1860＝窓 1855-64・1910＝窓 1904-13）：
  英（アイルランドを含む）1860 16,800・1910 37,700／仏 9,200・49,000／普（1910 は独）5,800・61,200／墺（1910 は墺洪）5,200・43,000／露 1,600・66,600／
  米 49,300・386,700／日 0・8,700／清 0・9,000／西 1,900・15,000／オスマン 70・6,000。1815-24 は全ての国で0（最初の公共鉄道は1825）。
読み（AI の読み・推奨で進めた＝(154) の国どうしの比と同じ形）：
  ①窓ごとに 種ごとの窓の年の平均（国）÷窓の年の平均（英）＝比 → 種の中央値。シミュ÷max(史実の比, 床 0.02)（両方が床の下なら可）が 10 以上か 0.1 以下で NG・3〜10 倍は記録。
  ②1815-24（英も史実0＝比が立たない）＝国の容量÷その種の英の 1904-13 の平均（史実の規模の単位）＝史実0＝床 0.02 で割る。"""
import sys, os, statistics as st, collections
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'))
from instr import rows, num, year_end, seeds as seeds_of
D = sys.argv[1]; LAB = sys.argv[2] if len(sys.argv) > 2 else D
KM = {'イギリス': (16800, 37700), 'フランス': (9200, 49000), 'プロイセン': (5800, 61200), 'オーストリア': (5200, 43000), 'ロシア': (1600, 66600),
      'アメリカ': (49300, 386700), '日本': (0, 8700), '清': (0, 9000), 'スペイン': (1900, 15000), 'オスマン': (70, 6000)}
NAT = [n for n in KM if n != 'イギリス']
WIN = {'1815-24': (1815, 1824), '1855-64': (1855, 1864), '1904-13': (1904, 1913)}
FLOOR = 0.02
data = collections.defaultdict(dict)   # (seed, nation) -> yr -> railCap
seeds = seeds_of(D, 'stock')
for sd, r in rows(D, 'stock'):
    if year_end(r): data[(sd, r['nation'])][int(r['year'])] = num(r['railCapacityStock'])
def wmean(sd, n, a, b):
    xs = [data[(sd, n)][y] for y in range(a, b + 1) if (sd, n) in data and y in data[(sd, n)] and data[(sd, n)][y] == data[(sd, n)][y]]
    return sum(xs) / len(xs) if xs else float('nan')
def med(xs):
    xs = [x for x in xs if x == x]
    return st.median(xs) if xs else float('nan')
def flag(ratio):
    return 'NG' if (ratio >= 10 or ratio <= 0.1) else ('記録' if (ratio >= 3 or ratio <= 1 / 3) else '')
print(f'######## {LAB}（種 {len(seeds)}・鉄道の容量・10年の窓・英＝1・種の中央値・［史実＝線路の長さの比【推】］・シミュ÷max(史実, 床 {FLOOR})）')
ng = 0
for w, (a, b) in WIN.items():
    cells = []
    for n in NAT:
        if w == '1815-24':
            rs = []
            for sd in seeds:
                x = wmean(sd, n, a, b); u = wmean(sd, 'イギリス', 1904, 1913)
                if x == x and u == u and u > 0: rs.append(x / u)
            r = med(rs); h = 0.0
        else:
            k = 0 if w == '1855-64' else 1
            rs = []
            for sd in seeds:
                x = wmean(sd, n, a, b); u = wmean(sd, 'イギリス', a, b)
                if x == x and u == u and u > 0: rs.append(x / u)
            r = med(rs); h = KM[n][k] / KM['イギリス'][k]
        if not (r == r): cells.append(f'{n} —'); continue
        ratio = r / max(h, FLOOR)   # ★(172) 反証役 vd3#4＝旧 max(シミュ,床)÷max(史実,床)＝README の定義（シミュ÷max(史実,床)・両方が床の下なら可）へ
        if max(r, h) < FLOOR: ratio = 1.0
        fg = flag(ratio)
        if fg == 'NG': ng += 1
        cells.append(f'{n} {r:.3g}［{h:.3g}］{ratio:.3g}{fg}')
    lab = '1815-24（英の1904-13 を1）' if w == '1815-24' else w
    print(f'{lab}: ' + '・'.join(cells))
# 英そのものの 1815-24（英の 1904-13 を1）
ub = med([wmean(sd, 'イギリス', 1815, 1824) / wmean(sd, 'イギリス', 1904, 1913) for sd in seeds if wmean(sd, 'イギリス', 1904, 1913) > 0])
ubr = max(ub, FLOOR) / FLOOR
print(f'英 1815-24（英の1904-13 を1）: {ub:.3g}［0］{ubr:.3g}{flag(ubr) if ub >= FLOOR else ""}')
print(f'NG のセル（9国×3窓＋英の1815-24）＝{ng + (1 if ub >= FLOOR and flag(ubr) == "NG" else 0)}')
