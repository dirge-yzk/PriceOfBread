#!/usr/bin/env bash
# (915) c79ce 腕の後の読み手を 1 本で回す（読むだけ）。
#   bash post915.sh <タグ c79ceB_10|c79ceS_10_s11> <基準の ins（D:/ の形）> <種の起点 1|11> <基準の成績表（measurements/ の名）>
#   出力＝D:/PoB_work/c79ce_work/post_<タグ>/（線が measurements/m915_20261107/ へ写す）。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
T=/d/PoB_work/wt_c79ce; S=$T/scripts/instruments; W=D:/PoB_work/c79ce_work
TAG=${1:?タグ}; BASE=${2:?基準の ins}; SB=${3:-1}; BREP=${4:?基準の成績表}
M=$W/out_$TAG/ins; P=$W/post_$TAG
mkdir -p $P; cd $T
python -X utf8 -I $S/c77ce/gate902.py $M $P/gate_$TAG.txt
python -X utf8 -I $S/c77ce/dom902.py $M $P/dom_$TAG.txt
python -X utf8 -I $S/c77ce/dom902.py $BASE $P/dom_base.txt
python -X utf8 -I $S/c77ce/ratio902.py $M $BASE D:/PoB_work/keep_905/n1 D:/PoB_work/keep_905/n2 > $P/ratio_$TAG.txt 2>&1
python -X utf8 -I $S/c77ce/gdpzero_born902.py $M > $P/gdpzero_born_$TAG.txt 2>&1
bash $S/c78gz/stoplines906.sh $M $P/stop906_$TAG.txt $SB
python -X utf8 $S/hol/an_h2.py $M -v4 > $P/hol_$TAG.txt 2>&1
python -X utf8 $S/hol/an_h2.py $BASE -v4 > $P/hol_base.txt 2>&1
python -X utf8 -I $S/tools/nullgate.py $W/b10_$TAG.txt $T/measurements/$BREP $T/measurements/b10_20261106_m905_n1_ins $T/measurements/b10_20261106_m905_n2_ins > $P/nullgate_$TAG.txt 2>&1
bash $S/ng.sh $M $TAG $W/ng_$TAG rd:measurements/ng905_20261106:m905 > $P/ng_$TAG.txt 2>&1
echo POSTDONE
