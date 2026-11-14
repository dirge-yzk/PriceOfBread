# (1142) C8c・C8d を〈国の中の対照〉で数え直す：国ごとに、L が前の 4 四半期の平均の 0.8 倍以下の四半期の平均 nowRule0Share の 4 四半期の変化 −
#   同じ国の 0.95〜1.05 倍の四半期の変化、を落ちの件数で重みを付けて国の間で平均（国の顔ぶれの交絡を外す＝q_c8d_why.txt）。
#   O＝0／O＞0 は落ちの四半期の O で分ける（oEngNationIndex が有限ならそれ・無ければ影の oNationIndex）。平時＝その四半期と 4 四半期前の全てで
#   wNationIndex＝0 かつ wEngageNationIndex＝0。種ごと→種の中央。
# 呼び方：python -X utf8 -I q_c8within.py <出力フォルダ> base=<生> n1=<生> n2=<生> armB=<生>
import sys, os, math, statistics as st
from collections import defaultdict
outd = sys.argv[1]; arms = [a.split('=', 1) for a in sys.argv[2:]]
def fl(s):
    try: return float(s)
    except ValueError: return float('nan')
def fin(*v): return all(not (math.isnan(x) or math.isinf(x)) for x in v)
out = open(os.path.join(outd, 'q_c8within.txt'), 'w', encoding='utf-8', newline='\n')
def P(*a): print(*a, file=out)
summ = {}
for lb, ins in arms:
    res = defaultdict(list)   # (peaceOnly, Ogrp) -> 種ごとの値
    for seed in range(1, 11):
        p = os.path.join(ins, f'rulepower_s{seed}.tsv')
        if not os.path.exists(p): continue
        natq = defaultdict(dict)
        with open(p, encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
            iq, inn, i0, iw, ie, iL, iO, iOe = ix['quarter'], ix['nation'], ix['nowRule0Share'], ix['wNationIndex'], ix['wEngageNationIndex'], ix['lNationIndex'], ix['oNationIndex'], ix.get('oEngNationIndex')
            for ln in f:
                c = ln.rstrip('\n').split('\t')
                q = int(c[iq]); nat = c[inn]
                e = natq[q].get(nat)
                if e is None:
                    O = fl(c[iOe]) if iOe is not None else float('nan')
                    if not fin(O): O = fl(c[iO])
                    e = natq[q][nat] = [fl(c[iL]), O, 0.0, 0, fl(c[iw]) == 0 and fl(c[ie]) == 0]
                n0 = fl(c[i0])
                if fin(n0): e[2] += n0; e[3] += 1
        qs = sorted(natq)
        for peaceOnly in (False, True):
            per = defaultdict(lambda: {'d0': [], 'd1': [], 's': []})   # nation -> 落ち（O＝0・O＞0）・変わらない
            for qi, q in enumerate(qs):
                if qi < 4: continue
                for nat, v in natq[q].items():
                    back = [natq[qs[qi - j]].get(nat) for j in range(1, 5)]
                    if any(b is None for b in back) or v[3] == 0 or back[3][3] == 0: continue
                    if peaceOnly and not (v[4] and all(b[4] for b in back)): continue
                    mL = sum(b[0] for b in back) / 4
                    if not (mL > 0 and fin(v[0], v[1])): continue
                    r = v[0] / mL; dt = v[2] / v[3] - back[3][2] / back[3][3]
                    if r <= 0.8: per[nat]['d0' if v[1] == 0 else 'd1'].append(dt)
                    elif 0.95 <= r <= 1.05: per[nat]['s'].append(dt)
            for g in ('d0', 'd1'):
                num = den = 0.0
                for nat, d in per.items():
                    if d[g] and d['s']:
                        num += len(d[g]) * (sum(d[g]) / len(d[g]) - sum(d['s']) / len(d['s'])); den += len(d[g])
                if den > 0: res[(peaceOnly, g)].append(num / den)
                res[(peaceOnly, g + 'n')].append(den)
    summ[lb] = {k: (st.median(v) if v else float('nan')) for k, v in res.items()}
    for k in sorted(res, key=str):
        P(f'{lb}\t平時だけ={k[0]}\t{k[1]}\t中央 {summ[lb][k]:.4g}\t種ごと ' + ' '.join(f'{x:.3g}' for x in res[k]))
for peaceOnly in (False, True):
    for g in ('d0', 'd1'):
        vs = [summ[a][(peaceOnly, g)] for a in ('base', 'n1', 'n2') if a in summ]
        nm = 'C8c′（O＝0）' if g == 'd0' else 'C8d′（O＞0）'
        P(f'{nm} 平時だけ={peaceOnly} ヌル腕 ' + ' '.join(f'{v:.4g}' for v in vs) + f'  最小−幅 {min(vs) - (max(vs) - min(vs)):.4g}  最大＋幅 {max(vs) + (max(vs) - min(vs)):.4g}' + (f'  腕B {summ["armB"][(peaceOnly, g)]:.4g}' if 'armB' in summ else ''))
out.close()
