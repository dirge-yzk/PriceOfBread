#!/usr/bin/env bash
# (862) c71po 腕 B の読み（読むだけ）：門（c71gt の gates_po860.py）・止める線（stoplines862.sh）・空洞化の振り分け（holclass839.py・T1）・門 A′（nullgate.py・記録）・NG（ng.sh・記録）。
#   bash readB862.sh   入力 D:/PoB_work/c71po_work/out_c71poB_10/ins と同じフォルダの成績表・出力 D:/PoB_work/wt_c71po/measurements/m862_20261105/
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
cd D:/PoB_work/wt_c71po; S=scripts/instruments; W=D:/PoB_work/c71po_work; O=measurements/m862_20261105
A=$W/out_c71poB_10; I=$A/ins
mkdir -p $O
python -X utf8 -I $S/c71po/gt860/gates_po860.py $I D:/PoB_work/keep_852/base_ins D:/PoB_work/keep_852/n1 D:/PoB_work/keep_852/n2 $O/gatesB.txt 10 1 $S/c71po/gt860/gates_po860_thresholds.tsv > $O/gatesB.log 2>&1
echo "gates rc=$?"
bash $S/c71po/stoplines862.sh $I $O/stop_B.txt
python -X utf8 -I $S/m839/holclass839.py $O/stop_B.txt $O/stop_null/stop_M852.txt > $O/holclass_B.txt 2>&1
echo "holclass rc=$?"
B=$(ls $W/b10_c71poB_10.txt 2>/dev/null || ls $A/*.txt 2>/dev/null | head -1)
echo "成績表 $B"
python -X utf8 -I $S/tools/nullgate.py $B measurements/b10_20261105_m852_ins measurements/b10_20261105_m852_n1_ins measurements/b10_20261105_m852_n2_ins > $O/nullgateB.txt 2>&1
echo "nullgate rc=$?"
bash $S/ng.sh D:/PoB_work/c71po_work/out_c71poB_10/ins c71poB D:/PoB_work/c71po_work/ngB rd:measurements/ng852_20261105:m852 > $O/ngB.txt 2>&1
echo "ng rc=$?"
echo READDONE
