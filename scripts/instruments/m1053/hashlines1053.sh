#!/usr/bin/env bash
# (1053) 合流：線の生（cd・db の旗 0・ov の旗 0 の ident の ib_10 と db の腕 B）と基準 keep_1044/base_ins の表ごとの sha1（全体）と、
#   列が増えうる 3 表（tradeskip_s・sellr_s・lordstat_s）の〈基準の列の数までの先頭の列〉の sha1 を、消す前に取る（読むだけ・m1044/hashlines1044.sh の型）。
#   bash hashlines1053.sh
export PYTHONUTF8=1
S=D:/PoB_work/wt_m1053/scripts/instruments; H="python -X utf8 -I $S/m1044/hash1044.py"; P="python -X utf8 -I $S/m1044/prefix1044.py"
O=D:/PoB_work/m1053_work/hash; B=D:/PoB_work/keep_1044/base_ins; HD=tradeskip_s,sellr_s,lordstat_s
mkdir -p $O
$H $B $O/base1044.tsv; echo "base $?"; $P $B $O/pre_base.tsv $B $HD
for t in cd:D:/PoB_work/c99cd_work/ident/out_ib_10/ins dbz:D:/PoB_work/c99db_work/ident_z/out_ib_10/ins ovz:D:/PoB_work/ident_c99ovz/out_ib_10/ins dbB:D:/PoB_work/c99db_work/armB/out_c99dbB_10/ins; do
  k=${t%%:*}; d=${t#*:}; $H $d $O/$k.tsv $B; echo "$k $?"; $P $d $O/pre_$k.tsv $B $HD
done
for k in cd dbz ovz; do echo "== $k vs base"; $H --cmp $O/base1044.tsv $O/$k.tsv burscan_s,clock_s; $P --cmp $O/pre_base.tsv $O/pre_$k.tsv | head -3; done
echo HASHDONE
