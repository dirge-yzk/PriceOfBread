#!/usr/bin/env bash
# (1095) (1083) の紙の台本を m1087 の生（keep_1087/base_ins）で取り直す。台本は main の木の scripts/instruments/r1083/ を読むだけ。
S=C:/Users/dirge/Documents/PriceOfBread/scripts/instruments/r1083
D=D:/PoB_work/keep_1087/base_ins
O=D:/PoB_work/r1095_work/m1087
mkdir -p "$O"
P="python -X utf8 -I"
$P $S/q0_extract.py $D $O/open_rows.tsv > $O/q0_out.txt || exit 1
$P $S/q1_breakdown.py $O/open_rows.tsv > $O/q1_out.txt || exit 2
$P $S/q4_rho_once.py $O/open_rows.tsv > $O/q4_out.txt || exit 3
$P $S/q6_combined.py $O/open_rows.tsv $D > $O/q6_out.txt || exit 4
$P $S/q7_q_now.py $D > $O/q7_out.txt || exit 5
$P $S/q1d_early_quarters.py $D > $O/q1d_out.txt || exit 6
$P $S/q1f_shadow_drift.py $D > $O/q1f_out.txt || exit 7
echo ok
