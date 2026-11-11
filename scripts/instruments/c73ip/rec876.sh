#!/usr/bin/env bash
# (876) c73ip 腕 B の記録の読み（読むだけ・採否に使わない）：門 A′（nullgate・m873 の M・N1・N2）・.cost・NG（ng.sh・基準 rd:measurements/ng873_20261106:m873）。
#   使い方: bash rec876.sh <腕の名前> <ins（D:/ の形）> <成績表の頭（D:/…）> <出力ファイル>
#   ng.sh の基準は相対パス＝worktree の根で回す（(869) の台本の不備）。ng.sh の読み手の出力フォルダは D:/… の形で渡す。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
cd D:/PoB_work/wt_c73ip; S=scripts/instruments
NAME=${1:?名前}; B=${2:?ins}; BT=${3:?成績表}; OUT=${4:?出力}
: > $OUT
sec(){ echo "## ===== $1" >> $OUT; }
sec "記録：門 A′（nullgate・m873 の M・N1・N2）"; python -X utf8 $S/tools/nullgate.py $BT measurements/b10_20261106_m873_ins measurements/b10_20261106_m873_n1_ins measurements/b10_20261106_m873_n2_ins >> $OUT 2>&1
sec "記録：.cost"; grep -h "推定" $BT.cost >> $OUT 2>&1
sec "記録：成績表の頭"; grep -h "exact\|near\|勝者\|未完走\|不履行" $BT | head -12 >> $OUT 2>&1
sec "記録：NG（基準 rd:measurements/ng873_20261106:m873）"; bash $S/ng.sh $B $NAME D:/PoB_work/c73ip_work/ng_$NAME rd:measurements/ng873_20261106:m873 >> $OUT 2>&1
echo RECDONE >> $OUT
echo RECDONE $NAME
