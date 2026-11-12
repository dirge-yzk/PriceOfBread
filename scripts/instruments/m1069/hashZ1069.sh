#!/usr/bin/env bash
# (1053) 腕 Z（ident_m1069z）の生の表ごとの sha1 と先頭の列を、線の旗 0 の ident の生（cd・db・ov）と突き合わせる（読むだけ）。
export PYTHONUTF8=1
S=D:/PoB_work/wt_m1069/scripts/instruments; W=D:/PoB_work/m1069_work; KB=D:/PoB_work/keep_1044/base_ins; Z=D:/PoB_work/ident_m1069z/out_ib_10/ins
H="python -X utf8 -I $S/m1044/hash1044.py"; P="python -X utf8 -I $S/m1044/prefix1044.py"
mkdir -p $W/read
$H $Z $W/hash/z.tsv $KB; $P $Z $W/hash/pre_z.tsv $KB tradeskip_s,sellr_s,lordstat_s
for t in cd dbz ovz; do { $H --cmp $W/hash/$t.tsv $W/hash/z.tsv burscan_s,clock_s; $P --cmp $W/hash/pre_$t.tsv $W/hash/pre_z.tsv; } > $W/read/rawcmp_Z_vs_$t.txt; echo "== Z vs $t"; grep -v '^違い \(cearn\|cpop\|flow\|tradeskip\|sellr\|lordstat\)' $W/read/rawcmp_Z_vs_$t.txt; done
echo HASHZDONE
