# -*- coding: utf-8 -*-
"""(333) 計器 region_s<seed>.tsv から、南アジア（BCG の範囲）の州の人口の和を窓ごとに読む（読むだけ）。
python sa333_20261014_an_region.py <腕名=ins のディレクトリ> [<腕名=ディレクトリ> ...]
列：quarter・region（州の名前）・populationK。州の名前の一覧は spec.REGION_NAMES（国が変わっても名前で追う・後から生まれる州は数えない）。
G2＝年率が BCG の 1/10〜10 倍／G3＝|ln(シム÷BCG)| が Q72・Q152・Q352 で基準（最初の腕）より小さいか。10 種の中央値。"""
import sys, os, glob, csv, math, statistics
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import sa333_20261014_spec as spec

WINDOWS = [(1, '開幕'), (40, '1793'), (72, '1801 BCG207'), (80, '1803'), (152, '1821 BCG205'), (160, '1823'), (240, '1843'), (352, '1871 BCG256'), (668, '1950')]
BCG_AT = {72: 207.0, 152: 205.0, 352: 256.0}


def read_seed(path):
    tot = {}
    china = {}
    with open(path, encoding='utf-8') as f:
        rd = csv.reader(f, delimiter='\t')
        hdr = next(rd)
        iq, ir, ip, ina = hdr.index('quarter'), hdr.index('region'), hdr.index('populationK'), hdr.index('nation')
        for r in rd:
            q = int(r[iq])
            if r[ir] in spec.REGION_NAMES:
                tot[q] = tot.get(q, 0.0) + float(r[ip])
    return tot


def read_arm(d):
    out = {}
    for p in sorted(glob.glob(os.path.join(d, 'region_s*.tsv'))):
        seed = os.path.basename(p)[len('region_s'):-4]
        out[seed] = read_seed(p)
    return out


def at(tot, q):
    if q in tot:
        return tot[q]
    ks = [k for k in tot if k <= q]
    return tot[max(ks)] if ks else float('nan')   # 死んだ試合・短い試合は最後の値


def rate(a, b, years):
    return math.log(b / a) / years if a > 0 and b > 0 else float('nan')


def main():
    arms = [a.split('=', 1) for a in sys.argv[1:]]
    data = {n: read_arm(d) for n, d in arms}
    base = arms[0][0]
    med = {}
    for n, seeds in data.items():
        print('=== %s（%d 種）===' % (n, len(seeds)))
        mm = {}
        for q, lab in WINDOWS:
            vals = [at(t, q) / 1e3 for t in seeds.values()]   # 百万
            m = statistics.median(vals) if vals else float('nan')
            mm[q] = m
            short = sum(1 for t in seeds.values() if max(t) < q)
            print('  Q%-3d %-12s 範囲内の和 中央値 %7.1f 百万（最小 %.1f・最大 %.1f）%s' % (q, lab, m, min(vals), max(vals), ('　⚠この窓に届かない試合 %d（最後の値で代用）' % short) if short else ''))
        med[n] = mm
        # 年率（種ごとに出して中央値）
        def med_rate(q0, q1):
            rs = [rate(at(t, q0), at(t, q1), (q1 - q0) / 4.0) for t in seeds.values()]
            return statistics.median(rs)
        for (q0, q1, lo, hi, nm) in [(1, 72, 0.00017, 0.017, 'G2a Q1→Q72（BCG 0.17%）'), (72, 352, 0.0003, 0.03, 'G2b Q72→Q352（BCG 0.30%）'), (1, 40, None, None, 'G4 Q1→Q40'), (1, 160, None, None, 'G4 Q1→Q160'), (40, 160, None, None, 'Q40→Q160')]:
            r = med_rate(q0, q1)
            tag = '' if lo is None else ('　合格' if lo <= r <= hi else '　線の外')
            print('  %s 年率 中央値 %.3f%%%s' % (nm, r * 100, tag))
    if len(data) > 1:
        print('=== G3 |ln(シム÷BCG)| の比べ（基準＝%s）===' % base)
        for n in data:
            print('  %s: ' % n + '　'.join('Q%d %.2f' % (q, abs(math.log(med[n][q] / BCG_AT[q]))) for q in BCG_AT))


if __name__ == '__main__':
    main()
