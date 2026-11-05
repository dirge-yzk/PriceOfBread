# -*- coding: utf-8 -*-
# (871) c72po 種 11〜20 の生を種 1〜10 の名へハードリンクで並べ替える（読むだけ・中身は 1 バイトも変えない）。
#   門の台本 gates_po860.py は種 1〜N しか読まない＝台本を書き換えずに種 11〜20 を読むための並べ替え。
#   python -X utf8 -I remap871.py <種 11〜20 の ins> <出力フォルダ（新しく作る）> [表の名…＝既定 secession_s rule_s]
import sys, os
src, dst = sys.argv[1], sys.argv[2]
tables = sys.argv[3:] or ["secession_s", "rule_s"]
os.makedirs(dst, exist_ok=False)
n = 0
for t in tables:
    for s in range(11, 21):
        a = os.path.join(src, "%s%d.tsv" % (t, s))
        if not os.path.exists(a): raise SystemExit("欠け：" + a)
        os.link(a, os.path.join(dst, "%s%d.tsv" % (t, s - 10))); n += 1
print("remap %s → %s：%d 本（種 11〜20 → 1〜10・表 %s）" % (src, dst, n, ",".join(tables)))
