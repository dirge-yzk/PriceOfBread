# (1142) C8c・C8d を平時の国×四半期に限って数え直す（L の落ちが講和の後の動員解除と重なる交絡を外す）＋露の極東の港の外国の主（腕 B）
# 呼び方：python -X utf8 -I q_c8peace.py <出力フォルダ> base=<生> n1=<生> n2=<生> armB=<生>
# 平時＝その四半期と 4 四半期前の全てで wNationIndex＝0 かつ wEngageNationIndex＝0（国の最初の行で読む）。他は gates_rc.py:169〜183 と同じ。
import sys, os, math, statistics as st
from collections import defaultdict, Counter
outd = sys.argv[1]; arms = [a.split('=', 1) for a in sys.argv[2:]]
def fl(s):
    try: return float(s)
    except ValueError: return float('nan')
def fin(*v): return all(not (math.isnan(x) or math.isinf(x)) for x in v)
FE = ['オホーツク', 'ペトロパヴロフスク', 'ウラジオストク', 'アナディリ']
out = open(os.path.join(outd, 'q_c8peace.txt'), 'w', encoding='utf-8', newline='\n')
def P(*a): print(*a, file=out)
summ = {}
for lb, ins in arms:
    c8c, c8d, nd0, nd1 = [], [], [], []
    fe = Counter(); fen = Counter()
    for seed in range(1, 11):
        p = os.path.join(ins, f'rulepower_s{seed}.tsv')
        if not os.path.exists(p): continue
        natq = defaultdict(dict)
        with open(p, encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
            iq, inn, ir = ix['quarter'], ix['nation'], ix['region']
            i0, i2, iw, ie, iL, iO, iOe, ik = ix['nowRule0Share'], ix['nowRule2Share'], ix['wNationIndex'], ix['wEngageNationIndex'], ix['lNationIndex'], ix['oNationIndex'], ix.get('oEngNationIndex'), ix['foreignTopKey']
            for ln in f:
                c = ln.rstrip('\n').split('\t')
                q = int(c[iq]); nat = c[inn]
                if lb == 'armB' and nat == 'ロシア' and c[ir] in FE:
                    x = fl(c[i2]); fen[c[ir]] += 1
                    if x >= 0.25: fe[(c[ir], c[ik])] += 1
                e = natq[q].get(nat)
                if e is None:
                    O = fl(c[iOe]) if iOe is not None else float('nan')
                    if not fin(O): O = fl(c[iO])
                    peace = fl(c[iw]) == 0 and fl(c[ie]) == 0
                    e = natq[q][nat] = [fl(c[iL]), O, 0.0, 0, peace]
                n0 = fl(c[i0])
                if fin(n0): e[2] += n0; e[3] += 1
        qs = sorted(natq); d0, s0, d1, s1 = [], [], [], []
        for qi, q in enumerate(qs):
            if qi < 4: continue
            for nat, v in natq[q].items():
                back = [natq[qs[qi - j]].get(nat) for j in range(1, 5)]
                if any(b is None for b in back) or v[3] == 0 or back[3][3] == 0: continue
                if not (v[4] and all(b[4] for b in back)): continue
                mL = sum(b[0] for b in back) / 4
                if not (mL > 0 and fin(v[0], v[1])): continue
                r = v[0] / mL; dt = v[2] / v[3] - back[3][2] / back[3][3]
                grp = (d0, s0) if v[1] == 0 else (d1, s1)
                if r <= 0.8: grp[0].append(dt)
                elif 0.95 <= r <= 1.05: grp[1].append(dt)
        if d0 and s0: c8c.append(sum(d0) / len(d0) - sum(s0) / len(s0))
        if d1 and s1: c8d.append(sum(d1) / len(d1) - sum(s1) / len(s1))
        nd0.append(len(d0)); nd1.append(len(d1))
    m = lambda xs: st.median(xs) if xs else float('nan')
    summ[lb] = (m(c8c), m(c8d))
    P(f'## {lb}  平時の C8c 中央 {m(c8c):.4g}（種 {len(c8c)}・落ちの国×四半期の中央 {m(nd0)}）  C8d 中央 {m(c8d):.4g}（種 {len(c8d)}・{m(nd1)}）')
    P('  C8c 種ごと ' + ' '.join(f'{x:.3g}' for x in c8c)); P('  C8d 種ごと ' + ' '.join(f'{x:.3g}' for x in c8d))
    if fe:
        P('  露の極東の港で x≧0.25 の四半期×種（港・foreignTopKey）：' + '・'.join(f'{a}/{b} {n}' for (a, b), n in fe.most_common(12)))
for j, k in enumerate(['C8c', 'C8d']):
    vs = [summ[a][j] for a in ('base', 'n1', 'n2') if a in summ]
    P(f'{k} 平時 ヌル腕 ' + ' '.join(f'{v:.4g}' for v in vs) + f'  最小−幅 {min(vs) - (max(vs) - min(vs)):.4g}  最大＋幅 {max(vs) + (max(vs) - min(vs)):.4g}' + (f'  腕B {summ["armB"][j]:.4g}' if 'armB' in summ else ''))
out.close()
