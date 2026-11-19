# (1238) 読むだけ：1900-50 に英と開戦した相手（wars_s・内戦を除く）を数える（10 種の和）と、英がインドの州（ボンベイ・マドラス・中核ベンガル・内陸ベンガル）を 1900 に握る種の数。引数：生 種0 種1
import sys, csv, collections
d, s0, s1 = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
c = collections.Counter(); ind = collections.Counter()
IND = ("ボンベイ","マドラス","中核ベンガル","内陸ベンガル")
for s in range(s0, s1 + 1):
    with open(f"{d}/wars_s{s}.tsv", encoding="utf-8", newline="") as f:
        for r in csv.DictReader(f, delimiter="\t"):
            if r["civilWarFlag"] == "0" and int(r["startYear"]) >= 1900 and "イギリス" in (r["sideA"], r["sideB"]):
                c[r["sideB"] if r["sideA"] == "イギリス" else r["sideA"]] += 1
    with open(f"{d}/region_s{s}.tsv", encoding="utf-8", newline="") as f:
        for r in csv.DictReader(f, delimiter="\t"):
            if r["year"] == "1900" and int(r["quarter"]) % 4 == 0 and r["region"] in IND and r["nation"] == "イギリス": ind[r["region"]] += 1
print("相手の上位", c.most_common(12)); print("1900 にインドの州を握る種", dict(ind))
