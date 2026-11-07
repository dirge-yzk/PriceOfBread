# (937) c82lt 新÷旧の表（事前登録 [8]・記録）：腕の ins と基準の ins を、英・清の窓ごとに比べる。
#   python -X utf8 tab937.py <腕の ins> <基準の ins> [種の起点 腕=1] [種の起点 基準=1]
#   比＝窓ごとに〈種ごとの窓の中央〉の種の中央（腕）÷ 同じ（基準）。同じ盤面の比（landfix_s の新の規則÷旧の規則）は腕だけで読む。
import csv, glob, math, os, sys
from collections import defaultdict

csv.field_size_limit(1 << 30)
LIST = ['イギリス', '清']
WIN = [(1783, 1785), (1786, 1799), (1800, 1849), (1850, 1899), (1900, 1950)]
COLS = [('landbase', 'landTaxPaidPerMonth'), ('cornint', 'rentDomPerMonth0'), ('cornint', 'rentDomPerMonth1'), ('cornint', 'rentDomPerMonth2'),
        ('nation', 'nominalGdpPerYear'), ('nation', 'landTaxEndShare'), ('nation', 'landTaxTargetEndShare')]


def num(s):
    try:
        return float(s)
    except Exception:
        return float('nan')


def med(xs):
    xs = sorted(x for x in xs if x == x)
    if not xs:
        return float('nan')
    n = len(xs)
    return xs[n // 2] if n % 2 else 0.5 * (xs[n // 2 - 1] + xs[n // 2])


def win(y):
    for a, b in WIN:
        if a <= y <= b:
            return '%d-%02d' % (a, b % 100)
    return None


def read(ins, table, col, sb):
    d = defaultdict(lambda: defaultdict(list))   # (nation, win) -> seed -> values
    for s in range(sb, sb + 10):
        p = os.path.join(ins, '%s_s%d.tsv' % (table, s))
        if not os.path.exists(p):
            continue
        with open(p, encoding='utf-8', newline='') as fh:
            for r in csv.DictReader(fh, delimiter='\t'):
                if r['nation'] in LIST:
                    w = win(int(r['year']))
                    if w:
                        d[(r['nation'], w)][s].append(num(r[col]))
    return {k: med([med(v) for v in sv.values()]) for k, sv in d.items()}


def main():
    arm, base = sys.argv[1], sys.argv[2]
    sa = int(sys.argv[3]) if len(sys.argv) > 3 else 1
    sbb = int(sys.argv[4]) if len(sys.argv) > 4 else 1
    out = ['# (937) tab937 腕 %s（種 %d〜）÷ 基準 %s（種 %d〜）・英と清・窓ごと' % (arm, sa, base, sbb)]
    ws = ['%d-%02d' % (a, b % 100) for a, b in WIN]
    for t, c in COLS:
        A, B = read(arm, t, c, sa), read(base, t, c, sbb)
        for n in LIST:
            out.append('%s %s.%s 新÷旧：%s' % (n, t, c, '・'.join('%s %.3f' % (w, A.get((n, w), float('nan')) / B.get((n, w), float('nan')) if B.get((n, w), 0) else float('nan')) for w in ws)))
    cl, cp = read(arm, 'landfix', 'landClaimAtTaxPerMonth', sa), read(arm, 'landfix', 'landClaimPropAtTaxPerMonth', sa)
    for n in LIST:
        out.append('%s 同じ盤面 landClaimAtTax÷landClaimPropAtTax：%s' % (n, '・'.join('%s %.3f' % (w, cl.get((n, w), float('nan')) / cp.get((n, w), float('nan')) if cp.get((n, w), 0) else float('nan')) for w in ws)))
    print('\n'.join(out))


if __name__ == '__main__':
    main()
