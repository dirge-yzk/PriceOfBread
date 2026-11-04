#!/usr/bin/env bash
# (828) c66dw の門と止める線と記録の駆動（読むだけ・エンジンを呼ばない）。c14fl/rec815.sh の型。
#   使い方: bash rec828.sh <腕の ins> <出力ファイル> <腕の名前> <旗 0 の ins（ident c66dwz の腕 b＝cpwdft_s のある物）>
#   基準 keep_826/base_ins・新しいヌル腕 keep_826/n1・n2・旧のヌル腕 keep_797/n1・n2 を同じ台本で数え直して並べる（⑰）。python が読む道は D:/ の形。
#   走っている間にこの台本を書き換えない（README ★(778)）。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
cd D:/PoB_work/wt_c66dw; S=scripts/instruments
B=${1:?腕の ins}; OUT=${2:?出力}; NAME=${3:?名前}; Z=${4:?旗 0 の ins}
BASE=D:/PoB_work/keep_826/base_ins; FL0=D:/PoB_work/keep_818/base_ins
N1=D:/PoB_work/keep_826/n1; N2=D:/PoB_work/keep_826/n2; O1=D:/PoB_work/keep_797/n1; O2=D:/PoB_work/keep_797/n2
: > $OUT
sec(){ echo "## ===== $1" >> $OUT; }
sec "門 L1・C・W1・W2・U1（gate828・腕＝旗 1）"; python -X utf8 -I $S/c66dw/gate828.py $B 1-10 >> $OUT 2>&1
sec "門 L1 の旗 0 の側（gate828・ident c66dwz の腕 b＝旗 0）"; python -X utf8 -I $S/c66dw/gate828.py $Z 1-10 >> $OUT 2>&1
sec "門 U2：(815) の G1〜G4（gate815・腕と旗 fl 0 の盤面 keep_818/base_ins）"; python -X utf8 -I $S/c14fl/gate815.py $B $FL0 $FL0 1-10 >> $OUT 2>&1
sec "比べる側：基線 m826 の G1〜G4（gate815・keep_826/base_ins と keep_818/base_ins）"; python -X utf8 -I $S/c14fl/gate815.py $BASE $FL0 $FL0 1-10 >> $OUT 2>&1
sec "門 U2 の G5＝名目 GDP 0 の続き（gdp_zero10・腕・基準・新旧のヌル腕）"; python -X utf8 -I $S/m808/gdp_zero10.py ${NAME}=$B base=$BASE n1new=$N1 n2new=$N2 n1old=$O1 n2old=$O2 >> $OUT 2>&1
sec "止める線：価格水準の 1 四半期の比（pxjump・同じ台本で数え直し）"; python -X utf8 -I $S/c11fl/pxjump.py ${NAME}=$B base=$BASE n1new=$N1 n2new=$N2 n1old=$O1 n2old=$O2 >> $OUT 2>&1
sec "止める線：名目 GDP の 1 四半期の比（gdp_jump797・同じ台本で数え直し）"; python -X utf8 -I $S/m797/gdp_jump797.py ${NAME}=$B base=$BASE n1new=$N1 n2new=$N2 n1old=$O1 n2old=$O2 >> $OUT 2>&1
sec "記録：開幕の 1000 倍以上（plvl_seeds）"; for t in "${NAME}:$B" "base:$BASE"; do n=${t%%:*}; d=${t#*:}; echo "-- $n" >> $OUT; python -X utf8 -I $S/brk693/plvl_seeds.py $d >> $OUT 2>&1; done
sec "記録：英の国債の不履行（ukdefault）"; for t in "${NAME}:$B" "base:$BASE"; do n=${t%%:*}; d=${t#*:}; echo "-- $n" >> $OUT; python -X utf8 -I $S/c10cp/ukdefault.py $d 1 10 >> $OUT 2>&1; done
sec "記録：払いの和・未払いの割合・外資の負債÷年 GDP（records810）"; python -X utf8 -I $S/c13fl/records810.py ${NAME}=$B base=$BASE >> $OUT 2>&1
sec "止める線：空洞化（an_h2 -v4・規則 A2）腕"; python -X utf8 $S/hol/an_h2.py $B -v4 >> $OUT 2>&1
sec "比べる側：空洞化（an_h2 -v4）基準 keep_826/base_ins"; python -X utf8 $S/hol/an_h2.py $BASE -v4 >> $OUT 2>&1
echo RECDONE >> $OUT
