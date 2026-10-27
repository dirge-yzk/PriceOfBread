"""(608) gc7：計器の生を、基準（m600 の生）と今で表ごとにバイトで比べる。違ってよい表を名指しで許し、ほかの差は DIFF で列挙する。
許す差：
  fiscal   ＝基準から列 claimStateBuildPerMonth を落とした表が今と全行一致（今の見出しにその列が無い）
  nation   ＝基準から列 stateConstructionPaidPerMonth を落とした表が今と全行一致（今の見出しにその列が無い）
  burscan  ＝壁時計の列 seconds だけ
  clock    ＝kind が C の行の worldTotalPerMonth（壁時計の秒）だけ
  flow     ＝列 sourceLine だけ（コードの行番号が動く）
使い方：python -I -X utf8 cmp_raw608.py <基準の生のフォルダ> <今の生のフォルダ>"""
import os
import sys
from collections import Counter

A, B = sys.argv[1], sys.argv[2]


def read_table(p):
    L = [x.rstrip(b'\r') for x in open(p, 'rb').read().split(b'\n')]
    if L and L[-1] == b'':
        L = L[:-1]
    if not L:
        return None
    rows = [r.split(b'\t') for r in L]
    n = len(rows[0])
    if any(len(r) != n for r in rows):
        return None
    return rows[0], rows[1:]


def drop_cols(t, names):
    h, rows = t
    keep = [i for i, c in enumerate(h) if c not in names]
    return [h[i] for i in keep], [[r[i] for i in keep] for r in rows]


def mask(t, names, only_kind=None):
    h, rows = t
    idx = {h.index(n) for n in names}
    kc = h.index(only_kind[0]) if only_kind else None
    out = []
    for r in rows:
        if only_kind is not None and r[kc] != only_kind[1]:
            out.append(list(r))
        else:
            out.append([b'<wall>' if i in idx else c for i, c in enumerate(r)])
    return h, out


fa, fb = sorted(os.listdir(A)), sorted(os.listdir(B))
same = diff = 0
notes = []
for f in sorted(set(fa) | set(fb)):
    if f not in fa or f not in fb:
        notes.append(('only', f.split('_s')[0], f in fa))
        continue
    pa, pb = os.path.join(A, f), os.path.join(B, f)
    if open(pa, 'rb').read() == open(pb, 'rb').read():
        same += 1
        continue
    diff += 1
    base = f.split('_s')[0]
    ta, tb = read_table(pa), read_table(pb)
    ok = False
    if ta is not None and tb is not None:
        if base == 'fiscal':
            c = b'claimStateBuildPerMonth'
            ok = c in ta[0] and c not in tb[0] and drop_cols(ta, {c}) == tb
        elif base == 'nation':
            c = b'stateConstructionPaidPerMonth'
            ok = c in ta[0] and c not in tb[0] and drop_cols(ta, {c}) == tb
        elif base == 'burscan' and ta[0] == tb[0] and b'seconds' in ta[0]:
            ok = mask(ta, [b'seconds']) == mask(tb, [b'seconds'])
        elif base == 'clock' and ta[0] == tb[0] and b'kind' in ta[0] and b'worldTotalPerMonth' in ta[0]:
            ok = mask(ta, [b'worldTotalPerMonth'], (b'kind', b'C')) == mask(tb, [b'worldTotalPerMonth'], (b'kind', b'C'))
        elif base == 'flow' and ta[0] == tb[0] and b'sourceLine' in ta[0]:
            ok = mask(ta, [b'sourceLine']) == mask(tb, [b'sourceLine'])
    notes.append(('colsok' if ok else 'DIFF', base))
print('same', same, 'differ', diff, 'of', same + diff)
print(Counter((n[0], n[1]) for n in notes))
bad = [n for n in notes if n[0] in ('DIFF', 'only')]
for n in bad:
    print(n)
print('BAD', len(bad))
sys.exit(1 if bad else 0)
