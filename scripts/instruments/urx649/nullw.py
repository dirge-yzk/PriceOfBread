import csv, statistics as st, sys
# (638) gates.py の ④ と同じ定義：本国（tier Core）の都市の割合 = (人口 - 農村) / 人口、四半期 q の州の和。
# ヌル腕の幅 = N1・N2 それぞれの「同じ種の対の差」の中央値の絶対値の大きい方。read-only。
D = 'D:/PoB_work/keep_645'
UN = ['イギリス', 'フランス', 'プロイセン']
UQ = {1850: 268, 1880: 388, 1913: 520}

def us(arm, s):
    acc = {}
    with open(f'{D}/{arm}/region_s{s}.tsv', encoding='utf-8') as fh:
        r = csv.reader(fh, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
        iq, inat, itier, ip, ir = ix['quarter'], ix['nation'], ix['tier'], ix['populationK'], ix['ruralPopulationK']
        for row in r:
            q = int(row[iq])
            if q in UQ.values() and row[inat] in UN and row[itier] == 'Core':
                a = acc.setdefault((row[inat], q), [0.0, 0.0]); a[0] += float(row[ip]); a[1] += float(row[ir])
    return {k: (v[0] - v[1]) / v[0] for k, v in acc.items() if v[0] > 0}, {k: v[0] for k, v in acc.items()}

data = {a: {s: us(a, s) for s in range(1, 11)} for a in ('m645_ins', 'n1', 'n2')}
for n in UN:
    for y, q in UQ.items():
        base = [data['m645_ins'][s][0].get((n, q)) for s in range(1, 11)]
        line = f'{n} {y}  m645 中央値 {st.median([b for b in base if b is not None]):.3f}'
        pk = [data['m645_ins'][s][1].get((n, q), 0) for s in range(1, 11)]
        line += f'  人口(千)中央値 {st.median(pk):.0f}'
        ws = []
        for a in ('n1', 'n2'):
            diffs = [data[a][s][0][(n, q)] - data['m645_ins'][s][0][(n, q)] for s in range(1, 11) if (n, q) in data[a][s][0] and (n, q) in data['m645_ins'][s][0]]
            md = st.median(diffs); ws.append(abs(md))
            line += f'  | {a}-m645 対の差の中央値 {md:+.3f} (範囲 {min(diffs):+.3f}〜{max(diffs):+.3f}, 絶対値の中央 {st.median([abs(x) for x in diffs]):.3f})'
        line += f'  | (638) 流の幅 {max(ws):.3f}'
        print(line)
