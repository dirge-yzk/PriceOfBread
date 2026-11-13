# (1095) 鉄道の門（機関車の発見）が開いた四半期と、国の鉄道の容量が初めて正になった四半期の差を、種×国ごとに数える。
# 表は rulepower_s（同じ行に国の名・railCapacityIndex＝nn.W.RailCapacity〔InstrumentsRulePower.cs:366〕・ρ の 3 通りがある）。
# 門の判定（計器の列から）：rhoDirectRailGateIndex（今の比＝発見の門）が rhoDirectFullRailIndex（比 3）と等しく、rhoDirectNoRailIndex（比 12）と違う州の行がある国×四半期＝門が開いている。
#   陸の脚が無い対（海だけ・同じ値）は判定に使わない（3 通りが全て等しい行は飛ばす）。
# 使い方: python -X utf8 -I q_gatelag.py <表のフォルダ> > out
import sys, os, collections, statistics as st
sys.stdout.reconfigure(newline='\n')
d = sys.argv[1]
lags = []; rows = []; early = collections.Counter()
for s in range(1, 11):
    gate = {}; cap = {}; yr = {}
    with open(os.path.join(d, f'rulepower_s{s}.tsv'), encoding='utf-8') as fi:
        h = fi.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        iq, iy, inn = ix['quarter'], ix['year'], ix['nation']
        ig, inr, ifr, ic = ix['rhoDirectRailGateIndex'], ix['rhoDirectNoRailIndex'], ix['rhoDirectFullRailIndex'], ix['railCapacityIndex']
        for line in fi:
            c = line.rstrip('\n').split('\t')
            n = c[inn]; q = int(c[iq])
            if n not in cap:
                try:
                    if float(c[ic]) > 0: cap[n] = q; yr[(n, 'c')] = c[iy]
                except ValueError: pass
            if n not in gate:
                g, nr, fr = c[ig], c[inr], c[ifr]
                if g != nr and g == fr and g not in ('nan', 'NaN'):
                    gate[n] = q; yr[(n, 'g')] = c[iy]
    for n, gq in gate.items():
        cq = cap.get(n)
        lag = (cq - gq) / 4.0 if cq is not None else None
        rows.append((s, n, yr[(n, 'g')], yr.get((n, 'c'), '-'), lag))
        if lag is not None: lags.append(lag)
        else: early[s] += 1
print('種\t国\t門が開いた年\t容量が正になった年\t差（年）')
for r in sorted(rows, key=lambda x: (x[0], float(x[2]))):
    print(f'{r[0]}\t{r[1]}\t{r[2]}\t{r[3]}\t{"-" if r[4] is None else f"{r[4]:.2f}"}')
if lags:
    lags.sort()
    print(f'\n差（年）の分布：件数 {len(lags)}・中央 {st.median(lags):.2f}・9 割点 {lags[int(len(lags)*0.9)]:.2f}・最大 {lags[-1]:.2f}・差 ≧10 年の件数 {sum(1 for x in lags if x >= 10)}')
print('門が開いたが容量が最後まで正にならない国（種ごと）', dict(early))
