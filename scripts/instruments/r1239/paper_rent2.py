# (1239) 紙：FarmRentCap（Simulation.cs:1441）の案ごとに、中核・統合・植民地格の州×年で上限と実際の地代率がどう動くか。読むだけ。
# 使い方: python -X utf8 -I paper_rent2.py <INSRAW> <種の下> <種の上> <world.json> <出力>
# 地代率の出所（Simulation.cs の構築）：
#   本国の道（開幕の Core・:733 CatalogEconomy(min(FarmLordShare, 1-0.15))）＝ふつうの RGO は max(0.3, min(FLS, 0.85))（:913）・
#     賦存の RGO（州の resources）は max(その値, 0.6)（:760〜:764・所有は貴族）。
#   植民地の道（Colonial で現れた州・:1279 CatalogEconomy(0.0)）＝ふつうの RGO 0.3・輸出作物の賦存 RGO は 0.6（:1298〜:1301・所有は資本家）。
#   ⇒ 植民地の道の州は全てに輸出作物がある前提（上の見積もり）。
# 実際の地代率＝min(地代率, 上限)（EconomyStep.cs:639）。上限＝min(RentCap(土地改革,0.3), RentCap(農奴解放,0.5))（RentCap は :2343〜:2348）。
import sys, json
from collections import defaultdict
ins, lo, hi, wj, outp = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4], sys.argv[5]
W = json.load(open(wj, encoding="utf-8"))
FLS = {n["name"]: float(n.get("farmLordShare", 0.6)) for n in W["nations"]}
RES = {}
for n in W["nations"]:
    for p in n.get("provinces") or []: RES[p["city"]] = len(p.get("resources") or []) > 0
def rc(code, full): return {0: 1.0, 1: full + (1 - full) * 0.5, 2: full}[code]
OPTS = ["甲", "甲′", "丁", "戊", "乙"]
# 集計: grp -> 物
G = defaultdict(lambda: {"n": 0, "capup05": 0, "capup": [], "bind_cur": 0,
                         **{f"chg_{o}": 0 for o in OPTS}, **{f"dr_{o}": 0.0 for o in OPTS}, **{f"bind_{o}": 0 for o in OPTS}})
NAT = defaultdict(lambda: [0, 0, 0, 0])  # 中核：州×年・今削る・甲で削る量の差がある・丁で差がある
unk = set(); out = []
def P(s): out.append(s)
for s in range(lo, hi + 1):
    first = {}; seen = set()
    with open(f"{ins}/rule_s{s}.tsv", encoding="utf-8") as f:
        h = f.readline().rstrip("\n").split("\t"); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            c = line.rstrip("\n").split("\t")
            reg = c[ix["region"]]; y = int(float(c[ix["year"]])); nat = c[ix["nation"]]; tier = c[ix["tier"]]
            if reg not in first:
                path = "本国" if (tier != "Colonial" and y <= 1784) else ("植民" if tier == "Colonial" else "昇格")
                first[reg] = (path, nat)
            key = (reg, y)
            if key in seen: continue
            seen.add(key)
            path, builder = first[reg]
            if path == "本国":
                fl = max(0.3, min(FLS.get(builder, 0.6), 0.85))
                if reg not in RES: unk.add(reg)
                rents = [("ふつう", fl, True)] + ([("賦存", max(fl, 0.6), True)] if RES.get(reg, True) else [])
            elif path == "植民":
                rents = [("ふつう", 0.3, False), ("賦存", 0.6, False)]
            else:
                rents = [("ふつう", 0.3, False)]
            cap = min(rc(int(float(c[ix["landReformLawCode"]])), 0.3), rc(int(float(c[ix["serfLawCode"]])), 0.5))
            r0 = float(c[ix["ruleStateShare"]]); r1 = float(c[ix["ruleLocalShare"]])
            caps = {"甲": 1 - r0 * (1 - cap), "甲′": 1 - min(1.0, r0 + r1) * (1 - cap),
                    "丁": 1.0 if tier == "Colonial" else cap, "戊": cap, "乙": cap}
            grp = f"{tier}・{path}"
            g = G[grp]; g["n"] += 1
            d = caps["甲"] - cap
            if d > 0.05: g["capup05"] += 1
            if cap < 1: g["capup"].append(d)
            bc = any(r > cap for _, r, _ in rents)
            if bc: g["bind_cur"] += 1
            for o in OPTS:
                chg = False; dr = 0.0; bnd = False
                for kind, r, noble in rents:
                    rr = r
                    cp = caps[o]
                    if o == "戊" and not noble: cp = 1.0            # 戊：地主（貴族）の取り分だけに当てる
                    if o == "乙" and path == "植民" and kind == "賦存": rr = 0.5   # 乙：植民地の大農園の地代 0.6→0.5（データ）
                    e_new = min(rr, cp); e_old = min(r, cap)
                    if abs(e_new - e_old) > 1e-9: chg = True; dr += e_new - e_old
                    if rr > cp: bnd = True
                if chg: g[f"chg_{o}"] += 1; g[f"dr_{o}"] += dr
                if bnd: g[f"bind_{o}"] += 1
            if tier == "Core":
                v = NAT[nat]; v[0] += 1; v[1] += bc
                v[2] += any(abs(min(r, caps["甲"]) - min(r, cap)) > 1e-9 for _, r, _ in rents)
                v[3] += any(abs(min(r, caps["丁"]) - min(r, cap)) > 1e-9 for _, r, _ in rents)
P(f"種 {lo}-{hi}・州×年（年の最初の行）。〈今削る〉＝地代率のどれかが今の上限を超える州×年。〈変わる〉＝案で実際の地代率が今と違う州×年。Δ＝実際の地代率の差の和（州の中の RGO の種類ごとに足す）。")
P("区分（今の tier・構築の道） | 州×年 | 甲で上限が 0.05 超上がる | 今削る | " + " | ".join(f"{o}：変わる・Δの和・削る" for o in OPTS))
for k in sorted(G):
    g = G[k]; n = g["n"]
    P(f"{k} | {n} | {g['capup05']}（{100*g['capup05']/max(n,1):.1f}%） | {g['bind_cur']} | " +
      " | ".join(f"{g['chg_'+o]}・{g['dr_'+o]:+.1f}・{g['bind_'+o]}" for o in OPTS))
P("中核（Core）の国ごと（州×年・今削る・甲で地代率が変わる・丁で変わる）上位 15：")
for n_, v in sorted(NAT.items(), key=lambda kv: -kv[1][2])[:15]:
    P(f"  {n_}: {v[0]}・{v[1]}・{v[2]}・{v[3]}")
P(f"province の表に無い本国の道の州（賦存ありと置いた）：{len(unk)} {sorted(unk)[:8]}")
with open(outp, "w", encoding="utf-8", newline="\n") as f: f.write("\n".join(out) + "\n")
