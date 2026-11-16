# 読むだけ (1169)：注と空白を除いたコードの差（OLD→NEW）を出す。関数名の指定があればその関数だけ。
# 使い方: python -X utf8 -I codediff.py <repo> <OLD> <NEW> <ファイル名> [関数名...]
import sys, difflib, importlib.util, os
repo, OLD, NEW, fn = sys.argv[1:5]; names = sys.argv[5:]
sys.argv = ['x', repo, OLD, NEW, os.devnull, os.devnull]
here = os.path.dirname(os.path.abspath(__file__))
src = open(os.path.join(here, 'cmp_rows.py'), 'rb').read().decode('utf-8')
src = src[:src.index('CO, CN = coef_decl(OLD)')]
g = {}; exec(compile(src, 'cmp_rows', 'exec'), g)
po, pn = g['resolve'](OLD, fn), g['resolve'](NEW, fn)
so, sn = g['slines'](OLD, po), g['slines'](NEW, pn)
def pick(commit, path, S):
    if not names: return [(k + 1, x) for k, x in enumerate(S) if x]
    r = []
    for nm in names:
        for k, x in enumerate(S):
            m = g['SIG'].match(x)
            if m and m.group(1) == nm and not x.endswith(';'):
                e = g['enclosing'](commit, path, k + 1)
                if e: r += [(q + 1, S[q]) for q in range(e[1], e[2] + 1) if S[q]]
    return r
A, B = pick(OLD, po, so), pick(NEW, pn, sn)
sm = difflib.SequenceMatcher(a=[x for _, x in A], b=[x for _, x in B], autojunk=False)
for op, i1, i2, j1, j2 in sm.get_opcodes():
    if op == 'equal': continue
    for k in range(i1, i2): print(f'- {A[k][0]}: {A[k][1][:200]}')
    for k in range(j1, j2): print(f'+ {B[k][0]}: {B[k][1][:200]}')
    print('--')
