#!/usr/bin/env bash
# (856) c70po 基線とヌル腕（新 m852・旧 m839・m826・m797）を stoplines856.sh で同じ台本で数え直す（逐次・読むだけ）。
#   bash stopnull856.sh <出力フォルダ>
O=${1:?出力}; mkdir -p $O
for spec in M852=D:/PoB_work/keep_852/base_ins N1_852=D:/PoB_work/keep_852/n1 N2_852=D:/PoB_work/keep_852/n2 N1_839=D:/PoB_work/keep_839/n1 N2_839=D:/PoB_work/keep_839/n2 N1_826=D:/PoB_work/keep_826/n1 N2_826=D:/PoB_work/keep_826/n2 N1_797=D:/PoB_work/keep_797/n1 N2_797=D:/PoB_work/keep_797/n2; do
  n=${spec%%=*}; d=${spec#*=}
  bash D:/PoB_work/wt_c70po/scripts/instruments/c70po/stoplines856.sh $d $O/stop_$n.txt
  echo "$n done"
done
echo STOPNULLDONE
