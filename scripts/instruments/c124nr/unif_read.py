"""(1230) c124nr 門と記録の読み手（読むだけ）：史実の桁 Hu1・Hu2・Hw（wars_s）・入口の札ごとの件数・計器 unifwar_s と formation2_s の集計。
  Hu1＝統一の宣戦（entryCode=declare・内戦を除く）の件数の 10 種の平均（赤＝100 超）。Hu2＝種ごとの同じ組（攻め手×標的）の最多の 10 種の平均（赤＝30 超）。
  Hw＝内戦を除く戦争の数 腕÷基準（赤＝10 以上）。記録：同盟の参戦（ally）・宗主の防衛（suzerain）の件数・unifwar_s の旧と新とエンジンの判定・formation2_s の旗 C の門の食い違い。
  使い方: python unif_read.py <基準の生> <腕の生>"""
import sys, os, csv, collections, statistics as st
BASE, ARM = sys.argv[1], sys.argv[2]

def wars(d):
    hu1, hu2, tot, ent = [], [], 0, collections.Counter()
    for s in range(1, 11):
        n = 0; pairs = collections.Counter()
        with open(f"{d}/wars_s{s}.tsv", encoding="utf-8", newline="") as f:
            for r in csv.DictReader(f, delimiter="\t"):
                if r["civilWarFlag"] != "0": continue
                tot += 1; ent[r["entryCode"]] += 1
                if r["entryCode"] == "declare": n += 1; pairs[(r["sideA"], r["sideB"])] += 1
        hu1.append(n); hu2.append(max(pairs.values()) if pairs else 0)
    return hu1, hu2, tot, ent

wb, wa = wars(BASE), wars(ARM)
for tag, w in (("基準", wb), ("腕", wa)):
    print(f"== {tag}：Hu1 種ごと {w[0]} 平均 {sum(w[0]) / 10:.1f}・Hu2 種ごとの最多 {w[1]} 平均 {sum(w[1]) / 10:.1f}・戦争（内戦を除く）{w[2]}")
    print("   入口の札", dict(w[3].most_common()))
hw = wa[2] / wb[2] if wb[2] else float("nan")
hu1, hu2 = sum(wa[0]) / 10, sum(wa[1]) / 10
print(f"門 Hu1 {hu1:.1f}（赤＝100 超）{'赤' if hu1 > 100 else '通る'}・旧の下書きの 80 を超えたか {'超えた' if hu1 > 80 else '超えない'}")
print(f"門 Hu2 {hu2:.1f}（赤＝30 超）{'赤' if hu2 > 30 else '通る'}")
print(f"門 Hw {hw:.3f}（赤＝10 以上）{'赤' if hw >= 10 else '通る'}・ally {wa[3]['ally']}÷{wb[3]['ally']}・suzerain {wa[3]['suzerain']}÷{wb[3]['suzerain']}")

def unifwar(d):
    c = collections.Counter(); att = collections.Counter()
    for s in range(1, 11):
        p = f"{d}/unifwar_s{s}.tsv"
        if not os.path.exists(p): return None
        with open(p, encoding="utf-8", newline="") as f:
            for r in csv.DictReader(f, delimiter="\t"):
                c[(r["oldCode"], r["newCode"], r["engineCode"])] += 1
                if r["engineCode"] == "4": att[r["attacker"]] += 1
    return c, att

u = unifwar(ARM)
if u:
    print("== unifwar_s（旧・新・エンジン）の件数", dict(u[0].most_common()))
    print("   エンジンが厭戦で塞いだ攻め手", u[1].most_common(10))
else:
    print("== unifwar_s 無し")

def form(d):
    diff = collections.Counter(); rows = 0; rel = collections.defaultdict(list)
    for s in range(1, 11):
        p = f"{d}/formation2_s{s}.tsv"
        if not os.path.exists(p): return None
        with open(p, encoding="utf-8", newline="") as f:
            for r in csv.DictReader(f, delimiter="\t"):
                rows += 1
                if r["rule"] == "北ドイツ連邦": continue
                a, cu = float(r["natAll"]), float(r["natCulture"])
                if (a >= 0.25) != (cu >= 0.25): diff[(r["nation"], "全体≧門" if a >= 0.25 else "文化≧門")] += 1
                rel[r["nation"]].append(a - cu)
    return rows, diff, {k: st.median(v) for k, v in rel.items()}

fm = form(ARM)
if fm:
    print(f"== formation2_s 行 {fm[0]}・旗 C の門が食い違う国×四半期（μ7 の世界の差）", dict(fm[1].most_common(12)))
    print("   全体−文化の取り分の中央", {k: round(v, 4) for k, v in sorted(fm[2].items(), key=lambda kv: -abs(kv[1]))[:12]})
else:
    print("== formation2_s 無し")
