#!/usr/bin/env bash
# (1277) c131pf 先読み（事前登録 [2]）：器のコミットの木で S11 の種 20 だけを旗 0／1 で回す（計器つき・表を絞る）。bg.sh で起こす。
#   bash scripts/instruments/c131pf/probe20.sh <コミット> <旗 0|1>
C=${1:?コミット}; F=${2:?旗}
cd "$(dirname "$0")/../../.."; S=$(pwd)/scripts/instruments
export POB_WORK=D:/PoB_work/c131pf_work/probe; mkdir -p $POB_WORK/tmp
export TEMP=$POB_WORK/tmp TMP=$POB_WORK/tmp
ARM=c131pfp$F
SMALL=$(grep '^SMALL=' $S/ident/ident.cfg | cut -d= -f2)
export POB_INSTRUMENTS_TABLES="$SMALL,popclose_s,port_s,capskip_s,nationgood_s,sellr_s"
bash $S/batch.sh prep $ARM commit:$C plain > $POB_WORK/prep_$ARM.txt 2>&1 || { echo PREPFAIL; exit 1; }
if [ "$F" = 1 ]; then echo "CapSkipSellerPotential=1" > $POB_WORK/coef_$ARM.txt; else rm -f $POB_WORK/coef_$ARM.txt; fi
INS=1 bash $S/batch.sh run $ARM 1 20 > $POB_WORK/run_$ARM.txt 2>&1; RC=$?
echo "probe $ARM rc=$RC $(tail -1 $POB_WORK/run_$ARM.txt)"
exit $RC
