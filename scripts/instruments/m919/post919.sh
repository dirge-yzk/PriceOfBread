#!/usr/bin/env bash
# (919) 合流の腕の後の読み手（c79ce の post915.sh の写し・木を wt_m919・出力を D:/PoB_work/m919/read/<タグ> にし、成績表と生の場所を引数にした・読むだけ）。
#   bash post919.sh <タグ> <腕の ins> <基準の ins> <種の起点 1|11> <基準の成績表（measurements/ の名）> <腕の成績表>
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
T=/d/PoB_work/wt_m919; S=$T/scripts/instruments
TAG=${1:?タグ}; M=${2:?腕の ins}; BASE=${3:?基準の ins}; SB=${4:-1}; BREP=${5:?基準の成績表}; BAND=${6:?腕の成績表}
P=D:/PoB_work/m919/read/$TAG
mkdir -p $P; cd $T
python -X utf8 -I $S/c77ce/gate902.py $M $P/gate_$TAG.txt
python -X utf8 -I $S/c77ce/dom902.py $M $P/dom_$TAG.txt
python -X utf8 -I $S/c77ce/ratio902.py $M $BASE D:/PoB_work/keep_905/n1 D:/PoB_work/keep_905/n2 > $P/ratio_$TAG.txt 2>&1
python -X utf8 -I $S/c77ce/gdpzero_born902.py $M > $P/gdpzero_born_$TAG.txt 2>&1
bash $S/c78gz/stoplines906.sh $M $P/stop906_$TAG.txt $SB
python -X utf8 $S/hol/an_h2.py $M -v4 > $P/hol_$TAG.txt 2>&1
python -X utf8 -I $S/tools/nullgate.py $BAND $T/measurements/$BREP $T/measurements/b10_20261106_m905_n1_ins $T/measurements/b10_20261106_m905_n2_ins > $P/nullgate_$TAG.txt 2>&1
bash $S/ng.sh $M $TAG D:/PoB_work/m919/ng_$TAG rd:measurements/ng905_20261106:m905 > $P/ng_$TAG.txt 2>&1
python -X utf8 -I $S/tools/keepsum.py $M $P/keepsum_$TAG.tsv
echo POSTDONE
