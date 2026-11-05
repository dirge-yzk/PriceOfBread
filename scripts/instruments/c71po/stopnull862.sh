#!/usr/bin/env bash
# (862) c71po 基線とヌル腕（新 m852・旧 m839）を stoplines862.sh で同じ台本で数え直す（逐次・読むだけ）。(856) の stopnull856.sh の写し（m826・m797 の生は消えた＝外した）。
#   bash stopnull862.sh <出力フォルダ>
O=${1:?出力}; mkdir -p $O
for spec in M852=D:/PoB_work/keep_852/base_ins N1_852=D:/PoB_work/keep_852/n1 N2_852=D:/PoB_work/keep_852/n2 N1_839=D:/PoB_work/keep_839/n1 N2_839=D:/PoB_work/keep_839/n2; do
  n=${spec%%=*}; d=${spec#*=}
  bash D:/PoB_work/wt_c71po/scripts/instruments/c71po/stoplines862.sh $d $O/stop_$n.txt
  echo "$n done"
done
echo STOPNULLDONE
