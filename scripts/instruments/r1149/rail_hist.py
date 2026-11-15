# (1149) c112il 読み手：世界の線路の量（trrail_s の railTrackEnd の和）の年の比と国の取り分。読むだけ・標準出力だけ。
#   python -X utf8 -I rail_hist.py <ins の生> <種の始め> <種の終わり>
# 量は容量の単位（Transport.cs:205〜:206＝買った線路と機関車の単位×TransportCapPerUnit÷地形）＝km ではない。
# 次元の無い量（1900 年に対する比・取り分）だけを史実と並べる。rec1143.py と同じ和（水の州も含む）を先に出して突き合わせる。
import sys, os, statistics
from collections import defaultdict

ins, s0, s1 = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
YEARS = (1840, 1850, 1860, 1870, 1880, 1890, 1900, 1913, 1930, 1950)
GER = {'プロイセン', 'バイエルン', 'ザクセン', 'ハノーファー', 'ヴュルテンベルク', 'ライン諸邦'}
PICK = ('アメリカ', 'イギリス', 'フランス', 'ロシア', 'オーストリア', '清', '日本', 'オスマン', 'スペイン')
def fl(x):
    try: return float(x)
    except ValueError: return float('nan')
per_year = defaultdict(list); shares = defaultdict(list); land1900 = []
for s in range(s0, s1 + 1):
    p = os.path.join(ins, f'trrail_s{s}.tsv')
    if not os.path.exists(p): print(f'種 {s}：trrail_s が無い'); continue
    tot = defaultdict(float); byn = defaultdict(float); land = 0.0
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for ln in f:
            c = ln.rstrip('\n').split('\t')
            if len(c) < len(h): continue
            y = int(c[ix['year']]); v = fl(c[ix['railTrackEnd']])
            if not (v == v): continue
            if y in YEARS: tot[y] += v
            if y == 1900:
                n = c[ix['nation']]; byn['ドイツの諸邦の和' if n in GER else n] += v
                if c[ix['waterFlag']] != '1': land += v
    for y in YEARS: per_year[y].append(tot[y])
    t = tot[1900]
    if t > 0:
        for n in PICK + ('ドイツの諸邦の和',): shares[n].append(byn[n] / t)
        top = sorted(byn.items(), key=lambda kv: -kv[1])[:3]
        print(f'種 {s}：1900 年の和 {t:.4g}・上位 ' + '・'.join(f'{n} {v / t:.1%}' for n, v in top))
    land1900.append(land)
m = lambda xs: statistics.mean(xs) if xs else float('nan')
print(f'== {ins} 種 {s0}〜{s1}')
print(f'1900 年の和（種の平均・rec1143 と同じ和）{m(per_year[1900]):.4g}・水の州を除く {m(land1900):.4g}')
print('年の和÷1900 年（種ごとの比の中央）：' + '・'.join(f'{y} {statistics.median([a / b for a, b in zip(per_year[y], per_year[1900]) if b > 0]):.3g}' for y in YEARS))
print('1900 年の国の取り分（種の中央・最小〜最大）：' + '・'.join(f'{n} {statistics.median(v):.1%}（{min(v):.1%}〜{max(v):.1%}）' for n, v in shares.items()))
