#!/usr/bin/env bash
# (1137) c111rb 腕 B の読み（読むだけ）。出力は measurements/m1137_20261114/ へ。
export PYTHONUTF8=1 TEMP=D:/PoB_work/c111rb_work/tmp TMP=D:/PoB_work/c111rb_work/tmp
R=/d/PoB_work/wt_c111rb; M=$R/measurements/m1137_20261114; SC=$R/scripts/instruments
B=D:/PoB_work/c111rb_work/out_c111rbB_10/ins; K=D:/PoB_work/keep_1136/base_ins; N1=D:/PoB_work/keep_1122/n1; N2=D:/PoB_work/keep_1122/n2
cd $R
python -X utf8 -I $SC/c111rb/gates_rb.py $B B $M/gates_armB.txt > /dev/null
python -X utf8 -I $SC/c111rb/newold_rb.py $B $K $M/newold_armB.txt > /dev/null
python -X utf8 -I $SC/tools/nullgate.py D:/PoB_work/c111rb_work/b10_c111rbB_10.txt measurements/b10_20261114_m1122_ins measurements/b10_20261114_m1122_n1_ins measurements/b10_20261114_m1122_n2_ins > $M/nullgate.txt 2>&1
python -X utf8 -I $SC/hol/an_h2.py $B -v4 > $M/hol_armB.txt 2>&1
python -X utf8 -I $SC/c11fl/pxjump.py M=$K N1=$N1 N2=$N2 B=$B > $M/pxjump.txt 2>&1
python -X utf8 -I $SC/m1017/popjump1017.py M=$K N1=$N1 N2=$N2 B=$B > $M/popjump.txt 2>&1
python -X utf8 -I $SC/m808/gdp_zero10.py M=$K N1=$N1 N2=$N2 B=$B > $M/gdpzero.txt 2>&1
{ echo "== B"; python -X utf8 -I $SC/brk693/plvl_seeds.py $B; python -X utf8 -I $SC/c10cp/ukdefault.py $B; } > $M/plvl_ukdefault.txt 2>&1
{ grep -h "推定の計算量\|GDP：" D:/PoB_work/c111rb_work/b10_c111rbB_10.txt.cost; echo "-- 基線 m1122"; grep -h "推定の計算量\|GDP：" measurements/b10_20261114_m1122_ins.cost; } > $M/cost.txt 2>&1
mkdir -p D:/PoB_work/c111rb_work/ng
bash $SC/ng.sh $B c111rbB D:/PoB_work/c111rb_work/ng rd:measurements/ng1122_20261114:m1122 > $M/ng.txt 2>&1
echo READDONE
