"""(1085) 台帳の行の TSV の点検：12 列・id の重複 0・錨（ファイル:行:語）の行にその語が在ること（読む木＝main の作業ツリー decb50fa5）。
使い方: python -X utf8 -I check_tsv.py <tsv> <repo>
"""
import sys, os, subprocess

tsv, repo = sys.argv[1], sys.argv[2]
files = subprocess.run(["git", "-C", repo, "ls-files"], capture_output=True, text=True, encoding="utf-8").stdout.split("\n")
head = subprocess.run(["git", "-C", repo, "rev-parse", "--short=9", "HEAD"], capture_output=True, text=True).stdout.strip()
print("HEAD", head)
bad = 0; ids = set(); n = 0; anchors = 0
with open(tsv, encoding="utf-8") as f:
    hdr = f.readline().rstrip("\n").split("\t")
    if len(hdr) != 12: print("header cols", len(hdr)); bad += 1
    for line in f:
        c = line.rstrip("\n").split("\t"); n += 1
        if len(c) != 12: print("cols", c[0], len(c)); bad += 1; continue
        if c[0] in ids: print("dup", c[0]); bad += 1
        ids.add(c[0])
        for a in c[4].split(";"):
            if a in ("", "—"): continue
            fn, ln, tok = a.split(":", 2)
            cand = [p for p in files if os.path.basename(p) == fn]
            if len(cand) != 1: print("file?", c[0], fn, len(cand)); bad += 1; continue
            with open(os.path.join(repo, cand[0]), encoding="utf-8-sig") as g:
                lines = g.read().split("\n")
            anchors += 1
            if not (0 < int(ln) <= len(lines) and tok in lines[int(ln) - 1]):
                print("anchor miss", c[0], a); bad += 1
print(f"rows {n} anchors {anchors} bad {bad}")
