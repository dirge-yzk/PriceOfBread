#!/usr/bin/env bash
# (1172) c116t1 旗 1 の短い走り（2 種×80 四半期・計器は t1buy_s と止める線の表だけ・写しの木＝<コミット>）。bg.sh で起こす。
#   bash scripts/instruments/c116t1/short.sh <コミット> <作業フォルダ（D:/…）>
#   出力：<作業フォルダ>/short.txt（成績表）・<作業フォルダ>/short_ins（計器）・<作業フォルダ>/short_gates.txt（t1gates3.py の数え・2 種 80 四半期＝床は 10 種全期間の物＝向きと桁だけ読む）
C=${1:?コミット}; export POB_WORK=${2:?作業フォルダ}
cd "$(dirname "$0")/../../.."; R=$(pwd); S=$R/scripts/instruments
export TEMP=$POB_WORK/tmp TMP=$POB_WORK/tmp; mkdir -p $POB_WORK/tmp
bash $S/batch.sh prep t1short commit:$C plain > $POB_WORK/prep_short.txt 2>&1 || { echo "PREPFAIL"; exit 1; }
bash $S/tools/arms.sh acquire t1short $$ || { echo "ARMFULL"; exit 3; }
trap 'bash $S/tools/arms.sh release t1short $$' EXIT
O=$POB_WORK/short_ins; rm -rf $O $POB_WORK/snap_short; mkdir -p $O
export POB_INSTRUMENTS=$O POB_INSTRUMENTS_TABLES=t1buy_s,tb_s,nation_s,pxmonth_s,region_s,scalars_s
export POB_INSTRUMENTS_NATIONS="清,ロシア,オスマン,フランス,イギリス,プロイセン,オーストリア,日本,アメリカ"
SEEDS=2 SNAP_DIR=$POB_WORK/snap_short bash $POB_WORK/src_t1short/measure.sh $POB_WORK/short.txt selfplay-batch 2 - 0 1 80 > $POB_WORK/short_ms.log 2>&1; RC=$?
python -X utf8 -I $S/c116t1/t1gates3.py $O 1 2 46539221 3476739 > $POB_WORK/short_gates.txt 2>&1
echo "short rc=$RC $(tail -1 $POB_WORK/short.txt 2>/dev/null)"
exit $RC
