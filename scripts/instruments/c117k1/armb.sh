#!/usr/bin/env bash
# (1179) c117k1 腕 B（計器つき 10 種・種 1〜10・旗 1 の木）と読み手を 1 本で（bg.sh で起こす）。c116k1/armb.sh の写し。
#   bash scripts/instruments/c117k1/armb.sh <コミット> <作業フォルダ（D:/…）>
C=${1:?コミット}; export POB_WORK=${2:?作業フォルダ}
cd "$(dirname "$0")/../../.."; R=$(pwd); S=$R/scripts/instruments
export TEMP=$POB_WORK/tmp TMP=$POB_WORK/tmp; mkdir -p $POB_WORK/tmp
if [ "${3:-full}" = narrow ]; then   # D: の空きが〈35GB＋26GB〉に届かない時の絞った形（c115sv/armb.sh の narrow の型・sf_s を足し soldiervac_s は既定の中に残る）
  SMALL=$(grep "^SMALL=" $S/ident/ident.cfg | cut -d= -f2)
  export POB_INSTRUMENTS_TABLES="cpop_s,nation_s,admrule_s,rule_s,soldiervac_s,monlaw_s,sf_s,pxmonth_s,region_s,fiscal_s,goods_s,scalars_s,secession_s,cessp_s,migration_s,landsale_s,stock_s,$SMALL"
fi
bash $S/batch.sh prep c117k1B commit:$C plain > $POB_WORK/prep_B.txt 2>&1 || { echo "PREPFAIL"; exit 1; }
INS=1 bash $S/batch.sh run c117k1B 10 > $POB_WORK/run_B.txt 2>&1; RC=$?
echo "armB rc=$RC $(tail -1 $POB_WORK/run_B.txt)"
O=$POB_WORK/out_c117k1B_10/ins
bash $S/c117k1/read_k1.sh "$(cd $POB_WORK && pwd -W)/out_c117k1B_10/ins" "$(cd $POB_WORK && pwd -W)/b10_c117k1B_10.txt" c117k1B "$(cd $POB_WORK && pwd -W)/read" 1 1 > $POB_WORK/read_B.log 2>&1
python -X utf8 -I $S/r1162/r1059_d1_copy.py "$O" 1 10 $POB_WORK/read/d1_B.txt B > /dev/null 2>&1
python -X utf8 -I $S/c116k1/paper_k1.py "$O" 1 10 $POB_WORK/read/paper_B.txt B > /dev/null 2>&1
python -X utf8 -I $S/c116k1/ratio_k1.py "$O" 1 10 > $POB_WORK/read/ratio_B.txt 2>&1
python -X utf8 -I $S/c116k1/g1_split.py "$O" 1 10 > $POB_WORK/read/g1_split_B.txt 2>&1
echo "ARMBDONE $(date '+%F %T')"
exit $RC
