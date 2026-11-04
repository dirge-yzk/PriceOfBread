# (832) flow_s を呼び出し位置（sourceFile・sourceLine）を除いた行の多重集合で比べる（読むだけ・ident_check.py の FLOW_LOC と同じ考え）。
#   python -X utf8 -I flowcmp826.py <新しい生の ins> <古い生の ins> [種の起点] [種の数]
import sys, os, collections
new, old = sys.argv[1], sys.argv[2]
s0 = int(sys.argv[3]) if len(sys.argv) > 3 else 1
n = int(sys.argv[4]) if len(sys.argv) > 4 else 10
DROP = {b'sourceFile', b'sourceLine'}
def ms(p):
    ls = open(p, 'rb').read().split(b'\n'); h = ls[0].rstrip(b'\r').split(b'\t')
    keep = [i for i, c in enumerate(h) if c not in DROP]
    return collections.Counter(tuple(r.rstrip(b'\r').split(b'\t')[i] for i in keep) for r in ls[1:] if r.strip()), [h[i] for i in keep]
bad = 0
for s in range(s0, s0 + n):
    a, ha = ms(os.path.join(new, f'flow_s{s}.tsv')); b, hb = ms(os.path.join(old, f'flow_s{s}.tsv'))
    ok = a == b and ha == hb
    bad += 0 if ok else 1
    print(f'flow_s{s}: 行 新 {sum(a.values())}・古 {sum(b.values())}・呼び出し位置を除いた多重集合 {"一致" if ok else "不一致"}')
print('判定:', '一致' if bad == 0 else f'不一致 {bad}')
