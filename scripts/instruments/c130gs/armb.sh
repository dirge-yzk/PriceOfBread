#!/usr/bin/env bash
# (1271) c130gs 腕 B（計器つき 10 種・種 1〜10・旗 1 の木・NR_TABLES で表を絞る）と読み手を 1 本で（bg.sh で起こす・c128uf/armb.sh の写し）。
#   NR_TABLES=<表の並び> bash scripts/instruments/c130gs/armb.sh <コミット> <作業フォルダ（D:/…）>
C=${1:?コミット}; export POB_WORK=${2:?作業フォルダ}
[ -n "${NR_TABLES:-}" ] && export POB_INSTRUMENTS_TABLES="$NR_TABLES"
export POB_INSTRUMENTS_NATIONS="清,ロシア,オスマン,フランス,イギリス,プロイセン,オーストリア,日本,アメリカ"
cd "$(dirname "$0")/../../.."; R=$(pwd); S=$R/scripts/instruments
export TEMP=$POB_WORK/tmp TMP=$POB_WORK/tmp; mkdir -p $POB_WORK/tmp
echo "armB start $(date '+%F %T') Dfree=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,1))")GB armsfree=$(bash $S/tools/arms.sh free)" > $POB_WORK/armB_start.txt
bash $S/batch.sh prep c130gsB commit:$C plain > $POB_WORK/prep_B.txt 2>&1 || { echo "PREPFAIL"; exit 1; }
INS=1 bash $S/batch.sh run c130gsB 10 > $POB_WORK/run_B.txt 2>&1; RC=$?
echo "armB rc=$RC $(tail -1 $POB_WORK/run_B.txt)"
WD=$(cd $POB_WORK && pwd -W)
bash $S/c130gs/read_gs.sh "$WD/out_c130gsB_10/ins" "$WD/b10_c130gsB_10.txt" c130gsB "$WD/read" 1 > $POB_WORK/read_B.log 2>&1
echo "ARMBDONE $(date '+%F %T')"
exit $RC
