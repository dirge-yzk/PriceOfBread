export PYTHONUTF8=1
S=D:/PoB_work/wt_m1053/scripts/instruments; W=D:/PoB_work/m1053_work; KB=D:/PoB_work/keep_1044/base_ins
H="python -X utf8 -I $S/m1044/hash1044.py"; P="python -X utf8 -I $S/m1044/prefix1044.py"
mkdir -p $W/read
$H $W/out_mM_10/ins $W/hash/mM.tsv $KB; $P $W/out_mM_10/ins $W/hash/pre_mM.tsv $KB tradeskip_s,sellr_s,lordstat_s
{ $H --cmp $W/hash/dbB.tsv $W/hash/mM.tsv burscan_s,clock_s; $P --cmp $W/hash/pre_dbB.tsv $W/hash/pre_mM.tsv; } > $W/read/rawcmp_M_vs_c99dbB.txt
cat $W/read/rawcmp_M_vs_c99dbB.txt | grep -v '^違い \(tradeskip\|sellr\)'
