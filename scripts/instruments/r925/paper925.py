# (925) c80dm 紙の台本：割譲（Ceded）の直後の Dominance の初期値を案ごとに置き換え、同じ盤面（m919 の生）で記録された
#   新しい持ち主の下の marginIndex（＝holdShareIndex−pressureIndex＝支配度の積分の 1 歩÷DominanceSpeed）で 10 年積分し直す。
#   一次の近似（フィードバック無し）：分離独立が起きれば盤面が変わるが、ここでは記録の margin をそのまま使う。
#   secession_s は picked 以外の国は年の末だけ＝行の無い四半期は直前の行の margin を持ち越す（最初の行の前は最初の行）。
#   案：A＝今（記録の dominanceAfter）／B＝min(前の値, ruleStateBefore)／C＝min(1, 前の値×新の holdShare÷旧の holdShare)／Z＝0（(55) 回1-1 で差し戻した形・対照）。
# 使い方: python -X utf8 -I paper925.py <生のフォルダ> <出力>
import sys, os, glob, statistics
src, outp = sys.argv[1], sys.argv[2]
SPEED, Q10, TOP = 0.025, 40, 0.999
seeds = sorted(int(os.path.basename(p)[len('cederule_s'):-4]) for p in glob.glob(os.path.join(src, 'cederule_s*.tsv')))
def f(s):
    try: return float(s)
    except ValueError: return float('nan')
res = {}
def R(k, v): res.setdefault(k, []).append(v)
cnt = {}
def C(k): cnt[k] = cnt.get(k, 0) + 1
for sd in seeds:
    ev = []
    with open(os.path.join(src, f'cederule_s{sd}.tsv'), encoding='utf-8') as fh:
        h = fh.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in fh:
            c = line.rstrip('\n').split('\t')
            if c[0] == 'quarter' or len(c) < len(h): continue
            ev.append((int(c[0]), c[ix['how']], c[ix['sender']], c[ix['receiver']], c[ix['region']], f(c[ix['dominanceBeforeShare']]), f(c[ix['dominanceAfterShare']]), f(c[ix['ruleStateBeforeShare']])))
    keys = {(e[2], e[4]) for e in ev if e[1] == 'Ceded'} | {(e[3], e[4]) for e in ev if e[1] == 'Ceded'}
    sec = {}
    with open(os.path.join(src, f'secession_s{sd}.tsv'), encoding='utf-8') as fh:
        h = fh.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        im, ih, idm = ix['marginIndex'], ix['holdShareIndex'], ix['dominanceShare']
        for line in fh:
            c = line.split('\t')
            if c[0] == 'quarter': continue
            k = (c[2], c[3])
            if k in keys: sec.setdefault(k, {})[int(c[0])] = (f(c[im]), f(c[ih]), f(c[idm]))
    for (q, how, snd, rcv, reg, db, da, r0b) in ev:
        if how != 'Ceded': continue
        nxt = min((e[0] for e in ev if e[4] == reg and e[0] > q), default=10**9)
        new = sec.get((rcv, reg), {}); old = sec.get((snd, reg), {})
        after = sorted(t for t in new if q < t < nxt and t <= q + Q10 and new[t][0] == new[t][0])
        if not after: C('no_margin_rows'); continue
        before = [t for t in old if t <= q and old[t][1] == old[t][1]]
        hOld = old[max(before)][1] if before else float('nan')
        hNew = new[after[0]][1]
        C('n')
        cand = {'A': da, 'B': min(db, r0b), 'Z': 0.0}
        if hOld == hOld and hOld > 1e-9: cand['C'] = min(1.0, db * hNew / hOld)
        else: C('C_no_old_hold')
        # 積分の検算用：A の道を記録の dominanceShare と比べる（最後の行の差）
        for name, d0 in cand.items():
            d = d0; went = None; top = None; mins = d0
            last = None; ai = 0
            for t in range(q + 1, min(q + Q10, nxt - 1, 667) + 1):
                while ai < len(after) and after[ai] <= t: last = after[ai]; ai += 1
                m = new[last][0] if last is not None else new[after[0]][0]
                d = d + m * SPEED
                if not (d > 0): d = 0.0
                elif d > 1: d = 1.0
                mins = min(mins, d)
                if not (d > 0): went = t; break
                if top is None and d >= TOP: top = t
            R(f'{name}\tdom0', d0); R(f'{name}\tmin10y', mins)
            if went is not None: C(f'{name}\tgo_within_10y')
            if d0 < TOP:
                C(f'{name}\tstart_below_top')
                if top is not None: R(f'{name}\tyears_to_top', (top - q) / 4.0)
            if name == 'A' and after:
                obs = new[after[-1]][2]
                if obs == obs: R('A\tcheck_abs_diff_last_row', abs(d - obs) if went is None else abs(0 - obs))
def dist(v):
    v = sorted(x for x in v if x == x)
    if not v: return 'n 0'
    g = lambda p: v[min(len(v) - 1, int(round(p * (len(v) - 1))))]
    return f'n {len(v)}\tp10 {g(.1):.4f}\tmed {g(.5):.4f}\tp90 {g(.9):.4f}\tmax {v[-1]:.4f}'
lines = [f'# (925) 紙：割譲の後の Dominance の初期値の案（{src}・種 {seeds}・速度 {SPEED}・10 年）']
for k in sorted(cnt): lines.append(f'{k}\t{cnt[k]}')
for k in sorted(res): lines.append(f'{k}\t{dist(res[k])}')
with open(outp, 'wb') as fh: fh.write(('\n'.join(lines) + '\n').encode('utf-8'))
print('done', outp)
