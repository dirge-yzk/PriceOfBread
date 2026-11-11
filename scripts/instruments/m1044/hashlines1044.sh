#!/usr/bin/env bash
# (1044) 合流：線の生（lc・da・lw・wr の旗 0 の ident の ib_10 と lw の腕 B）と基準 keep_1035/base_ins の表ごとの sha1（先頭の列も）を、消す前に取る（読むだけ）。
#   bash hashlines1044.sh
export PYTHONUTF8=1
S=D:/PoB_work/wt_m1044/scripts/instruments; H="python -X utf8 -I $S/m1044/hash1044.py"; O=D:/PoB_work/m1044_work/hash; B=D:/PoB_work/keep_1035/base_ins
mkdir -p $O
$H $B $O/base1035.tsv; echo "base $?"
$H D:/PoB_work/c98lc_work/ident/out_ib_10/ins $O/lc_ib.tsv $B; echo "lc $?"
$H D:/PoB_work/c98da_work/ident/out_ib_10/ins $O/da_ib.tsv $B; echo "da $?"
$H D:/PoB_work/c98lw_work/identz/out_ib_10/ins $O/lwz_ib.tsv $B; echo "lwz $?"
$H D:/PoB_work/c98wr_work/ident_z/out_ib_10/ins $O/wrz_ib.tsv $B; echo "wrz $?"
$H D:/PoB_work/c98lw_work/out_c98lwB_10/ins $O/lw_armB.tsv $B; echo "lwB $?"
for t in lc_ib da_ib lwz_ib wrz_ib; do echo "== $t vs base"; $H --cmp $O/base1035.tsv $O/$t.tsv burscan_s,clock_s; done
echo HASHDONE
