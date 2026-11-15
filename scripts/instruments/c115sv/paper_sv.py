# (1166) c115sv 兵 0 の崖：紙の台本（門の 3 本の数え＝旗 0 の盤面で取る・腕 B でも同じ台本で読む）。
# 引数：生のフォルダ 種の始め 種の終わり 出力 [--seats-only]
# 読む表：cpop_s（植民地格の州の兵士の POP＝className 兵士・行は四半期）・nation_s（conscriptionLaw＝0 志願・1 選抜・2 皆兵）
#         admrule_s（seatsK・soldierK・atWarFlag＝国の定員と兵）・rule_s（nowRuleDelivery＝州の届き）
# 門（事前登録 measurements/m1166_20261116_prereg_sv.txt）
#   G1 働いた：志願の国の植民地格の州の兵 0 の州×四半期（cpop_s の兵士の行・popK ≦1e-9）の種の和
#   G2 上：志願の国の兵の総数÷定員（admrule_s soldierK÷seatsK・seatsK＞0）の、種ごとの最大の、種の最大
#   G3 形：志願の国で、兵士の行が最初に現れた四半期に兵 0 の州のうち、その後 4 四半期のどこかで兵が正になる州の割合
#           （分母は、その 5 四半期のどこかで州の届き nowRuleDelivery＞0 かつ国の定員 seatsK＞0 の州＝空きが立ちうる州）
import sys, os, csv
from collections import defaultdict

EPS = 1e-9
LAW = {0: "志願", 1: "選抜", 2: "皆兵"}


def fnum(s):
    try:
        return float(s)
    except ValueError:
        return float("nan")


def rd(path):
    with open(path, encoding="utf-8", newline="") as f:
        r = csv.reader(f, delimiter="\t")
        h = next(r)
        ix = {k: i for i, k in enumerate(h)}
        for x in r:
            yield ix, x


def law_map(raw, seed):
    law = {}
    p = os.path.join(raw, f"nation_s{seed}.tsv")
    for ix, x in rd(p):
        law[(int(x[ix["quarter"]]), x[ix["nation"]])] = int(float(x[ix["conscriptionLaw"]]))
    return law


def seats_max(raw, seed, law):
    """G2：志願の国×四半期の soldierK÷seatsK の最大（seatsK＞0）。平時だけの最大も返す。"""
    p = os.path.join(raw, f"admrule_s{seed}.tsv")
    if not os.path.exists(p):
        return None
    mx = 0.0; mxp = 0.0; arg = None; n = 0
    for ix, x in rd(p):
        q = int(x[ix["quarter"]]); nat = x[ix["nation"]]
        if law.get((q, nat), -1) != 0:
            continue
        sk = fnum(x[ix["seatsK"]]); so = fnum(x[ix["soldierK"]])
        if not (sk > 0) or not (so >= 0):
            continue
        n += 1
        v = so / sk
        if v > mx:
            mx = v; arg = (q, nat, so, sk)
        if x[ix["atWarFlag"]] == "0" and v > mxp:
            mxp = v
    return mx, mxp, arg, n


def main():
    raw, s0, s1, out = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4]
    seats_only = "--seats-only" in sys.argv
    cnt = defaultdict(int); natZero = defaultdict(int)
    g2 = []
    g3n = 0; g3d = 0; g3all = 0
    for seed in range(s0, s1 + 1):
        law = law_map(raw, seed)
        sm = seats_max(raw, seed, law)
        if sm is not None:
            g2.append((seed,) + sm)
        if seats_only:
            continue
        seats = {}
        for ix, x in rd(os.path.join(raw, f"admrule_s{seed}.tsv")):
            seats[(int(x[ix["quarter"]]), x[ix["nation"]])] = fnum(x[ix["seatsK"]])
        deliv = {}
        for ix, x in rd(os.path.join(raw, f"rule_s{seed}.tsv")):
            deliv[(int(x[ix["quarter"]]), x[ix["nation"]], x[ix["region"]])] = fnum(x[ix["nowRuleDelivery"]])
        ser = defaultdict(list)
        for ix, x in rd(os.path.join(raw, f"cpop_s{seed}.tsv")):
            if x[ix["className"]] != "兵士":
                continue
            ser[(x[ix["nation"]], x[ix["region"]])].append((int(x[ix["quarter"]]), fnum(x[ix["popK"]])))
        for (nat, reg), s in ser.items():
            s.sort()
            for q, v in s:
                L = law.get((q, nat), -1)
                cnt[f"行：法={LAW.get(L, '不明')}"] += 1
                if v <= EPS:
                    cnt[f"兵 0 の行：法={LAW.get(L, '不明')}"] += 1
                    if L == 0:
                        natZero[nat] += 1
                        dv = deliv.get((q, nat, reg), float("nan")); sk = seats.get((q, nat), float("nan"))
                        if dv > EPS and sk > EPS:
                            cnt["G1 の内訳：届き＞0×定員＞0（空きが立ちうる）"] += 1
                        elif not (dv > EPS):
                            cnt["G1 の内訳：届き 0 か表に無い"] += 1
                        else:
                            cnt["G1 の内訳：届き＞0×定員 0"] += 1
            q0, v0 = s[0]
            if v0 <= EPS and law.get((q0, nat), -1) == 0:
                g3all += 1
                win = [(q, v) for q, v in s if q0 <= q <= q0 + 4]
                elig = any(deliv.get((q, nat, reg), 0) > EPS and seats.get((q, nat), 0) > EPS for q, _ in win)
                if elig:
                    g3d += 1
                    if any(v > EPS for q, v in win):
                        g3n += 1
    with open(out, "w", encoding="utf-8", newline="\n") as o:
        o.write(f"# (1166) c115sv 紙の台本 生={raw} 種={s0}〜{s1}\n")
        if not seats_only:
            g1 = cnt["兵 0 の行：法=志願"]
            o.write(f"G1\t志願の国の植民地格の兵 0 の州×四半期\t{g1}\n")
            for k in sorted(cnt):
                o.write(f"{k}\t{cnt[k]}\n")
            o.write(f"G3\t兵士の行が最初に兵 0 の志願の州\t{g3all}\t分母（届き＞0×定員＞0 が窓にある）\t{g3d}\t4 四半期のうちに正\t{g3n}\t割合\t{(g3n / g3d) if g3d else float('nan'):.4f}\n")
            o.write("# 兵 0 の行の多い国（志願）\n")
            for n, c in sorted(natZero.items(), key=lambda z: -z[1])[:8]:
                o.write(f"{n}\t{c}\n")
        gm = max((t[1] for t in g2), default=float("nan")); gmp = max((t[2] for t in g2), default=float("nan"))
        o.write(f"G2\t志願の国 soldierK÷seatsK の種ごとの最大の最大\t{gm:.6f}\t平時だけ\t{gmp:.6f}\n")
        for t in g2:
            o.write(f"G2 種\t{t[0]}\t{t[1]:.6f}\t{t[2]:.6f}\t{t[3]}\t行\t{t[4]}\n")


if __name__ == "__main__":
    main()
