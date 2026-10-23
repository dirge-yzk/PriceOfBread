"""(513) hz6：計器の生の flow_s*.tsv を sourceLine の列（呼び出しの行番号）を落として比べる。
使い方：python -I -X utf8 flow_noline.py <基準の生のフォルダ> <今の生のフォルダ> [種の数=10]
コードの行が増減すると flow の sourceLine だけが動く（行の並び・量は変わらない）。cmpraw490.py は flow を DIFF と読むので、行番号を落として並びまで比べる。"""
import sys
A, B = sys.argv[1], sys.argv[2]
n = int(sys.argv[3]) if len(sys.argv) > 3 else 10
bad = 0
for s in range(1, n + 1):
    def rd(p):
        L = [x.rstrip(b'\r').split(b'\t') for x in open(p, 'rb').read().split(b'\n') if x.strip(b'\r') != b'']
        h = L[0]; i = h.index(b'sourceLine')
        return h, [tuple(r[:i] + r[i + 1:]) for r in L[1:]]
    ha, ra = rd(f'{A}/flow_s{s}.tsv'); hb, rb = rd(f'{B}/flow_s{s}.tsv')
    ok = ha == hb and ra == rb
    print(s, 'flow-eq-without-sourceLine' if ok else 'DIFF', len(ra)); bad += (not ok)
print('FLOWBAD', bad)
sys.exit(1 if bad else 0)
