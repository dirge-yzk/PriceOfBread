# (802) この形に固有の誤りの量の記録：四半期の利札の払い÷外資の負債の元本（paidIndex÷fci）の分布（読むだけ・門にしない＝結果の後の量）。
#   python -I paid_fci.py 名前=ins ...   同じ四半期の 2 回目以降の行（助走の呼び出し）は最後の行だけ読む（divhost_read.py と同じ）。
import csv, sys, glob, os
sys.stdout.reconfigure(encoding='utf-8')
for arg in sys.argv[1:]:
    k, d = arg.split('=', 1); rat = []; top = []
    for f in sorted(glob.glob(os.path.join(d, 'divhost_s*.tsv'))):
        s = os.path.basename(f)[9:-4]; last = {}
        r = csv.reader(open(f, encoding='utf-8'), delimiter='\t'); h = next(r)
        iq, ina, iy, ifc, ip = h.index('quarter'), h.index('nation'), h.index('year'), h.index('fci'), h.index('paidIndex')
        for row in r: last[(row[iq], row[ina])] = row
        for (q, n), row in last.items():
            fc, p = float(row[ifc]), float(row[ip])
            if fc > 1e-9 and p > 0:
                x = p / fc; rat.append(x); top.append((x, s, n, row[iy]))
    rat.sort(); top.sort(reverse=True); N = len(rat)
    q = lambda a: rat[min(N - 1, int(a * N))] if N else float('nan')
    gt1 = sum(1 for x in rat if x > 1); gt02 = sum(1 for x in rat if x > 0.02 * (1 + 1e-9))
    print(f'{k}: 払いのある行 {N}・paid÷fci の中央 {q(0.5):.4g}・p99 {q(0.99):.4g}・最大 {rat[-1] if N else 0:.4g}・旧の率 0.02 を超える行 {gt02}（{gt02 / max(N, 1):.1%}）・1 を超える行（四半期の利札が元本を超える）{gt1}', '・上位 ' + '・'.join(f'種{s} {n} {y} {x:.2f}' for x, s, n, y in top[:5]))
