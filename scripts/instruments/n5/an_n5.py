"""(311) 線 n5 読み手＝読むだけ・挙動は変えない。事前登録 measurements/n5_20261014_prereg.txt の [4][5]。
python an_n5.py 腕名=<ins> [腕名=<ins>…]   （stock_s<seed>.tsv の種の中央〔最小〜最大〕）
出す物：
 ・T1 艦の数（fleetUnitsStock）の国別の表 ＝ 開幕の艦の表（fixlist 段5 D1 の目安 19 国）の順位との Spearman（平均順位・種ごと）
 ・T2 順位の線（L1／L2）＝ 英の艦が {米・清・朝鮮・ハワイ・日本} の全てより多い種の数（warm・q3・q7・q19・q39・q79・q159・q239）と、英が全ての国の中で何位か
 ・T3 英の陸海の維持費の割合（navyUpkeepShare・armyUpkeepShare）の Q1〜Q40 の平均の種の中央
 ・T4 兵士÷人口（soldierToPopulationShare）の国別 ＝ warm と q39（fixlist 段5 D2 の目安つき・記録だけ）
限界の自己申告：stock_s は全ての国で warm・年末（q%4==3）の行がある（英・仏・蘭・葡・普は毎四半期）。艦の数は隻の単位が任意（軍艦の財 1 単位）で、史実の目安の「隻」と桁は比べない（順位だけ）。
fixlist の目安は記憶による【推】（出典未取得）。"""
import sys, os, statistics, collections
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'))
from instr import rows, num, seeds as seeds_of

ARMS = collections.OrderedDict(a.split('=', 1) for a in sys.argv[1:])
# fixlist 段5 D1 の目安（隻・【推】）。0 の国は史実の戦列艦 0。
HIST = {'イギリス': 150, 'ロシア': 150, 'オスマン': 90, 'フランス': 75, 'スペイン': 65, 'ネーデルラント': 35, 'デンマーク': 30, 'ヴェネツィア': 30,
        'スウェーデン': 25, '両シチリア': 25, 'ポルトガル': 12, 'プロイセン': 0, 'ハノーファー': 0, 'ライン諸邦': 0, 'アメリカ': 0, '日本': 0,
        'ハワイ王国': 0, 'エチオピア': 0, 'ペルシャ': 0}
# fixlist 段5 D2 の兵士÷人口の目安（【推】）
SOLD = {'プロイセン': 0.033, 'ハノーファー': 0.025, 'ネーデルラント': 0.0165, 'スウェーデン': 0.0155, 'デンマーク': 0.0165, 'オーストリア': 0.012,
        'フランス': 0.006, 'イギリス': 0.0035, 'アメリカ': 0.0002}
WRONG = ['アメリカ', '清', '朝鮮', 'ハワイ王国', '日本']   # fixlist D1 で英より多かった国（米 535・清 555・朝鮮 505）＋史実 0 の日本・ハワイ
TIMES = ['warm', 'q3', 'q7', 'q19', 'q39', 'q79', 'q159', 'q239']
SHOW = ['イギリス', 'フランス', 'スペイン', 'ネーデルラント', 'ロシア', 'オスマン', 'ポルトガル', 'デンマーク', 'スウェーデン', 'プロイセン', 'アメリカ', '日本', '清', '朝鮮', 'ハワイ王国']

def med(xs):
    xs = [x for x in xs if x == x]
    return statistics.median(xs) if xs else float('nan')
def cell(xs, nd=3):
    ys = [x for x in xs if x == x]
    return '—' if not ys else f'{statistics.median(ys):.{nd}f} ({min(ys):.{nd}f}〜{max(ys):.{nd}f})'
def ranks(x):
    s = sorted(range(len(x)), key=lambda i: x[i]); r = [0.0] * len(x); i = 0
    while i < len(s):
        j = i
        while j + 1 < len(s) and x[s[j + 1]] == x[s[i]]: j += 1
        for k in range(i, j + 1): r[s[k]] = (i + j) / 2
        i = j + 1
    return r
