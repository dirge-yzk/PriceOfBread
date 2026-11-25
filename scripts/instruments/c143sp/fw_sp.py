# (1357) c143sp 予言（フォートウィリアム型）の読み手（採否に使わない・事前登録の〈予言〉の数を数える）。
# 使い方 python fw_sp.py <ins（D:/ の形）> [種の起点 種の数]
#  (a) 名指しの州（フォートウィリアム・バーミンガム・セントポール）ごとに 種ごとの 最初の行の人口・ピーク・落ち込みの最小（÷その時点までのピーク＝an_h2 と同じ読み）。
#  (b) 最初の行の 3 倍以上に膨らみ、落ち込みの最小が 1/4 を割る州の件数（種ごと・合計）。ピーク 1 千人以上の州だけ。例外の州（規則 A2）も数える（記録）。
import os, sys, math, collections
d = sys.argv[1]
sb = int(sys.argv[2]) if len(sys.argv) > 2 else 1
ns = int(sys.argv[3]) if len(sys.argv) > 3 else 10
NAMED = ['フォートウィリアム', 'バーミンガム', 'セントポール']
tot = 0
for sd in range(sb, sb + ns):
    p = os.path.join(d, 'region_s%d.tsv' % sd)
    if not os.path.exists(p):
        print('seed %d 無い' % sd); continue
    R = collections.defaultdict(list)
    with open(p, encoding='utf-8') as fh:
        h = fh.readline().rstrip('\n').split('\t')
        iq, ir, ip = h.index('quarter'), h.index('region'), h.index('populationK')
        last = {}
        for line in fh:
            c = line.rstrip('\n').split('\t')
            q = int(c[iq]); reg = c[ir]
            try: v = float(c[ip])
            except ValueError: continue
            if not math.isfinite(v): continue
            if last.get(reg) == q: continue   # 同じ四半期に 2 行は最初の行だけ
            last[reg] = q
            R[reg].append(v)
    n = 0; names = []
    for reg, vs in R.items():
        v0 = vs[0]; pk = 0.0; dd = 1.0
        for v in vs:
            if v > pk: pk = v
            if pk >= 1.0 and v / pk < dd: dd = v / pk
        if reg in NAMED:
            print('seed %d %s 最初 %.2f ピーク %.2f 落ち込みの最小 %.3f' % (sd, reg, v0, pk, dd))
        if pk >= 1.0 and v0 > 0 and pk >= 3.0 * v0 and dd < 0.25:
            n += 1; names.append('%s(%.3f)' % (reg, dd))
    tot += n
    print('seed %d 3倍以上に膨らんで1/4を割る州 %d %s' % (sd, n, ' '.join(names)))
print('合計 %d' % tot)
