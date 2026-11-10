#!/usr/bin/env bash
# (1027) 合流：線の生（ident の ib_10 と rd の腕 B・S11）の表ごとの sha1 を取る（消す前・読むだけ・m1017/hash1017.py を呼ぶ）。
#   bash hashlines1027.sh
export PYTHONUTF8=1
H="python -X utf8 -I D:/PoB_work/wt_m1027/scripts/instruments/m1017/hash1017.py"; O=D:/PoB_work/m1027_work/hash
$H D:/PoB_work/ident_c96wn/out_ib_10/ins $O/wn_ib.tsv; echo "wn $?"
$H D:/PoB_work/ident_c96kf/out_ib_10/ins $O/kf_ib.tsv; echo "kf $?"
$H D:/PoB_work/c96rd_work/out_c96rdB_10/ins $O/rd_armB.tsv; echo "rdB $?"
$H D:/PoB_work/c96rd_work/out_c96rdS_10_s11/ins $O/rd_armS11.tsv; echo "rdS $?"
echo HASHDONE
