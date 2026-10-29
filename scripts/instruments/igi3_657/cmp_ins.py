# (657) 線 igi3：計器つきの腕の ins フォルダを基線の ins フォルダと表ごとにバイト比較する（読むだけ）。
# 使い方：python cmp_ins.py <基線の ins> <腕の ins>
# 規則：burscan は秒の列（毎回違う）なので比べない／flow は sourceLine の列を除いて比べる／それ以外は 1 バイトも違わないこと。
# 出力：同じ表の数・違う表（名前）・基線にあって腕に無い表・腕にだけ在る表（新しい表）。
import sys, os, hashlib
a, b = sys.argv[1], sys.argv[2]
def h(p): return hashlib.sha256(open(p, 'rb').read()).hexdigest()
def drop(p, col):
    L = open(p, 'rb').read().decode('utf-8').split('\n'); hd = L[0].split('\t')
    if col not in hd: return '\n'.join(L)
    i = hd.index(col); out = []
    for x in L:
        f = x.split('\t')
        if len(f) > i: del f[i]
        out.append('\t'.join(f))
    return '\n'.join(out)
fa, fb = set(os.listdir(a)), set(os.listdir(b))
same = diff = skipped = 0; diffs = []
for n in sorted(fa & fb):
    if n.startswith('burscan_'): skipped += 1; continue
    if n.startswith('flow_'):
        ok = drop(f"{a}/{n}", 'sourceLine') == drop(f"{b}/{n}", 'sourceLine')
    else: ok = h(f"{a}/{n}") == h(f"{b}/{n}")
    if ok: same += 1
    else: diff += 1; diffs.append(n)
print("same", same, "diff", diff, "skipped(burscan)", skipped)
print("diff_tables", diffs[:40])
print("missing_in_arm", sorted(fa - fb)[:40])
newt = sorted(fb - fa); print("new_in_arm", len(newt), sorted({x.rsplit('_s', 1)[0] for x in newt}))
