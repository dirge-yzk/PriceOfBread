# (1210) ① picked の国の取り分の上位の四半期と、その四半期の敗戦の衝撃・戦争中か（scalars_s）。用法: natpeak.py <生> <種0> <種1> <国> <出力>
import sys, csv
raw, s0, s1, nat, out = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4], sys.argv[5]
def rows(p):
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for x in r: yield ix, x
o = open(out, 'w', encoding='utf-8', newline='\n')
o.write('# 国=%s・取り分の上位 3 四半期（四半期の行）と同じ四半期の defeatShock・atWar\n種\t四半期\t年\t取り分\tdefeatShock\tatWar\t取り分≥0.15 の四半期の数\tうち defeatShock>0.05\n' % nat)
for s in range(s0, s1 + 1):
    sh = {}
    for ix, x in rows(f'{raw}/ideology_s{s}.tsv'):
        if x[ix['nation']] == nat: sh[int(x[ix['quarter']])] = (int(x[ix['year']]), float(x[ix['ideoNationalNationalismShare']]))
    sc = {}
    for ix, x in rows(f'{raw}/scalars_s{s}.tsv'):
        if x[ix['nation']] == nat: sc[int(x[ix['quarter']])] = (float(x[ix['defeatShock']]), x[ix['atWar']])
    top = sorted(sh.items(), key=lambda kv: -kv[1][1])[:3]
    hi = [q for q, v in sh.items() if v[1] >= 0.15]
    hid = [q for q in hi if q in sc and sc[q][0] > 0.05]
    for q, (y, v) in top:
        d = sc.get(q, ('', ''))
        o.write(f'{s}\t{q}\t{y}\t{v:.3f}\t{d[0]}\t{d[1]}\t{len(hi)}\t{len(hid)}\n')
print('ok', out)
