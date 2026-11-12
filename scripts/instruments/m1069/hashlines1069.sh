#!/usr/bin/env bash
# (1069) 合流：線の生（uf の ident の ib_10・py の腕 B・vs の腕 B・py の旗 0 の ident の残り）の表ごとの sha1 を、消す前に取る（読むだけ・m1053/hashlines1053.sh の型）。
#   基準 keep_1053/base_ins の sha1 は measurements/m1053_20261112/hash/mM.tsv（(1053) の M＝今の keep_1053/base_ins）。列の増えた表は無い＝先頭の列の sha1 は取らない。
#   bash hashlines1069.sh
export PYTHONUTF8=1
S=D:/PoB_work/wt_m1069/scripts/instruments; H="python -X utf8 -I $S/m1044/hash1044.py"
O=D:/PoB_work/m1069_work/hash; B=D:/PoB_work/keep_1053/base_ins; BT=D:/PoB_work/wt_m1069/measurements/m1053_20261112/hash/mM.tsv
mkdir -p $O
for t in ufz:D:/PoB_work/ident_c101ufz/out_ib_10/ins pyB:D:/PoB_work/c101py_work/armB/out_c101pyB_10/ins vsB:D:/PoB_work/c101vs_work/out_c101vsB_10/ins pyz:D:/PoB_work/c101py_work/ident_z/out_ib_10/ins; do
  k=${t%%:*}; d=${t#*:}; [ -d "$d" ] || { echo "$k 無し $d"; continue; }; $H $d $O/$k.tsv $B; echo "$k $?"
done
for k in ufz pyB vsB pyz; do [ -f $O/$k.tsv ] || continue; echo "== $k vs base1053"; $H --cmp $BT $O/$k.tsv burscan_s,clock_s; done
echo HASHDONE
