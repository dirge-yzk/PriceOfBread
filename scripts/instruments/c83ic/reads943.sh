#!/usr/bin/env bash
# (943) c83ic 腕の読み（読むだけ）＝(929) c81cw/reads929.sh の型。門は c83ic/paper943.py（M0＝生をそのまま読む）・基準は keep_933 と m933 の成績表。
#   使い方: bash reads943.sh <腕の出力 out_<TAG>（D:/ の形）> <成績表 b10_<TAG>.txt> <読みの出力フォルダ（D:/ の形）> [種の起点 1|11]
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
cd "$(dirname "$0")/../../.."; S=scripts/instruments
O=${1:?out}; B=${2:?成績表}; R=${3:?読み}; SB=${4:-1}; K=D:/PoB_work/keep_933
mkdir -p $R
python -X utf8 -I $S/c83ic/paper943.py $O/ins > $R/gates.txt 2>&1
if [ "$SB" = 1 ]; then
  python -X utf8 -I $S/tools/nullgate.py $B measurements/b10_20261107_m933_ins measurements/b10_20261107_m933_n1_ins measurements/b10_20261107_m933_n2_ins > $R/nullgate.txt 2>&1
  python -X utf8 -I $S/brk693/home_break.py arm=$O/ins base=$K/base_ins n1=$K/n1 n2=$K/n2 --test arm > $R/home_break.txt 2>&1
else
  python -X utf8 -I $S/brk693/home_break.py arm=$O/ins a11=$K/a11 --test arm > $R/home_break.txt 2>&1
fi
echo GATESDONE
bash $S/c78gz/stoplines906.sh $O/ins $R/stop906.txt $SB
python -X utf8 $S/hol/an_h2.py $O/ins -v4 > $R/hol_v4.txt 2>&1
echo HOLDONE
if [ "$SB" = 1 ]; then bash $S/ng.sh $O/ins c83icB $R/ng rd:measurements/ng933_20261107:m933 > $R/ng.txt 2>&1; echo NGDONE; fi
echo READSDONE
