#!/usr/bin/env bash
# (1277) c131pf 腕 B（計器つき 10 種・種 1〜10・旗 1 の木・表を絞る＝指示の ⓪(a)）と読み手を 1 本で（bg.sh で起こす・c126pf/armb.sh の写し）。
#   表＝止める線・NG・史実の桁・計器の読み手が読む 13 表（事前登録の追記 5）。sellr_s と監視の表は外す（D: の床）。写しは作業ツリーから dir:（measurements・docs を除く）。
#   bash scripts/instruments/c131pf/armb.sh <コミット> <作業フォルダ（D:/…）>
C=${1:?コミット}; export POB_WORK=${2:?作業フォルダ}
cd "$(dirname "$0")/../../.."; R=$(pwd); S=$R/scripts/instruments
export TEMP=$POB_WORK/tmp TMP=$POB_WORK/tmp; mkdir -p $POB_WORK/tmp
SMALL=$(grep '^SMALL=' $S/ident/ident.cfg | cut -d= -f2)
export POB_INSTRUMENTS_TABLES="nation_s,stock_s,wars_s,casualty_s,region_s,migration_s,pxmonth_s,scalars_s,fiscal_s,nationgood_s,port_s,capskip_s,popclose_s"   # 追記 5：門と記録の読み手が読む表だけ（SMALL の監視の表は外す）
[ -z "$(git status --porcelain)" ] && [ "$(git rev-parse HEAD)" = "$(git rev-parse $C)" ] || { echo "木が汚れているか HEAD がコミットと違う"; exit 1; }   # 追記 5：写しは作業ツリーから（measurements と docs を除く＝dir:）
bash $S/batch.sh prep c131pfB dir:$R plain > $POB_WORK/prep_B.txt 2>&1 || { echo "PREPFAIL"; exit 1; }
INS=1 bash $S/batch.sh run c131pfB 10 > $POB_WORK/run_B.txt 2>&1; RC=$?
echo "armB rc=$RC $(tail -1 $POB_WORK/run_B.txt)"
rm -rf $POB_WORK/src_c131pfB
WD=$(cd $POB_WORK && pwd -W)
bash $S/c131pf/read_pf.sh "$WD/out_c131pfB_10/ins" "$WD/b10_c131pfB_10.txt" c131pfB "$WD/read" 1 > $POB_WORK/read_B.log 2>&1
echo "ARMBDONE $(date '+%F %T')"
exit $RC
