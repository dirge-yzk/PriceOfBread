#!/usr/bin/env bash
# (820) c65op 腕 B の後の読み（読むだけ）：門（gate_op.py）・止める線（100 倍・1e10 倍・名目 GDP 0 の続き・空洞化）・記録（開幕の 1000 倍・英の国債の不履行・home_break・門 A′・NG）・.cost。
#   使い方: bash post820.sh <腕の ins（D:/ の形）> <腕の名前> <出力フォルダ（measurements/…）> <成績表の頭（measurements/…）>（Bash の run_in_background で）
#   基準とヌル腕の止める線の数え直しは、同じ台本で (808) が数えた measurements/m808_20261103/stoplines_null.txt（keep_797/n1・n2・a11）と (818) の measurements/m818_*（keep_818/base_ins）を並べる（生が同じ）。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
cd D:/PoB_work/wt_c65op; S=scripts/instruments
B=${1:?腕の ins}; NAME=${2:?名前}; O=${3:?出力フォルダ}; BT=${4:?成績表}
BASE=D:/PoB_work/keep_818/base_ins; N1=D:/PoB_work/keep_797/n1; N2=D:/PoB_work/keep_797/n2
mkdir -p $O; OUT=$O/rec_$NAME.txt; : > $OUT
sec(){ echo "## ===== $1" >> $OUT; }
sec "門（gate_op）"; python -X utf8 -I $S/c65op/gate_op.py $B $NAME >> $OUT 2>&1
sec "止める線：価格水準の 1 四半期の比（pxjump）"; python -X utf8 -I $S/c11fl/pxjump.py ${NAME}=$B >> $OUT 2>&1
sec "止める線：名目 GDP の 1 四半期の比（gdp_jump797）"; python -X utf8 -I $S/m797/gdp_jump797.py ${NAME}=$B >> $OUT 2>&1
sec "記録の先頭：名目 GDP 0 の続き（gdp_zero10）"; python -X utf8 -I $S/m808/gdp_zero10.py ${NAME}=$B >> $OUT 2>&1
sec "記録：開幕の 1000 倍以上（plvl_seeds）"; python -X utf8 -I $S/brk693/plvl_seeds.py $B >> $OUT 2>&1
sec "記録：英の国債の不履行（ukdefault）"; python -X utf8 -I $S/c10cp/ukdefault.py $B 1 10 >> $OUT 2>&1
sec "止める線：空洞化（an_h2 -v4・規則 A2）"; python -X utf8 $S/hol/an_h2.py $B -v4 >> $OUT 2>&1
sec "記録：〈壊す〉①（home_break・本国の版）"; python -X utf8 -I $S/brk693/home_break.py ${NAME}=$B base=$BASE n1=$N1 n2=$N2 --test ${NAME} >> $OUT 2>&1
sec "記録：門 A′（nullgate）"; python -X utf8 $S/tools/nullgate.py $BT measurements/b10_20261103_m797_ins measurements/b10_20261103_m797_n1_ins measurements/b10_20261103_m797_n2_ins >> $OUT 2>&1
sec "記録：.cost"; cat $BT.cost >> $OUT 2>&1; grep -h "推定" measurements/b10_20261103_m797_ins.cost >> $OUT 2>&1
sec "記録：NG（ng.sh・基準 rd:measurements/ng797_20261103:m797）"; bash $S/ng.sh $B $NAME D:/PoB_work/c65op_work/ng_$NAME rd:measurements/ng797_20261103:m797 >> $OUT 2>&1
echo POSTDONE >> $OUT
