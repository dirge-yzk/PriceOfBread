#!/usr/bin/env bash
# (1277) c131pf 腕 B（計器つき 10 種・種 1〜10・旗 1 の木・表を絞る＝指示の ⓪(a)）と読み手を 1 本で（bg.sh で起こす・c126pf/armb.sh の写し）。
#   表＝ident.cfg の SMALL（止める線・NG・監視の読み手の表）＋popclose_s・port_s・capskip_s・nationgood_s（史実の桁の読み手と計器）。sellr_s（1 種 192MB・記録の H5 だけ）は外す（D: の床・事前登録の追記 4）。
#   bash scripts/instruments/c131pf/armb.sh <コミット> <作業フォルダ（D:/…）>
C=${1:?コミット}; export POB_WORK=${2:?作業フォルダ}
cd "$(dirname "$0")/../../.."; R=$(pwd); S=$R/scripts/instruments
export TEMP=$POB_WORK/tmp TMP=$POB_WORK/tmp; mkdir -p $POB_WORK/tmp
SMALL=$(grep '^SMALL=' $S/ident/ident.cfg | cut -d= -f2)
export POB_INSTRUMENTS_TABLES="$SMALL,popclose_s,port_s,capskip_s,nationgood_s"
bash $S/batch.sh prep c131pfB commit:$C plain > $POB_WORK/prep_B.txt 2>&1 || { echo "PREPFAIL"; exit 1; }
INS=1 bash $S/batch.sh run c131pfB 10 > $POB_WORK/run_B.txt 2>&1; RC=$?
echo "armB rc=$RC $(tail -1 $POB_WORK/run_B.txt)"
rm -rf $POB_WORK/src_c131pfB
WD=$(cd $POB_WORK && pwd -W)
bash $S/c131pf/read_pf.sh "$WD/out_c131pfB_10/ins" "$WD/b10_c131pfB_10.txt" c131pfB "$WD/read" 1 > $POB_WORK/read_B.log 2>&1
echo "ARMBDONE $(date '+%F %T')"
exit $RC
