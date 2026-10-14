# -*- coding: utf-8 -*-
"""(333) 計器 region_s<seed>.tsv から、南アジア（BCG の範囲）の州の人口の和を窓ごとに読む（読むだけ）。
python sa333_20261014_an_region.py [--base N1,N2,N3,N4] <腕名=ins のディレクトリ> [<腕名=ディレクトリ> ...]
列：quarter・region（州の名前）・populationK。州の名前の一覧は spec.REGION_NAMES（国が変わっても名前で追う・後から生まれる州は数えない）。
窓の quarter は計器の番号どおり Q0＝開幕〜Q667＝1950 の最後（追記3・Q668 は無い）。
G2＝年率が BCG の 1/10〜10 倍（3〜10 倍は記録の帯・合格と出さない）／G3＝符号つきの ln(シム÷BCG) と |ln|。10 種の中央値。
窓ごとに 20 州のうち見つかった州の数を種ごとに数え、20 未満の種があれば印を付ける（州が消えた・名前が変わった・試合が届かない）。
基準：A の region_s の生データが無い（r331 の帯は要約だけ）ので、A の側は「同じ木の N1〜N4」の窓ごとの中央値の中央値を基準にする（--base で腕名を指定）。"""
import sys, os, glob, csv, math, statistics
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import sa333_20261014_spec as spec

WINDOWS = [(0, '開幕1783'), (40, '1793'), (72, '1801 BCG207'), (80, '1803'), (152, '1821 BCG205'), (160, '1823'), (240, '1843'), (352, '1871 BCG256'), (667, '1950')]
BCG_AT = {72: 207.0, 152: 205.0, 352: 256.0}
BCG_RATE = {'G2a': 0.0017, 'G2b': 0.0030}
NREG = len(spec.REGION_NAMES)


def read_seed(path):
    tot, cnt = {}, {}
    with open(path, encoding='utf-8') as f:
        rd = csv.reader(f, delimiter='\t')
        hdr = next(rd)
        iq, ir, ip = hdr.index('quarter'), hdr.index('region'), hdr.index('populationK')
        for r in rd:
            if r[ir] in spec.REGION_NAMES:
                q = int(r[iq])
                tot[q] = tot.get(q, 0.0) + float(r[ip])
                cnt[q] = cnt.get(q, 0) + 1
    return tot, cnt


def read_arm(d):
    out = {}
    for p in sorted(glob.glob(os.path.join(d, 'region_s*.tsv'))):
        out[os.path.basename(p)[len('region_s'):-4]] = read_seed(p)
    return out


def at(tot, q):
    if q in tot:
        return tot[q]
    ks = [k for k in tot if k <= q]
    return tot[max(ks)] if ks else float('nan')   # 死んだ試合・短い試合は最後の値（印を付けて数える）


def rate(a, b, years):
    return math.log(b / a) / years if a > 0 and b > 0 else float('nan')


def ratio_note(r, bcg):
    k = r / bcg
    if k >= 10 or k <= 0.1:
        return '線の外（%.1f 倍）' % k
    if k >= 3 or k <= 1 / 3:
        return '記録の帯（%.1f 倍・3〜10 倍）' % k
    return '合格（%.1f 倍）' % k


def main():
    args = sys.argv[1:]
    base = []
    if args and args[0] == '--base':
        base = args[1].split(','); args = args[2:]
    arms = [a.split('=', 1) for a in args]
    data = {n: read_arm(d) for n, d in arms}
    med = {}
    for n, seeds in data.items():
        print('=== %s（%d 種）===' % (n, len(seeds)))
        mm = {}
        for q, lab in WINDOWS:
            vals, found, short = [], [], 0
            for tot, cnt in seeds.values():
                vals.append(at(tot, q) / 1e3)
                found.append(cnt.get(q, 0))
                if max(tot) < q: short += 1
            m = statistics.median(vals) if vals else float('nan')
            mm[q] = m
            warn = ''
            if short: warn += '　⚠この窓に届かない試合 %d（最後の値で代用）' % short
            lo = min(found) if found else 0
            if lo < NREG: warn += '　⚠見つかった州の数が 20 未満の種あり（最小 %d）' % lo
            print('  Q%-3d %-12s 範囲内の和 中央値 %7.1f 百万（最小 %.1f・最大 %.1f）州数 最小 %d%s' % (q, lab, m, min(vals), max(vals), lo, warn))
        med[n] = mm

        def med_rate(q0, q1):
            return statistics.median([rate(at(t, q0), at(t, q1), (q1 - q0) / 4.0) for t, _ in seeds.values()])
        for (q0, q1, key, nm) in [(0, 72, 'G2a', 'G2a Q0→Q72（BCG 0.17%）'), (72, 352, 'G2b', 'G2b Q72→Q352（BCG 0.30%）')]:
            r = med_rate(q0, q1)
            print('  %s 年率 中央値 %.3f%%　%s' % (nm, r * 100, ratio_note(r, BCG_RATE[key])))
        for (q0, q1, nm) in [(0, 40, 'G4 Q0→Q40'), (0, 160, 'G4 Q0→Q160'), (40, 160, 'Q40→Q160')]:
            print('  %s 年率 中央値 %.3f%%（記録）' % (nm, med_rate(q0, q1) * 100))
    # G3：符号つきの ln と |ln|（基準＝N 腕の中央値の中央値）
    if base and all(b in med for b in base):
        ref = {q: statistics.median([med[b][q] for b in base]) for q in set(BCG_AT) | {0, 40, 160, 240}}
        print('=== G3 符号つきの ln(シム÷BCG)（正＝上振れ）と |ln|（基準 A＝%s の窓ごとの中央値の中央値）===' % ','.join(base))
        print('  A(基準): ' + '　'.join('Q%d %.1f 百万 ln %+.2f' % (q, ref[q], math.log(ref[q] / BCG_AT[q])) for q in BCG_AT))
        for n in data:
            if n in base: continue
            print('  %s: ' % n + '　'.join('Q%d %.1f 百万 ln %+.2f |ln| %.2f（A %.2f）%s' % (q, med[n][q], math.log(med[n][q] / BCG_AT[q]), abs(math.log(med[n][q] / BCG_AT[q])), abs(math.log(ref[q] / BCG_AT[q])), '小' if abs(math.log(med[n][q] / BCG_AT[q])) < abs(math.log(ref[q] / BCG_AT[q])) else '大') for q in BCG_AT))
        print('  ※G3 は記録（採否の規則に入れない）。上振れ（ln＞0）の大きさは記録だけ。')
        spread = {q: (max(med[b][q] for b in base) - min(med[b][q] for b in base)) for q in BCG_AT}
        print('  N 腕の幅（最大−最小・百万）: ' + '　'.join('Q%d %.2f' % (q, spread[q]) for q in BCG_AT))


if __name__ == '__main__':
    main()
