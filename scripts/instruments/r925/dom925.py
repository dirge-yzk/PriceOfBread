# (925) c80dm 読むだけの台本：割譲・接収・昇格・独立での Region.Dominance の渡り方と、割譲の後の Dominance と Rule[0] の戻り（盤面 m919 の生）。
# 使い方: python -X utf8 -I dom925.py <生のフォルダ> <出力の txt>
# 読む表: cederule_s*.tsv（事象の前後）・rule_s*.tsv（州ごと・四半期ごとの dominanceShare と ruleStateShare）・secession_s*.tsv（marginIndex＝引き留め−圧力・goFlag）。
# エンジンの関数は呼ばない。持ち主は rule_s の nation 列（四半期の行がある国）で数える。
import sys, os, glob, statistics

src, outp = sys.argv[1], sys.argv[2]
seeds = sorted(int(os.path.basename(p)[len('cederule_s'):-4]) for p in glob.glob(os.path.join(src, 'cederule_s*.tsv')))
TOP = 0.999
Q4, Q10 = 16, 40

def q(vals, p):
    if not vals: return float('nan')
    s = sorted(vals); i = min(len(s) - 1, max(0, int(round(p * (len(s) - 1)))))
    return s[i]

def dist(name, vals):
    if not vals: return f'{name}\tn 0'
    return f'{name}\tn {len(vals)}\tp10 {q(vals,.1):.4f}\tmed {q(vals,.5):.4f}\tp90 {q(vals,.9):.4f}\t<{TOP} {sum(1 for v in vals if v < TOP)}\t==0 {sum(1 for v in vals if not (v > 0))}'

def fnum(s):
    try: return float(s)
    except ValueError: return float('nan')

out = []
agg = {}
def A(k, v): agg.setdefault(k, []).append(v)
cnt = {}
def C(k, n=1): cnt[k] = cnt.get(k, 0) + n

