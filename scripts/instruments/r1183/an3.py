# (1183) 鉱物ごとに〈開幕の産出の何年分を、その年までに掘ったか〉＝累積の掘り÷(12×最初の断面の月の産出)（推し・台形）。
# 鉱物ごとの在庫で、年数 Y を置いたときに床（残り 0）・膝（残り 0.5）に届く州の割合を静的に読む材料（産出の道は固定＝反作用なし）。
# 使い方: python -X utf8 -I an3.py <生のフォルダ> <種の列> <出力>
import sys, os, collections, statistics as S
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from dep_rec import load, simulate, MIN
RAW, SEEDS, OUT = sys.argv[1], [int(s) for s in sys.argv[2].split(',')], sys.argv[3]
YS = [1850, 1913, 1950]
ratio = collections.defaultdict(lambda: collections.defaultdict(list))   # good -> year -> [(R, outAtYear)]
for sd in SEEDS:
    snaps, owner, bon = load(RAW, sd)
    y0 = min(snaps)
    res = simulate(snaps, bon, years=1.0)   # years=1 ＝ init_g＝12×最初の月の産出
    first = {}
    for y in sorted(snaps):
        for reg, c in snaps[y].items():
            for g, o in c.items():
                if o > 0 and (reg, g) not in first: first[(reg, g)] = (y, o)
    for y in YS:
        if y not in res: continue
        for reg, d in res[y].items():
            for g, cum in d['cum'].items():
                f = first.get((reg, g))
                if not f or f[0] != y0: continue   # 開幕から掘っている州だけ（後から開いた州は別）
                ratio[g][y].append((cum/(12*f[1]), d['out'].get(g, 0.0)))
L = [f'# (1183) 累積の掘り÷開幕の年率（年）・州×鉱物（開幕の断面 から掘る州）・種 {SEEDS}・推し']
def wq(v, p):
    v = sorted(v); tot = sum(o for _, o in v); acc = 0
    for r, o in v:
        acc += o
        if acc >= p*tot: return r
    return float('nan')
for g in MIN:
    if g not in ratio: continue
    parts = []
    for y in YS:
        v = ratio[g][y]
        if not v: continue
        rs = sorted(r for r, _ in v)
        parts.append(f'{y}: 州 {len(v)}・中央 {S.median(rs):.0f}・90% {rs[int(0.9*(len(rs)-1))]:.0f}・産出の重みの中央 {wq(v,0.5):.0f}・重みの90% {wq(v,0.9):.0f}・>100 の割合 {sum(1 for r in rs if r>100)/len(rs):.2f}・>50 {sum(1 for r in rs if r>50)/len(rs):.2f}')
    L.append(f'{g}: ' + ' ／ '.join(parts))
open(OUT, 'w', encoding='utf-8', newline='\n').write('\n'.join(L) + '\n')
