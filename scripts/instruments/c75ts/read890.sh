#!/usr/bin/env bash
# (890) c75ts 腕の生の読み（読むだけ・c74ts/read882.sh の写し・門を gates_ts890 と閾値の表 gates_ts890_thresholds.tsv へ・止める線を stoplines890 へ・基線を keep_885/base_ins へ）
#   bash read890.sh B <ins>   種 1〜10（(882) の腕 B の生＝★(873) の型の近似・基線 keep_885/base_ins・ヌル keep_873/n1・n2・止める線の基準 m873 armM/stop_mM.txt）
#   bash read890.sh S <ins>   種 11〜20（線の S11・ヌル a11 の 1 本＝幅の門は近似・記録・止める線の基準 m873 armS11/stop_mS.txt）
#   出力：D:/PoB_work/c75ts_work/read_<B|S>/
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
K=${1:?B|S}; INS=${2:?ins}; W=/d/PoB_work/wt_c75ts; S=$W/scripts/instruments; C=$S/c75ts; O=D:/PoB_work/c75ts_work/read_$K; mkdir -p $O
M=/c/Users/dirge/Documents/PriceOfBread/measurements/m873_20261106; PY="python -X utf8"; NJ=$W/measurements/m882_20261106
if [ "$K" = B ]; then
  $PY $C/agg882.py $O/agg882_arm.json ARM=$INS --seeds 1-10 --jobs 10 > $O/agg.log 2>&1
  $PY $C/gw882.py --arm ARM=$O/agg882_arm.json $NJ/agg882_null.json $C/gw882_thresholds.tsv $O/gates_gw882.txt 1-10 > /dev/null 2>&1
  $PY $C/gates_ts890.py $INS D:/PoB_work/keep_885/base_ins D:/PoB_work/keep_873/n1 D:/PoB_work/keep_873/n2 $O/gates_ts890.txt 10 $C/gates_ts890_thresholds.tsv 1 > /dev/null 2>&1
  bash $C/stoplines890.sh $INS $O/stop_B.txt 1 > /dev/null 2>&1
  $PY -I $S/m839/holclass839.py $O/stop_B.txt $M/armM/stop_mM.txt > $O/holclass_B.txt 2>&1
  $PY -I $C/holown880.py $O/stop_B.txt $INS $O/holown_B.txt --base $M/armM/stop_mM.txt D:/PoB_work/keep_885/base_ins > $O/holown_B.log 2>&1   # R880＝記録（免除に使わない）
else
  $PY $C/agg882.py $O/agg882_arm.json ARM=$INS --seeds 11-20 --jobs 10 > $O/agg.log 2>&1
  $PY $C/gw882.py --s11 ARM=$O/agg882_arm.json $NJ/agg882_a11.json $C/gw882_thresholds.tsv $O/gates_gw882_s11.txt 11-20 > /dev/null 2>&1
  $PY $C/gates_ts890.py $INS D:/PoB_work/keep_873/a11 D:/PoB_work/keep_873/a11 D:/PoB_work/keep_873/a11 $O/gates_ts890_s11.txt 10 $C/gates_ts890_thresholds.tsv 11 > /dev/null 2>&1   # 記録：G2・G3a・G4・G4m だけを門として読む（ヌル腕が 1 本＝幅の門は意味がない）
  bash $C/stoplines890.sh $INS $O/stop_S.txt 11 > /dev/null 2>&1
  $PY -I $S/m839/holclass839.py $O/stop_S.txt $M/armS11/stop_mS.txt > $O/holclass_S.txt 2>&1
  $PY -I $C/holown880.py $O/stop_S.txt $INS $O/holown_S.txt --base $M/armS11/stop_mS.txt D:/PoB_work/keep_873/a11 > $O/holown_S.log 2>&1   # R880＝記録
fi
echo READDONE $K
