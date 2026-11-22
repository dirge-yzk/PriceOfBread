#!/usr/bin/env bash
# (1305) c135k47 腕 B（計器つき 10 種・種 1〜10・旗 1 の木・表を絞る＝⓪(a)）と読み手を 1 本で（bg.sh で起こす・c134f1/armb.sh の写し）。
#   表＝止める線・NG の読み手が読む 9 表＋この線の史実の桁と記録の recipeout_s・prodcap_s・goods_s。写しは作業ツリーから dir:（measurements・docs を除く）。
#   bash scripts/instruments/c135k47/armb.sh <コミット> <作業フォルダ（D:/…）>
C=${1:?コミット}; export POB_WORK=${2:?作業フォルダ}
cd "$(dirname "$0")/../../.."; R=$(pwd); S=$R/scripts/instruments
export TEMP=$POB_WORK/tmp TMP=$POB_WORK/tmp; mkdir -p $POB_WORK/tmp
export POB_INSTRUMENTS_TABLES="nation_s,stock_s,wars_s,casualty_s,region_s,migration_s,pxmonth_s,scalars_s,fiscal_s,recipeout_s,prodcap_s,goods_s"
[ -z "$(git status --porcelain)" ] && [ "$(git rev-parse HEAD)" = "$(git rev-parse $C)" ] || { echo "木が汚れているか HEAD がコミットと違う"; exit 1; }
bash $S/batch.sh prep c135k47B dir:$R plain > $POB_WORK/prep_B.txt 2>&1 || { echo "PREPFAIL"; exit 1; }
INS=1 bash $S/batch.sh run c135k47B 10 > $POB_WORK/run_B.txt 2>&1; RC=$?
echo "armB rc=$RC $(tail -1 $POB_WORK/run_B.txt)"
rm -rf $POB_WORK/src_c135k47B
WD=$(cd $POB_WORK && pwd -W)
bash $S/c135k47/read_k47.sh "$WD/out_c135k47B_10/ins" "$WD/b10_c135k47B_10.txt" c135k47B "$WD/read" 1 > $POB_WORK/read_B.log 2>&1
echo "ARMBDONE $(date '+%F %T')"
exit $RC
