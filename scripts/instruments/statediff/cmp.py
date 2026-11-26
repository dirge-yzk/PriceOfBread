import sys
def load(p):
    d = {}
    for l in open(p, encoding='utf-8'):
        q, g, h = l.rstrip('\n').split('\t'); d.setdefault(int(q), {})[g] = h
    return d
a, b = load(sys.argv[1]), load(sys.argv[2])
for q in sorted(a):
    if q not in b: print(f"最初のずれ：四半期 {q}：線の側に無い"); sys.exit(1)
    bad = sorted(set(a[q]) ^ set(b[q])) + [g for g in a[q] if g in b[q] and a[q][g] != b[q][g]]
    if bad:
        print(f"最初のずれ：四半期 {q}：{len(bad)} 群（先頭 5）"); [print("  " + g) for g in bad[:5]]; sys.exit(1)
print(f"一致：{len(a)} 点（四半期 0〜{max(a)}）・{len(next(iter(a.values())))} 群")
