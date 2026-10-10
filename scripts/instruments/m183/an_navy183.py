"""(183) 海軍の桁の読み手（読むだけ）。python an_navy183.py <out/navy>
inj_navy の Y 行（年1回）から10年の窓（1784-93 … 1944-50）ごとに、種ごとの窓の平均→種の中央値：
 ①英の艦隊÷次の2国の艦隊の和（二国標準【推】＝史実 1815〜1900 年ごろ約1・1889 の海軍防衛法が明文化）＝列強の候補（仏・普・露・墺・米・日・清・西・蘭・オスマン）から窓ごとに大きい2国
 ②同じことを海軍力（艦と乗員の弱い方）で
 ③英÷各国の艦隊・海軍力（英＝1 の逆＝各国÷英）
 ④英に対する制海権（相手の海軍力÷(相手＋英)）が 0.65 を超える窓の数（種ごとに数え、種の中央値）＝上陸の門（SeaInvasionControlMin）を英に対して越える窓"""
import sys, os, glob, statistics as st
D = sys.argv[1]
CAND = ['フランス', 'プロイセン', 'ロシア', 'オーストリア', 'アメリカ', '日本', '清', 'スペイン', 'ネーデルラント', 'オスマン']
SHOW = ['フランス', 'プロイセン', 'ロシア', 'アメリカ', '日本', '清']
WIN = [(a, min(a + 9, 1950)) for a in range(1784, 1951, 10)]
COLS = None; Y = {}; seeds = []
def fl(x):
    try: return float(x)
    except Exception: return float('nan')
for f in sorted(glob.glob(os.path.join(D, 'n*.txt')), key=lambda p: int(os.path.basename(p)[1:-4])):
    sd = int(os.path.basename(f)[1:-4]); seeds.append(sd)
    for ln in open(f, encoding='utf-8'):
        t = ln.rstrip('\n').split('\t')
        if t[0] == '#Y': COLS = t[1:]
        elif t[0] == 'Y':
            d = dict(zip(COLS, t[1:]))
            Y[(sd, int(d['yr']), d['nation'])] = (fl(d['fleet']), fl(d['naval']))
def wm(s, n, k, a, b):
    xs = [Y[(s, y, n)][k] for y in range(a, b + 1) if (s, y, n) in Y and Y[(s, y, n)][k] == Y[(s, y, n)][k]]
    return sum(xs) / len(xs) if xs else float('nan')
def med(xs):
    xs = [x for x in xs if x == x]
    return st.median(xs) if xs else float('nan')
print(f'######## {D}（種 {len(seeds)}・10年の窓・種ごとの窓の平均→種の中央値）')
for k, nm in ((0, '艦隊'), (1, '海軍力')):
    print(f'## {nm}：英÷次の2国の和［二国標準【推】史実 1815〜1900 約1］・各国÷英')
    print('窓'.ljust(10) + '英÷2国'.rjust(9) + ''.join(n[:4].rjust(9) for n in SHOW))
    for a, b in WIN:
        tp = []; rel = {n: [] for n in SHOW}
        for s in seeds:
            uk = wm(s, 'イギリス', k, a, b)
            vals = sorted((wm(s, n, k, a, b) for n in CAND), key=lambda v: -(v if v == v else -1))
            top2 = [v for v in vals if v == v][:2]
            if uk == uk and len(top2) == 2 and sum(top2) > 0: tp.append(uk / sum(top2))
            for n in SHOW:
                x = wm(s, n, k, a, b)
                if uk == uk and uk > 0 and x == x: rel[n].append(x / uk)
        print(f'{a}-{b % 100:02d}'.ljust(10) + f'{med(tp):9.3g}' + ''.join(f'{med(rel[n]):9.3g}' for n in SHOW))
print('## 英に対する制海権（相手の海軍力÷(相手＋英)）が 0.65 を超える窓の数（17窓・種ごとに数えた種の中央値）')
row = []
for n in CAND:
    cs = []
    for s in seeds:
        c = 0
        for a, b in WIN:
            uk = wm(s, 'イギリス', 1, a, b); x = wm(s, n, 1, a, b)
            if uk == uk and x == x and x + uk > 0 and x / (x + uk) > 0.65: c += 1
        cs.append(c)
    row.append(f'{n} {med(cs):g}')
print('・'.join(row))
