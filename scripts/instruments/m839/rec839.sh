#!/usr/bin/env bash
# (839) 合流の腕の読み（読むだけ）：c65md の門（gate821.py・c10cp/gates.py）・c66dw の門（gate828・colcheck839〔C# の列〕・gate815 の G2〔U2 の保存〕）・止める線（stoplines839.sh）・記録（NG・門 A′・.cost）。
#   使い方: bash rec839.sh <腕の名前> <ins（D:/ の形）> <成績表の頭（measurements/… か D:/…）> <種の起点 1|11> <出力フォルダ>
#   U2 の G3・G4（旗 fl 0 の盤面 keep_818/base_ins との比）は比べる側の生が無い（作者のセッションが消した）＝読まない（予言 [4] で結果の前に名乗った）。
#   ヌル腕の止める線は同じ台本 stoplines839.sh で数える（呼び手が N1・N2・基準・旧のヌル腕にも当てる）。走っている間にこの台本を書き換えない。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
cd D:/PoB_work/wt_m839; S=scripts/instruments
NAME=${1:?名前}; B=${2:?ins}; BT=${3:?成績表}; SB=${4:?種の起点}; O=${5:?出力}
SE=$((SB+9)); mkdir -p $O; OUT=$O/rec_$NAME.txt; : > $OUT
sec(){ echo "## ===== $1" >> $OUT; }
sec "c65md の門 G0・G1・G2（gate821・腕の ins の全ての種）"; python -X utf8 -I $S/c65md/gate821.py $B >> $OUT 2>&1
sec "c65md の門 G3（c10cp の gates.py・種 $SB-$SE）"; python -X utf8 -I $S/c10cp/gates.py $B --seeds $SB..$SE >> $OUT 2>&1
sec "c66dw の門 L1・C・W1・W2・U1（gate828・種 $SB-$SE）"; python -X utf8 -I $S/c66dw/gate828.py $B $SB-$SE >> $OUT 2>&1
sec "c66dw の門 L1・C の定義の列（colcheck839・C# のビットの比較）"; python -X utf8 -I $S/m839/colcheck839.py $B $SB >> $OUT 2>&1
sec "c66dw の門 U2 の G1・G2（gate815 --short・比べる側なし）"; python -X utf8 -I $S/c14fl/gate815.py $B NONE NONE $SB-$SE --short >> $OUT 2>&1
sec "止める線（stoplines839）"; bash $S/m839/stoplines839.sh $B $O/stop_$NAME.txt $SB > /dev/null 2>&1; cat $O/stop_$NAME.txt >> $OUT
sec "記録：門 A′（nullgate・旧のヌル腕 m797）"; python -X utf8 $S/tools/nullgate.py $BT measurements/b10_20261103_m797_ins measurements/b10_20261103_m797_n1_ins measurements/b10_20261103_m797_n2_ins >> $OUT 2>&1
sec "記録：門 A′（nullgate・新のヌル腕 m826）"; python -X utf8 $S/tools/nullgate.py $BT measurements/b10_20261104_m826_ins measurements/b10_20261104_m826_n1_ins measurements/b10_20261104_m826_n2_ins >> $OUT 2>&1
sec "記録：.cost"; grep -h "推定" $BT.cost >> $OUT 2>&1
sec "記録：NG（基準 rd:measurements/ng826_20261104:m826）"; bash $S/ng.sh $B $NAME D:/PoB_work/m839/ng_$NAME rd:measurements/ng826_20261104:m826 >> $OUT 2>&1
echo RECDONE >> $OUT
echo RECDONE $NAME
