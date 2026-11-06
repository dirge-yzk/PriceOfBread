#!/usr/bin/env bash
# (919) 合流：新しいヌル腕（N1・N2）と、新しいヌル腕の幅で読む同じ盤面の新÷旧（読むだけ）。
#   bash readN919.sh <タグ n1|n2> <ins>      出力＝D:/PoB_work/m919/read/<タグ>/
#   bash readN919.sh ratio                   M・N1・N2 の新÷旧（ratio902.py を 1 字も変えずに呼ぶ：腕 M・基準 keep_912/base_ins・ヌル N1・N2＝合わせた木の物）
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
T=/d/PoB_work/wt_m919; S=$T/scripts/instruments; W=D:/PoB_work/m919
cd $T
if [ "$1" = ratio ]; then
  mkdir -p $W/read/ratio
  python -X utf8 -I $S/c77ce/ratio902.py $W/out_mM_10/ins D:/PoB_work/keep_912/base_ins $W/out_n1_10/ins $W/out_n2_10/ins > $W/read/ratio/ratio_mM_newnull.txt 2>&1
  python -X utf8 -I $S/c77ce/ratio902.py $W/out_mS_10_s11/ins D:/PoB_work/keep_905/a11 $W/out_n1_10/ins $W/out_n2_10/ins > $W/read/ratio/ratio_mS_newnull_approx.txt 2>&1
  echo RATIODONE; exit 0
fi
TAG=${1:?タグ}; M=${2:?ins}; P=$W/read/$TAG; mkdir -p $P
bash $S/c78gz/stoplines906.sh $M $P/stop906_$TAG.txt 1
python -X utf8 $S/hol/an_h2.py $M -v4 > $P/hol_$TAG.txt 2>&1
python -X utf8 -I $S/c77ce/gate902.py $M $P/gate_$TAG.txt
bash $S/ng.sh $M $TAG $W/ng_$TAG rd:measurements/ng905_20261106:m905 > $P/ng_$TAG.txt 2>&1
python -X utf8 -I $S/tools/keepsum.py $M $P/keepsum_$TAG.tsv
echo READNDONE
