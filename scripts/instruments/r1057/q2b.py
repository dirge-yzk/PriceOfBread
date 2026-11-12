"""(1057) 問い2 の補い：6 国の参政権の段（1870・1913・1950）と WomenRights の戦線が開いた国の符号の内訳。読むだけ。"""
import csv, sys, os, collections
csv.field_size_limit(10**9)
ROOT = sys.argv[1]
ARMS = [("base", "base_ins", range(1, 11)), ("n1", "n1", range(1, 11)), ("n2", "n2", range(1, 11)), ("a11", "a11", range(11, 21))]
SIX = ["イギリス", "フランス", "プロイセン", "アメリカ", "日本", "ロシア"]
tab = collections.Counter()
for arm, sub, seeds in ARMS:
    for s in seeds:
        last = {}
        with open(os.path.join(ROOT, sub, f"monlaw_s{s}.tsv"), encoding="utf-8", newline="") as f:
            for x in csv.DictReader(f, delimiter="\t"):
                if x["nation"] in SIX and x["year"] in ("1870", "1913", "1950"):
                    last[(x["nation"], x["year"])] = x["Suffrage"]
        for (nm, y), v in last.items():
            tab[(arm, y, nm, v)] += 1
for arm, _, _ in ARMS:
    for y in ("1870", "1913", "1950"):
        print(arm, y, "  ".join(nm + ":" + ",".join(f"{k[3]}={v}" for k, v in sorted(tab.items()) if k[0] == arm and k[1] == y and k[2] == nm) for nm in SIX))
