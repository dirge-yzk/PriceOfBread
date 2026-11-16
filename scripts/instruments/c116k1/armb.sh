#!/usr/bin/env bash
# (1173) c116k1 腕 B（計器つき 10 種・種 1〜10・旗 1 の木）と読み手を 1 本で（bg.sh で起こす）。c115sv/armb.sh の写し。
#   bash scripts/instruments/c116k1/armb.sh <コミット> <作業フォルダ（D:/…）>
C=${1:?コミット}; export POB_WORK=${2:?作業フォルダ}
cd "$(dirname "$0")/../../.."; R=$(pwd); S=$R/scripts/instruments
export TEMP=$POB_WORK/tmp TMP=$POB_WORK/tmp; mkdir -p $POB_WORK/tmp
bash $S/batch.sh prep c116k1B commit:$C plain > $POB_WORK/prep_B.txt 2>&1 || { echo "PREPFAIL"; exit 1; }
INS=1 bash $S/batch.sh run c116k1B 10 > $POB_WORK/run_B.txt 2>&1; RC=$?
echo "armB rc=$RC $(tail -1 $POB_WORK/run_B.txt)"
O=$POB_WORK/out_c116k1B_10/ins
bash $S/c116k1/read_k1.sh "$(cd $POB_WORK && pwd -W)/out_c116k1B_10/ins" "$(cd $POB_WORK && pwd -W)/b10_c116k1B_10.txt" c116k1B "$(cd $POB_WORK && pwd -W)/read" 1 1 > $POB_WORK/read_B.log 2>&1
python -X utf8 -I $S/r1162/r1059_d1_copy.py "$O" 1 10 $POB_WORK/read/d1_B.txt B > /dev/null 2>&1
python -X utf8 -I $S/c116k1/paper_k1.py "$O" 1 10 $POB_WORK/read/paper_B.txt B > /dev/null 2>&1
echo "ARMBDONE $(date '+%F %T')"
exit $RC
