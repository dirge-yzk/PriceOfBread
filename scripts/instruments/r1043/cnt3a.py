# (1043) 紙 3a-3 が 0 でない行の数（前が立憲・事象なし・王冠あり）と、王冠の無い行・協力度≧0 の行（形の誤りの量の旗 0 の値）
import sys, glob, os, csv
csv.field_size_limit(1 << 30)
n = nz = rows = coopNeg = 0
for p in sorted(glob.glob(os.path.join(sys.argv[1], "crownpc_s*.tsv"))):
    with open(p, encoding="utf-8", newline="") as f:
        for r in csv.DictReader(f, delimiter="\t"):
            if r["firstRowFlag"] == "1" or r["regimeEventFlag"] == "1" or r["explainCode"] not in ("0", "9"): continue
            if r["polityPrevCode"] != "3" or r["crownPrevCode"] == "0": continue
            rows += 1
            c, l, a, pc = (float(r[k]) for k in ("coopParliamentCrownPrevIndex", "legitimacyIndex", "armyAllegiancePrevIndex", "pcPrevShare"))
            if c < 0: coopNeg += 1
            if l > 0.7 and a > 0.3 and c < 0 and pc > 0: nz += 1
print(f"前が立憲・王冠あり・事象なしの行 {rows}・協力度＜0 {coopNeg}（{100*coopNeg/rows:.1f}%）・紙 3a-3 が 0 でない {nz}（{100*nz/rows:.1f}%）")
