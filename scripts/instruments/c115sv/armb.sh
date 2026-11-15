#!/usr/bin/env bash
# (1166) c115sv 腕 B（計器つき 10 種・種 1〜10・旗 1 の木）と読み手を 1 本で（bg.sh で起こす）。
#   bash scripts/instruments/c115sv/armb.sh <コミット> <作業フォルダ（D:/…）> <full|narrow>
#   narrow＝事前登録の〈絞った形〉：POB_INSTRUMENTS_TABLES＝門の台本の表＋止める線の表＋標準の読み手の表＋ident.cfg の SMALL（＋landsale_s・stock_s）。
C=${1:?コミット}; export POB_WORK=${2:?作業フォルダ}; MODE=${3:?full|narrow}
cd "$(dirname "$0")/../../.."; R=$(pwd); S=$R/scripts/instruments
export TEMP=$POB_WORK/tmp TMP=$POB_WORK/tmp; mkdir -p $POB_WORK/tmp
if [ "$MODE" = narrow ]; then
  SMALL=$(grep '^SMALL=' $S/ident/ident.cfg | cut -d= -f2)
  export POB_INSTRUMENTS_TABLES="cpop_s,nation_s,admrule_s,rule_s,soldiervac_s,monlaw_s,pxmonth_s,region_s,fiscal_s,goods_s,scalars_s,secession_s,cessp_s,migration_s,landsale_s,stock_s,$SMALL"
fi
bash $S/batch.sh prep c115svB commit:$C plain > $POB_WORK/prep_B.txt 2>&1 || { echo "PREPFAIL"; exit 1; }
INS=1 bash $S/batch.sh run c115svB 10 > $POB_WORK/run_B.txt 2>&1; RC=$?
echo "armB rc=$RC $(tail -1 $POB_WORK/run_B.txt)"
O=$POB_WORK/out_c115svB_10/ins; CARD=$POB_WORK/b10_c115svB_10.txt
bash $S/c115sv/read_sv.sh "$(cd $POB_WORK && pwd -W)/out_c115svB_10/ins" "$(cd $POB_WORK && pwd -W)/b10_c115svB_10.txt" c115svB "$(cd $POB_WORK && pwd -W)/read" 1 1 > $POB_WORK/read_B.log 2>&1
P=D:/PoB_work/r1162_work/scripts/r1162
python -X utf8 -I $P/r1059_d1_copy.py "$O" 1 10 $POB_WORK/read/d1_B.txt B > /dev/null 2>&1 && python -X utf8 -I $P/soldier0.py "$O" 1 10 $POB_WORK/read/d1_B.txt $POB_WORK/read/soldier0_B.txt > /dev/null 2>&1
python -X utf8 -I $S/c115sv/rec_sv.py M=D:/PoB_work/keep_1164/base_ins B=$O > $POB_WORK/read/rec.txt 2>&1
echo "ARMBDONE $(date '+%F %T')"
exit $RC
