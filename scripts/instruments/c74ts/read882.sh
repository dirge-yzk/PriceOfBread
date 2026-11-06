#!/usr/bin/env bash
# (882) c74ts 腕の読み（読むだけ）：門（gw882 の GW・G5h と gates_ts877 の写しの門）・止める線（stoplines866 の写し）・空洞化の振り分け（holclass839）
#   bash read882.sh B <ins>   種 1〜10（基線 keep_879/base_ins・ヌル keep_873/n1・n2・止める線の基準 m873 armM/stop_mM.txt）
#   bash read882.sh S <ins>   種 11〜20（ヌル a11 の 1 本＝門は近似・記録・止める線の基準 m873 armS11/stop_mS.txt）
#   出力：D:/PoB_work/c74ts_work/read_<B|S>/
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
K=${1:?B|S}; INS=${2:?ins}; W=/d/PoB_work/wt_c74ts; S=$W/scripts/instruments; C=$S/c74ts; O=D:/PoB_work/c74ts_work/read_$K; mkdir -p $O
M=/c/Users/dirge/Documents/PriceOfBread/measurements/m873_20261106; PY="python -X utf8"
if [ "$K" = B ]; then
  $PY $C/agg882.py $O/agg882_arm.json ARM=$INS --seeds 1-10 --jobs 10 > $O/agg.log 2>&1
  $PY $C/gw882.py --arm ARM=$O/agg882_arm.json $C/../../../measurements/m882_20261106/agg882_null.json $C/gw882_thresholds.tsv $O/gates_gw882.txt 1-10 > /dev/null 2>&1
  $PY $C/gates_ts877.py $INS D:/PoB_work/keep_879/base_ins D:/PoB_work/keep_873/n1 D:/PoB_work/keep_873/n2 $O/gates_ts877.txt 10 $C/gates_ts882_thresholds.tsv 1 > /dev/null 2>&1
  bash $C/stoplines866.sh $INS $O/stop_B.txt 1 > /dev/null 2>&1
  $PY -I $S/m839/holclass839.py $O/stop_B.txt $M/armM/stop_mM.txt > $O/holclass_B.txt 2>&1
else
  $PY $C/agg882.py $O/agg882_arm.json ARM=$INS --seeds 11-20 --jobs 10 > $O/agg.log 2>&1
  $PY $C/gw882.py --s11 ARM=$O/agg882_arm.json $C/../../../measurements/m882_20261106/agg882_a11.json $C/gw882_thresholds.tsv $O/gates_gw882_s11.txt 11-20 > /dev/null 2>&1
  $PY $C/gates_ts877.py $INS D:/PoB_work/keep_873/a11 D:/PoB_work/keep_873/a11 D:/PoB_work/keep_873/a11 $O/gates_ts877_s11.txt 10 $C/gates_ts882_thresholds.tsv 11 > /dev/null 2>&1   # 記録：G2・G3a・G4・G4m だけを読む（ヌル腕が 1 本＝幅の門は意味がない）
  bash $C/stoplines866.sh $INS $O/stop_S.txt 11 > /dev/null 2>&1
  $PY -I $S/m839/holclass839.py $O/stop_S.txt $M/armS11/stop_mS.txt > $O/holclass_S.txt 2>&1
fi
echo READDONE $K
