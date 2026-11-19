#!/usr/bin/env bash
# (1244) c126pf 腕 B（計器つき 10 種・種 1〜10・旗 1 の木・全表）と読み手を 1 本で（bg.sh で起こす・c126pf/armb.sh の写し）。
#   bash scripts/instruments/c126pf/armb.sh <コミット> <作業フォルダ（D:/…）>
C=${1:?コミット}; export POB_WORK=${2:?作業フォルダ}
cd "$(dirname "$0")/../../.."; R=$(pwd); S=$R/scripts/instruments
export TEMP=$POB_WORK/tmp TMP=$POB_WORK/tmp; mkdir -p $POB_WORK/tmp
bash $S/batch.sh prep c126pfB commit:$C plain > $POB_WORK/prep_B.txt 2>&1 || { echo "PREPFAIL"; exit 1; }
INS=1 bash $S/batch.sh run c126pfB 10 > $POB_WORK/run_B.txt 2>&1; RC=$?
echo "armB rc=$RC $(tail -1 $POB_WORK/run_B.txt)"
WD=$(cd $POB_WORK && pwd -W)
bash $S/c126pf/read_pf.sh "$WD/out_c126pfB_10/ins" "$WD/b10_c126pfB_10.txt" c126pfB "$WD/read" 1 > $POB_WORK/read_B.log 2>&1
echo "ARMBDONE $(date '+%F %T')"
exit $RC