for sd in seeds:
    # 事象
    ev = []
    with open(os.path.join(src, f'cederule_s{sd}.tsv'), encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            c = line.rstrip('\n').split('\t')
            if len(c) < len(h) or c[0] == 'quarter': continue
            ev.append(dict(q=int(c[ix['quarter']]), how=c[ix['how']], snd=c[ix['sender']], rcv=c[ix['receiver']], reg=c[ix['region']],
                           tb=c[ix['tierBefore']], ta=c[ix['tierAfter']], db=fnum(c[ix['dominanceBeforeShare']]), da=fnum(c[ix['dominanceAfterShare']]),
                           r0b=fnum(c[ix['ruleStateBeforeShare']]), r0a=fnum(c[ix['ruleStateAfterShare']]), own=c[ix['ownerAtQuarterEnd']]))
    # 州の系列（rule_s）
    ser = {}   # (nation, region) -> {q: (dom, r0, tier)}
    with open(os.path.join(src, f'rule_s{sd}.tsv'), encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        iq, ina, ire, iti, ido, ir0 = ix['quarter'], ix['nation'], ix['region'], ix['tier'], ix['dominanceShare'], ix['ruleStateShare']
        for line in f:
            c = line.split('\t')
            if c[0] == 'quarter': continue
            d = ser.setdefault((c[ina], c[ire]), {})
            qq = int(c[iq])
            if qq not in d: d[qq] = (fnum(c[ido]), fnum(c[ir0]), c[iti])
    # 引き留め−圧力（secession_s）：割譲・返還の州だけ
    want = {(e['rcv'], e['reg']) for e in ev if e['how'] in ('Ceded', 'Returned')}
    openCol = {k for k, s in ser.items() if 0 in s and s[0][2] == 'Colonial'}
    want |= openCol
    sec = {}
    with open(os.path.join(src, f'secession_s{sd}.tsv'), encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        iq, ina, ire, imr, igo = ix['quarter'], ix['nation'], ix['region'], ix['marginIndex'], ix['goFlag']
        for line in f:
            c = line.split('\t')
            if c[0] == 'quarter': continue
            k = (c[ina], c[ire])
            if k in want: sec.setdefault(k, {})[int(c[iq])] = (fnum(c[imr]), c[igo] == '1')
    # 1. 事象の種類ごとの前後
    for e in ev:
        A(f"{e['how']}\tdom_before", e['db']); A(f"{e['how']}\tdom_after", e['da'])
        A(f"{e['how']}\tr0_before", e['r0b']); A(f"{e['how']}\tr0_after", e['r0a'])
        C(f"{e['how']}\tn")
        if e['how'] == 'Ceded':
            C(f"Ceded\ttierBefore={e['tb']}")
    # 2. 割譲・返還の後の系列
    for e in ev:
        if e['how'] not in ('Ceded', 'Returned'): continue
        k = (e['rcv'], e['reg']); s = ser.get(k)
        if s is None: C(f"{e['how']}\tno_track"); continue
        # 持ち主が続く間＝この事象の後、同じ州の名の次の事象（種類を問わない）の前まで。rule_s と secession_s は
        #   計器の選んだ国（picked）だけ毎四半期・他の国は年の末（q%4==3）だけ＝行は間引き。時間は四半期の番号の差で数える（分解能は最大 1 年）。
        nxt = min((x['q'] for x in ev if x['reg'] == e['reg'] and x['q'] > e['q']), default=10**9)
        tr = [(t, s[t]) for t in sorted(s) if e['q'] < t < nxt]
        if not tr: C(f"{e['how']}\tno_row_after"); continue
        sk = sec.get(k, {})
        H10 = e['q'] + Q10
        # Dominance が上限に届くまで
        if e['da'] < TOP:
            hit = next((tt for tt, (d, _, _) in tr if d >= TOP), None)
            if hit is not None: A(f"{e['how']}\tyears_to_top(after<top)", (hit - e['q']) / 4.0)
            else: C(f"{e['how']}\tnever_top_censored(after<top)")
            lo = min((d for t_, (d, _, _) in tr if t_ <= H10 and d == d), default=float('nan'))
            if lo == lo: A(f"{e['how']}\tmin_dom_10y(after<top)", lo)
            gq = next((tt for tt in sorted(sk) if e['q'] < tt <= H10 and tt < nxt and sk[tt][1]), None)
            if gq is not None: C(f"{e['how']}\tgo_within_10y(after<top)")
        else:
            drop = next((tt for tt, (d, _, _) in tr if tt <= H10 and d < TOP), None)
            if drop is not None: C(f"{e['how']}\tleft_top_within_10y(after>=top)"); A(f"{e['how']}\tmin_dom_10y(after>=top,left)", min(d for t_, (d, _, _) in tr if t_ <= H10 and d == d))
            else: C(f"{e['how']}\tstayed_top_10y_or_censored(after>=top)")
            gq = next((tt for tt in sorted(sk) if e['q'] < tt <= H10 and tt < nxt and sk[tt][1]), None)
            if gq is not None: C(f"{e['how']}\tgo_within_10y(after>=top)")
        # 引き留め−圧力の符号（10 年・持ち主が続く間）
        ms = [sk[tt][0] for tt in sorted(sk) if e['q'] < tt <= H10 and tt < nxt and sk[tt][0] == sk[tt][0]]
        if ms:
            A(f"{e['how']}\tmargin_med_10y", statistics.median(ms))
            A(f"{e['how']}\tmargin_negshare_10y", sum(1 for m in ms if m < 0) / len(ms))
        # Rule[0] の戻り（割譲だけ・前の値の 50%・90%）
        if e['how'] == 'Ceded' and e['r0b'] > 1e-6:
            for frac, lab in ((0.5, '50'), (0.9, '90')):
                hit = next((tt for tt, (_, r0, _) in tr if r0 >= frac * e['r0b']), None)
                if hit is not None: A(f"Ceded\tyears_r0_to_{lab}pct_before", (hit - e['q']) / 4.0)
                else: C(f"Ceded\tr0_{lab}pct_censored")
            for yy, lab in ((Q4, '4y'), (Q10, '10y')):
                at = next(((t_, v) for t_, v in tr if e['q'] + yy <= t_ < e['q'] + yy + 4), None)
                if at is not None: A(f'Ceded\tr0_{lab}', at[1][1]); A(f'Ceded\tdom_{lab}', at[1][0])
            A('Ceded\ttrack_years', (tr[-1][0] - e['q']) / 4.0)
    # 3. 開幕の植民地格（四半期 0 に Colonial の行）と、他の州の四半期 0
    for (na, re), s in ser.items():
        if 0 not in s: continue
        d0, r00, t0 = s[0]
        A(f"open_{t0}\tdom_q0", d0)
        if t0 == 'Colonial':
            hit = next((tt for tt in sorted(s) if s[tt][0] >= TOP), None)
            if hit is not None: A('open_Colonial\tyears_to_top', hit / 4.0)
            else: C('open_Colonial\tnever_top')
            at10 = next((s[t] for t in sorted(s) if 40 <= t < 44), None)
            at30 = next((s[t] for t in sorted(s) if 120 <= t < 124), None)
            if at10 is not None: A('open_Colonial\tdom_10y', at10[0])
            if at30 is not None: A('open_Colonial\tdom_30y', at30[0])
            sk = sec.get((na, re), {})
            if any(sk[t][1] for t in sk if t < 40): C('open_Colonial\tgo_within_10y')
            if any(sk[t][1] for t in sk): C('open_Colonial\tgo_ever_same_owner')
            ms = [sk[t][0] for t in sorted(sk) if t < 8 and sk[t][0] == sk[t][0]]
            if ms: A('open_Colonial\tmargin_med_first2y', statistics.median(ms))
    # 4. 跳び（州の名ごとに国を問わず続く 2 行で、四半期の差 dt に対し +0.065×dt を超える＝積分の 1 歩の上限 0.025×(1+1.43)≈0.061 を超える＝積分でない書き手＝独立の確定 Rehome(Sovereign)）
    byName = {}
    for (na, re), s in ser.items():
        for t, v in s.items(): byName.setdefault(re, {}).setdefault(t, []).append((na, v[0], v[2]))
    for re, d in byName.items():
        ks = sorted(t for t in d if len(d[t]) == 1)
        for a, b in zip(ks, ks[1:]):
            da, db = d[a][0][1], d[b][0][1]
            if db - da > 0.065 * (b - a):
                C('jump\tn'); A('jump\tdom_before_jump', da); A('jump\tdom_after_jump', db)
                C(f"jump\tsame_nation_name={'1' if d[a][0][0] == d[b][0][0] else '0'}")
                C(f"jump\ttier_after={d[b][0][2]}")
    # 5. 全ての州×四半期で Dominance が上限に張り付く割合
    tot = top = 0
    for s in ser.values():
        for d, _, _ in s.values():
            tot += 1; top += d >= TOP
    C('all\tregion_quarters', tot); C('all\tregion_quarters_at_top', top)

out.append(f'# (925) c80dm Dominance の渡り方と戻り（{src}・種 {seeds}）・上限の判定 {TOP}')
for k in sorted(cnt): out.append(f'{k}\t{cnt[k]}')
for k in sorted(agg): out.append(dist(k, agg[k]))
with open(outp, 'wb') as f: f.write(('\n'.join(out) + '\n').encode('utf-8'))
print('done', outp, len(out))
