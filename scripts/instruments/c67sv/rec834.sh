#!/usr/bin/env bash
# (834) 腕 B の読み（読むだけ）：門（paper827.py --mode arm）・止める線（c66sv/stoplines827.sh＝100 倍は旧の台本 gdp_jump797.py）・記録（門 A′・.cost・NG）。
#   使い方: bash rec834.sh <腕の名前> <ins（D:/ の形）> <成績表（D:/… か measurements/…）> <出力フォルダ>
#   走っている間にこの台本を書き換えない。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
cd D:/PoB_work/wt_c67sv || exit 1
S=scripts/instruments
NAME=${1:?名前}; B=${2:?ins}; BT=${3:?成績表}; O=${4:?出力}
mkdir -p $O; OUT=$O/rec_$NAME.txt; : > $OUT
sec(){ echo "## ===== $1" >> $OUT; }
sec "門 W1・W1m・W2b・E1・U1・S1・T1・T2（paper827 --mode arm）"; python -X utf8 -I $S/c66sv/paper827.py $B --mode arm >> $OUT 2>&1
sec "止める線（stoplines827）"; bash $S/c66sv/stoplines827.sh $B $O/stop_$NAME.txt 1 > /dev/null 2>&1; cat $O/stop_$NAME.txt >> $OUT
sec "記録：門 A′（nullgate・旧のヌル腕 m797）"; python -X utf8 $S/tools/nullgate.py $BT measurements/b10_20261103_m797_ins measurements/b10_20261103_m797_n1_ins measurements/b10_20261103_m797_n2_ins >> $OUT 2>&1
sec "記録：門 A′（nullgate・新のヌル腕 m826）"; python -X utf8 $S/tools/nullgate.py $BT measurements/b10_20261104_m826_ins measurements/b10_20261104_m826_n1_ins measurements/b10_20261104_m826_n2_ins >> $OUT 2>&1
sec "記録：.cost"; grep -h "推定" $BT.cost >> $OUT 2>&1
sec "記録：NG（基準 rd:measurements/ng826_20261104:m826）"; bash $S/ng.sh $B $NAME D:/PoB_work/c67sv_work/ng_$NAME rd:measurements/ng826_20261104:m826 >> $OUT 2>&1
echo RECDONE >> $OUT
echo RECDONE $NAME
