#!/usr/bin/env bash
# (1202) c120t2 腕 B（計器つき 10 種・種 1〜10・旗 1 の木・全表）と読み手を 1 本で（bg.sh で起こす・c119nd/armb.sh の写し）。
#   bash scripts/instruments/c120t2/armb.sh <コミット> <作業フォルダ（D:/…）> <A0 の生（ident の out_ib_10/ins・D:/…）>
C=${1:?コミット}; export POB_WORK=${2:?作業フォルダ}; A0=${3:?A0}
[ -n "${T2_TABLES:-}" ] && export POB_INSTRUMENTS_TABLES="$T2_TABLES"
cd "$(dirname "$0")/../../.."; R=$(pwd); S=$R/scripts/instruments
export TEMP=$POB_WORK/tmp TMP=$POB_WORK/tmp; mkdir -p $POB_WORK/tmp
bash $S/batch.sh prep c120t2B commit:$C plain > $POB_WORK/prep_B.txt 2>&1 || { echo "PREPFAIL"; exit 1; }
INS=1 bash $S/batch.sh run c120t2B 10 > $POB_WORK/run_B.txt 2>&1; RC=$?
echo "armB rc=$RC $(tail -1 $POB_WORK/run_B.txt)"
WD=$(cd $POB_WORK && pwd -W)
bash $S/c120t2/read_t2.sh "$WD/out_c120t2B_10/ins" "$WD/b10_c120t2B_10.txt" c120t2B "$WD/read" 1 "$A0" > $POB_WORK/read_B.log 2>&1
echo "ARMBDONE $(date '+%F %T')"
exit $RC
