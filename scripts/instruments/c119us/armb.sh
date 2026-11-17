#!/usr/bin/env bash
# (1195) c119us 腕 B（計器つき 10 種・種 1〜10・判定の 1 本化の木・全表）と読み手を 1 本で（bg.sh で起こす）。
#   bash scripts/instruments/c119us/armb.sh <コミット> <作業フォルダ（D:/…）>
C=${1:?コミット}; export POB_WORK=${2:?作業フォルダ}
cd "$(dirname "$0")/../../.."; R=$(pwd); S=$R/scripts/instruments
export TEMP=$POB_WORK/tmp TMP=$POB_WORK/tmp; mkdir -p $POB_WORK/tmp
bash $S/batch.sh prep c119usB commit:$C plain > $POB_WORK/prep_B.txt 2>&1 || { echo "PREPFAIL"; exit 1; }
INS=1 bash $S/batch.sh run c119usB 10 > $POB_WORK/run_B.txt 2>&1; RC=$?
echo "armB rc=$RC $(tail -1 $POB_WORK/run_B.txt)"
WD=$(cd $POB_WORK && pwd -W)
bash $S/c119us/read_us.sh "$WD/out_c119usB_10/ins" "$WD/b10_c119usB_10.txt" c119usB "$WD/read" 1 > $POB_WORK/read_B.log 2>&1
echo "ARMBDONE $(date '+%F %T')"
exit $RC
