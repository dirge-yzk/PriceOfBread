#!/usr/bin/env bash
# (1172) c116t1 腕 B（計器つき 10 種・種 1〜10・旗 1 の木・全ての表）と読み手を 1 本で（bg.sh で起こす）。
#   bash scripts/instruments/c116t1/armb.sh <コミット> <作業フォルダ（D:/…）>
C=${1:?コミット}; export POB_WORK=${2:?作業フォルダ}
cd "$(dirname "$0")/../../.."; R=$(pwd); S=$R/scripts/instruments
export TEMP=$POB_WORK/tmp TMP=$POB_WORK/tmp; mkdir -p $POB_WORK/tmp
export POB_INSTRUMENTS_NATIONS="清,ロシア,オスマン,フランス,イギリス,プロイセン,オーストリア,日本,アメリカ"
bash $S/batch.sh prep c116t1B commit:$C plain > $POB_WORK/prep_B.txt 2>&1 || { echo "PREPFAIL"; exit 1; }
INS=1 bash $S/batch.sh run c116t1B 10 > $POB_WORK/run_B.txt 2>&1; RC=$?
echo "armB rc=$RC $(tail -1 $POB_WORK/run_B.txt)"
W=$(cd $POB_WORK && pwd -W)
bash $S/c116t1/read_t1.sh "$W/out_c116t1B_10/ins" "$W/b10_c116t1B_10.txt" c116t1B "$W/read" 1 46539221 3476739 > $POB_WORK/read_B.log 2>&1
echo "ARMBDONE $(date '+%F %T')"
exit $RC
