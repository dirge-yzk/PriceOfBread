# (1225) 紙だけ：m1216 の paper/f_open.tsv（開幕・主の文化の人口）から f の 3 つの形を国ごとに並べる（主の文化の行だけ・少数の州は f_open に無い＝近似）
# 形 0（今）：1−自国の X÷世界の X。形 1：1−〈X を主にする国のうち最大の国の X〉÷世界の X（同じ文化の国は同じ値）。形 2：1−Σ（各国の X÷世界の X）²（X を主にする国だけで数える）。
import sys, csv, collections
rows = list(csv.DictReader(open(sys.argv[1], encoding="utf-8", newline=""), delimiter="\t"))
by = collections.defaultdict(list)
for r in rows: by[r["main"]].append(r)
out = open(sys.argv[2], "w", encoding="utf-8", newline="\n")
out.write("nation\tmain\tf0_now\tf1_outside_largest\tf2_hhi\tlargest\n")
for cul, rs in sorted(by.items()):
    world = float(rs[0]["worldMainK"]); own = [(float(r["ownMainK"]), r["nation"]) for r in rs]
    mx, mxn = max(own)
    f1 = 1 - mx / world; f2 = 1 - sum((o / world) ** 2 for o, _ in own)
    for r in rs:
        out.write(f"{r['nation']}\t{cul}\t{1-float(r['ownMainK'])/world:.3f}\t{f1:.3f}\t{f2:.3f}\t{mxn}\n")
out.close()
