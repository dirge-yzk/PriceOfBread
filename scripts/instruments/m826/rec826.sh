#!/usr/bin/env bash
# (826) 合流の腕の読み（読むだけ）：線の門（c65fl＝c14fl/gate815.py・c65op＝c65op/gate_op.py）・止める線（stoplines826.sh）・記録（NG・門 A′・〈壊す〉①・国の依存の在庫）。
#   使い方: bash rec826.sh <腕の名前> <ins（D:/ の形）> <成績表の頭（measurements/… か D:/…）> <種の起点 1|11> <fl の門の比べる側の ins（旗 0）> <出力フォルダ>
#   ヌル腕の止める線は同じ台本で数える（呼び手が N1・N2 にも当てる）。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
cd D:/PoB_work/wt_m826; S=scripts/instruments
NAME=${1:?名前}; B=${2:?ins}; BT=${3:?成績表}; SB=${4:?種の起点}; F0=${5:?旗0のins}; O=${6:?出力}
SE=$((SB+9)); mkdir -p $O; OUT=$O/rec_$NAME.txt; : > $OUT
sec(){ echo "## ===== $1" >> $OUT; }
sec "c65fl の門 G1〜G4（gate815・比べる側 $F0・種 $SB-$SE）"; python -X utf8 -I $S/c14fl/gate815.py $B $F0 $F0 $SB-$SE >> $OUT 2>&1
sec "c65op の門 L1・L2・U1〜U3（gate_op）"; python -X utf8 -I $S/c65op/gate_op.py $B $NAME >> $OUT 2>&1
sec "記録：国の依存の在庫（natadd）"; python -X utf8 -I $S/c65op/natadd.py $NAME=$B base=$F0 >> $OUT 2>&1
sec "止める線（stoplines826）"; bash $S/m826/stoplines826.sh $B $O/stop_$NAME.txt $SB > /dev/null 2>&1; cat $O/stop_$NAME.txt >> $OUT
sec "記録：〈壊す〉①（home_break・本国の版）"; python -X utf8 -I $S/brk693/home_break.py $NAME=$B base=$F0 n1=D:/PoB_work/keep_797/n1 n2=D:/PoB_work/keep_797/n2 --test $NAME --opening-only >> $OUT 2>&1
sec "記録：門 A′（nullgate）"; python -X utf8 $S/tools/nullgate.py $BT measurements/b10_20261103_m797_ins measurements/b10_20261103_m797_n1_ins measurements/b10_20261103_m797_n2_ins >> $OUT 2>&1
sec "記録：.cost"; grep -h "推定" $BT.cost >> $OUT 2>&1
sec "記録：NG（基準 rd:measurements/ng797_20261103:m797）"; bash $S/ng.sh $B $NAME D:/PoB_work/m826/ng_$NAME rd:measurements/ng797_20261103:m797 >> $OUT 2>&1
echo RECDONE >> $OUT
echo RECDONE $NAME
