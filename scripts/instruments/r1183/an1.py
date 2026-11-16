# (1183) 推しの突き合わせ（★(1136)）・Q1 合算の害・Q2 床の州・Q3 遍在の 3 財・案ごとの床の州の数。
# 使い方: python -X utf8 -I an1.py <生のフォルダ> <種の列> <出力>
import sys, os, collections, statistics as S
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from dep_rec import *
RAW, SEEDS, OUT = sys.argv[1], [int(s) for s in sys.argv[2].split(',')], sys.argv[3]
HARD = ['石炭','鉄鉱石','金','銀','銅鉱','鉛鉱','錫鉱','硫黄','原油','クロム','硝石']
YRS = [1800, 1820, 1850, 1880, 1900, 1913, 1950]
L = []
def P(*a): L.append(' '.join(str(x) for x in a))
val = collections.defaultdict(list)   # year -> [(absdiff, engOut)]
outr = []; sgn = []; valnf = []; sgy = collections.defaultdict(list)
floor = collections.defaultdict(lambda: collections.defaultdict(list))   # variant -> year -> [(nfloor, n)]
harm = collections.defaultdict(lambda: collections.defaultdict(list))    # good -> year -> [share]
gap = collections.defaultdict(lambda: collections.defaultdict(list))     # good -> year -> [mean gOwn-grade]
ubi = collections.defaultdict(list)   # key -> [share]
firstfloor = collections.defaultdict(list)   # seed -> [year]
for sd in SEEDS:
    snaps, owner, bon = load(RAW, sd)
    mc = load_minecap(RAW, sd)
    base = simulate(snaps, bon)
    # 突き合わせ：国×財で推しの品位の産出の重みの平均 vs minecap の gradeOutW（格を足し合わせる）
    eng = collections.defaultdict(lambda: [0.0, 0.0])
    for (y, n, g, c), a in mc.items():
        e = eng[(y, n, g)]; e[0] += a[0]; e[1] += a[1]
    rec = collections.defaultdict(lambda: [0.0, 0.0])
    for y, regs in base.items():
        for reg, d in regs.items():
            n = owner.get((y, reg))
            for g, o in d['out'].items():
                r = rec[(y, n, g)]; r[0] += o; r[1] += o*d['grade']
    for k, (o, og) in rec.items():
        if k not in eng or not (o > 0): continue
        eo, eg = eng[k]
        if not (eo > 0): continue
        outr.append(o/eo)
        if k[0] == min(snaps): continue   # 最初の断面は初期の捕え方だけで決まる（推しは常に 1）
        val[k[0]].append((abs(og/o - eg/eo), eo)); sgn.append(og/o - eg/eo); sgy[k[0]].append(og/o - eg/eo)
        if eg/eo > FLOOR + 0.01: valnf.append(abs(og/o - eg/eo))
    # Q2 床の州（その断面で鉱物の産出が正の州）
    variants = {'base': base, 'exclUbi': simulate(snaps, bon, exclude_ubi=True), 'prop': simulate(snaps, bon, mode='prop'),
                'y200': simulate(snaps, bon, years=200.0), 'propExUbi': simulate(snaps, bon, mode='prop', exclude_ubi=True)}
    for vn, res in variants.items():
        for y in YRS:
            if y not in res: continue
            act = [d for d in res[y].values() if sum(d['out'].values()) > 0]
            floor[vn][y].append((sum(1 for d in act if d['grade'] <= FLOOR+1e-9), len(act)))
            if vn in ('base', 'exclUbi', 'prop'):
                # 鉱物ごとの在庫の反実仮想：州の主の財（その断面で産出の最大の財）の品位が床
                nf = 0
                for d in act:
                    gm = max(d['out'], key=d['out'].get)
                    if gm in d['gOwn'] and d['gOwn'][gm] <= FLOOR+1e-9: nf += 1
                floor[vn+'_perMin'][y].append((nf, len(act)))
    # 床に届いた最初の断面の年
    seen = set()
    for y in sorted(base):
        for reg, d in base[y].items():
            if reg not in seen and d['grade'] <= FLOOR+1e-9: seen.add(reg); firstfloor[sd].append(y)
    # Q1 害：品位＜1 かつ 累積の掘りの半分超が他の鉱物
    for y in YRS:
        if y not in base: continue
        num = collections.Counter(); den = collections.Counter(); gs = collections.Counter()
        for d in base[y].values():
            ct = sum(d['cum'].values())
            for g, o in d['out'].items():
                den[g] += o
                if ct > 0 and d['grade'] < 1.0 and (ct - d['cum'].get(g, 0.0)) > 0.5*ct: num[g] += o
                if g in d['gOwn']: gs[g] += o*(d['gOwn'][g] - d['grade'])
        for g in den:
            if den[g] > 0: harm[g][y].append(num[g]/den[g]); gap[g][y].append(gs[g]/den[g])
    # Q3 遍在の 3 財の割合（産出・累積の掘り）
    y0 = min(snaps)
    for y in [y0, 1850, 1913]:
        if y not in base: continue
        to = sum(sum(d['out'].values()) for d in base[y].values()); uo = sum(o for d in base[y].values() for g, o in d['out'].items() if g in UBI)
        tc = sum(sum(d['cum'].values()) for d in base[y].values()); uc = sum(o for d in base[y].values() for g, o in d['cum'].items() if g in UBI)
        ubi[('out', y if y != y0 else 'first')].append(uo/to if to else float('nan'))
        if y != y0: ubi[('cum', y)].append(uc/tc if tc else float('nan'))
