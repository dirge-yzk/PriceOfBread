#!/usr/bin/env bash
# (1223) c123iv 腕 B（計器つき 10 種・種 1〜10・旗 1 の木・全表）と読み手を 1 本で（bg.sh で起こす・c122nr/armb.sh の写し）。
#   bash scripts/instruments/c123iv/armb.sh <コミット> <作業フォルダ（D:/…）>
C=${1:?コミット}; export POB_WORK=${2:?作業フォルダ}
# D: が全表（約 26GB）に足りないときは POB_INSTRUMENTS_TABLES で門と止める線の読み手が開く表だけに絞る（呼び手が渡す・空なら全表）。
[ -n "${IV_TABLES:-}" ] && export POB_INSTRUMENTS_TABLES="$IV_TABLES"
cd "$(dirname "$0")/../../.."; R=$(pwd); S=$R/scripts/instruments
export TEMP=$POB_WORK/tmp TMP=$POB_WORK/tmp; mkdir -p $POB_WORK/tmp
bash $S/batch.sh prep c123ivB commit:$C plain > $POB_WORK/prep_B.txt 2>&1 || { echo "PREPFAIL"; exit 1; }
INS=1 bash $S/batch.sh run c123ivB 10 > $POB_WORK/run_B.txt 2>&1; RC=$?
echo "armB rc=$RC $(tail -1 $POB_WORK/run_B.txt)"
WD=$(cd $POB_WORK && pwd -W)
bash $S/c123iv/read_iv.sh "$WD/out_c123ivB_10/ins" "$WD/b10_c123ivB_10.txt" c123ivB "$WD/read" 1 > $POB_WORK/read_B.log 2>&1
echo "ARMBDONE $(date '+%F %T')"
exit $RC
