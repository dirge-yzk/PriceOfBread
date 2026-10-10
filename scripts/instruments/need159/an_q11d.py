"""inj_disc の d<seed>.txt（D 行）から Q11 の錨＝種ごとに英仏普米のうち最も早く知った年（開幕の既知は1783・知らなければ1950）→ 種の中央値 − 史実の年【推】。
python an_q11d.py <disc の出力ディレクトリ> [ラベル]"""
import sys, os, glob, statistics as st, collections
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from an_q11 import HIST, PROC, PROD
D = sys.argv[1]; LAB = sys.argv[2] if len(sys.argv) > 2 else D
ANCH = ('イギリス', 'フランス', 'プロイセン', 'アメリカ')
def base(nn):
    for a in ('イギリス', 'フランス', 'プロイセン', 'アメリカ', 'ロシア', '清', '日本', 'オーストリア', 'オスマン', 'スペイン'):
        if nn.endswith(a): return a
    return nn
yr = collections.defaultdict(dict)   # (row) -> seed -> earliest year among anchors
per = collections.defaultdict(lambda: collections.defaultdict(dict))  # row -> nation -> seed -> year
seeds = []
for f in sorted(glob.glob(os.path.join(D, 'd*.txt')), key=lambda p: int(os.path.basename(p)[1:-4])):
    sd = int(os.path.basename(f)[1:-4]); seeds.append(sd)
    for ln in open(f, encoding='utf-8'):
        t = ln.rstrip('\n').split('\t')
        if t[0] != 'D': continue
        q, y, nn, row = int(t[1]), int(t[2]), t[3], t[4]
        if q == 1: y = 1783
        b = base(nn)
        if sd not in per[row][b] or y < per[row][b][sd]: per[row][b][sd] = y
def anchor(row, sd):
    ys = [per[row][a][sd] for a in ANCH if sd in per[row][a]]
    return min(ys) if ys else 1950
def med(xs): return st.median(xs) if xs else float('nan')
out = []; mae = {'製法': [], '新製品': [], 'ほか': []}
for row, h in HIST.items():
    v = [anchor(row, sd) for sd in seeds]
    m = med(v); dv = m - h
    k = '製法' if row in PROC else '新製品' if row in PROD else 'ほか'
    mae[k].append(abs(dv))
    out.append(f'{row}{dv:+.0f}')
print(f'##### {LAB}（{len(seeds)}種）Q11 の錨（英仏普米の最早・種の中央）−史実：製法 |差|平均 {sum(mae["製法"])/len(mae["製法"]):.1f}年・新製品 {sum(mae["新製品"])/len(mae["新製品"]):.1f}年・新兵器と制度と思想 {sum(mae["ほか"])/len(mae["ほか"]):.1f}年')
print('  ' + '　'.join(out))
for nat in ('ロシア', '清', '日本'):
    parts = []
    for row in ('蒸気機関', '機関車', '汽船', '転炉鋼', '石炭火力'):
        ys = [per[row][nat][sd] for sd in seeds if sd in per[row][nat]]
        parts.append(f'{row} {int(med(ys)) if ys else "—"}〔{len(ys)}/{len(seeds)}〕')
    print(f'  {nat}: ' + '・'.join(parts))
