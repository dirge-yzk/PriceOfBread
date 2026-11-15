# (1151) c113cr：rponset_s（(1147) の頭の区分の表）から C15〈尾の質量〉と G1′〈遠い漂い〉を数える。表を読むだけ・エンジンを呼ばない。
#   C15＝pair 行で tailFlag＝1（classHead＝1〈①だけ〉かつ 0＜sHead＜0.01）かつ dataStateFlag＝0 の tailMass（＝sHead）の、四半期ごとの和の
#        四半期の平均（四半期の数＝表の最初の四半期から最後の四半期まで）・種の中央。
#   G1′＝pair 行で classHead＝1・tier＝Core・ownerAtWarFlag＝0・sEnd≧0.25・dataStateFlag＝0 の州（region の名で数える）の数・種×四半期の最大の、種ごとの和。
#   記録の列：同じ定義の C15 をデータの州だけ（dataStateFlag＝1）・講和の付与から始まった対（それより前の四半期に classHead＝4 で出た (州, 外国)）だけで。
# 使い方（台本の部品）：import onset_cr; onset_cr.onset(<表のフォルダ>, seeds)
import os, statistics as st, collections

def fl(x):
    try: return float(x)
    except ValueError: return float('nan')

def onset(folder, seeds=range(1, 11)):
    tails, tails_data, tails_grant, g1s = [], [], [], []
    nseen = 0
    for s in seeds:
        p = os.path.join(folder, f'rponset_s{s}.tsv')
        if not os.path.exists(p): continue
        nseen += 1
        byq = collections.Counter(); byq_d = collections.Counter(); byq_g = collections.Counter(); g1q = collections.defaultdict(set)
        qmin = qmax = None; granted = set()
        with open(p, encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); ix = {k: j for j, k in enumerate(h)}
            iq, ik, ic, ish, ise, ita, itm, ids, iow, iti, ir = (ix[k] for k in ('quarter', 'rowKind', 'classHead', 'sHead', 'sEnd', 'tailFlag', 'tailMass', 'dataStateFlag', 'ownerAtWarFlag', 'tier', 'region'))
            for line in f:
                c = line.rstrip('\n').split('\t')
                q = int(c[iq]); qmin = q if qmin is None else min(qmin, q); qmax = q if qmax is None else max(qmax, q)
                if c[ik] != 'pair': continue
                ds = c[ids] == '1'; cls = c[ic]; sh = fl(c[ish])
                key = (c[ir], c[ix['foreignKey']])
                if c[ita] == '1':
                    if ds: byq_d[q] += fl(c[itm])
                    else: byq[q] += fl(c[itm])
                    if key in granted and not ds: byq_g[q] += fl(c[itm])
                if cls == '4': granted.add(key)
                if cls == '1' and not ds and c[iti] == 'Core' and c[iow] == '0' and fl(c[ise]) >= 0.25: g1q[q].add(c[ir])
        nq = (qmax - qmin + 1) if qmin is not None else 0
        tails.append(sum(byq.values()) / nq if nq else 0.0)
        tails_data.append(sum(byq_d.values()) / nq if nq else 0.0)
        tails_grant.append(sum(byq_g.values()) / nq if nq else 0.0)
        g1s.append(max((len(v) for v in g1q.values()), default=0))
    med = lambda v: st.median(v) if v else float('nan')
    return {'C15': med(tails), 'C15_data': med(tails_data), 'C15_grant': med(tails_grant), 'G1p_sum': sum(g1s), 'G1p_per': g1s, 'seeds': nseen}

def upper(vals):
    """上側＝最大×(1＋2×(幅−1))・幅＝最大÷最小（最小が 0 なら 2×最大）。"""
    mx, mn = max(vals), min(vals)
    if not (mn > 0): return 2 * mx
    return mx * (1 + 2 * (mx / mn - 1))

def c8c(folder, seeds=range(1, 11)):
    """C8c″・C8d″（r1142/q_c8fixed.py の定義の写し・平時に限らない）：L が前の 4 四半期の平均の 0.8 倍以下（落ち）と 0.95〜1.05 倍（変わらない）の国×四半期で、
    q と q−4 の両方で同じ国が持つ州の nowRule0Share の変化の平均を、国の中で 落ち−変わらない（落ちの件数で重み）・種の中央。O＝0（d0＝C8c″）と O＞0（d1＝C8d″）。
    O は oEngNationIndex（無いか nan なら影の oNationIndex）。"""
    import collections, math
    def fin(*v): return all(not (math.isnan(x) or math.isinf(x)) for x in v)
    res = {'d0': [], 'd1': []}
    for seed in seeds:
        p = os.path.join(folder, f'rulepower_s{seed}.tsv')
        if not os.path.exists(p): continue
        nat = collections.defaultdict(dict); reg = collections.defaultdict(dict)
        with open(p, encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
            iq, inn, ir, i0, iL, iO, iOe = ix['quarter'], ix['nation'], ix['region'], ix['nowRule0Share'], ix['lNationIndex'], ix['oNationIndex'], ix.get('oEngNationIndex')
            for ln in f:
                c = ln.rstrip('\n').split('\t')
                q = int(c[iq]); n = c[inn]
                if n not in nat[q]:
                    O = fl(c[iOe]) if iOe is not None else float('nan')
                    if not fin(O): O = fl(c[iO])
                    nat[q][n] = [fl(c[iL]), O]; reg[q][n] = {}
                v = fl(c[i0])
                if fin(v): reg[q][n][c[ir]] = v
        qs = sorted(nat)
        per = collections.defaultdict(lambda: {'d0': [], 'd1': [], 's': []})
        for qi, q in enumerate(qs):
            if qi < 4: continue
            for n, v in nat[q].items():
                back = [nat[qs[qi - j]].get(n) for j in range(1, 5)]
                if any(b is None for b in back): continue
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
                if d[g] and d['s']: num += len(d[g]) * (sum(d[g]) / len(d[g]) - sum(d['s']) / len(d['s'])); den += len(d[g])
            if den > 0: res[g].append(num / den)
    med = lambda v: st.median(v) if v else float('nan')
    return (med(res['d0']), med(res['d1']), len(res['d0']))
