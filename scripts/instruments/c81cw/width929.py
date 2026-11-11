# (929) c81cw 新÷旧の表をヌル腕の幅と並べる（読むだけ・tab922.py の出力を読むだけ）。
#   使い方: python width929.py <腕 B の tab.txt（arm・n1・n2 の列）> <S11 の tab.txt（arm の列）> <(903) の表 tab903_first.txt>
#   幅＝[min(1, n1÷基線, n2÷基線), max(…)]（基線そのものも旗 0 の 1 本＝1.0 を含める）。S11 にヌル腕は無い＝S11 は種 1〜10 の幅を参考に並べる。
#   〈3 本とも外〉＝腕 B・S11・(903) の旗 1 の 3 本が同じ側で幅の外。
import sys

def read(p):
    rows = {}
    with open(p, encoding='utf-8') as f:
        lines = [l.rstrip('\r\n') for l in f if not l.startswith('#')]
    head = lines[0].split('\t')
    for l in lines[1:]:
        c = l.split('\t')
        if len(c) < len(head): continue
        rows[c[0]] = dict(zip(head, c))
    return rows

def num(x):
    try: return float(x)
    except Exception: return float('nan')

B, S, F = (read(p) for p in sys.argv[1:4])
print('列\t腕B÷基線\tN1÷基線\tN2÷基線\t幅\t腕B\tS11÷a11\tS11\t(903)÷基線\t(903)\t3本')
def side(v, lo, hi):
    if v != v: return '-'
    return '下' if v < lo else ('上' if v > hi else '内')
for k, r in B.items():
    a = num(r.get('arm÷基線')); n1 = num(r.get('n1÷基線')); n2 = num(r.get('n2÷基線'))
    lo = min(1.0, n1, n2); hi = max(1.0, n1, n2)
    s = num(S.get(k, {}).get('arm÷基線')); f3 = num(F.get(k, {}).get('arm903÷基線'))
    sa, ss, sf = side(a, lo, hi), side(s, lo, hi), side(f3, lo, hi)
    three = sa if (sa == ss == sf and sa in ('上', '下')) else ('2本' if sa == ss and sa in ('上', '下') else '')
    print(f'{k}\t{a:.4f}\t{n1:.4f}\t{n2:.4f}\t[{lo:.4f},{hi:.4f}]\t{sa}\t{s:.4f}\t{ss}\t{f3:.4f}\t{sf}\t{three}')
