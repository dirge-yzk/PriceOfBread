cd /c/Users/dirge/Documents/PriceOfBread
P="python -X utf8 -I scripts/instruments/r1252/swell.py"; W=/d/PoB_work/r1252_work; K=/d/PoB_work
$P M   $K/keep_1233/base_ins 1 10 $K/keep_1233/n1 > $W/q3_M.tsv
$P N1  $K/keep_1233/n1 1 10 $K/keep_1233/base_ins > $W/q3_N1.tsv
$P S11 $K/keep_1233/a11 11 20 - > $W/q3_S11.tsv
$P alB $K/keep_1247_rej/alB 1 10 $K/keep_1233/base_ins > $W/q3_alB.tsv
$P k99B $K/keep_1247_rej/k99B 1 10 $K/keep_1233/base_ins > $W/q3_k99B.tsv
$P pfM $K/keep_1247_rej/pfM 1 10 $K/keep_1233/base_ins > $W/q3_pfM.tsv
$P pfS11 $K/keep_1247_rej/pfS11 11 20 $K/keep_1233/a11 > $W/q3_pfS11.tsv
echo done > $W/q3.done
