# (865) 確かめ（読むだけ）：cpop の landRentSettlePerQuarter を階級ごとに足し、cornint の層の割合と突き合わせる（計器の国の名簿の 8 国だけ・q0・10 種の中央）。
# 限界：cpop は国内の地代（LandRentPerMonth の清算の値）だけ＝輸出の地代は入らない。
import csv, io, statistics as st
BASE = 'D:/PoB_work/keep_852/base_ins'
OUT = 'D:/PoB_work/r865_work/cpop_check865.txt'
acc = {}
for s in range(1, 11):
    raw = open(f'{BASE}/cpop_s{s}.tsv', 'rb').read()
    rows = list(csv.reader(io.StringIO(raw.decode('utf-8-sig')), delimiter='\t'))
    h = rows[0]
    ix = {k: i for i, k in enumerate(h)}
    tot = {}
    for r in rows[1:]:
        if r[ix['quarter']] != '0':
            continue
        try:
            v = float(r[ix['landRentSettlePerQuarter']])
        except ValueError:
            continue
        if not (v > 0):
            continue
        d = tot.setdefault(r[ix['nation']], {})
        d[r[ix['className']]] = d.get(r[ix['className']], 0.0) + v
    for n, d in tot.items():
        t = sum(d.values())
        for c, v in d.items():
            acc.setdefault(n, {}).setdefault(c, []).append(v / t)
lines = ['# nation\tclass\tshareOfDomesticLandRent(median over seeds present)\tseeds']
for n in sorted(acc):
    for c in sorted(acc[n], key=lambda k: -st.median(acc[n][k])):
        L = acc[n][c]
        lines.append(f'{n}\t{c}\t{st.median(L):.4f}\t{len(L)}')
data = ('\n'.join(lines) + '\n').encode('utf-8')
fo = open(OUT, 'wb')
fo.write(data)
fo.close()
