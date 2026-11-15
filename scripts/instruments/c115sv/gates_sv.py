# (1166) c115sv 門の台本：G1・G3 は paper_sv.py の出力を、G2 は soldiervac_s（国×四半期）を読む。閾値は事前登録 measurements/m1166_20261116_prereg_sv.txt のまま。
# 引数：生のフォルダ（soldiervac_s のある所） paper_sv.py の出力 種の始め 種の終わり
import sys, os, csv
sys.stdout.reconfigure(newline="\n")

G1_MAX = 3065
G3_MIN = 0.9
G3_FLOOR = 100
G2_REL = 1e-6


def main():
    raw, paper, s0, s1 = sys.argv[1], sys.argv[2], int(sys.argv[3]), int(sys.argv[4])
    g1 = None; g3 = None; g3d = None
    with open(paper, encoding="utf-8") as f:
        for line in f:
            t = line.rstrip("\n").split("\t")
            if t[0] == "G1 の内訳：届き＞0×定員＞0（空きが立ちうる）":
                g1 = int(t[1])
            if t[0] == "G3":
                g3d = int(t[4]); g3 = float(t[8])
    fill = 0.0; over = 0.0; fillPos = 0.0; overPos = 0.0; zs = 0; zv = 0; zf = 0; rows = 0; eqn = 0; eqs = 0.0; eqm = 0.0; eqsN = 0.0; eqsK = 0.0; eqmK = 0.0; seeds = 0
    for seed in range(s0, s1 + 1):
        p = os.path.join(raw, f"soldiervac_s{seed}.tsv")
        if not os.path.exists(p):
            continue
        seeds += 1
        with open(p, encoding="utf-8", newline="") as fh:
            r = csv.reader(fh, delimiter="\t"); h = next(r); ix = {k: i for i, k in enumerate(h)}
            for x in r:
                rows += 1
                fill += float(x[ix["fillZeroK"]]); over += float(x[ix["overfillZeroK"]])
                fillPos += float(x[ix["fillPosK"]]); overPos += float(x[ix["overfillPosK"]])
                zs += int(x[ix["zeroStart"]]); zv += int(x[ix["zeroVac"]]); zf += int(x[ix["zeroFilled"]])
                n = int(x[ix["eqN"]]); eqn += n; eqs += float(x[ix["eqSumLogLast"]]); eqsN += float(x[ix["eqSumLogNow"]])
                eqm = max(eqm, float(x[ix["eqMaxAbsLogLast"]]))
                if "eqSumLogK" in ix:
                    eqsK += float(x[ix["eqSumLogK"]]); eqmK = max(eqmK, float(x[ix["eqMaxAbsLogK"]]))
    print(f"# (1166) 門 生={raw} 紙={paper} 種={s0}〜{s1}（soldiervac_s の種 {seeds}・行 {rows}）")
    if g1 is not None:
        print(f"G1 働いた\t{g1}\t≦{G1_MAX}\t{'緑' if g1 <= G1_MAX else '赤'}")
    if g3 is not None:
        ok = g3d >= G3_FLOOR and g3 >= G3_MIN
        print(f"G3 形\t{g3:.4f}\t分母 {g3d}\t≧{G3_MIN}（床 {G3_FLOOR}）\t{'緑' if ok else '赤'}")
    if seeds:
        if fill > 0:
            ok2 = over <= G2_REL * fill
            print(f"G2 上\toverfillZeroK {over:.6g}\tfillZeroK {fill:.6g}\t≦{G2_REL}×埋まり\t{'緑' if ok2 else '赤'}")
        else:
            print(f"G2 上\toverfillZeroK {over:.6g}\tfillZeroK {fill:.6g}\t判定不能（埋まり 0）")
        print(f"記録\tzeroStart {zs}\tzeroVac {zv}\tzeroFilled {zf}\tfillPosK {fillPos:.6g}\toverfillPosK {overPos:.6g}")
        if eqn:
            import math
            print(f"記録 eq（新しい読み÷今の読み・大きさ＞0 の兵）\tN {eqn}\t幾何平均 last {math.exp(eqs / eqn):.6f}\tnow {math.exp(eqsN / eqn):.6f}\t|log| の最大 last {eqm:.4f}	採る形 K {math.exp(eqsK / eqn):.6f}	|log| の最大 K {eqmK:.4f}")


if __name__ == "__main__":
    main()
