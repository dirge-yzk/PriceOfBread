#!/usr/bin/env bash
# (952) c84kp 腕の読み（読むだけ・c83ic/reads943.sh の型）。門は c84kp/paper952.py（G1〜G3・G4 は記録）・基準は keep_947 と m947 の成績表。
#   使い方: bash reads952.sh <腕の出力 out_<TAG>（D:/ の形）> <成績表 b10_<TAG>.txt> <読みの出力フォルダ（D:/ の形）> [種の起点 1|11]
#   〈100 倍〉の〈それ以外〉＝c84gz の印 rule=pair（sha a3c17862 の judge949.py・gdp_fall949.py の写し D:/PoB_work/c84kp_work/gz949）。stoplines906.sh は書き換えずに呼ぶ。
#   走っている間にこの台本を書き換えない（README ★(778)）。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
cd "$(dirname "$0")/../../.."; S=scripts/instruments
O=${1:?out}; B=${2:?成績表}; R=${3:?読み}; SB=${4:-1}; K=D:/PoB_work/keep_947; GZ=D:/PoB_work/c84kp_work/gz949
mkdir -p $R
if [ "$SB" = 1 ]; then
  python -X utf8 -I $S/c84kp/paper952.py arm=$O/ins base=$K/base_ins n1=$K/n1 n2=$K/n2 > $R/gates.txt 2>&1
  python -X utf8 -I $S/tools/nullgate.py $B measurements/b10_20261108_m947_ins measurements/b10_20261108_m947_n1_ins measurements/b10_20261108_m947_n2_ins > $R/nullgate.txt 2>&1
  python -X utf8 -I $S/brk693/home_break.py arm=$O/ins base=$K/base_ins n1=$K/n1 n2=$K/n2 --test arm > $R/home_break.txt 2>&1
  NULLS="measurements/m947_20261108/null/stop906_n1.txt measurements/m947_20261108/null/stop906_n2.txt"
else
  python -X utf8 -I $S/c84kp/paper952.py arm=$O/ins a11=$K/a11 > $R/gates.txt 2>&1
  python -X utf8 -I $S/brk693/home_break.py arm=$O/ins a11=$K/a11 --test arm > $R/home_break.txt 2>&1
  NULLS="measurements/m947_20261108/null/stop906_n1.txt measurements/m947_20261108/null/stop906_n2.txt"
fi
echo GATESDONE
bash $S/c78gz/stoplines906.sh $O/ins $R/stop906.txt $SB > /dev/null
NA=""; for n in $NULLS; do NA="$NA --null $n"; done
{ echo "== 判定（949・c84gz a3c17862）：100 倍の〈それ以外〉（rule=pair）"
  python -X utf8 -I $GZ/judge949.py --rule pair --arm $R/stop906.txt $NA; echo "rc=$?"
  echo "== 記録（949）：名目 GDP の下がり（1/100 以下・止める線にしない）"
  python -X utf8 -I $GZ/gdp_fall949.py --rows M="$O/ins"; } > $R/judge949.txt 2>&1
python -X utf8 $S/hol/an_h2.py $O/ins -v4 > $R/hol_v4.txt 2>&1
echo HOLDONE
python -X utf8 -I $S/m808/gdp_zero10.py arm=$O/ins > $R/gdp_zero10.txt 2>&1
if [ "$SB" = 1 ]; then bash $S/ng.sh $O/ins c84kpB $R/ng rd:measurements/ng947_20261108:m947 > $R/ng.txt 2>&1; echo NGDONE; fi
echo READSDONE
