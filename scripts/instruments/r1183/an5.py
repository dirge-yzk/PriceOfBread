# (1183) エンジンの州の品位（mintreg_s の grade・depositFrac＝InstrumentsMintreg.cs:134〜135・四半期ごと・全ての州）で Q2 を数え直し、
# 推し（dep_rec）の州の品位と行ごとに突き合わせる（★(1136)）。
# 使い方: python -X utf8 -I an5.py <生のフォルダ> <種の列> <出力>
import sys, os, csv, collections, statistics as S
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from dep_rec import load, simulate, FLOOR
csv.field_size_limit(10**9)
RAW, SEEDS, OUT = sys.argv[1], [int(s) for s in sys.argv[2].split(',')], sys.argv[3]
YRS = [1800, 1810, 1820, 1830, 1840, 1850, 1880, 1900, 1913, 1950]
L = []
def P(*a): L.append(' '.join(str(x) for x in a))
cnt = collections.defaultdict(list); first = collections.defaultdict(collections.Counter); match = []; nanfr = collections.defaultdict(list)
for sd in SEEDS:
    eng = {}   # (year, region) -> (grade, frac)   年末の四半期
    ff = {}
    with open(f'{RAW}/mintreg_s{sd}.tsv', encoding='utf-8', newline='') as f:
        rd = csv.reader(f, delimiter='\t'); h = next(rd); ix = {k:i for i,k in enumerate(h)}
        for r in rd:
            try: gr = float(r[ix['grade']])
            except ValueError: continue
            fr = r[ix['depositFrac']]
            y = int(r[ix['year']]); reg = r[ix['region']]
            if fr in ('', 'nan'):
                if int(r[ix['q']]) % 4 == 3 and y in YRS: nanfr[y].append(reg)
                continue
            if gr <= FLOOR + 1e-9 and reg not in ff: ff[reg] = y
            if int(r[ix['q']]) % 4 == 3: eng[(y, reg)] = (gr, float(fr))
    for y in YRS:
        regs = [v for (yy, _), v in eng.items() if yy == y]
        if regs: cnt[y].append((sum(1 for g, _ in regs if g <= FLOOR + 1e-9), sum(1 for g, _ in regs if g < 1.0), len(regs)))
    for reg, y in ff.items(): first[sd][(y // 10) * 10] += 1
    snaps, owner, bon = load(RAW, sd)
    rec = simulate(snaps, bon)
    for y, regs in rec.items():
        for reg, d in regs.items():
            if (y, reg) in eng: match.append((y, abs(d['grade'] - eng[(y, reg)][0]), eng[(y, reg)][0]))
P('# (1183) エンジンの州の品位（mintreg_s・年末の四半期）・種', SEEDS)
P('## 床（≦0.15＋1e-9）の州／品位＜1 の州／鉱脈を捕えた州（種ごと）と床の割合')
for y in YRS:
    v = cnt[y]
    if v: P(f'  {y}: ' + ' '.join(f'{a}/{b}/{c}' for a, b, c in v) + f'｜床の割合 中央 {S.median([a/c for a,b,c in v]):.3f}（{min(a/c for a,b,c in v):.3f}〜{max(a/c for a,b,c in v):.3f}）・捕えていない州 {S.median([len(nanfr[y])/len(SEEDS)]) if nanfr[y] else 0}')
P('## 最初に床に届いた十年（州の数・種ごと）')
for sd in SEEDS: P(f'  種{sd}: ' + ' '.join(f'{k}:{first[sd][k]}' for k in sorted(first[sd])))
P('## 推しとエンジンの州の品位の突き合わせ（断面の年×州）')
for lo, hi in [(1790, 1800), (1810, 1830), (1840, 1850), (1860, 1950)]:
    v = [(d, e) for y, d, e in match if lo <= y <= hi]
    if not v: continue
    nf = [d for d, e in v if e > FLOOR + 1e-9]
    P(f'  {lo}〜{hi}: 行 {len(v)}・|差|≦0.02 {sum(1 for d,_ in v if d<=0.02)/len(v):.3f}・≦0.05 {sum(1 for d,_ in v if d<=0.05)/len(v):.3f}・エンジンが床より上の行 {len(nf)}・その |差|≦0.05 {sum(1 for d in nf if d<=0.05)/max(1,len(nf)):.3f}・|差|の中央 {S.median(nf) if nf else float("nan"):.3f}')
open(OUT, 'w', encoding='utf-8', newline='\n').write('\n'.join(L) + '\n')
