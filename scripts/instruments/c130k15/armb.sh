#!/usr/bin/env bash
# (1269) c130k15 腕 B（計器つき 10 種・種 1〜10・旗 1 の木・表を絞る＝ident.cfg の SMALL）と読み手を 1 本で（bg.sh で起こす・c123k3/armb.sh の写し）。
#   bash scripts/instruments/c130k15/armb.sh <コミット> <作業フォルダ（D:/…）>
C=${1:?コミット}; export POB_WORK=${2:?作業フォルダ}
cd "$(dirname "$0")/../../.."; R=$(pwd); S=$R/scripts/instruments
export TEMP=$POB_WORK/tmp TMP=$POB_WORK/tmp; mkdir -p $POB_WORK/tmp
export POB_INSTRUMENTS_TABLES=$(grep '^SMALL=' $S/ident/ident.cfg | cut -d= -f2)
export POB_INSTRUMENTS_NATIONS="清,ロシア,オスマン,フランス,イギリス,プロイセン,オーストリア,日本,アメリカ"
bash $S/batch.sh prep c130k15B commit:$C plain > $POB_WORK/prep_B.txt 2>&1 || { echo "PREPFAIL"; exit 1; }
INS=1 bash $S/batch.sh run c130k15B 10 > $POB_WORK/run_B.txt 2>&1; RC=$?
echo "armB rc=$RC $(tail -1 $POB_WORK/run_B.txt)"
WD=$(cd $POB_WORK && pwd -W)
bash $S/c130k15/read_k15.sh "$WD/out_c130k15B_10/ins" "$WD/b10_c130k15B_10.txt" c130k15B "$WD/read" 1 D:/PoB_work/keep_1267/base_ins D:/PoB_work/keep_1260/n1 > $POB_WORK/read_B.log 2>&1
echo "ARMBDONE $(date '+%F %T')"
exit $RC
