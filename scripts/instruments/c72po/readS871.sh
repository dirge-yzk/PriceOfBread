#!/usr/bin/env bash
# (871) c72po S11（c72poS・種 11〜20）の読み（読むだけ）：止める線（stoplines871.sh・種の起点 11）・空洞化の振り分け（holclass839.py＝基線 keep_852/a11 を同じ台本で数えた出力・
#   c72gt の holsplit867.py は印の後に別に呼ぶ）・門（gates_po860.py・種 11〜20 を種 1〜10 の名へ並べ替えて読む＝記録・近似）。
#   bash readS871.sh   入力 D:/PoB_work/c72po_work/out_c72poS_10_s11/ins・出力 D:/PoB_work/wt_c72po/measurements/m871_20261106/
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
cd D:/PoB_work/wt_c72po; S=scripts/instruments; W=D:/PoB_work/c72po_work; O=measurements/m871_20261106
I=$W/out_c72poS_10_s11/ins
mkdir -p $O
bash $S/c72po/stoplines871.sh $I $O/stop_S11.txt 11
python -X utf8 -I $S/m839/holclass839.py $O/stop_S11.txt $O/stop_a11.txt > $O/holclass_S11.txt 2>&1
echo "holclass rc=$?"
python -X utf8 -I $S/c72po/gt867/holsplit867.py $O/stop_S11.txt $I $O/holsplit_S11.txt --null $O/stop_a11.txt D:/PoB_work/keep_852/a11 > $O/holsplit_S11.log 2>&1
echo "holsplit rc=$?"
rm -rf $W/remap_S11 $W/remap_a11
python -X utf8 -I $S/c72po/remap871.py $I $W/remap_S11 && python -X utf8 -I $S/c72po/remap871.py D:/PoB_work/keep_852/a11 $W/remap_a11
python -X utf8 -I $S/c72po/gt860/gates_po860.py $W/remap_S11 $W/remap_a11 $W/remap_a11 $W/remap_a11 $O/gates_S11.txt 10 1 $S/c72po/gt860/gates_po860_thresholds.tsv > $O/gates_S11.log 2>&1
echo "gates rc=$?"
echo READDONE
