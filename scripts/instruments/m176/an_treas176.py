"""(176) 国庫の厚み（読むだけ）＝Q 行（inj_war171）：国庫÷月の歳入・国庫÷年GDP（hist の A 行）・国債÷年GDP・器÷歳入・請求÷払える額（主要国×時代×交戦/平時）。
python an_treas176.py <out_dir> [ラベル]"""
import sys, os, glob, statistics as st, collections
D = sys.argv[1]; LAB = sys.argv[2] if len(sys.argv) > 2 else D
NAT = ['イギリス', 'フランス', 'プロイセン', 'ロシア', 'オーストリア', 'アメリカ', '日本', '清']
ERAS = [(1784, 1849), (1850, 1913), (1914, 1950)]
def fl(x):
    try: return float(x)
    except Exception: return float('nan')
def med(x):
    x = [y for y in x if y == y]
    return st.median(x) if x else float('nan')
A = {}
for f in glob.glob(os.path.join(D, 'hist', 'a*.txt')):
    sd = int(os.path.basename(f)[1:-4])
    for ln in open(f, encoding='utf-8'):
        t = ln.rstrip('\n').split('|')
        if t[0] == 'A': A[(sd, int(t[1]), int(t[3]))] = fl(t[6])
R = collections.defaultdict(list)
for f in glob.glob(os.path.join(D, 'w171', 'q*.txt')):
    sd = int(os.path.basename(f)[1:-4])
    for ln in open(f, encoding='utf-8'):
        t = ln.rstrip('\n').split('|')
        if t[0] != 'Q' or t[4] not in NAT: continue
        v = [fl(x) for x in t[5:]]
        if not (v[0] > 0) or not (v[20] > 0): continue
        yr = int(t[2]); gdp = A.get((sd, int(t[1]), int(t[3])), float('nan'))
        e = next((f'{a}-{b}' for a, b in ERAS if a <= yr <= b), None)
        if e is None: continue
        war = 'war' if v[16] > 0 else 'peace'
        R[(t[4], e, war)].append((v[21] / (v[20] / v[0]), v[21] / gdp if gdp > 0 else float('nan'), v[22] / gdp if gdp > 0 else float('nan'), v[1] / v[20], v[10] / v[11] if v[11] > 0 else float('nan')))
print(f'######## {LAB}＝国庫÷月の歳入・国庫÷年GDP・国債÷年GDP・器÷歳入・請求÷払える額（国×四半期の中央）')
print('国 | 時代 | 区分 | 国×四半期 | 国庫÷月の歳入 | 国庫÷年GDP | 国債÷年GDP | 器÷歳入 | 請求÷払える額')
for n in NAT:
    for a, b in ERAS:
        for w in ('peace', 'war'):
            rs = R.get((n, f'{a}-{b}', w))
            if not rs: continue
            print(f"{n} | {a}-{b} | {'平時' if w == 'peace' else '交戦'} | {len(rs)} | {med([r[0] for r in rs]):.1f} | {med([r[1] for r in rs]):.3f} | {med([r[2] for r in rs]):.3f} | {med([r[3] for r in rs]):.2f} | {med([r[4] for r in rs]):.3f}")
