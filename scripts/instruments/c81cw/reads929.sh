#!/usr/bin/env bash
# (929) c81cw 腕の読み（読むだけ）＝(922) c80cw/reads922.sh の写し。違いは ng.sh のタグを c81cwB にしたことと、home_break.py の読み（--test arm）を足したことだけ（門と止める線の台本は c80cw・c78gz の物をそのまま呼ぶ）。
#   使い方: bash reads929.sh <腕の出力 out_<TAG>（D:/ の形）> <成績表 b10_<TAG>.txt> <読みの出力フォルダ（D:/ の形）> [種の起点 1|11]
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
cd "$(dirname "$0")/../../.."; S=scripts/instruments
O=${1:?out}; B=${2:?成績表}; R=${3:?読み}; SB=${4:-1}; K=D:/PoB_work/keep_919
mkdir -p $R
if [ "$SB" = 1 ]; then
  python -X utf8 -I $S/c80cw/gates922.py $O/ins $K/base_ins $K/base_ins $K/n1 $K/n2 > $R/gates.txt 2>&1
  python -X utf8 -I $S/c80cw/tab922.py $K/base_ins arm=$O/ins n1=$K/n1 n2=$K/n2 > $R/tab.txt 2>&1
  python -X utf8 -I $S/tools/nullgate.py $B measurements/b10_20261107_m919_ins measurements/b10_20261107_m919_n1_ins measurements/b10_20261107_m919_n2_ins > $R/nullgate.txt 2>&1
  python -X utf8 -I $S/brk693/home_break.py arm=$O/ins base=$K/base_ins n1=$K/n1 n2=$K/n2 --test arm > $R/home_break.txt 2>&1
else
  SB=11 python -X utf8 -I $S/c80cw/gates922.py $O/ins $K/a11 $K/base_ins $K/n1 $K/n2 > $R/gates.txt 2>&1
  SB=11 python -X utf8 -I $S/c80cw/tab922.py $K/a11 arm=$O/ins > $R/tab.txt 2>&1
  python -X utf8 -I $S/brk693/home_break.py arm=$O/ins a11=$K/a11 --test arm > $R/home_break.txt 2>&1
fi
echo GATESDONE
bash $S/c78gz/stoplines906.sh $O/ins $R/stop906.txt $SB
python -X utf8 $S/hol/an_h2.py $O/ins -v4 > $R/hol_v4.txt 2>&1
echo HOLDONE
if [ "$SB" = 1 ]; then bash $S/ng.sh $O/ins c81cwB $R/ng rd:measurements/ng919_20261107:m919 > $R/ng.txt 2>&1; echo NGDONE; fi
echo READSDONE
