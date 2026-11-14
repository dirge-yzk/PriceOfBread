# (1142) C8c・C8d を〈同じ州の集合〉と〈国の中の対照〉で数え直す：nowRule0Share の 4 四半期の変化を、q と q−4 の両方で同じ国が持つ州だけで平均
#   （州の出入り＝分離・割譲・併合で国の平均が動く交絡を外す）。他は q_c8within.py と同じ（L の比・O の分け・平時・落ちの件数の重み・種の中央）。
# 呼び方：python -X utf8 -I q_c8fixed.py <出力フォルダ> base=<生> n1=<生> n2=<生> armB=<生>
import sys, os, math, statistics as st
from collections import defaultdict
outd = sys.argv[1]; arms = [a.split('=', 1) for a in sys.argv[2:]]
def fl(s):
    try: return float(s)
    except ValueError: return float('nan')
def fin(*v): return all(not (math.isnan(x) or math.isinf(x)) for x in v)
out = open(os.path.join(outd, 'q_c8fixed.txt'), 'w', encoding='utf-8', newline='\n')
def P(*a): print(*a, file=out)
summ = {}
for lb, ins in arms:
    res = defaultdict(list)
    for seed in range(1, 11):
        p = os.path.join(ins, f'rulepower_s{seed}.tsv')
        if not os.path.exists(p): continue
        nat = defaultdict(dict)    # q -> nation -> [L, O, peace]
        reg = defaultdict(dict)    # q -> nation -> {region: nowRule0}
        with open(p, encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
            iq, inn, ir, i0, iw, ie, iL, iO, iOe = ix['quarter'], ix['nation'], ix['region'], ix['nowRule0Share'], ix['wNationIndex'], ix['wEngageNationIndex'], ix['lNationIndex'], ix['oNationIndex'], ix.get('oEngNationIndex')
            for ln in f:
                c = ln.rstrip('\n').split('\t')
                q = int(c[iq]); n = c[inn]
                if n not in nat[q]:
                    O = fl(c[iOe]) if iOe is not None else float('nan')
                    if not fin(O): O = fl(c[iO])
                    nat[q][n] = [fl(c[iL]), O, fl(c[iw]) == 0 and fl(c[ie]) == 0]
                    reg[q][n] = {}
                v = fl(c[i0])
                if fin(v): reg[q][n][c[ir]] = v
        qs = sorted(nat)
        for peaceOnly in (False, True):
            per = defaultdict(lambda: {'d0': [], 'd1': [], 's': []})
            for qi, q in enumerate(qs):
                if qi < 4: continue
                for n, v in nat[q].items():
                    back = [nat[qs[qi - j]].get(n) for j in range(1, 5)]
                    if any(b is None for b in back): continue
                    if peaceOnly and not (v[2] and all(b[2] for b in back)): continue
                    mL = sum(b[0] for b in back) / 4
                    if not (mL > 0 and fin(v[0], v[1])): continue
                    now, old = reg[q][n], reg[qs[qi - 4]][n]
                    both = [r for r in now if r in old]
                    if not both: continue
                    dt = sum(now[r] - old[r] for r in both) / len(both)
                    r_ = v[0] / mL
                    if r_ <= 0.8: per[n]['d0' if v[1] == 0 else 'd1'].append(dt)
                    elif 0.95 <= r_ <= 1.05: per[n]['s'].append(dt)
            for g in ('d0', 'd1'):
                num = den = 0.0
                for n, d in per.items():
                    if d[g] and d['s']:
                        num += len(d[g]) * (sum(d[g]) / len(d[g]) - sum(d['s']) / len(d['s'])); den += len(d[g])
                if den > 0: res[(peaceOnly, g)].append(num / den)
    summ[lb] = {k: (st.median(v) if v else float('nan')) for k, v in res.items()}
    for k in sorted(res, key=str):
        P(f'{lb}\t平時だけ={k[0]}\t{k[1]}\t中央 {summ[lb][k]:.4g}\t種ごと ' + ' '.join(f'{x:.3g}' for x in res[k]))
for peaceOnly in (False, True):
    for g in ('d0', 'd1'):
        vs = [summ[a][(peaceOnly, g)] for a in ('base', 'n1', 'n2') if a in summ]
        nm = 'C8c″（O＝0）' if g == 'd0' else 'C8d″（O＞0）'
        P(f'{nm} 平時だけ={peaceOnly} ヌル腕 ' + ' '.join(f'{v:.4g}' for v in vs) + f'  最小−幅 {min(vs) - (max(vs) - min(vs)):.4g}  最大＋幅 {max(vs) + (max(vs) - min(vs)):.4g}' + (f'  腕B {summ["armB"][(peaceOnly, g)]:.4g}' if 'armB' in summ else ''))
out.close()
