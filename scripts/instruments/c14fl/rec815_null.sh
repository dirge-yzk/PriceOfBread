#!/usr/bin/env bash
# (815) c14fl 止める線と記録の読み手を基準 m797（keep_813/base_ins）とヌル腕 N1・N2（keep_797/n1・n2）で数え直す（読むだけ・⑰＝腕と同じ台本で）。
#   使い方: bash rec815_null.sh <出力ファイル>（Bash の run_in_background で）
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
cd D:/PoB_work/wt_c14fl; S=scripts/instruments
OUT=${1:?出力}
BASE=D:/PoB_work/keep_813/base_ins; N1=D:/PoB_work/keep_797/n1; N2=D:/PoB_work/keep_797/n2
: > $OUT
sec(){ echo "## ===== $1" >> $OUT; }
sec "名目 GDP 0 の続き（gdp_zero10）"; python -X utf8 -I $S/m808/gdp_zero10.py base=$BASE n1=$N1 n2=$N2 >> $OUT 2>&1
sec "価格水準の 1 四半期の比（pxjump）"; python -X utf8 -I $S/c11fl/pxjump.py base=$BASE n1=$N1 n2=$N2 >> $OUT 2>&1
sec "名目 GDP の 1 四半期の比（gdp_jump797）"; python -X utf8 -I $S/m797/gdp_jump797.py base=$BASE n1=$N1 n2=$N2 >> $OUT 2>&1
sec "開幕の 1000 倍以上（plvl_seeds）"; for t in "base:$BASE" "n1:$N1" "n2:$N2"; do n=${t%%:*}; d=${t#*:}; echo "-- $n" >> $OUT; python -X utf8 -I $S/brk693/plvl_seeds.py $d >> $OUT 2>&1; done
sec "英の国債の不履行（ukdefault）"; for t in "base:$BASE" "n1:$N1" "n2:$N2"; do n=${t%%:*}; d=${t#*:}; echo "-- $n" >> $OUT; python -X utf8 -I $S/c10cp/ukdefault.py $d 1 10 >> $OUT 2>&1; done
echo RECNULLDONE >> $OUT
