#!/usr/bin/env bash
# (832) 合流の腕の読み（読むだけ）：c66dw の門（gate828・colcheck828・gate815〔U2〕）・止める線（stoplines832.sh）・記録（NG・門 A′・.cost・未払いの割合）。
#   使い方: bash rec832.sh <腕の名前> <ins（D:/ の形）> <成績表の頭（measurements/… か D:/…）> <種の起点 1|11> <U2 の旗 fl 0 の ins> <出力フォルダ> [<L1 の旗 0 の ins>]
#   ヌル腕の止める線は同じ台本 stoplines832.sh で数える（呼び手が N1・N2・基準・旧のヌル腕にも当てる）。走っている間にこの台本を書き換えない。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
cd D:/PoB_work/wt_m832; S=scripts/instruments
NAME=${1:?名前}; B=${2:?ins}; BT=${3:?成績表}; SB=${4:?種の起点}; F0=${5:?旗fl0のins}; O=${6:?出力}; Z=${7:-}
SE=$((SB+9)); mkdir -p $O; OUT=$O/rec_$NAME.txt; : > $OUT
sec(){ echo "## ===== $1" >> $OUT; }
sec "c66dw の門 L1・C・W1・W2・U1（gate828・腕＝旗 1・種 $SB-$SE）"; python -X utf8 -I $S/c66dw/gate828.py $B $SB-$SE >> $OUT 2>&1
sec "c66dw の門 L1・C の定義の列（colcheck828・C# のビットの比較）"; python -X utf8 -I $S/c66dw/colcheck828.py $B >> $OUT 2>&1
if [ -n "$Z" ]; then sec "c66dw の門 L1 の旗 0 の側（gate828・$Z）"; python -X utf8 -I $S/c66dw/gate828.py $Z $SB-$SE >> $OUT 2>&1; fi
sec "c66dw の門 U2：(815) の G1〜G5（gate815・比べる側 $F0）"; python -X utf8 -I $S/c14fl/gate815.py $B $F0 $F0 $SB-$SE >> $OUT 2>&1
sec "止める線（stoplines832）"; bash $S/m832/stoplines832.sh $B $O/stop_$NAME.txt $SB > /dev/null 2>&1; cat $O/stop_$NAME.txt >> $OUT
sec "記録：門 A′（nullgate・旧のヌル腕 m797）"; python -X utf8 $S/tools/nullgate.py $BT measurements/b10_20261103_m797_ins measurements/b10_20261103_m797_n1_ins measurements/b10_20261103_m797_n2_ins >> $OUT 2>&1
sec "記録：門 A′（nullgate・新のヌル腕 m826）"; python -X utf8 $S/tools/nullgate.py $BT measurements/b10_20261104_m826_ins measurements/b10_20261104_m826_n1_ins measurements/b10_20261104_m826_n2_ins >> $OUT 2>&1
sec "記録：.cost"; grep -h "推定" $BT.cost >> $OUT 2>&1
if [ "$SB" = 1 ]; then sec "記録：未払いの割合・外資の負債÷年 GDP（records810・種 1〜10）"; python -X utf8 -I $S/c13fl/records810.py ${NAME}=$B base=D:/PoB_work/keep_826/base_ins >> $OUT 2>&1; fi
sec "記録：NG（基準 rd:measurements/ng826_20261104:m826）"; bash $S/ng.sh $B $NAME D:/PoB_work/m832/ng_$NAME rd:measurements/ng826_20261104:m826 >> $OUT 2>&1
echo RECDONE >> $OUT
echo RECDONE $NAME
