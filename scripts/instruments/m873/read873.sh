#!/usr/bin/env bash
# (873) 合流の腕の読み（読むだけ・事前登録 measurements/m873_20261106/prereg_873.txt の [4]〜[6]）。線の台本は 1 字も変えずに呼ぶ。
#   bash read873.sh m <腕の名> <ins>   種 1〜10：止める線・空洞化の振り分け（holclass839 と holsplit867）・門（ss と po の両側）
#   bash read873.sh s <腕の名> <ins>   種 11〜20：止める線・振り分け・門（ss は gates_s11_870.py・po は remap871＋gates_po860＝近似の記録）
#   bash read873.sh n <腕の名> <ins>   ヌル腕：止める線だけ
#   出力：D:/PoB_work/m873/read/<腕の名>/
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
R=D:/PoB_work/wt_m873; S=$R/scripts/instruments; K=D:/PoB_work/keep_852; B=$R/measurements/m852_20261105
MODE=${1:?m|s|n}; L=${2:?腕の名}; I=${3:?ins}; O=D:/PoB_work/m873/read/$L
mkdir -p $O
PY="python -X utf8 -I"
case $MODE in
  m)
    bash $S/m866/stoplines866.sh $I $O/stop_$L.txt 1 > /dev/null
    $PY $S/m839/holclass839.py $O/stop_$L.txt $B/armM/stop_mM.txt > $O/holclass_$L.txt 2>&1
    $PY $S/c72gt/holsplit867.py $O/stop_$L.txt $I $O/holsplit_$L.txt --null $B/armM/stop_mM.txt $K/base_ins --null $B/null/stop_n1_852.txt $K/n1 --null $B/null/stop_n2_852.txt $K/n2 > $O/holsplit_$L.log 2>&1
    $PY $S/c72gt/gates_ss867.py $I $K/base_ins $K/n1 $K/n2 $O/gates_ss_$L.txt 10 1 $S/c72gt/gates_ss867_thresholds.tsv > $O/gates_ss_$L.log 2>&1; echo "gates_ss rc=$?"
    $PY $S/c72po/gt860/gates_po860.py $I $K/base_ins $K/n1 $K/n2 $O/gates_po_$L.txt 10 1 $S/c72po/gt860/gates_po860_thresholds.tsv > $O/gates_po_$L.log 2>&1; echo "gates_po rc=$?" ;;
  s)
    bash $S/m866/stoplines866.sh $I $O/stop_$L.txt 11 > /dev/null
    $PY $S/m839/holclass839.py $O/stop_$L.txt $B/armS11/stop_mS.txt > $O/holclass_$L.txt 2>&1
    $PY $S/c72gt/holsplit867.py $O/stop_$L.txt $I $O/holsplit_$L.txt --null $B/armS11/stop_mS.txt $K/a11 --null $B/armS11/stop_mS.txt $K/a11 --null $B/armS11/stop_mS.txt $K/a11 > $O/holsplit_$L.log 2>&1
    rm -rf $O/view_ss $O/remap_arm $O/remap_a11
    $PY $S/c72ss/gates_s11_870.py $I $K/a11 $O/view_ss $O/gates_ss_$L.txt > /dev/null 2> $O/gates_ss_$L.err; echo "gates_ss rc=$?"
    $PY $S/c72po/remap871.py $I $O/remap_arm && $PY $S/c72po/remap871.py $K/a11 $O/remap_a11
    $PY $S/c72po/gt860/gates_po860.py $O/remap_arm $O/remap_a11 $O/remap_a11 $O/remap_a11 $O/gates_po_$L.txt 10 1 $S/c72po/gt860/gates_po860_thresholds.tsv > $O/gates_po_$L.log 2>&1; echo "gates_po rc=$?"
    rm -rf $O/view_ss $O/remap_arm $O/remap_a11 ;;
  n)
    bash $S/m866/stoplines866.sh $I $O/stop_$L.txt 1 > /dev/null ;;
esac
echo READDONE
