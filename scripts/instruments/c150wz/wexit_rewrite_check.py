# (1408) c150wz 項 2(c)：wexit_s の行の中身が、旧の崩壊の書き換えの前の戦勝点を読む形へ直った（計器だけ）ことの検算。
# 呼び方：python wexit_rewrite_check.py <基準の表> <腕の表>。0＝宣言どおり（行の数と列の数が同じ・違う行は旧の崩壊の旗が立つ行だけ・
# 違う列は scoreIndex・oldDecisiveFlag・newEndsFlag だけ）。非 0＝不一致。
import sys
def rows(p):
    with open(p, encoding='utf-8') as f:
        return [l.rstrip('\n').split('\t') for l in f]
a, b = rows(sys.argv[1]), rows(sys.argv[2])
if len(a) != len(b): print(f'行の数が違う {len(a)} {len(b)}'); sys.exit(1)
if a[0] != b[0]: print('見出しが違う'); sys.exit(1)
h = a[0]; ok_cols = {h.index(x) for x in ('scoreIndex', 'oldDecisiveFlag', 'newEndsFlag')}
ia, ib = h.index('oldCollapseAFlag'), h.index('oldCollapseBFlag')
diff = 0
for x, y in zip(a[1:], b[1:]):
    if x == y: continue
    diff += 1
    if len(x) != len(y): print('列の数が違う'); sys.exit(1)
    if x[ia] != '1' and x[ib] != '1': print('崩壊の旗が無い行が違う', x[:4]); sys.exit(1)
    bad = [h[i] for i in range(len(x)) if x[i] != y[i] and i not in ok_cols]
    if bad: print('宣言の外の列が違う', bad); sys.exit(1)
print(f'wexit_s 検算 OK 違う行 {diff}（全て旧の崩壊の旗が立つ行・scoreIndex・oldDecisiveFlag・newEndsFlag だけ）')
