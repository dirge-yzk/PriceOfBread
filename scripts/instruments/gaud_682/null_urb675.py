# (682) 門の棚卸し：urb (629) の ③〈本国の都市の割合の下側〉を m675 の三つ組に一つ抜きで当てる（読むだけ）。
# 定義は scripts/instruments/urb629/urbshare.py の写し（英・仏・普の Core の州・1−農村÷人口・四半期 268/388/520）。
# 線＝残り 2 本の種中央値の最小 − 2×標準誤差（標準誤差＝残り 2 本の 1 本目の種ごとのばらつき÷√10）。下側だけ（ⓖ）。
import csv, sys, collections, statistics as st
K = sys.argv[1] if len(sys.argv) > 1 else 'D:/PoB_work/keep_675'
ARMS = {'m675': f'{K}/m675_ins', 'N1': f'{K}/n1', 'N2': f'{K}/n2'}
NATS = ['イギリス', 'フランス', 'プロイセン']; QS = {1850: 268, 1880: 388, 1913: 520}; QV = set(QS.values())
res = collections.defaultdict(dict)
for a, d in ARMS.items():
    for s in range(1, 11):
        f = open(f'{d}/region_s{s}.tsv', encoding='utf-8'); r = csv.reader(f, delimiter='\t'); h = next(r)
        iq, inn, it, ip, ir = h.index('quarter'), h.index('nation'), h.index('tier'), h.index('populationK'), h.index('ruralPopulationK')
        acc = collections.defaultdict(lambda: [0.0, 0.0])
        for x in r:
            if x[iq].isdigit() and int(x[iq]) in QV and x[inn] in NATS and x[it] == 'Core':
                k = (x[inn], int(x[iq])); acc[k][0] += float(x[ip]); acc[k][1] += float(x[ir])
        f.close()
        for (n, q), (p, rr) in acc.items():
            if p > 0: res[(a, n, q)][s] = 1 - rr / p
fails = 0; cells = 0
for a in ARMS:
    refs = [x for x in ARMS if x != a]
    for n in NATS:
        for y, q in QS.items():
            v = st.median(res[(a, n, q)].values())
            rm = [st.median(res[(r, n, q)].values()) for r in refs]
            se = st.pstdev(list(res[(refs[0], n, q)].values())) / 10 ** 0.5
            lo = min(rm) - 2 * se; cells += 1; bad = v < lo; fails += bad
            print(f'{a} 対 {"・".join(refs)}\t{n} {y}\t{v:.3f}\t線 {lo:.3f}（残り {", ".join(f"{x:.3f}" for x in rm)}・標準誤差 {se:.3f}）\t{"落ちる" if bad else "通る"}')
print(f'計 {fails}/{cells} マスが落ちる。腕に見立てた 1 本のどれかのマスが落ちる＝その一つ抜きで門が落ちる。')
