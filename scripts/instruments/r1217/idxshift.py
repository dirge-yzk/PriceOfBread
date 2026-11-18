# 直近 8 四半期（q660..667）で nation_s の nationIndex が変わった国の数（SelfPlay.cs:1499 の i 番目の窓の取り違えの数え）
import sys
raw, rng = sys.argv[1], sys.argv[2]
s0, s1 = map(int, rng.split('-'))
for s in range(s0, s1+1):
    try: f = open(f"{raw}/nation_s{s}.tsv", encoding="utf-8")
    except FileNotFoundError: print(s, "表なし"); continue
    idx = {}
    with f:
        f.readline()
        for l in f:
            c = l.split("\t", 4)
            q = int(c[0])
            if q >= 660: idx.setdefault(c[3], set()).add(int(c[2]))
    sh = [n for n, v in idx.items() if len(v) > 1]
    print(s, "窓の中で番号が変わった国", len(sh), "／", len(idx), ",".join(sh[:6]))
