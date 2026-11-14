# (1130) c110lo の門の台本（事前登録 measurements/m1130_20261114_prereg_lo.txt §門）。railspend_s だけを読む（エンジンを呼ばない）。
#   python -X utf8 -I gates_lo.py <腕の ins> <旗 0 の ins> <種 1,2,...>
#   G1：腕の pre の機関車の払い（建設＋保守・金建て）の和＝0（許し 1e-12）／G2：腕の pre の保守の機関車＝0／
#   G1b：腕の post の機関車の払いの和÷旗 0 の同じ和が 0.5〜2.0／G3：腕の pre の線路の払い（建設＋保守）の和÷旗 0 の同じ和が 0.5 以上。
#   働きの大きさ（記録）：旗 0 で pre の機関車の払い÷機関車の払いの全期間（額＝行の和＝月の払いの和）。
import sys, os
def load(d, s):
    p = os.path.join(d, f"railspend_s{s}.tsv")
    if not os.path.exists(p): return None
    t = {}
    with open(p, encoding="utf-8") as f:
        h = next(f).rstrip("\n").split("\t")
        for k in h[4:]: t[k] = 0.0
        n = 0
        for ln in f:
            c = ln.rstrip("\n").split("\t")
            if len(c) < len(h): continue
            for i, k in enumerate(h):
                if i >= 4: t[k] += float(c[i])
            n += 1
    t["_rows"] = n
    return t
def g(t, *ks): return sum(t[k] for k in ks)
LOCO_PRE = ("preBuildLocoAmtGold", "preMaintLocoAmtGold"); LOCO_POST = ("postBuildLocoAmtGold", "postMaintLocoAmtGold")
RAIL_PRE = ("preBuildRailAmtGold", "preMaintRailAmtGold")
def main():
    a, b, seeds = sys.argv[1], sys.argv[2], [int(x) for x in sys.argv[3].split(",")]
    S = {"aLocoPre": 0, "aMaintLocoPre": 0, "aLocoPost": 0, "bLocoPost": 0, "aRailPre": 0, "bRailPre": 0, "bLocoPre": 0, "bMaintLocoPre": 0}
    print("seed\trowsArm\trowsBase\tarmLocoPre\tarmMaintLocoPre\tbaseLocoPre\tbaseLocoPreShare\tpostRatio\tpreRailRatio")
    miss = 0
    for s in seeds:
        A, B = load(a, s), load(b, s)
        if A is None or B is None: print(f"{s}\t欠け"); miss += 1; continue
        lp, mlp, lpo, rp = g(A, *LOCO_PRE), A["preMaintLocoAmtGold"], g(A, *LOCO_POST), g(A, *RAIL_PRE)
        blp, blpo, brp = g(B, *LOCO_PRE), g(B, *LOCO_POST), g(B, *RAIL_PRE)
        S["aLocoPre"] += lp; S["aMaintLocoPre"] += mlp; S["aLocoPost"] += lpo; S["aRailPre"] += rp
        S["bLocoPost"] += blpo; S["bRailPre"] += brp; S["bLocoPre"] += blp; S["bMaintLocoPre"] += B["preMaintLocoAmtGold"]
        sh = blp / (blp + blpo) if blp + blpo > 0 else float("nan")
        print(f"{s}\t{A['_rows']}\t{B['_rows']}\t{lp:.6g}\t{mlp:.6g}\t{blp:.6g}\t{sh:.4f}\t{(lpo / blpo if blpo > 0 else float('nan')):.4f}\t{(rp / brp if brp > 0 else float('nan')):.4f}")
    r1b = S["aLocoPost"] / S["bLocoPost"] if S["bLocoPost"] > 0 else float("nan")
    r3 = S["aRailPre"] / S["bRailPre"] if S["bRailPre"] > 0 else float("nan")
    share = S["bLocoPre"] / (S["bLocoPre"] + S["bLocoPost"]) if S["bLocoPre"] + S["bLocoPost"] > 0 else float("nan")
    G = {"G1": abs(S["aLocoPre"]) <= 1e-12, "G2": abs(S["aMaintLocoPre"]) <= 1e-12, "G1b": 0.5 <= r1b <= 2.0, "G3": r3 >= 0.5}
    print(f"和\tarmLocoPre={S['aLocoPre']:.6g}\tarmMaintLocoPre={S['aMaintLocoPre']:.6g}\tbaseLocoPre={S['bLocoPre']:.6g}\tbaseMaintLocoPre={S['bMaintLocoPre']:.6g}\tpostRatio={r1b:.4f}\tpreRailRatio={r3:.4f}\tbaseLocoPreShare={share:.4f}\t欠け={miss}")
    print("門\t" + "\t".join(f"{k}={'緑' if v else '赤'}" for k, v in G.items()))
if __name__ == "__main__": main()
