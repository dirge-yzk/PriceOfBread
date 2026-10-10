"""(153) 戦争の戦死（10年あたり・世界の和）の史実比較の読み手（wars 表＝戦争1本ごとの両側の戦死の和〔千人〕・内戦を含む。(245) 旧の inj_navy の WEV 行を標準の計器へ畳んだ）。
python an_wardead.py <計器の出力ディレクトリ>
史実【推・記憶の概数・軍の戦死（病死を含む）・世界の和】：1790年代 約100万（革命戦争）・1800年代 約150万・1810年代 約200万（ナポレオン戦争）・
1850年代 約50万（クリミア）・1860年代 約100万（南北戦争 62万・パラグアイ・普墺）・1870年代 約50万（普仏・露土）・1910年代 約1000万（第一次大戦）。"""
import sys, os, statistics as st, collections
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'))
from instr import rows, seeds as seeds_of
D = sys.argv[1]
HIST = {1790: 1000, 1800: 1500, 1810: 2000, 1850: 500, 1860: 1000, 1870: 500, 1910: 10000, 1940: 20000}   # ★(182) 反証役 vm2#3：1940年代＝第二次大戦の軍の戦死 約2000万【推・記憶の概数】
dec = collections.defaultdict(lambda: collections.defaultdict(float)); seeds = []
seeds = seeds_of(D, 'wars')
for sd, r in rows(D, 'wars'):
    dec[sd][int(r['startYear']) // 10 * 10] += float(r['deadK'])
print(f'# 種 {len(seeds)}・戦争の戦死（千人・10年ごとの世界の和・両側・内戦を含む）の種の中央値 ÷ 史実【推】')
for d in range(1790, 1950, 10):
    v = [dec[s][d] for s in seeds]
    m = st.median(v)
    h = HIST.get(d)
    print(f'{d}年代: {m:.0f}' + (f'（史実 約{h}・史実÷シミュ {h / m:.1f}倍{"＝NG" if (h / m >= 10 or m / h >= 10) else ""}）' if h and m > 0 else ''))
