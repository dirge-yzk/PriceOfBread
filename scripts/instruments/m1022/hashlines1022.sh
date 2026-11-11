#!/usr/bin/env bash
# (1022) 合流：線の生（ident の ib_10 と ub の腕 B・S11）と keep_1017/base_ins の表ごとの sha1 を取る（消す前・読むだけ・m1017/hash1017.py を呼ぶ）。
#   bash hashlines1022.sh
export PYTHONUTF8=1
H="python -X utf8 -I D:/PoB_work/wt_m1022/scripts/instruments/m1017/hash1017.py"; O=D:/PoB_work/m1022_work/hash
$H D:/PoB_work/c95wn_work/ident/out_ib_10/ins $O/wn_ib.tsv; echo "wn $?"
$H D:/PoB_work/ident_c95pc/out_ib_10/ins $O/pc_ib.tsv; echo "pc $?"
$H D:/PoB_work/ident_c95ubz/out_ib_10/ins $O/ubz_ib.tsv; echo "ubz $?"
$H D:/PoB_work/c95ub_work/arm_B/out_c95ubB_10/ins $O/ub_armB.tsv; echo "ubB $?"
$H D:/PoB_work/c95ub_work/arm_S11/out_c95ubS_10_s11/ins $O/ub_armS11.tsv; echo "ubS $?"
$H D:/PoB_work/keep_1017/base_ins $O/keep1017_base.tsv; echo "keep $?"
echo HASHDONE
