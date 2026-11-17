#!/usr/bin/env bash
# (1193) c119nd 素の 10 種（計器なし＝D: をほとんど使わない）の成績表と .cost（D: が腕 B に足りないときの先行の腕）。
#   bash scripts/instruments/c119nd/armp.sh <コミット> <作業フォルダ（D:/…）>
C=${1:?コミット}; export POB_WORK=${2:?作業フォルダ}
cd "$(dirname "$0")/../../.."; R=$(pwd); S=$R/scripts/instruments
export TEMP=$POB_WORK/tmp TMP=$POB_WORK/tmp; mkdir -p $POB_WORK/tmp
bash $S/batch.sh prep c119ndP commit:$C plain > $POB_WORK/prep_P.txt 2>&1 || { echo "PREPFAIL"; exit 1; }
bash $S/batch.sh run c119ndP 10 > $POB_WORK/run_P.txt 2>&1; RC=$?
echo "armP rc=$RC $(tail -1 $POB_WORK/run_P.txt)"
echo "ARMPDONE $(date '+%F %T')"
exit $RC
