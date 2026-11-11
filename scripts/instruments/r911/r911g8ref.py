# (911) c78lt：G8c の物差しを州で組み直す（記録・採否でない）。
#   python -X utf8 r911g8ref.py <出力.txt> <腕の名=ins のフォルダ> ...
#   B_reg(種, q, 国)＝Σ_{q に国にある州} [q0 の基数（州に q0 の行が無ければ今の基数）] ÷ Σ 今の基数（landbasereg_s・州を鍵＝割譲で州に付いて行く）
#   landbase_s の baseOverFirstSeenRatio（国の q0 の R÷今の R＝失った州の q0 の地代が分子に残る）と |log| の中央を並べる。
import csv, glob, math, os, re, sys
from collections import defaultdict

NAT = ['イギリス', '清']


def med(xs):
    xs = sorted(xs)
    if not xs:
        return float('nan')
    n = len(xs)
    return xs[n // 2] if n % 2 else 0.5 * (xs[n // 2 - 1] + xs[n // 2])


def main():
    out = []
    for a in sys.argv[2:]:
        name, ins = a.split('=', 1)
        lg = defaultdict(list); lgp = defaultdict(list)
        for p in sorted(glob.glob(os.path.join(ins, 'landbasereg_s*.tsv'))):
            seed = int(re.search(r'_s(\d+)\.tsv$', p).group(1))
            base0 = {}; num = defaultdict(float); den = defaultdict(float)
            with open(p, encoding='utf-8', newline='') as fh:
                rd = csv.reader(fh, delimiter='\t'); h = next(rd)
                iq, inat, ireg, ib = h.index('quarter'), h.index('nation'), h.index('region'), h.index('regionBasePerMonth')
                for row in rd:
                    q = int(row[iq]); b = float(row[ib])
                    if q == 0:
                        base0[row[ireg]] = b
                    if row[inat] in NAT:
                        num[(q, row[inat])] += base0.get(row[ireg], b); den[(q, row[inat])] += b
            for (q, n), d in den.items():
                if d > 0 and num[(q, n)] > 0:
                    lg[n].append(abs(math.log(num[(q, n)] / d)))
            lp = os.path.join(ins, 'landbase_s%d.tsv' % seed)
            with open(lp, encoding='utf-8', newline='') as fh:
                rd = csv.reader(fh, delimiter='\t'); h = next(rd)
                inat, ir = h.index('nation'), h.index('baseOverFirstSeenRatio')
                for row in rd:
                    if row[inat] in NAT:
                        v = float(row[ir]) if row[ir] != 'nan' else float('nan')
                        if v == v and v > 0:
                            lgp[row[inat]].append(abs(math.log(v)))
        for n in NAT:
            m1, m2 = med(lg[n]), med(lgp[n])
            out.append('%s %s：州で組んだ |log B_reg| の中央 %.3f（行 %d）・国の |log baseOverFirstSeenRatio| の中央 %.3f（行 %d）・比 %.3f' % (name, n, m1, len(lg[n]), m2, len(lgp[n]), m1 / m2 if m2 > 0 else float('nan')))
    with open(sys.argv[1], 'wb') as fh:
        fh.write(('\n'.join(out) + '\n').encode('utf-8'))


if __name__ == '__main__':
    main()