def spearman(a, b):
    ra, rb = ranks(a), ranks(b); n = len(a)
    ma, mb = sum(ra) / n, sum(rb) / n
    num_ = sum((x - ma) * (y - mb) for x, y in zip(ra, rb))
    da = sum((x - ma) ** 2 for x in ra) ** .5; db = sum((y - mb) ** 2 for y in rb) ** .5
    return num_ / (da * db) if da * db > 0 else float('nan')

def load(D):
    S = collections.defaultdict(dict)   # (seed,time) -> nation -> row
    for sd, r in rows(D, 'stock'):
        S[(sd, r['time'])][r['nation']] = r
    return S, seeds_of(D, 'stock')

for arm, D in ARMS.items():
    S, seeds = load(D)
    print(f'######## {arm}（{D}・種 {len(seeds)}・stock_s）')
    print('T1 艦の数（fleetUnitsStock）の種の中央〔最小〜最大〕＋史実の目安の順位との Spearman（19 国）')
    print('時点\t' + '\t'.join(n[:4] for n in SHOW) + '\tSpearman(19国)')
    for t in TIMES:
        cells = []
        for n in SHOW:
            cells.append(cell([num(S[(sd, t)][n]['fleetUnitsStock']) for sd in seeds if n in S.get((sd, t), {})], 0))
        sps = []
        for sd in seeds:
            d = S.get((sd, t), {})
            ns = [n for n in HIST if n in d]
            if len(ns) >= 10: sps.append(spearman([HIST[n] for n in ns], [num(d[n]['fleetUnitsStock']) for n in ns]))
        print(t + '\t' + '\t'.join(cells) + '\t' + cell(sps, 3))
    print('T2 順位の線：英の艦が {米・清・朝鮮・ハワイ・日本} の全てより多い種の数／英の艦の順位（全ての国の中・種の中央）')
    for t in TIMES:
        ok = 0; rk = []; tot = 0
        for sd in seeds:
            d = S.get((sd, t), {})
            if 'イギリス' not in d or any(n not in d for n in WRONG): continue
            tot += 1
            uk = num(d['イギリス']['fleetUnitsStock'])
            if all(uk > num(d[n]['fleetUnitsStock']) for n in WRONG): ok += 1
            allv = sorted((num(r['fleetUnitsStock']) for r in d.values() if num(r['fleetUnitsStock']) == num(r['fleetUnitsStock'])), reverse=True)
            rk.append(1 + sum(1 for v in allv if v > uk))
        print(f'{t}\t英が全てより多い {ok}/{tot}\t英の順位 {med(rk)}')
    print('T3 英の陸海の維持費の割合（Q1〜Q40 の平均の種の中央〔最小〜最大〕・stock_s の英の四半期の行）')
    nv, av = [], []
    for sd in seeds:
        q = []
        for (s2, t), d in S.items():
            if s2 != sd or not t.startswith('q') or 'イギリス' not in d: continue
            k = int(t[1:])
            if 0 <= k <= 39: q.append((k, num(d['イギリス']['navyUpkeepShare']), num(d['イギリス']['armyUpkeepShare'])))
        if q: nv.append(sum(x[1] for x in q) / len(q)); av.append(sum(x[2] for x in q) / len(q))
    print(f'navyUpkeepShare\t{cell(nv, 4)}\tarmyUpkeepShare\t{cell(av, 4)}')
    print('T4 兵士÷人口（soldierToPopulationShare）の種の中央〔最小〜最大〕＝記録だけ（B の触らない量）・［目安］')
    for t in ('warm', 'q39'):
        print(t + '\t' + '\t'.join(f'{n[:4]} {cell([num(S[(sd, t)][n]["soldierToPopulationShare"]) for sd in seeds if n in S.get((sd, t), {})], 4)}' + (f'［{SOLD[n]}］' if n in SOLD else '') for n in SHOW))
    print()
