# (1183) エンジンの値だけで数える（推しなし）：minecap_s の床の産出の割合・格 N（非産地）の石炭と鉄鉱石の産出・nationgood_s の越境の鉱物の輸入。
# ヌル腕の幅：minecap は keep_1150/n1・n2（同じ木どうしの比）、nationgood は keep_1178 の base と n1・n2。
# 使い方: python -X utf8 -I an2.py <出力>
import sys, csv, collections, statistics as S
csv.field_size_limit(10**9)
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
def agg(raw):
    o=collections.defaultdict(float); fl=collections.defaultdict(float); oN=collections.defaultdict(float); oNci=collections.defaultdict(float); oci=collections.defaultdict(float)
    for sd in seeds:
        a=minecap(raw, sd)
        for y in YRS:
            o[y]+=sum(a[0][y].values()); fl[y]+=sum(a[1][y].values())
            for g in ('石炭','鉄鉱石'): oNci[y]+=a[2][y][g]; oci[y]+=a[0][y][g]
    return {y:(fl[y]/o[y] if o[y] else float('nan'), oNci[y]/oci[y] if oci[y] else float('nan'), oci[y]) for y in YRS}
A1=agg(K50+'/n1'); A2=agg(K50+'/n2'); B=agg(K78+'/base_ins')
for y in YRS:
    w=[max(A1[y][i]/A2[y][i], A2[y][i]/A1[y][i]) for i in range(3)]
    P(f'{y}: 床の割合 基線 {B[y][0]:.4f}・n1 {A1[y][0]:.4f}・n2 {A2[y][0]:.4f}（幅 {w[0]:.4f}）｜格Nの石炭+鉄鉱石の割合 基線 {B[y][1]:.4f}（幅 {w[1]:.4f}）｜石炭+鉄鉱石の産出 基線 {B[y][2]:.0f}（幅 {w[2]:.4f}）')
def iagg(raw):
    t=collections.defaultdict(float)
    for sd in seeds:
        im=imports(raw, sd)
        for y in (1850,1880,1913,1950): t[y]+=sum(im[y][g] for g in MIN)
    return t
Ib=iagg(K78+'/base_ins'); I1=iagg(K78+'/n1'); I2=iagg(K78+'/n2')
for y in (1850,1880,1913,1950):
    P(f'越境の鉱物の輸入 14 財 10 種の和 {y}: 基線 {Ib[y]:.0f}・n1/基線 {I1[y]/Ib[y]:.4f}・n2/基線 {I2[y]/Ib[y]:.4f}')
open('D:/PoB_work/r1183_work/out_an4.txt','w',encoding='utf-8',newline='\n').write('\n'.join(L)+'\n')
