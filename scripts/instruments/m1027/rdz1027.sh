#!/usr/bin/env bash
# (1027) 合流：腕 Z の生（ident_m1027z の ib_10）の表ごとの sha1 を wn・kf の ident の生と突き合わせる（読むだけ）。
export PYTHONUTF8=1
S=D:/PoB_work/wt_m1027/scripts/instruments; W=D:/PoB_work/m1027_work; R=$W/read; mkdir -p $R
H="python -X utf8 -I $S/m1017/hash1017.py"; Z=D:/PoB_work/ident_m1027z/out_ib_10/ins
$H $Z $W/hash/z.tsv || exit 1
$H --cmp $W/hash/wn_ib.tsv $W/hash/z.tsv burscan_s,clock_s > $R/rawcmp_Z_vs_c96wn.txt; cat $R/rawcmp_Z_vs_c96wn.txt
$H --cmp $W/hash/kf_ib.tsv $W/hash/z.tsv burscan_s,clock_s > $R/rawcmp_Z_vs_c96kf.txt; cat $R/rawcmp_Z_vs_c96kf.txt
echo RDZDONE
