# (1238) 読むだけ：英の握る州の数（region_s・1900 と 1914 の年の頭）と tier の内訳。引数：生 種0 種1 札
import sys, csv, collections
d, s0, s1, tag = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4]
for s in range(s0, s1 + 1):
    c = collections.Counter()
    with open(f"{d}/region_s{s}.tsv", encoding="utf-8", newline="") as f:
        for r in csv.DictReader(f, delimiter="\t"):
            if r["nation"] == "イギリス" and int(r["quarter"]) % 4 == 0 and r["year"] in ("1900","1914"): c[(r["year"], r["tier"])] += 1
    print(tag, s, "1900", sum(v for k, v in c.items() if k[0]=="1900"), "1914", sum(v for k, v in c.items() if k[0]=="1914"), dict((k[1], v) for k, v in c.items() if k[0]=="1914"), sep="\t")
