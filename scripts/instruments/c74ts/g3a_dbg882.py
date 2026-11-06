import sys; sys.path.insert(0, "D:/PoB_work/wt_c74ts/scripts/instruments/c74ts")
import gates_ts877 as G
arm, base = sys.argv[1], sys.argv[2]
tot = 0
for seed in range(1, 11):
    A = G.load_rows(arm, seed); B = G.load_rows(base, seed)
    bb = {}
    for (q, reg), r in B.items():
        if r["kind"] == 2 and r["rs"] == 1 and reg not in bb and r["rho2"] == r["rho2"]: bb[reg] = q
    baked = set()
    for (q, reg), r in A.items():
        if r["kind"] == 2 and r["rs"] == 1 and reg not in baked and r["rho2"] == r["rho2"]:
            baked.add(reg)
            rel = abs(r["s2BasisEngine"] / r["rho2"] - 1.0) if r["rho2"] > 0 else float("inf")
            if not (rel <= 1e-9 and r["s2BakeSigmaDiff"] <= 1e-9):
                tot += 1
                if tot <= 25: print("s%d %s q%d rel %.3g bakeSigDiff %.3g T0 %.4f d0 %.4f 基線の初の焼き q%s" % (seed, reg, q, rel, r["s2BakeSigmaDiff"], r["s2Tech0Inst"], r["s2D0Inst"], bb.get(reg)))
print("不一致", tot)
