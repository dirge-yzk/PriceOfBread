# (877) c73ts 次の回の材料（記録・この線の採否に使わない）：G5p を〈旗 0 の紙で T÷T0 が上位 1/4 の行〉に絞ったら旗 0（S1-a）を落とせるか。
#   旗 0 の 3 本（m873 の base_ins・N1・N2）だけから数える＝腕を見ない。上位 1/4 の境は基線の行の窓ごとの T÷T0 の 75 百分位。
#   呼び方：python -X utf8 material877.py <基線> <N1> <N2> <出力> [種の数=10]
import sys, os, collections
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gates_ts877 as G

def q75(xs):
    xs = sorted(x for x in xs if x == x)
    return float("nan") if not xs else xs[int(0.75 * (len(xs) - 1))]

def main():
    b, n1, n2, out = sys.argv[1:5]; nseed = int(sys.argv[5]) if len(sys.argv) > 5 else 10
    dirs = {"M": b, "N1": n1, "N2": n2}
    triples = [(a, x, y) for a in dirs for x in dirs for y in dirs if len({a, x, y}) == 3]
    R = collections.defaultdict(list)   # (窓, 区分) → [(種, T÷T0, mu0 の e, 一つ抜きの (e, w) の 6 通り, wp)]
    for seed in range(1, nseed + 1):
        D = {k: G.load_rows(v, seed) for k, v in dirs.items()}
        A = D["M"]; open0 = {reg: r for (q, reg), r in A.items() if q == 0}
        for key, r in A.items():
            q, reg = key
            if q == 0 or r["rs"] != 1 or r["kind"] != 2 or not (r["s2Tech0Inst"] > 0): continue
            if not all(key in D[k] for k in dirs): continue
            xs = {k: D[k][key]["x2"] for k in dirs}
            if any(v != v for v in xs.values()) or r["x1"] != r["x1"]: continue
            o = open0.get(reg)
            p5 = o is not None and r["cap"] > 0 and o["cap"] > 0 and abs(r["sold"] / r["cap"] - o["sold"] / o["cap"]) <= 0.01
            tr = r["s2TechHead"] / r["s2Tech0Inst"]
            e0 = r["x1"] - xs["M"]   # 旗 0（S1-a）の腕の dF と Σ2′ の紙の差（同じ盤面）
            loo = [(xs[t[0]] - xs[t[1]], abs(xs[t[1]] - xs[t[2]])) for t in triples]
            wp = max(xs.values()) - min(xs.values())
            wn = G.win(r["y"])
            R[(wn, "全て")].append((tr, e0, loo, wp))
            if p5: R[(wn, "P5")].append((tr, e0, loo, wp))
        del D
    L = ["(877) 次の回の材料（記録）：G5p を T÷T0 の上位 1/4 の行に絞った形（旗 0 の 3 本だけ・種 1〜%d）" % nseed,
         "列：窓・母集合・行・境（T÷T0 の 75 百分位）・旗 0（S1-a）の |e の中央|・wp の中央・旗 0 を落とすか・一つ抜きの空振り（6 通りのうち落ちた数）"]
    for wn in G.WINS:
        for part in ("全て", "P5"):
            rows = R[(wn, part)]
            if not rows: continue
            th = q75([x[0] for x in rows]); top = [x for x in rows if x[0] >= th and th > 1.0 + 1e-9]
            if not top: L.append("%s・%s：行 %d・境 %.4f＝上位 1/4 の行が T＝T0（絞れない）" % (wn, part, len(rows), th)); continue
            em = G.med([x[1] for x in top]); wpm = G.med([x[3] for x in top])
            fails = 0
            for i in range(6):
                ei = G.med([x[2][i][0] for x in top]); wi = G.med([x[2][i][1] for x in top])
                if not (abs(ei) <= wi): fails += 1
            L.append("%s・%s：行 %d（上位 %d）・境 %.4f・旗 0 の |e の中央| %.5f・wp の中央 %.5f → %s・一つ抜きの空振り %d／6" % (
                wn, part, len(rows), len(top), th, abs(em), wpm, "旗 0 を落とす" if abs(em) > wpm else "旗 0 も通る", fails))
    with open(out, "w", encoding="utf-8", newline="\n") as f: f.write("\n".join(L) + "\n")
    print("\n".join(L)); return 0

if __name__ == "__main__":
    sys.exit(main())
