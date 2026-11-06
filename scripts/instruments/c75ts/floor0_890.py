# (890) 段 0：旗 0（S1-a）の実の生で、正しい実装（エンジン＝S1-a）の列どうしを G10 の印字のまま比べ、許し 1e-9 と伝播の許しの両方で数える（読むだけ）。
#   G2 の型：|s2SigmaEngine − s2SigmaInst|（2 値）・G3a の型：|s2BasisEngine÷s2RhoInst−1|（焼いた最初の行・2 値）
#   G4 の型（打ち消しつき）：E＝s2UEnd − need×s2HeadsEnd÷(s2RhoInst×s2TechEnd)・ub＝clamp01(E÷s2CapEnd) と s2UnempEngine（6 値）
import sys, os
sys.path.insert(0, sys.argv[1]); import gates_ts890 as G
base = sys.argv[2]; seeds = range(int(sys.argv[3]), int(sys.argv[4]) + 1)
import collections
C = collections.Counter(); mx = collections.defaultdict(float)
for s in seeds:
    A = G.load_rows(base, s, True)
    baked = set()
    for (q, reg), r in A.items():
        if r["kind"] != 2 or r["rs"] != 1: continue
        if reg not in baked and r["s2RhoInst"] == r["s2RhoInst"]:
            baked.add(reg)
            rel = abs(r["s2BasisEngine"] / r["s2RhoInst"] - 1.0)
            tol = G.tol_rel(2)
            C["G3a 型 行"] += 1; C["G3a 型 1e-9 超"] += rel > 1e-9; C["G3a 型 伝播超"] += rel > tol; mx["G3a 型 rel"] = max(mx["G3a 型 rel"], rel)
        se, si = r["s2SigmaEngine"], r["s2SigmaInst"]
        if se == se and si == si:
            d = abs(se - si); tol = G.tol_rel(2) * max(abs(se), abs(si))
            C["G2 型 行"] += 1; C["G2 型 1e-9 超"] += d > 1e-9; C["G2 型 伝播超"] += d > tol; mx["G2 型 |d|"] = max(mx["G2 型 |d|"], d)
        u, n, h, rho, t, cap, ue = r["s2UEnd"], r["need"], r["s2HeadsEnd"], r["s2RhoInst"], r["s2TechEnd"], r["s2CapEnd"], r["s2UnempEngine"]
        if all(v == v for v in (u, n, h, rho, t, cap, ue)) and rho > 0 and t > 0 and not r["masked"]:
            L = n * h / (rho * t); e = u - L; ub = min(1.0, max(0.0, e / cap)) if cap > 0 else 0.0
            tol = G.tol_g4(u, L, cap, ub, ue, 4)   # L は 4 値（need・heads・ρ・T）の積と商
            pos = e > 0
            k = "G4 型 E＞0" if pos else "G4 型 E≦0"
            d = abs(ue - ub) if pos else ue
            C[k + " 行"] += 1; C[k + " 1e-9 超"] += (d > 1e-9) if pos else (d > 1e-12); C[k + " 伝播超"] += d > max(tol, 1e-12 if not pos else 0.0)
            mx[k + " |d|"] = max(mx[k + " |d|"], d)
for k in sorted(C): print("%s\t%d" % (k, C[k]))
for k in sorted(mx): print("最大 %s\t%.3g" % (k, mx[k]))
