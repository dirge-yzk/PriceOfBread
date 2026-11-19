# (1232) 紙：FarmRentCap（Simulation.cs:1441）が植民地格の州の大農園（地代 0.6・Simulation.cs:1298〜1301）を削る量。読むだけ。
# 使い方: python -X utf8 -I paper_rent.py <INSRAW> <種の下> <種の上> <出力>
# 上限＝min(RentCap(土地改革, 0.3), RentCap(農奴解放, 0.5))・RentCap＝None 1.0／Limited full＋(1−full)×0.5／Full full（Simulation.cs:2345〜2350）。
# 案 1（支配度の区分で読む）：州の上限＝1−Rule[0]×(1−国の上限)（法は国家の取り分の分だけ届く）。
import sys
from collections import defaultdict
ins, lo, hi, outp = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4]
out = []
def P(s): out.append(s)
def rc(code, full): return {0: 1.0, 1: full + (1 - full) * 0.5, 2: full}[code]
seen = set()
n_col = 0; n_cap = defaultdict(int); n_cap_era = defaultdict(int); st_list = []
alt_cap = 0; alt_cut = 0.0; alt2 = [0, 0.0]; cur_cut = 0.0; bynat = defaultdict(lambda: [0, 0, 0])
for s in range(lo, hi + 1):
    with open(f"{ins}/rule_s{s}.tsv", encoding="utf-8") as f:
        h = f.readline().rstrip("\n").split("\t"); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            c = line.rstrip("\n").split("\t")
            if c[ix["tier"]] != "Colonial": continue
            y = int(float(c[ix["year"]])); key = (s, c[ix["nation"]], c[ix["region"]], y)
            if key in seen: continue
            seen.add(key); n_col += 1
            cap = min(rc(int(float(c[ix["landReformLawCode"]])), 0.3), rc(int(float(c[ix["serfLawCode"]])), 0.5))
            st = float(c[ix["ruleStateShare"]])
            nat = c[ix["nation"]]; bynat[nat][0] += 1
            if cap < 0.6:
                n_cap[round(cap, 3)] += 1; bynat[nat][1] += 1
                era = "1783-1833" if y <= 1833 else ("1834-1888" if y <= 1888 else "1889-")
                n_cap_era[era] += 1
                st_list.append(st)
                cur_cut += 0.6 - cap
                a = 1 - st * (1 - cap)
                if a < 0.6: alt_cap += 1; alt_cut += 0.6 - a; bynat[nat][2] += 1
                a2 = 1 - (st + float(c[ix["ruleLocalShare"]])) * (1 - cap)
                if a2 < 0.6: alt2[0] += 1; alt2[1] += 0.6 - a2
P(f"植民地格の州×年（種 {lo}-{hi}・年の最初の行）{n_col}")
P(f"  上限＜0.6（大農園の地代 0.6 が削られる）{sum(n_cap.values())}・上限の値ごと " + " ".join(f"{k}:{v}" for k, v in sorted(n_cap.items())))
P("  時代ごと " + " ".join(f"{k}:{v}" for k, v in sorted(n_cap_era.items())))
if st_list:
    st_list.sort(); m = len(st_list)
    P(f"  その州の国家の取り分 Rule[0]：中央 {st_list[m//2]:.3f}・下 1/4 {st_list[m//4]:.3f}・上 1/4 {st_list[3*m//4]:.3f}")
P(f"  案 1（上限＝1−Rule[0]×(1−国の上限)）で 0.6 を下回る州×年 {alt_cap}・削る量の和 {alt_cut:.1f}（今の形 {cur_cut:.1f}）")
P(f"  案 1′（上限＝1−(Rule[0]＋Rule[1])×(1−国の上限)＝税の届き RuleDelivery と同じ読み）で 0.6 を下回る州×年 {alt2[0]}・削る量の和 {alt2[1]:.1f}")
P("国ごと（植民地の州×年・今の形で削られる・案 1 で削られる）：")
for n, v in sorted(bynat.items(), key=lambda kv: -kv[1][1])[:10]:
    P(f"  {n}: {v[0]}・{v[1]}・{v[2]}")
with open(outp, "w", encoding="utf-8", newline="\n") as f: f.write("\n".join(out) + "\n")
