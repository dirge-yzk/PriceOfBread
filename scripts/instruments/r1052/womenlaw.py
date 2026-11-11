# (1052) c99lb：女性の権利の法の段（monlaw_s の WomenRights・0 なし 1 財産権 2 教育権 3 選挙権）を、6 群×1870/1913/1950 で種ごとに並べる（読むだけ）。
# 使い方: python womenlaw.py <生のフォルダ> <出力の .txt>
import csv, os, sys
RAW, OUT = sys.argv[1], sys.argv[2]
GROUPS = [("英", ("イギリス",)), ("仏", ("フランス",)), ("独", ("プロイセン", "ドイツ")), ("米", ("アメリカ",)), ("露", ("ロシア", "ソ連", "ソビエト")), ("日", ("日本",))]
YEARS = (1870, 1913, 1950)
def grp(n):
    for g, ks in GROUPS:
        if any(k in n for k in ks): return g
res = {}
for s in range(1, 11):
    with open(os.path.join(RAW, f"monlaw_s{s}.tsv"), encoding="utf-8", newline="") as f:
        for r in csv.DictReader(f, delimiter="\t"):
            y = int(r["year"])
            if y not in YEARS: continue
            g = grp(r["nation"])
            if g: res[(g, y, s)] = r["WomenRights"]
lines = ["群@年\t" + "\t".join(f"s{s}" for s in range(1, 11))]
for g, _ in GROUPS:
    for y in YEARS:
        lines.append(f"{g}@{y}\t" + "\t".join(res.get((g, y, s), "-") for s in range(1, 11)))
with open(OUT, "wb") as f:
    f.write(("\n".join(lines) + "\n").encode("utf-8"))
print("ok", OUT)
