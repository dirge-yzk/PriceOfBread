# (1190) 紙：段 E の〈支配度を区分のストックへ畳む〉形（share 空間）の予言と、今の木の読み合わせ。
# 読むだけ。エンジンの関数は呼ばない。入力＝計器つきの生（secession_s・rule_s・rulepeace_s）。
# 使い方：python -X utf8 -I paper_e.py <INS のフォルダ> <出力.txt> [種の範囲 1-10]
import sys, math, collections

ins, outp = sys.argv[1], sys.argv[2]
lo, hi = (int(x) for x in (sys.argv[3] if len(sys.argv) > 3 else "1-10").split("-"))
SPEED = 0.025  # DominanceSpeed（Coefficients.p08_audit_soldiers.cs:110・今の値）
out = []
def P(s): out.append(s)

def rows(path):
    with open(path, encoding="utf-8") as f:
        head = f.readline().rstrip("\n").split("\t")
        ix = {h: i for i, h in enumerate(head)}
        for line in f:
            c = line.rstrip("\n").split("\t")
            if len(c) < len(head): continue
            yield ix, c

def fl(x):
    try: return float(x)
    except ValueError: return float("nan")

tot = collections.Counter()
paper_ign_by_tier = collections.Counter(); real_ign_by_tier = collections.Counter()
early_paper = 0; early_real = 0
open_auto_max = 0; open_rows = 0
both = 0; paper_only = 0; real_only = 0
lead = []  # 紙の点火の四半期 − 今の点火の四半期（両方ある州）
for s in range(lo, hi + 1):
    # rule_s：同じ四半期・国・州の外国と無統治の取り分
    gov = {}
    for ix, c in rows(f"{ins}/rule_s{s}.tsv"):
        k = (c[ix["quarter"]], c[ix["nation"]], c[ix["region"]])
        gov[k] = (fl(c[ix["ruleForeignShare"]]), fl(c[ix["ruleUngovShare"]]))
    ser = collections.defaultdict(list)
    for ix, c in rows(f"{ins}/secession_s{s}.tsv"):
        q = int(c[ix["quarter"]])
        ser[(c[ix["nation"]], c[ix["region"]])].append((q, c[ix["tier"]], fl(c[ix["pressureIndex"]]), fl(c[ix["holdShareIndex"]]),
                                                         fl(c[ix["dominanceShare"]]), c[ix["goFlag"]] == "1", c[ix["quarter"]]))
    for (nat, reg), lst in ser.items():
        lst.sort()
        a = 0.0; prev_d = None; prev_q = None
        pq = None; rq = None; tier0 = lst[0][1]
        for (q, tier, p, h, d, go, qs) in lst:
            # 突き合わせ：Δd＝(h−p)×SPEED（続いた四半期・切りに当たらない行）
            if prev_d is not None and prev_q == q - 1:
                want = prev_d + (h - p) * SPEED
                if 0 < want < 1:
                    tot["chk"] += 1
                    if abs(want - d) <= 1e-7: tot["chk_ok"] += 1
            prev_d, prev_q = d, q
            if rq is None and (go or not (d > 0)): rq = q  # goFlag は書かれない（SecObserve は点火の前）＝ d≦0 で読む
            fu = gov.get((qs, nat, reg))
            if fu is None: tot["nogov"] += 1; continue
            G = 1.0 - fu[0] - fu[1]
            if not (G > 0): continue
            rho = p / (p + h) if (p + h) > 0 and p >= 0 and h >= 0 else 0.0
            a += (rho * G - a) * SPEED
            if q == 0:
                open_rows += 1
                if rho * G > G - rho * G: open_auto_max += 1
            if pq is None and a > G - a: pq = q
        tot["regions"] += 1
        if pq is not None: paper_ign_by_tier[tier0] += 1
        if rq is not None: real_ign_by_tier[tier0] += 1
        if pq is not None and pq < 40: early_paper += 1
        if rq is not None and rq < 40: early_real += 1
        if pq is not None and rq is not None: both += 1; lead.append(pq - rq)
        elif pq is not None: paper_only += 1
        elif rq is not None: real_only += 1

P(f"# (1190) paper_e.py {ins} 種 {lo}-{hi}・DominanceSpeed {SPEED}")
P(f"突き合わせ（今の支配度の積分 Δd＝(holdShare−pressure)×{SPEED}・切りの外の続いた四半期）：{tot['chk_ok']}/{tot['chk']} 行が 1e-7 以内で一致")
P(f"州の系列 {tot['regions']}（国×州×種）・rule_s に対の無い行 {tot['nogov']}")
P(f"開幕（四半期 0）に share 空間の形で〈自立〉が最大の行：{open_auto_max}/{open_rows}")
P(f"点火した州（初回）：今の支配度 0 到達 {sum(real_ign_by_tier.values())}・紙の share 空間 {sum(paper_ign_by_tier.values())}")
P(f"  区分ごと（今）{dict(real_ign_by_tier)}")
P(f"  区分ごと（紙）{dict(paper_ign_by_tier)}")
P(f"  開幕の 10 年（四半期 0〜39）：今 {early_real}・紙 {early_paper}")
P(f"  重なり：両方 {both}・紙だけ {paper_only}・今だけ {real_only}")
if lead:
    lead.sort(); m = lead[len(lead) // 2]
    P(f"  両方ある州の〈紙−今〉の四半期の差：中央 {m}・下 1/10 {lead[len(lead)//10]}・上 1/10 {lead[(len(lead)*9)//10]}")
with open(outp, "w", encoding="utf-8", newline="\n") as f: f.write("\n".join(out) + "\n")
print("\n".join(out))
