"""計器の生の比べ（基準の生 vs 今の生）。違ってよい表だけを名指しで許し、ほかの差は DIFF として列挙する（原因を書くまで恒等と言わない）。
(490) で作り、(499) で次のように直した（基準は m491 の生＝層の列の改名・disc／disccontact の個体の列は基準に入っている）：
  burscan＝壁時計の列 seconds だけを落とす（calls・nations は決定論＝落とさない）。
  clock＝kind が C の行の worldTotalPerMonth（C 行では巡回の壁時計の秒）だけを落とす。W・T・E の行と C 行のほかの列は決定論＝落とさない。
  disccontact＝(499) で足した末尾の sellerUid の列を許す。売り手の国の個体で記帳の鍵を分けたため、同じ名の別の国の売りが 1 行に合算されていた行が分かれうる＝
    sellerUid を落として同じ鍵（quarter・buyer・row・seller・carrier・buyerUid）で合算し直したときに基準の行と合う（額は相対 1e-9 まで・足す順が変わるので丸めだけ違いうる）。分かれた行の数は split として数える。
  どの表も、行の列の数が見出しと違えば DIFF（短い行を黙って通さない）。見出し自体が違う表は上の許した列の差を除いて DIFF。
使い方：python cmpraw490.py <基準の生のフォルダ> <今の生のフォルダ>
"""
import math
import os
import sys
from collections import Counter

A, B = sys.argv[1], sys.argv[2]


def read_table(p):
    """バイトで読み、CR を落として (見出し, 行の並び) を返す。行の列の数が見出しと違えば None（DIFF）。"""
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


def masked(h, rows, names, only_kind=None):
    """見出し h の names の列を印に置き換える。only_kind が (kind の列名, 値) なら、その kind の行だけ置き換える（ほかの行はそのまま）。"""
    idx = {h.index(n) for n in names}
    kc = h.index(only_kind[0]) if only_kind else None
    out = []
    for r in rows:
        if only_kind is not None and r[kc] != only_kind[1]:
            out.append(list(r))
        else:
            out.append([b'<wall>' if i in idx else c for i, c in enumerate(r)])
    return out


def cmp_disccontact(ta, tb):
    """tb（今）は ta（基準）の見出しの末尾に sellerUid を足した形。sellerUid を落として合算し直して ta と合うか。(合う, 分かれた行の数)"""
    ha, ra = ta
    hb, rb = tb
    if hb[:-1] != ha or hb[-1] != b'sellerUid':
        return False, 0
    ia = {c: i for i, c in enumerate(ha)}
    keycols = [c for c in ha if c != b'valuePerQuarter']
    vi = ia[b'valuePerQuarter']

    def key(r):
        return tuple(r[ia[c]] for c in keycols)
    base = {}
    for r in ra:
        k = key(r)
        if k in base:
            return False, 0   # 基準の側で鍵が重なる（合算の前提が崩れる）
        base[k] = float(r[vi])
    summed, count = {}, Counter()
    for r in rb:
        k = key(r[:-1])
        summed[k] = summed.get(k, 0.0) + float(r[vi])
        count[k] += 1
    if set(summed) != set(base):
        return False, 0
    for k, v in summed.items():
        if not math.isclose(v, base[k], rel_tol=1e-9, abs_tol=0.0):
            return False, 0
    return True, sum(c - 1 for c in count.values())


fa = sorted(os.listdir(A))
fb = sorted(os.listdir(B))
same = diff = 0
notes = []
split = 0
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
    ta, tb = read_table(pa), read_table(pb)
    ok = False
    if ta is not None and tb is not None:
        if base == 'burscan' and ta[0] == tb[0] and b'seconds' in ta[0]:
            ok = masked(*ta, [b'seconds']) == masked(*tb, [b'seconds'])
        elif base == 'clock' and ta[0] == tb[0] and b'worldTotalPerMonth' in ta[0] and b'kind' in ta[0]:
            ok = masked(*ta, [b'worldTotalPerMonth'], (b'kind', b'C')) == masked(*tb, [b'worldTotalPerMonth'], (b'kind', b'C'))
        elif base == 'disccontact':
            ok, sp = cmp_disccontact(ta, tb)
            if ok:
                split += sp
    notes.append(('colsok' if ok else 'DIFF', f))
print('same', same, 'differ', diff, 'disccontactSplitRows', split)
print(Counter((n[0], n[1].split('_s')[0]) for n in notes))
bad = [n for n in notes if n[0] in ('DIFF', 'only')]
for n in bad:
    print(n)
print('BAD', len(bad))
