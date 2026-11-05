# (853) 印の被覆の確かめ（事前登録 [1-1]・唯一の旗 1 の読み）：反証役 (849) ① が数えた〈E＞0 で食い違う行〉が、gates_ss853.py の印（持ち主が変わった行）で全て外れるかを数える。
#   G4 の合否を腕 B に当てる読みではない（規則は替えない）。外れない行があれば別の根として報告する。
#   python -X utf8 -I diag_cover853.py <腕 B の ins> [種の数=10]
import sys, os, importlib.util
sd = os.path.dirname(os.path.abspath(__file__))
spec = importlib.util.spec_from_file_location("g", os.path.join(sd, "gates_ss853.py")); g = importlib.util.module_from_spec(spec); spec.loader.exec_module(g)
arm = sys.argv[1]; n = int(sys.argv[2]) if len(sys.argv) > 2 else 10
tot = cov = 0; unc = []; soldzero = 0; masked_E = 0; cand_E = 0
for s in range(1, n + 1):
    A, has2, own_ok = g.load_arm(arm, s)
    for (q, reg), r in A.items():
        if r.get("s2BasisKind") != 2 or r.get("s2RhoSource") != 1: continue
        e, ue, ub = r["s2EEnd"], r["s2UnempEngine"], r["s2UnempB"]
        if e == e and ue == ue and e > 0:
            cand_E += 1; masked_E += r["masked"]
            if abs(ue - ub) > 1e-9:
                tot += 1; cov += r["masked"]
                if not r["masked"] and len(unc) < 8: unc.append((s, q, reg))
    del A
print("腕 %s：E＞0 の候補 %d・うち印で外れる %d（%.4f）／E＞0 で食い違う行 %d・うち印で外れる %d・外れない %d %s" % (arm, cand_E, masked_E, masked_E / cand_E if cand_E else float("nan"), tot, cov, tot - cov, unc))
