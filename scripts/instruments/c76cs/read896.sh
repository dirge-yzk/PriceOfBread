#!/usr/bin/env bash
# (896) c76cs 腕の読み（読むだけ・事前登録 measurements/m896_20261106_prereg_cs.txt §3・§7・§8）。他の線の台本は 1 字も変えずに呼ぶ。
#   bash read896.sh b <ins>   種 1〜10：止める線・空洞化の振り分け（holclass839＝T1・holown880 は記録）・門 G1〜G4・下向きの跳び（記録）・nullgate（記録）・ng（記録）
#   bash read896.sh s <ins>   種 11〜20：止める線・振り分け・門（基準 keep_873/a11・幅は種 1〜10 の物＝近似）
#   出力：D:/PoB_work/c76cs_work/read/<b|s>/
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
R=C:/Users/dirge/Documents/PriceOfBread; S=$R/scripts/instruments; W=D:/PoB_work/wt_c76cs; C=$W/scripts/instruments/c76cs
MODE=${1:?b|s}; I=${2:?ins}; O=D:/PoB_work/c76cs_work/read/$MODE
mkdir -p $O
PY="python -X utf8 -I"
case $MODE in
  b)
    bash $S/m866/stoplines866.sh $I $O/stop_b.txt 1 > /dev/null
    $PY $S/m839/holclass839.py $O/stop_b.txt $R/measurements/m873_20261106/armM/stop_mM.txt > $O/holclass_b.txt 2>&1
    $PY $S/c74id/holown880.py $O/stop_b.txt $I $O/holown_b.txt --base $R/measurements/m873_20261106/armM/stop_mM.txt D:/PoB_work/keep_892/base_ins > $O/holown_b.log 2>&1
    $PY $S/c73gt/gdp_down875.py arm=$I base=D:/PoB_work/keep_892/base_ins N1=D:/PoB_work/keep_873/n1 N2=D:/PoB_work/keep_873/n2 > $O/gdpdown_b.txt 2>&1
    python -X utf8 $C/gates896.py $I D:/PoB_work/keep_892/base_ins D:/PoB_work/keep_892/base_ins D:/PoB_work/keep_873/n1 D:/PoB_work/keep_873/n2 1 $O/gates_b.txt > $O/gates_b.log 2>&1; echo "gates rc=$?"
    ;;
  s)
    bash $S/m866/stoplines866.sh $I $O/stop_s.txt 11 > /dev/null
    $PY $S/m839/holclass839.py $O/stop_s.txt $R/measurements/m873_20261106/armS11/stop_mS.txt > $O/holclass_s.txt 2>&1
    $PY $S/c74id/holown880.py $O/stop_s.txt $I $O/holown_s.txt --base $R/measurements/m873_20261106/armS11/stop_mS.txt D:/PoB_work/keep_873/a11 > $O/holown_s.log 2>&1
    $PY $S/c73gt/gdp_down875.py arm=$I base=D:/PoB_work/keep_873/a11 > $O/gdpdown_s.txt 2>&1
    python -X utf8 $C/gates896.py $I D:/PoB_work/keep_873/a11 D:/PoB_work/keep_892/base_ins D:/PoB_work/keep_873/n1 D:/PoB_work/keep_873/n2 11 $O/gates_s.txt > $O/gates_s.log 2>&1; echo "gates rc=$?"
    ;;
esac
echo READDONE
