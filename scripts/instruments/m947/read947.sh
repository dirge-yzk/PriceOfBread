#!/usr/bin/env bash
# (947) 合流の腕の読み（読むだけ）。c83ic の門は線の台本 c83ic/reads943.sh を 1 字も変えずに呼ぶ（paper943.py・nullgate・home_break・stoplines906・an_h2 -v4・SB=1 なら ng.sh）。
#   足すのは (ii) を拾う数 imp947.py と keepsum だけ。NG の新しい基準を作るときは ng を付ける（タグ m947 で ng.sh を回す）。
#   bash read947.sh <タグ> <腕の out フォルダ（D:/ の形・中に ins）> <成績表（D:/ の形）> <種の起点 1|11> [ng]
#   出力＝D:/PoB_work/m947/read/<タグ>/。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
T=D:/PoB_work/wt_m947; cd $T || exit 9; S=scripts/instruments
TAG=${1:?タグ}; O=${2:?out}; B=${3:?成績表}; SB=${4:-1}; NG=${5:-}
R=D:/PoB_work/m947/read/$TAG; mkdir -p $R
bash $S/c83ic/reads943.sh $O $B $R $SB > $R/reads943.log 2>&1; echo "reads943 rc=$?"
if [ "$SB" = 1 ]; then SEEDS=1,2,3,4,5,6,7,8,9,10; else SEEDS=11,12,13,14,15,16,17,18,19,20; fi
python -X utf8 -I $S/m947/imp947.py $O/ins $SEEDS > $R/imp947.txt 2>&1; echo "imp rc=$?"
python -X utf8 -I $S/tools/keepsum.py $O/ins $R/keepsum.tsv > $R/keepsum.log 2>&1; echo "keepsum rc=$?"
if [ "$NG" = ng ]; then bash $S/ng.sh $O/ins m947 D:/PoB_work/m947/ng_m947 rd:measurements/ng933_20261107:m933 > $R/ng947.txt 2>&1; echo "ng947 rc=$?"; fi
echo READDONE