def md(v): return f'{S.median(v):.3f}' if v else 'NA'
def q(v):
    v = sorted(v); return f'中央 {md(v)}・最小 {v[0]:.3f}・最大 {v[-1]:.3f}' if v else 'NA'
P('# (1183) 推しの数（種', SEEDS, '・生', RAW, '）')
P('## 突き合わせ（★(1136)）：国×財×断面の年（最初の断面を除く）で推しの品位の産出の重みの平均と minecap_s の gradeOutW の差')
allv = [x for y in val for x in val[y]]
P(f'行 {len(allv)}・|差|≦0.02 {sum(1 for d,_ in allv if d<=0.02)}・≦0.05 {sum(1 for d,_ in allv if d<=0.05)}・≦0.10 {sum(1 for d,_ in allv if d<=0.10)}・産出の重みで ≦0.05 の割合 {sum(o for d,o in allv if d<=0.05)/sum(o for _,o in allv):.3f}')
for y in sorted(val):
    v = val[y]; P(f'  {y}: 行 {len(v)}・|差|の中央 {S.median([d for d,_ in v]):.3f}・≦0.05 {sum(1 for d,_ in v if d<=0.05)/len(v):.3f}' + f'・符号つきの中央 {S.median(sgy[y]):.3f}')
P(f'符号つきの差（推し−エンジン）：{q(sgn)}・エンジンの品位が床より上の行 {len(valnf)}・その |差|≦0.05 の割合 {sum(1 for d in valnf if d<=0.05)/max(1,len(valnf)):.3f}・|差|の中央 {md(valnf)}')
P(f'産出の比（推しの州の和÷minecap の国×財の和）：{q(outr)}')
P('## Q2 床の州（その断面で鉱物の産出が正の州・推し）：種ごとの 床/全 と 床の割合')
for vn in floor:
    for y in YRS:
        if y in floor[vn]:
            v = floor[vn][y]; P(f'  {vn} {y}: ' + ' '.join(f'{a}/{b}' for a,b in v) + f'｜割合 {q([a/b for a,b in v if b])}')
P('## 床に届いた最初の断面の年（州の数・種ごと）')
for sd in SEEDS:
    c = collections.Counter(firstfloor[sd]); P(f'  種{sd}: ' + ' '.join(f'{y}:{c[y]}' for y in sorted(c)))
P('## Q1 害の割合（品位＜1 かつ 累積の掘りの半分超が他の鉱物・g の産出の重み）と 鉱物ごとの在庫の品位−合算の品位（産出の重みの平均）')
for g in MIN:
    if g not in harm: continue
    P(f'  {g}: ' + ' ／ '.join(f'{y} 害 {md(harm[g][y])} 差 {md(gap[g][y])}' for y in YRS if harm[g].get(y)))
P('## Q3 遍在の 3 財の割合')
for k in ubi: P(f'  {k}: {q(ubi[k])}')
open(OUT, 'w', encoding='utf-8', newline='\n').write('\n'.join(L) + '\n')
