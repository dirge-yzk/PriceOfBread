#!/usr/bin/env bash
# (1377) c146wy 腕 B（計器つき 10 種・種 1〜10・旗 1 の木・表を絞る＝⓪(a)）と読み手を 1 本で（bg.sh で起こす・c143k59/armb.sh の写し）。
#   表＝止める線・NG・記録の読み手が読む表＋この線の門と記録（scalars_s・center_s・cwend_s・crownpc_s・dcont_s）。写しは作業ツリーから dir:。
#   bash scripts/instruments/c146wy/armb.sh <コミット> <作業フォルダ（D:/…）>
C=${1:?コミット}; export POB_WORK=${2:?作業フォルダ}
cd "$(dirname "$0")/../../.."; R=$(pwd); S=$R/scripts/instruments
export TEMP=$POB_WORK/tmp TMP=$POB_WORK/tmp; mkdir -p $POB_WORK/tmp
export POB_INSTRUMENTS_TABLES="nation_s,stock_s,wars_s,casualty_s,region_s,class_s,migration_s,pxmonth_s,scalars_s,fiscal_s,center_s,cwend_s,crownpc_s,dcont_s"
[ -z "$(git status --porcelain)" ] && [ "$(git rev-parse HEAD)" = "$(git rev-parse $C)" ] || { echo "木が汚れているか HEAD がコミットと違う"; exit 1; }
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "armB start $(date '+%F %T') tree=$(git rev-parse --short=9 HEAD) Dfree=${FREE}GB armsfree=$(bash $S/tools/arms.sh free)" >> D:/PoB_work/c146wy_work/arms_start.txt
bash $S/batch.sh prep c146wyB dir:$R plain > $POB_WORK/prep_B.txt 2>&1 || { echo "PREPFAIL"; exit 1; }
INS=1 bash $S/batch.sh run c146wyB 10 > $POB_WORK/run_B.txt 2>&1; RC=$?
echo "armB rc=$RC $(tail -1 $POB_WORK/run_B.txt)"
echo "armB end $(date '+%F %T') rc=$RC" >> D:/PoB_work/c146wy_work/arms_start.txt
rm -rf $POB_WORK/src_c146wyB
WD=$(cd $POB_WORK && pwd -W)
bash $S/c146wy/read_wy.sh "$WD/out_c146wyB_10/ins" "$WD/b10_c146wyB_10.txt" c146wyB "$WD/read" 1 > $POB_WORK/read_B.log 2>&1
echo "ARMBDONE $(date '+%F %T')"
exit $RC
