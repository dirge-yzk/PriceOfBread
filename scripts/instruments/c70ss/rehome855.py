# (855) c70ss 段0 の紙：Rehome の次の月の σ の跳ね（(849) の反証役 ①）を (849) の腕 B の生から数え、G1a・G5 の母集合（gates849.py の定義）に占める割合を出す（読むだけ）。
# 呼び方：python -X utf8 rehome855.py <腕の ins> <基線の ins> <N1> <N2> <出力>
# 定義（2026-11-05 追記：持ち主が変わった行の σ と母集合の割合も数える＝頭0 の行は持ち主の変わりと重ならなかった）：
#   焼いた担い手の行＝s2BasisKind＝2 かつ s2RhoSource＝1。
#   頭の売れた量 0 の行＝その行のうち s2SoldHead＝0 かつ s2CapHead＞0（月の頭で U＝能力＝Rehome が LastSold を 0 に戻した後の最初の月の型）。
#   持ち主が変わった行＝同じ州が前の四半期に別の国の名で載っていた行（州の名で前の四半期の国を引く）。
#   G1a の母集合 P1・G5 の母集合 P5＝gates849.py と同じ（鍵は 国/州・q≧1・q0 の行が同じ鍵で在る・基線と N1・N2 に同じ鍵の行が在る）。
import sys, os, collections

def fnum(s):
    try: return float(s)
    except ValueError: return float("nan")

def med(xs):
    xs = sorted(x for x in xs if x == x); n = len(xs)
    return float("nan") if n == 0 else xs[n // 2] if n % 2 else 0.5 * (xs[n // 2 - 1] + xs[n // 2])

def load(base, seed, arm):
    D = {}
    with open(os.path.join(base, "subst_s%d.tsv" % seed), encoding="utf-8") as f:
        h = f.readline().rstrip("\n").split("\t"); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            c = line.rstrip("\n").split("\t")
            sp = fnum(c[ix["subPopK"]])
            if not (sp > 0): continue
            need = fnum(c[ix["stapleNeedPerCapita"]]); eng = fnum(c[ix["engineSubOutputPerMonth"]])
            r = {"s": eng / (need * sp) if need * sp > 0 and eng == eng else float("nan"), "d1": fnum(c[ix["unsoldD1Share"]]),
                 "cap": fnum(c[ix["farmCapPerMonth"]]), "sold": fnum(c[ix["farmSoldPerMonth"]]), "nation": c[ix["nation"]], "region": c[ix["region"]]}
            if arm:
                r["kind"] = int(c[ix["s2BasisKind"]]); r["src"] = int(c[ix["s2RhoSource"]])
                r["soldHead"] = fnum(c[ix["s2SoldHead"]]); r["capHead"] = fnum(c[ix["s2CapHead"]]); r["sig"] = fnum(c[ix["s2SigmaEngine"]])
            D[(int(c[ix["quarter"]]), c[ix["nation"]] + "/" + c[ix["region"]])] = r
    return D

def main(arm, b, n1, n2, out):
    C = collections.Counter(); L = collections.defaultdict(list)
    for seed in range(1, 11):
        A = load(arm, seed, True); B = [load(x, seed, False) for x in (b, n1, n2)]
        owner = {}
        for (q, key), r in A.items(): owner[(q, r["region"])] = r["nation"]
        open0 = {key: r for (q, key), r in A.items() if q == 0}
        for (q, key), r in A.items():
            baked = r["kind"] == 2 and r["src"] == 1
            if not baked: continue
            head0 = r["soldHead"] == 0 and r["capHead"] > 0
            prev = owner.get((q - 1, r["region"]))
            moved = prev is not None and prev != r["nation"]
            C["焼いた行"] += 1
            if head0: C["頭の売れた量 0"] += 1; L["σ 頭0"].append(r["sig"])
            else: L["σ 他"].append(r["sig"])
            if moved: C["持ち主が変わった"] += 1; L["σ 移"].append(r["sig"])
            if head0 and moved: C["頭0 かつ 持ち主が変わった"] += 1
            if q < 1: continue
            s0s = [x[(q, key)]["s"] for x in B if (q, key) in x]
            if len(s0s) < 3 or any(v != v for v in s0s) or r["s"] != r["s"]: continue
            o = open0.get(key)
            if o is None or r["d1"] != r["d1"] or o["d1"] != o["d1"]:
                if head0: C["頭0 で q0 の鍵が無い（P1・P5 の外）"] += 1
                continue
            if r["d1"] < o["d1"]:
                C["P1 の焼いた行"] += 1
                if head0: C["P1 の頭0"] += 1
                if moved: C["P1 の移"] += 1
            if r["cap"] > 0 and o["cap"] > 0 and abs(r["sold"] / r["cap"] - o["sold"] / o["cap"]) <= 0.01:
                C["P5 の焼いた行"] += 1
                if head0: C["P5 の頭0"] += 1
                if moved: C["P5 の移"] += 1
        del A, B
    def rt(a, b): return C[a] / C[b] if C[b] else float("nan")
    O = ["(855) Rehome の次の月の σ（腕 %s・基線 %s・N1 %s・N2 %s・10 種）" % (arm, b, n1, n2),
         "焼いた担い手の行 %d・頭の売れた量 0（U＝能力）%d・持ち主が変わった行 %d・両方 %d" % (C["焼いた行"], C["頭の売れた量 0"], C["持ち主が変わった"], C["頭0 かつ 持ち主が変わった"]),
         "σ の中央：頭0 %.4f・それ以外 %.4f・持ち主が変わった行 %.4f" % (med(L["σ 頭0"]), med(L["σ 他"]), med(L["σ 移"])),
         "P1 のうち持ち主が変わった行 %d（%.6f）・P5 のうち %d（%.6f）" % (C["P1 の移"], rt("P1 の移", "P1 の焼いた行"), C["P5 の移"], rt("P5 の移", "P5 の焼いた行")),
         "⚠頭の売れた量 0 の行は持ち主が変わった行と重ならない（例：英のセントマイケル・アラスカ＝売れた量が元から 0 の小さな州）＝Rehome の跳ねは四半期の末の月の行にはほぼ写らない（行は四半期の末の月・跳ねは Rehome の次の 1 か月だけ）",
         "G1a の母集合 P1（焼いた行）%d のうち 頭0 %d（%.6f）" % (C["P1 の焼いた行"], C["P1 の頭0"], rt("P1 の頭0", "P1 の焼いた行")),
         "G5 の母集合 P5（焼いた行）%d のうち 頭0 %d（%.6f）" % (C["P5 の焼いた行"], C["P5 の頭0"], rt("P5 の頭0", "P5 の焼いた行")),
         "頭0 で q0 の鍵（国/州）が無い＝P1・P5 の外 %d" % C["頭0 で q0 の鍵が無い（P1・P5 の外）"]]
    with open(out, "w", encoding="utf-8", newline="\n") as f: f.write("\n".join(O) + "\n")
    print("\n".join(O))

if __name__ == "__main__":
    main(*sys.argv[1:6])
