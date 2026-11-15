# (1164) 合流（m1157/ledger_s27.py の写し・系統を S22 へ）：読む線 c114s22 (1161) の台帳の S22 の 53 行の案（ledger_S22_<日>.tsv・12 列）で、機能の台帳.md の同じ id の行を入れ替える（行の位置と順は変えない・中身は書き換えない）。
#   python -X utf8 -I ledger_s22.py <機能の台帳.md> <ledger_S22_<日>.tsv>
# バイトで読み書き（LF・BOM なしを保つ）。TSV のセルに | があれば止める。
import sys
p, tsv = sys.argv[1], sys.argv[2]
b = open(p, 'rb').read().decode('utf-8')
assert '\r' not in b
T = open(tsv, 'rb').read().decode('utf-8').rstrip('\n').split('\n')
rows = {}
for ln in T[1:]:
    c = ln.split('\t')
    assert len(c) == 12, (c[0], len(c))
    assert not any('|' in x for x in c), c[0]
    rows[c[0]] = '| ' + ' | '.join(c) + ' |'
L = b.split('\n'); n = 0
for i, l in enumerate(L):
    if l.startswith('| S22-'):
        k = l.split('|')[1].strip()
        assert k in rows, k
        L[i] = rows.pop(k); n += 1
assert not rows, list(rows)[:5]
open(p, 'wb').write('\n'.join(L).encode('utf-8'))
print('replaced', n)
