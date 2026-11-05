#!/usr/bin/env bash
# (848) 基線とヌル腕 3 世代を同じ台本（stoplines848.sh）で数え直す（読むだけ・⑰）。使い方: bash stopnull848.sh <出力フォルダ>
O=${1:?出力フォルダ}; mkdir -p $O
for x in base:D:/PoB_work/keep_845/base_ins n1_839:D:/PoB_work/keep_839/n1 n2_839:D:/PoB_work/keep_839/n2 n1_826:D:/PoB_work/keep_826/n1 n2_826:D:/PoB_work/keep_826/n2 n1_797:D:/PoB_work/keep_797/n1 n2_797:D:/PoB_work/keep_797/n2; do
  bash D:/PoB_work/wt_c69eh/scripts/instruments/c69eh/stoplines848.sh ${x#*:} $O/stop_${x%%:*}.txt
done
echo NULLDONE
