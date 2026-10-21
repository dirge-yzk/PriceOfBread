"""(490) 計器の生の比べ（基準 m484 の生 vs hz4 の生）。違ってよい表だけを名指しで許す：
  burscan・clock＝壁時計の列／flow＝sourceLine の列（呼びの行番号）／pop＝層の列の名前（deathsBand{b}K・mu{b}PerYear → deathsFrom{下端}K・muFrom{下端}PerYear＝名前だけの改名・値は同じ）／
  disc＝末尾の nationUid の列を足した／disccontact＝末尾の buyerUid の列を足した。ほかの表の差は DIFF として列挙する（原因を書くまで恒等と言わない）。
使い方：python cmpraw490.py <基準の生のフォルダ> <今の生のフォルダ>
"""
import os
import sys
from collections import Counter

A, B = sys.argv[1], sys.argv[2]
LAYER_LOWER_11 = [0, 1, 6, 11, 16, 21, 31, 41, 51, 61, 71]


def lines(p):
    return [x.rstrip(b'\r') for x in open(p, 'rb').read().split(b'\n')]


def split_rows(L):
    return [r.split(b'\t') for r in L]


def drop_cols(L, drop):
    h = L[0].decode().split('\t')
    keep = [i for i, c in enumerate(h) if c not in drop]
    return [b'\t'.join(f[i] for i in keep if i < len(f)) for f in split_rows(L)]


def rename_pop(L):
    """新しい見出しの層の列を旧の番号の名前へ戻す（11 層）。"""
    rows = split_rows(L)
    h = rows[0]
    m = {}
    for b, lo in enumerate(LAYER_LOWER_11):
        m[f'deathsFrom{lo}K'.encode()] = f'deathsBand{b}K'.encode()
        m[f'muFrom{lo}PerYear'.encode()] = f'mu{b}PerYear'.encode()
    rows[0] = [m.get(c, c) for c in h]
    return [b'\t'.join(r) for r in rows]


fa = sorted(os.listdir(A))
fb = sorted(os.listdir(B))
same = diff = 0
notes = []
for f in sorted(set(fa) | set(fb)):
    if f not in fa or f not in fb:
        notes.append(('only', f, f in fa))
        continue
    pa, pb = os.path.join(A, f), os.path.join(B, f)
    if open(pa, 'rb').read() == open(pb, 'rb').read():
        same += 1
        continue
    base = f.split('_s')[0]
    diff += 1
    if base in ('burscan', 'clock'):
        notes.append(('wallclock', f)); continue
    La, Lb = lines(pa), lines(pb)
    if base == 'flow':
        ok = drop_cols(La, {'sourceLine'}) == drop_cols(Lb, {'sourceLine'})
    elif base == 'pop':
        ok = La == rename_pop(Lb)
    elif base == 'disc':
        ok = La == drop_cols(Lb, {'nationUid'})
    elif base == 'disccontact':
        ok = La == drop_cols(Lb, {'buyerUid'})
    else:
        ok = False
    notes.append(('colsok' if ok else 'DIFF', f))
print('same', same, 'differ', diff)
print(Counter((n[0], n[1].split('_s')[0]) for n in notes))
bad = [n for n in notes if n[0] in ('DIFF', 'only')]
for n in bad:
    print(n)
print('BAD', len(bad))
