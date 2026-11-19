# (1238) 読むだけ：英の 1914-18 に重なる戦争（内戦を除く）を種ごとに並べる。引数：生のフォルダ 種0 種1 [国名]
import sys, csv
d, s0, s1 = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]); nat = sys.argv[4] if len(sys.argv) > 4 else "イギリス"
for s in range(s0, s1 + 1):
    rows = []
    with open(f"{d}/wars_s{s}.tsv", encoding="utf-8", newline="") as f:
        for r in csv.DictReader(f, delimiter="\t"):
            if r["civilWarFlag"] != "0": continue
            if nat not in (r["sideA"], r["sideB"]): continue
            y0 = int(r["startYear"]); y1 = y0 + float(r["months"]) / 12.0
            if y0 <= 1918 and y1 >= 1914:
                other = r["sideB"] if r["sideA"] == nat else r["sideA"]
                side = "A" if r["sideA"] == nat else "B"
                rows.append(f"{y0}+{float(r['months']):.0f}m {side} vs {other} [{r['entryCode']}]")
    cas = 0.0
    with open(f"{d}/casualty_s{s}.tsv", encoding="utf-8", newline="") as f:
        for r in csv.DictReader(f, delimiter="\t"):
            if r["nation"] == nat and 1914 <= int(r["year"]) <= 1918: cas += float(r["soldierMonthsK"])
    print(f"s{s}\twar={len(rows)}\tsoldierMonthsK1914_18={cas:.0f}\t" + " | ".join(rows))
