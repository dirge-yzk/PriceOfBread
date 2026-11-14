# (1121) W1' の診断：基線の値段の鍵が無い行の割合と、W1'÷W1 を動かした財（記録だけ・物差しは替えない）
import os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import u1awb as U


def main(bdir, adir, year):
    tot = {}
    for s in U.seeds(bdir):
        bp = U.base_prices(bdir, s, None)
        miss_q = miss_x = all_q = all_x = 0.0
        byg = {}
        for y, n, gd, q, x, p in U.rows(adir, s):
            if y != year or not (p > 0): continue
            all_q += q * p; all_x += x * p
            pb = bp.get((y, n, gd), float('nan'))
            if not (pb > 0):
                miss_q += q * p; miss_x += x * p; continue
            e = byg.setdefault(gd, [0.0, 0.0, 0.0, 0.0])
            e[0] += x * p; e[1] += x * pb; e[2] += q * p; e[3] += q * pb
        print('種 %d %s 鍵の無い割合 作った額 %.4f 越境の輸出の額 %.4f' % (s, year, miss_q / all_q, miss_x / all_x if all_x > 0 else 0))
        for gd, e in byg.items():
            t = tot.setdefault(gd, [0.0, 0.0, 0.0, 0.0])
            for i in range(4): t[i] += e[i]
    # 輸出の額の増え（基線の値段−自分の値段）の大きい財 5 つ・10 種の和
    rows = sorted(tot.items(), key=lambda kv: -(kv[1][1] - kv[1][0]))[:5]
    for gd, e in rows:
        print('財 %s 越境の輸出の額 自分の値段 %.4g 基線の値段 %.4g ／作った額 %.4g → %.4g' % (gd, e[0], e[1], e[2], e[3]))


if __name__ == '__main__':
    main(sys.argv[1], sys.argv[2], sys.argv[3])
