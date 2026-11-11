# -*- coding: utf-8 -*-
# (860) c71gt：旗 1 の実の生で G2b の他の形（形 a・853 の形）の値を記録に出す（読むだけ・選ぶ根拠にしない＝事前登録 [1-1]）。
#   python -X utf8 -I flag1_forms860.py <旗 1 の ins> <同じ盤面の旗 0 の ins> [種の数=10]
import sys, os, importlib.util
sys.dont_write_bytecode = True
sys.stdout.reconfigure(encoding="utf-8")
sd = os.path.dirname(os.path.abspath(__file__))
spec = importlib.util.spec_from_file_location("g", os.path.join(sd, "gates_po860.py")); g = importlib.util.module_from_spec(spec); spec.loader.exec_module(g)
arm, base = sys.argv[1], sys.argv[2]; n = int(sys.argv[3]) if len(sys.argv) > 3 else 10
Ls = [40, 120]; Am = [1, 20]
sa = g.arm_stats(arm, n, Ls, "exact", Am); sb = g.arm_stats(base, n, Ls, "exact", Am)
print("(860) 旗 1 の実の生での他の形の値（記録・選ぶ根拠にしない）旗 1＝%s・旗 0＝%s・種 %d" % (arm, base, n))
for L in Ls:
    print("853 の形 C（L %d）：母集合 旗1 %d／旗0 %d・C 旗1 %.6g／旗0 %.6g・比 %.3f・|ΔC| %.6g" % (L, sa["Cn"][L], sb["Cn"][L], sa["C"][L], sb["C"][L], sa["C"][L] / sb["C"][L], abs(sa["C"][L] - sb["C"][L])))
    print("形 c（L %d）：残りの絶対値の平均 旗1 %.3g／旗0 %.3g・最大 旗1 %.3g" % (L, sa["R"][L], sb["R"][L], sa["Rmax"][L]))
for m in Am:
    print("形 a（途切れない・run≥%d）：母集合 旗1 %d／旗0 %d・ung の平均 旗1 %.6g／旗0 %.6g" % (m, sa["An"][m], sb["An"][m], sa["A"][m], sb["A"][m]))
