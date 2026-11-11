#!/usr/bin/env bash
# (957) c85kp 腕の読み（読むだけ・c84kp/reads952.sh の型）。門は c84kp/paper952.py（G1〜G3・G4 は記録）・基準は keep_947 と m947 の成績表。
#   使い方: bash reads957.sh <腕の出力 out_<TAG>（D:/ の形）> <成績表 b10_<TAG>.txt> <読みの出力フォルダ（D:/ の形）> [種の起点 1|11]
#   〈100 倍〉の〈それ以外〉＝木の scripts/instruments/c84gz/stoplines949.sh（a3c17862 と差 0・RULE=pair・4 区分・NULLS は m947 の n1・n2 の stop906 の出力）。
#   stoplines906.sh は stoplines949.sh の中で書き換えずに呼ばれる。走っている間にこの台本を書き換えない（README ★(778)）。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
cd "$(dirname "$0")/../../.."; S=scripts/instruments
O=${1:?out}; B=${2:?成績表}; R=${3:?読み}; SB=${4:-1}; K=D:/PoB_work/keep_947
mkdir -p $R
NULLS="measurements/m947_20261108/null/stop906_n1.txt measurements/m947_20261108/null/stop906_n2.txt"
if [ "$SB" = 1 ]; then
  python -X utf8 -I $S/c84kp/paper952.py arm=$O/ins base=$K/base_ins n1=$K/n1 n2=$K/n2 > $R/gates.txt 2>&1
  python -X utf8 -I $S/tools/nullgate.py $B measurements/b10_20261108_m947_ins measurements/b10_20261108_m947_n1_ins measurements/b10_20261108_m947_n2_ins > $R/nullgate.txt 2>&1
  python -X utf8 -I $S/brk693/home_break.py arm=$O/ins base=$K/base_ins n1=$K/n1 n2=$K/n2 --test arm > $R/home_break.txt 2>&1
else
  python -X utf8 -I $S/c84kp/paper952.py arm=$O/ins a11=$K/a11 > $R/gates.txt 2>&1
  python -X utf8 -I $S/brk693/home_break.py arm=$O/ins a11=$K/a11 --test arm > $R/home_break.txt 2>&1
fi
echo GATESDONE
RULE=pair NULLS="$NULLS" bash $S/c84gz/stoplines949.sh $O/ins $R/stop949.txt $SB > $R/stop949.log 2>&1; echo "stop949 rc=$?" >> $R/stop949.log
python -X utf8 $S/hol/an_h2.py $O/ins -v4 > $R/hol_v4.txt 2>&1
echo HOLDONE
python -X utf8 -I $S/m808/gdp_zero10.py arm=$O/ins > $R/gdp_zero10.txt 2>&1
if [ "$SB" = 1 ]; then bash $S/ng.sh $O/ins c85kpB $R/ng rd:measurements/ng947_20261108:m947 > $R/ng.txt 2>&1; echo NGDONE; fi
echo READSDONE
