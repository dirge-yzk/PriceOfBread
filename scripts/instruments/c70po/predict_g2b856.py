# -*- coding: utf-8 -*-
# (856) c70po 腕 B の前の G2b の予測（読むだけ）：(850) の旗 1 の腕 c69poB（木 f880f55c・盤面 m839）と同じ盤面の旗 0（ident c69poz の secession_s＋keep_845 の rule_s）を、
# c70gt の gates_po853.py の arm_stats（正確な母集合・窓 L）で数え、|ΔC| を m852 の W_C（閾値の表）と比べる。国別の内訳も出す。
#   python -X utf8 -I predict_g2b856.py <旗 1 の ins> <旗 0 の ins> [種の数=10]
import sys, os, importlib.util
sys.stdout.reconfigure(encoding='utf-8')
here = os.path.dirname(os.path.abspath(__file__))
spec = importlib.util.spec_from_file_location('g', os.path.join(here, 'gt853', 'gates_po853.py')); g = importlib.util.module_from_spec(spec); spec.loader.exec_module(g)
thr = g.read_thr(os.path.join(here, 'gt853', 'gates_po853_thresholds.tsv'))
arm, base = sys.argv[1], sys.argv[2]; n = int(sys.argv[3]) if len(sys.argv) > 3 else 10
Ls = [40, 80, 120, 160, 200]
sa = g.arm_stats(arm, n, Ls); sb = g.arm_stats(base, n, Ls)
print('旗 1 の腕 %s・旗 0 %s・種 %d・m852 の閾値 L＝%d・W_C＝%.6g（C_base m852 %.6g）' % (arm, base, n, int(thr['persist_L']), thr['W_C'], thr['C_base']))
print('ΔS（旗 1−旗 0・同じ盤面 m839）%.6g・T %.6g' % (sa['S'] - sb['S'], sa['T']))
for L in Ls:
    print('L %d\t母集合 旗1 %d／旗0 %d\tC 旗1 %.6g／旗0 %.6g\tΔC %.6g\t|ΔC|÷W_C(m852) %.3g' % (L, sa['Cn'][L], sb['Cn'][L], sa['C'][L], sb['C'][L], sa['C'][L] - sb['C'][L], abs(sa['C'][L] - sb['C'][L]) / thr['W_C']))
