# (1271) c130gs 紙の台本（段 0）：州ごとの兵と席の形を cpop_s・rule_s から読み、旗 1 の月の移しの量を近似で見込む。
#   エンジンの関数を呼ばない（計器の独立）。近似＝席の比 k は国の Σ兵÷Σ兵役の母数（国の総数が席に釣り合っている前提）・
#   兵役の母数＝兵×1.0＋農民と工場労働者×WMS（年齢の比を一定と置く）×届き方（AdminFill＝1 と置く）。
#   使い方: python -X utf8 -I paper_gs.py <ins の生> <種の起点> <種の終わり> [rate=0.08]
import sys, os, statistics as st
from collections import defaultdict
INS, SB, SE = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
RATE = float(sys.argv[4]) if len(sys.argv) > 4 else 0.08
WMS = 0.28          # 農民・工場労働者の 16〜60 歳の男の割合（近似・Pop.StdShare(16,60,0) の桁）
DELIVER = 1.0 - 0.3 # 1−PrivilegeTaxDiscount（AdminFill＝1 と置く近似）
YEARS = (1790, 1815, 1851, 1880, 1913, 1940)

def rows(p):
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            yield ix, line.rstrip('\n').split('\t')

tot = defaultdict(float)
capshare = defaultdict(list)      # 年 → [国の首都の州の兵の割合]
ratio_hist = defaultdict(int)     # 兵÷席 の階級（州×四半期・兵の大きさで重み）
over3 = 0; over3_k = 0.0; nq = 0
per_seed = []
for s in range(SB, SE + 1):
    cp = os.path.join(INS, f'cpop_s{s}.tsv'); ru = os.path.join(INS, f'rule_s{s}.tsv')
    if not (os.path.exists(cp) and os.path.exists(ru)):
        print(f'種 {s}: 表が無い'); continue
    deliv = {}; cap = {}
    for ix, c in rows(ru):
        try:
            k = (c[ix['quarter']], c[ix['nation']], c[ix['region']])
            r0 = float(c[ix['ruleStateShare']]); r1 = float(c[ix['ruleLocalShare']])
            v = max(0.0, min(1.0, r0)) + max(0.0, min(1.0, r1)) * DELIVER
            deliv[k] = v if 0 <= v <= 1 else 1.0
            if float(c[ix['capitalDistanceKm']]) == 0.0: cap[(c[ix['quarter']], c[ix['nation']])] = c[ix['region']]
        except (ValueError, KeyError):
            pass
    reg = defaultdict(lambda: [0.0, 0.0, False])   # (q,nation,region) → [兵, 民の母数, 兵の POP あり]
    yr = {}
    for ix, c in rows(cp):
        cid = c[ix['classId']]
        if cid not in ('1', '2', '5'): continue
        k = (c[ix['quarter']], c[ix['nation']], c[ix['region']])
        try: v = float(c[ix['popK']])
        except ValueError: continue
        yr[c[ix['quarter']]] = int(c[ix['year']])
        e = reg[k]
        if cid == '5': e[0] += v; e[2] = True
        else: e[1] += v
    nat = defaultdict(list)
    for (q, n, r), (S, A, has) in reg.items():
        if not has: continue
        m = (S + WMS * A) * deliv.get((q, n, r), 1.0)
        nat[(q, n)].append((r, S, m))
    sm_move = 0.0; sm_sold = 0.0
    firstq = {}
    for qq, yy in yr.items():
        if yy not in firstq or int(qq) < firstq[yy]: firstq[yy] = int(qq)
    for (q, n), lst in nat.items():
        SS = sum(x[1] for x in lst); MM = sum(x[2] for x in lst)
        if not (SS > 1e-9 and MM > 1e-9): continue
        kk = SS / MM
        E = sum(max(0.0, S - kk * m) for _, S, m in lst); V = sum(max(0.0, kk * m - S) for _, S, m in lst)
        mv = min(RATE * E, V)    # 1 か月の見込み（四半期の頭の盤面）
        sm_move += 3 * mv; sm_sold += SS; nq += 1
        for _, S, m in lst:
            t = kk * m
            if t > 1e-9:
                rr = S / t
                b = '0' if rr < 1e-6 else '<0.5' if rr < 0.5 else '<0.9' if rr < 0.9 else '0.9-1.1' if rr <= 1.1 else '<2' if rr < 2 else '<3' if rr < 3 else '>=3'
                ratio_hist[b] += S
                if rr >= 3: over3 += 1; over3_k += S
            elif S > 1e-9:
                ratio_hist['席0'] += S
        y = yr.get(q)
        cr = cap.get((q, n))
        if y in YEARS and q and cr is not None and SS > 1e-9:
            cs = next((S for r, S, m in lst if r == cr), None)
            if cs is not None and int(q) == firstq[y]: capshare[(y, n)].append(cs / SS)
    per_seed.append((s, sm_move, sm_sold))
    print(f'種 {s}: 見込みの移しの和 {sm_move:,.0f} 千人（四半期×3 か月・近似）・国×四半期の兵の和 {sm_sold:,.0f} 千人')
print(f'国×四半期 {nq}')
print('兵÷席（近似）の階級（兵の大きさの重み・千人）:', {k: round(v) for k, v in sorted(ratio_hist.items())})
print(f'兵÷席≧3 の州×四半期 {over3}・その兵の和 {over3_k:,.0f} 千人')
print('首都の州の兵の割合（年・国・種の中央）:')
for (y, n) in sorted(capshare):
    v = capshare[(y, n)]
    print(f'  {y} {n} {st.median(v):.3f} (n={len(v)})')
if per_seed:
    print(f'見込みの移しの和の種の中央 {st.median(x[1] for x in per_seed):,.0f} 千人')
