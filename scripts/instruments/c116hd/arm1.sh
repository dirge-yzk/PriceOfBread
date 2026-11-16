#!/usr/bin/env bash
# (1174) c116hd 突然変異の 1 種の計器つきの腕（表を絞る・旗 1 の木＋POB_COEF）。門は hd.py を種 1 のヌル腕（measurements/m1174_20261116/paper1_*.json）で読む（bg.sh で起こす）。
#   bash scripts/instruments/c116hd/arm1.sh <コミット> <作業フォルダ（D:/…）> <名> <POB_COEF の文字列>
C=${1:?コミット}; export POB_WORK=${2:?作業フォルダ}; N=${3:?名}; CO=${4:?POB_COEF}
cd "$(dirname "$0")/../../.."; R=$(pwd); S=$R/scripts/instruments; P=$R/measurements/m1174_20261116
export TEMP=$POB_WORK/tmp TMP=$POB_WORK/tmp; mkdir -p $POB_WORK/tmp
export POB_INSTRUMENTS_TABLES=nation_s,nationgood_s,armsfill_s,pop_s,epi_s,warpair_s,labor_s,tb_s,sf_s,wars_s,pxmonth_s
echo "$CO" > $POB_WORK/coef_hd_$N.txt
bash $S/batch.sh prep hd_$N commit:$C plain > $POB_WORK/prep_$N.txt 2>&1 || { echo "PREPFAIL $N"; exit 1; }
INS=1 bash $S/batch.sh run hd_$N 1 > $POB_WORK/run_$N.txt 2>&1; RC=$?
O=$POB_WORK/out_hd_${N}_1/ins
python -X utf8 -I $S/c116hd/hd.py paper $O 1 1 $POB_WORK/paper1_$N.json > $POB_WORK/paper1_$N.txt 2>&1
python -X utf8 -I $S/c116hd/hd.py gates $POB_WORK/paper1_$N.json $P/paper1_M.json $P/paper1_N1.json $P/paper1_N2.json $P/paper1_F1.json $P/paper1_F2.json > $POB_WORK/gates1_$N.txt 2>&1
echo "$N rc=$RC $(tail -1 $POB_WORK/run_$N.txt)"
exit $RC
