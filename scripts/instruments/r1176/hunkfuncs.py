# 読むだけ (1169)：持ち主のファイルごとに、注と空白を除いたコードの差の塊を、囲む関数の名で数える（OLD→NEW）。
# 使い方: python -X utf8 -I hunkfuncs.py <repo> <OLD> <NEW> <ファイル名...>
import sys, difflib, os, collections
repo, OLD, NEW = sys.argv[1:4]; fns = sys.argv[4:]
sys.argv = ['x', repo, OLD, NEW, os.devnull, os.devnull]
here = os.path.dirname(os.path.abspath(__file__))
src = open(os.path.join(here, 'cmp_rows.py'), 'rb').read().decode('utf-8')
src = src[:src.index('CO, CN = coef_decl(OLD)')]
g = {}; exec(compile(src, 'cmp_rows', 'exec'), g)
def fname(commit, path, ln):
    S = g['slines'](commit, path); i = ln - 1
    while i >= 0:
        m = g['SIG'].match(S[i])
        if m and not S[i].endswith(';'): return m.group(1)
        i -= 1
    return '(外)'
for fn in fns:
    po, pn = g['resolve'](OLD, fn), g['resolve'](NEW, fn)
    if not po or not pn: print(fn, '無い', po, pn); continue
    so, sn = g['slines'](OLD, po), g['slines'](NEW, pn)
    A = [(k + 1, x) for k, x in enumerate(so) if x]; B = [(k + 1, x) for k, x in enumerate(sn) if x]
    sm = difflib.SequenceMatcher(a=[x for _, x in A], b=[x for _, x in B], autojunk=False)
    cnt = collections.Counter()
    for op, i1, i2, j1, j2 in sm.get_opcodes():
        if op == 'equal': continue
        names = set()
        for k in range(i1, i2): names.add('-' + fname(OLD, po, A[k][0]))
        for k in range(j1, j2): names.add('+' + fname(NEW, pn, B[k][0]))
        for n in names: cnt[n] += 1
    print(fn, ' '.join(f'{k}:{v}' for k, v in sorted(cnt.items())))
