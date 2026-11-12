"""(1057) 問い2 の確かめと問い3：全ての国の L.WomenRights の戦線の符号・段の上下の回と直前の姿勢・段>0 の国の最後の行。読むだけ。
使い方: python -X utf8 -I q3.py <keep_1053 の親> > q3_out.txt
"""
import csv, sys, os, collections

ROOT = sys.argv[1]
ARMS = [("base", "base_ins", range(1, 11)), ("n1", "n1", range(1, 11)), ("n2", "n2", range(1, 11)), ("a11", "a11", range(11, 21))]
csv.field_size_limit(10**9)


def rows(p):
    with open(p, encoding="utf-8", newline="") as f:
        yield from csv.DictReader(f, delimiter="\t")


front_all = collections.Counter()      # (arm, code) 全ての国
front_nat = collections.Counter()      # (arm, nation) 戦線ありの四半期
edu_front = collections.Counter()      # 読みの確かめ：L.EducationLaw の符号（全ての国）
up_src = collections.Counter()         # (arm, 直前 8 四半期に姿勢 0/2 があったか)
down_all = collections.Counter()
fate = collections.Counter()           # (arm, 最後の行の段>0 か, 最後の年<1950 か)
fate_ex = []
last_year_of_run = {}

for arm, sub, seeds in ARMS:
    d = os.path.join(ROOT, sub)
    for s in seeds:
        last_year = 0
        stance_hist = collections.defaultdict(dict)  # nation -> {quarter: stance}
        for x in rows(os.path.join(d, f"modsig_s{s}.tsv")):
            stance_hist[x["nation"]][int(x["quarter"])] = x["stance"]
        for x in rows(os.path.join(d, f"nation_s{s}.tsv")):
            for it in x.get("openFrontStates", "").split(";"):
                if it.startswith("L.WomenRights:"):
                    front_all[(arm, it.split(":")[1])] += 1; front_nat[(arm, x["nation"])] += 1
                elif it.startswith("L.EducationLaw:"):
                    edu_front[(arm, it.split(":")[1])] += 1
        prev = {}; last = {}
        for x in rows(os.path.join(d, f"monlaw_s{s}.tsv")):
            u = x["nationUid"]; lv = int(x["WomenRights"]); q = int(x["quarter"]); nm = x["nation"]
            last_year = max(last_year, int(x["year"]))
            if u in prev and lv != prev[u]:
                if lv > prev[u]:
                    hs = stance_hist.get(nm, {})
                    had = any(hs.get(qq) in ("0", "2") for qq in range(q - 8, q + 1))
                    up_src[(arm, "姿勢0/2あり" if had else "姿勢0/2なし")] += 1
                else:
                    down_all[(arm, nm, x["year"], prev[u], lv)] += 1
            prev[u] = lv; last[u] = (nm, int(x["year"]), lv)
        for u, (nm, y, lv) in last.items():
            if lv > 0:
                dead = y < last_year
                fate[(arm, "国が消えた" if dead else "1950まで残る")] += 1
                if dead and len(fate_ex) < 30:
                    fate_ex.append((arm, s, nm, u, y, lv))

print("== 全ての国の L.WomenRights の戦線の符号（四半期の数）")
for k in sorted(front_all): print(k, front_all[k])
print("== 戦線ありの国（上位）")
for k, v in front_nat.most_common(25): print(k, v)
print("== 読みの確かめ：L.EducationLaw の符号")
for k in sorted(edu_front): print(k, edu_front[k])
print("\n== 段が上がった回と直前 8 四半期の姿勢（全ての国）")
for k in sorted(up_src): print(k, up_src[k])
print("== 段が下がった回（全ての国）", sum(down_all.values()))
for k in list(down_all)[:20]: print(k, down_all[k])
print("\n== 段>0 で終わった国の行の運命（uid ごと）")
for k in sorted(fate): print(k, fate[k])
print("== 例（国が消えた）")
for t in fate_ex: print(t)
