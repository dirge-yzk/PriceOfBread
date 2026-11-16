# (1183) エンジンの値だけで数える（推しなし）：minecap_s の床の産出の割合・格 N（非産地）の石炭と鉄鉱石の産出・nationgood_s の越境の鉱物の輸入。
# ヌル腕の幅：minecap は keep_1150/n1・n2（同じ木どうしの比）、nationgood は keep_1178 の base と n1・n2。
# 使い方: python -X utf8 -I an2.py <出力>
import sys, csv, collections, statistics as S
csv.field_size_limit(10**9)
OUT = sys.argv[1]
MIN = ['鉄鉱石','石炭','銅鉱','鉛鉱','金','硫黄','石灰石','珪砂','粘土','原油','クロム','錫鉱','硝石','銀']
YRS = [1800, 1820, 1850, 1880, 1900, 1913, 1950]
K78 = 'D:/PoB_work/keep_1178'; K50 = 'D:/PoB_work/keep_1150'
L = []
def P(*a): L.append(' '.join(str(x) for x in a))
def f(x):
    try: return float(x)
    except ValueError: return float('nan')
def minecap(raw, sd):
    """year -> dict: floorShare{g}, outN{g}, out{g}, outAll, floorAll"""
    o = collections.defaultdict(collections.Counter); fl = collections.defaultdict(collections.Counter); oN = collections.defaultdict(collections.Counter)
    gw = collections.defaultdict(collections.Counter)
    with open(f'{raw}/minecap_s{sd}.tsv', encoding='utf-8', newline='') as fh:
        rd = csv.reader(fh, delimiter='\t'); h = next(rd); ix = {k:i for i,k in enumerate(h)}
        for r in rd:
            y = int(r[ix['year']]); g = r[ix['good']]; out = f(r[ix['outputSum']])
            if not (out > 0): continue
            o[y][g] += out
            b0 = f(r[ix['gradeBin0']]); gr = f(r[ix['gradeOutW']])
            if b0 == b0: fl[y][g] += out*b0
            if gr == gr: gw[y][g] += out*gr
            if r[ix['class']] == 'N': oN[y][g] += out
    return o, fl, oN, gw
def imports(raw, sd):
    imp = collections.defaultdict(collections.Counter)
    with open(f'{raw}/nationgood_s{sd}.tsv', encoding='utf-8', newline='') as fh:
        rd = csv.reader(fh, delimiter='\t'); h = next(rd); ix = {k:i for i,k in enumerate(h)}
        for r in rd:
            g = r[ix['good']]
            if g not in MIN: continue
            v = f(r[ix['crossImportQty']])
            if v == v: imp[int(r[ix['year']])][g] += v
    return imp
def q(v):
    v = sorted(x for x in v if x == x)
    return f'中央 {S.median(v):.3f}（{v[0]:.3f}〜{v[-1]:.3f}）' if v else 'NA'
seeds = range(1, 11)
base = {sd: minecap(f'{K78}/base_ins', sd) for sd in seeds}
P('# (1183) エンジンの値（minecap_s・nationgood_s）・m1178 種 1〜10')
P('## 床の産出の割合（gradeBin0×産出÷産出）・全ての鉱物の和と主な財')
for gset, nm in [(MIN, '全鉱物'), (['石炭'], '石炭'), (['鉄鉱石'], '鉄鉱石'), (['金'], '金'), (['銀'], '銀'), (['粘土','珪砂','石灰石'], '遍在の3財')]:
    row = []
    for y in YRS:
        v = []
        for sd in seeds:
            o, fl, oN, gw = base[sd]
            to = sum(o[y][g] for g in gset)
            if to > 0: v.append(sum(fl[y][g] for g in gset)/to)
        row.append(f'{y} {q(v)}')
    P(f'  {nm}: ' + ' ／ '.join(row))
P('## 品位の産出の重みの平均（gradeOutW）・全鉱物')
P('  ' + ' ／ '.join(f"{y} {q([sum(base[sd][3][y].values())/sum(base[sd][0][y].values()) for sd in seeds if sum(base[sd][0][y].values())>0])}" for y in YRS))
P('## 格 N（非産地 0.4）の石炭と鉄鉱石の産出÷その財の世界の産出（m1178）')
for g in ['石炭', '鉄鉱石']:
    P(f'  {g}: ' + ' ／ '.join(f"{y} {q([base[sd][2][y][g]/base[sd][0][y][g] for sd in seeds if base[sd][0][y][g]>0])}" for y in YRS))
# ヌル腕の幅（keep_1150 の n1 と n2 の比）
P('## ヌル腕の幅（keep_1150 の n2÷n1・種ごと・max(比,1/比) の種の中で最大）')
n1 = {sd: minecap(f'{K50}/n1', sd) for sd in seeds}; n2 = {sd: minecap(f'{K50}/n2', sd) for sd in seeds}
def width(fn):
    out = {}
    for y in YRS:
        rs = []
        for sd in seeds:
            a, b = fn(n1[sd], y), fn(n2[sd], y)
            if a > 0 and b > 0: rs.append(max(a/b, b/a))
        out[y] = (max(rs) if rs else float('nan'), S.median(rs) if rs else float('nan'))
    return out
for nm, fn in [('全鉱物の床の割合', lambda m, y: sum(m[1][y].values())/sum(m[0][y].values()) if sum(m[0][y].values()) > 0 else 0),
               ('全鉱物の品位の平均', lambda m, y: sum(m[3][y].values())/sum(m[0][y].values()) if sum(m[0][y].values()) > 0 else 0),
               ('石炭の産出', lambda m, y: m[0][y]['石炭']), ('鉄鉱石の産出', lambda m, y: m[0][y]['鉄鉱石']),
               ('格Nの石炭の産出', lambda m, y: m[2][y]['石炭']), ('格Nの鉄鉱石の産出', lambda m, y: m[2][y]['鉄鉱石'])]:
    w = width(fn); P(f'  {nm}: ' + ' ／ '.join(f'{y} 最大 {w[y][0]:.3f}・中央 {w[y][1]:.3f}' for y in YRS))
P('## 越境の鉱物の輸入量（nationgood_s crossImportQty・14 財の和と石炭・鉄鉱石）・m1178 と keep_1178 の n1・n2 の比')
bi = {sd: imports(f'{K78}/base_ins', sd) for sd in seeds}
i1 = {sd: imports(f'{K78}/n1', sd) for sd in seeds}; i2 = {sd: imports(f'{K78}/n2', sd) for sd in seeds}
for gset, nm in [(MIN, '14財'), (['石炭'], '石炭'), (['鉄鉱石'], '鉄鉱石')]:
    for y in [1850, 1880, 1913, 1950]:
        lv = [sum(bi[sd][y][g] for g in gset) for sd in seeds]
        rs = []
        for sd in seeds:
            b = sum(bi[sd][y][g] for g in gset)
            for nn in (i1, i2):
                a = sum(nn[sd][y][g] for g in gset)
                if a > 0 and b > 0: rs.append(max(a/b, b/a))
        P(f'  {nm} {y}: 量 {q(lv)}・幅 最大 {max(rs) if rs else float("nan"):.3f}・中央 {S.median(rs) if rs else float("nan"):.3f}')
open(OUT, 'w', encoding='utf-8', newline='\n').write('\n'.join(L) + '\n')
