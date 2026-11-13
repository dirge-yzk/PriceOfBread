#!/usr/bin/env bash
# (1095) 段 C′ の門の幅：(1083) の q0・q1・q4・q7 をヌル腕 N1・N2（keep_1087/n1・n2）で回す。台本は main の木の scripts/instruments/r1083/ を読むだけ。
S=C:/Users/dirge/Documents/PriceOfBread/scripts/instruments/r1083
P="python -X utf8 -I"
for a in n1 n2; do
  D=D:/PoB_work/keep_1087/$a; O=D:/PoB_work/r1095_work/m1087_$a
  mkdir -p "$O"
  $P $S/q0_extract.py $D $O/open_rows.tsv > $O/q0_out.txt || exit 1
  $P $S/q1_breakdown.py $O/open_rows.tsv > $O/q1_out.txt || exit 2
  $P $S/q4_rho_once.py $O/open_rows.tsv > $O/q4_out.txt || exit 3
  $P $S/q7_q_now.py $D > $O/q7_out.txt || exit 4
done
echo ok
