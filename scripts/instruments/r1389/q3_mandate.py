# (1389) 問い 3：天命 Θ1・Θ3 と、その Θ5 型（状態を消す）を同じ物差しで並べる（22 国・国×四半期・10 種）
#   今 e0＝(Θ−0.5)×2（scalars dynMandate＝ならした後）
#   Θ1 の目標 T1＝Clamp01(0.5 − WantWeight 0.8 × Ḡ)・Ḡ＝dcont livingDropMeanIndex（大衆の層の人口の重み〈推〉：設計は未教育の重み）
#   Θ1s＝Toward(Θ, T1, 0.08/四半期)（始まり 0.5＝Politics.cs:209 の番兵と同じ）→ e1s＝(Θ1s−0.5)×2 ／ Θ6（Θ1 の Θ5 型）e6＝(T1−0.5)×2
#   Θ3s＝Toward(Θ, 1 − D̄, 0.08)・e3s＝Θ3s ／ Θ5 e5＝1 − D̄（D̄＝discontentMeanShare＝上限〈推〉）
import sys; sys.path.insert(0, 'D:/PoB_work/r1389_work/scripts')
from load import *
RATE, WW = 0.08, 0.8
def clamp01(x): return 0.0 if not (x > 0) else (1.0 if x > 1 else x)
names = ('e0', 'e1s', 'e6', 'e3s', 'e5')
E = {k: [] for k in names}; D = {k: [] for k in names}; runs = {k: [] for k in names}; lowleg = {k: [0, 0] for k in names}
agree = {'e1s': [0, 0], 'e6': [0, 0]}; agree_long = {'e1s': [0, 0], 'e6': [0, 0]}
for s in SEEDS:
    sc = {}
    for v in rows('scalars', s): sc.setdefault(v['nation'], {})[int(v['quarter'])] = (fl(v['dynMandate']), fl(v['legitimacy']))
    dc = {}
    for v in rows('dcont', s): dc.setdefault(v['nation'], {})[int(v['quarter'])] = (fl(v['livingDropMeanIndex']), fl(v['discontentMeanShare']))
    for nm, qs in sc.items():
        if nm not in dc: continue
        t1 = t3 = 0.5; prev = {}; cur = {k: 0 for k in names}; ser = []
        for q in sorted(qs):
            if q not in dc[nm]: continue
            th, leg = qs[q]; g, dd = dc[nm][q]
            if not (g == g and dd == dd and th == th and th >= 0): continue
            T1 = clamp01(0.5 - WW * g); t1 = t1 + RATE * (T1 - t1); t3 = t3 + RATE * (clamp01(1 - dd) - t3)
            e = {'e0': (th - 0.5) * 2, 'e1s': (t1 - 0.5) * 2, 'e6': (T1 - 0.5) * 2, 'e3s': t3, 'e5': clamp01(1 - dd)}
            ser.append(e)
            for k in names:
                E[k].append(e[k])
                if k in prev: D[k].append(abs(e[k] - prev[k]))
                prev[k] = e[k]
                if e[k] < 0: cur[k] += 1
                elif cur[k]: runs[k].append(cur[k]); cur[k] = 0
                if leg < 0.4: lowleg[k][0] += 1; lowleg[k][1] += e[k] < 0
        for k in names:
            if cur[k]: runs[k].append(cur[k])
        # 今の暗君の側（e0<0）の長い塊（20 四半期以上）の中で、Θ1・Θ6 も負か
        i = 0
        while i < len(ser):
            if ser[i]['e0'] < 0:
                j = i
                while j < len(ser) and ser[j]['e0'] < 0: j += 1
                for k in agree:
                    hit = sum(1 for x in ser[i:j] if x[k] < 0); agree[k][0] += j - i; agree[k][1] += hit
                    if j - i >= 20: agree_long[k][0] += j - i; agree_long[k][1] += hit
                i = j
            else: i += 1
for k in names:
    a = E[k]; n = len(a); neg = sum(1 for x in a if x < 0); r = runs[k]; tot = sum(r)
    l8 = sum(x for x in r if x >= 8); l20 = sum(x for x in r if x >= 20)
    m = sum(a) / n; ma = sum(abs(x) for x in a) / n
    print(f'{k}: {pct(a, (.01,.1,.5,.9,.99))} 負 {neg/n:.1%} 平均 {m:.3f} |e|平均 {ma:.3f}  負の塊の長さ {pct(r)} 負の四半期のうち 8 期以上の塊 {l8/max(1,tot):.1%}・20 期以上 {l20/max(1,tot):.1%}  |Δe| {pct(D[k])}  正統性<0.4 の四半期で負 {lowleg[k][1]}/{lowleg[k][0]}')
for k in agree: print(f'今の負（e0<0）の四半期で {k} も負: {agree[k][1]}/{agree[k][0]} ({agree[k][1]/max(1,agree[k][0]):.1%})・20 期以上の塊の中: {agree_long[k][1]}/{agree_long[k][0]} ({agree_long[k][1]/max(1,agree_long[k][0]):.1%})')
