#!/usr/bin/env bash
# (815) c14fl の門と止める線と記録の駆動（読むだけ）。腕（旗 1・計器つき）・旗 0 の計器つき（ident c14flz の腕 b＝cpworld_s のある物）・基線 m797（keep_813/base_ins）・ヌル腕 N1・N2（keep_797/n1・n2）の生を読む。
#   使い方: bash rec815.sh <腕の ins> <出力ファイル> <腕の名前> <種 1-10> <旗 0 の ins（cpworld_s のある物）>
#   python が読む道は D:/ の形。走っている間にこの台本を書き換えない（README ★(778)）。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
cd D:/PoB_work/wt_c14fl; S=scripts/instruments
B=${1:?腕の ins}; OUT=${2:?出力}; NAME=${3:?名前}; SD=${4:?種}; Z=${5:?旗 0 の ins}
BASE=D:/PoB_work/keep_813/base_ins; N1=D:/PoB_work/keep_797/n1; N2=D:/PoB_work/keep_797/n2
: > $OUT
sec(){ echo "## ===== $1" >> $OUT; }
sec "gate815（G1〜G4）"; python -X utf8 -I $S/c14fl/gate815.py $B $Z $BASE $SD >> $OUT 2>&1
sec "gate815 旗 0 の盤面（同じ読み手＝落ちる門の確かめ）"; python -X utf8 -I $S/c14fl/gate815.py $Z $Z $BASE $SD >> $OUT 2>&1
sec "G5 名目 GDP 0 の続き（gdp_zero10）"; python -X utf8 -I $S/m808/gdp_zero10.py ${NAME}=$B base=$BASE n1=$N1 n2=$N2 >> $OUT 2>&1
sec "止める線：価格水準の 1 四半期の比（pxjump・同じ台本で数え直し）"; python -X utf8 -I $S/c11fl/pxjump.py ${NAME}=$B base=$BASE n1=$N1 n2=$N2 >> $OUT 2>&1
sec "止める線：名目 GDP の 1 四半期の比（gdp_jump797）"; python -X utf8 -I $S/m797/gdp_jump797.py ${NAME}=$B base=$BASE n1=$N1 n2=$N2 >> $OUT 2>&1
sec "記録：開幕の 1000 倍以上（plvl_seeds）"; for t in "${NAME}:$B" "base:$BASE" "n1:$N1" "n2:$N2"; do n=${t%%:*}; d=${t#*:}; echo "-- $n" >> $OUT; python -X utf8 -I $S/brk693/plvl_seeds.py $d >> $OUT 2>&1; done
sec "記録：英の国債の不履行（ukdefault）"; for t in "${NAME}:$B" "base:$BASE" "n1:$N1" "n2:$N2"; do n=${t%%:*}; d=${t#*:}; echo "-- $n" >> $OUT; python -X utf8 -I $S/c10cp/ukdefault.py $d 1 10 >> $OUT 2>&1; done
sec "記録：払いの和の新÷旧・未払いの割合・外資の負債÷年 GDP・蘭と西の価格水準（records810）"; python -X utf8 -I $S/c13fl/records810.py ${NAME}=$B base=$BASE >> $OUT 2>&1
sec "止める線：空洞化（an_h2 -v4・規則 A2）"; python -X utf8 $S/hol/an_h2.py $B -v4 >> $OUT 2>&1
echo RECDONE >> $OUT
