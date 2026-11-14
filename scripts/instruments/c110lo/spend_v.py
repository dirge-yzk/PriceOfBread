# (1130) c110lo：既にある spend_s を (1127) の反証役 v の物差し〈額×月数・分けられる分だけ〉で読む（ヌル腕の幅の材料・記録）。
# 費目＝slotName〈建設に付く線路と機関車〉（線路と機関車の和・分けられない）と〈インフラ保守〉（セメント・線路・機関車・要員・石炭の和）。
# 窓（firstQuarter〜lastQuarter）を国の機関車の発見の四半期（disc_s の E 行の quarter の最小）と比べる：窓の終わりが発見より前＝pre・
# 窓の頭が発見以後＝post・またぐ＝mixed・E 行の無い国＝noE。額＝payPerMonth×monthsCount（月数で重みを付ける）。
import sys, os, collections
def run(d, seeds):
    print("seed\tslot\tpre\tmixed\tnoE\tpost\ttotal")
    for s in seeds:
        disc = {}
        with open(os.path.join(d, f"disc_s{s}.tsv"), encoding="utf-8") as f:
            next(f)
            for ln in f:
                c = ln.rstrip("\n").split("\t")
                if c[0] == "E" and c[4] == "機関車": disc[c[3]] = min(disc.get(c[3], 10**9), int(c[1]))
        acc = collections.defaultdict(float)
        with open(os.path.join(d, f"spend_s{s}.tsv"), encoding="utf-8") as f:
            h = next(f).rstrip("\n").split("\t"); ix = {k: i for i, k in enumerate(h)}
            for ln in f:
                c = ln.rstrip("\n").split("\t")
                sl = c[ix["slotName"]]
                if sl not in ("建設に付く線路と機関車", "インフラ保守"): continue
                try: v = float(c[ix["payPerMonth"]]) * float(c[ix["monthsCount"]])
                except ValueError: continue
                if not v == v: continue
                n = c[ix["nation"]]; q0 = int(c[ix["firstQuarter"]]); q1 = int(c[ix["lastQuarter"]])
                if n not in disc: k = "noE"
                elif q1 < disc[n]: k = "pre"
                elif q0 >= disc[n]: k = "post"
                else: k = "mixed"
                acc[(sl, k)] += v
        for sl in ("建設に付く線路と機関車", "インフラ保守"):
            t = sum(acc[(sl, k)] for k in ("pre", "mixed", "noE", "post"))
            print("\t".join([str(s), sl] + [f"{acc[(sl, k)] / t:.4f}" if t > 0 else "nan" for k in ("pre", "mixed", "noE", "post")] + [f"{t:.6g}"]))
if __name__ == "__main__":
    run(sys.argv[1], [int(x) for x in sys.argv[2].split(",")] if len(sys.argv) > 2 else list(range(1, 11)))
