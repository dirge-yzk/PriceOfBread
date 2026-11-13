# (1094) TSV の点検：12 列・id の重複 0・錨（ファイル:行:語）の語がその行に在る（木 23aa9d11a の作業ツリー）
# 使い方: python -X utf8 -I check_tsv.py <tsv> <SimCore dir>
import os, sys
tsv, src = sys.argv[1], sys.argv[2]
lines = open(tsv, "rb").read().decode("utf-8").rstrip("\n").split("\n")
bad = 0; ids = set(); n = 0; anc = 0
cache = {}
for i, l in enumerate(lines[1:], 2):
    c = l.split("\t")
    if len(c) != 12:
        print("cols", i, len(c)); bad += 1; continue
    if c[0] in ids:
        print("dup", c[0]); bad += 1
    ids.add(c[0]); n += 1
    for a in c[4].split(";"):
        if a in ("", "—"):
            continue
        f, ln, tok = a.split(":", 2)
        p = os.path.join(src, f)
        if p not in cache:
            cache[p] = open(p, "rb").read().decode("utf-8").split("\n")
        anc += 1
        if tok not in cache[p][int(ln) - 1]:
            print("anchor", c[0], a); bad += 1
print(f"rows {n} anchors {anc} bad {bad}")
