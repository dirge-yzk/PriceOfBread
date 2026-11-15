#!/usr/bin/env bash
# (1158) c114qn 門の突然変異（1 種の計器つきの腕・表を絞る・使い捨ての木＝<コミット>＋scripts/instruments/c113qn/mut.py）。形ごとに 1 回呼ぶ（bg.sh で起こす）。
#   bash scripts/instruments/c114qn/mutarms.sh <コミット> <作業フォルダ（D:/…）> <形>
#   出力：<作業フォルダ>/out_qn_<形>_1/ins。門は scripts/instruments/c113qn/gates.py で読む（1 種＝床を除いて読む＝--nofloor）。
C=${1:?コミット}; export POB_WORK=${2:?作業フォルダ}; F=${3:?形}
cd "$(dirname "$0")/../../.."; R=$(pwd); S=$R/scripts/instruments
export TEMP=$POB_WORK/tmp TMP=$POB_WORK/tmp; mkdir -p $POB_WORK/tmp
export POB_INSTRUMENTS_TABLES=colqn_s,invade_s,nation_s,pxmonth_s,scalars_s
QN_MUT=$F bash $S/batch.sh prep qn_$F commit:$C plain "$S/c113qn/mut.py" > $POB_WORK/prep_$F.txt 2>&1 || { echo "PREPFAIL $F"; exit 1; }
INS=1 bash $S/batch.sh run qn_$F 1 > $POB_WORK/run_$F.txt 2>&1; RC=$?
echo "$F rc=$RC $(tail -1 $POB_WORK/run_$F.txt)"
exit $RC
