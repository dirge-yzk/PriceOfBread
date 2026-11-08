#!/usr/bin/env bash
# (972) c87mt 腕の読み（(964) read964.sh の写し・基準を m968〔keep_968〕・門の台本の代わりに成績表の突き合わせ・生の突き合わせ・pinit の数え）。
#   bash scripts/instruments/tools/bg.sh <印> bash scripts/instruments/c87mt/read972.sh <腕の名 c87mtB|c87mtS> <種の起点 1|11>
#   比べる基準：keep_968/base_ins（S11 は keep_968/a11）・成績表 b10_20261108_m968_ins（S11 は b10s11_20261108_m968_ins）・止める線のヌル腕 measurements/m968_20261108/null/stop906_n{1,2}.txt。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
SP="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"; REPO="$(cd "$SP/../../.." && pwd)"; cd "$REPO" || exit 9
ARM=${1:?腕}; SB=${2:-1}; W=D:/PoB_work/c87mt_work/arm_$ARM; SUF=_10; [ "$SB" = 11 ] && SUF=_10_s11; I=$W/out_${ARM}${SUF}/ins; O=D:/PoB_work/c87mt_work/reads_$ARM; mkdir -p $O
K=D:/PoB_work/keep_968; if [ "$SB" = 11 ]; then BASE=$K/a11; BAND=measurements/b10s11_20261108_m968_ins; else BASE=$K/base_ins; BAND=measurements/b10_20261108_m968_ins; fi
[ -d "$I" ] || { echo "ins が無い：$I"; exit 2; }
python -X utf8 -I $SP/bandcmp972.py $W/b10_${ARM}${SUF}.txt $BAND > $O/band.txt 2>&1; echo "band rc=$?"
python -X utf8 -I $SP/pinit972.py $I $SB > $O/pinit.txt 2>&1; echo "pinit rc=$?"
python -X utf8 -I scripts/instruments/m764/rawcmp.py $I $BASE pinit_s > $O/rawcmp.txt 2>&1; echo "rawcmp rc=$?"
RULE=pair NULLS="measurements/m968_20261108/null/stop906_n1.txt measurements/m968_20261108/null/stop906_n2.txt" bash scripts/instruments/c84gz/stoplines949.sh $I $O/stop949.txt $SB; echo "stop949 rc=$?"
python -X utf8 scripts/instruments/hol/an_h2.py $I -v4 > $O/hol.txt 2>&1; echo "hol rc=$?"
python -X utf8 -I scripts/instruments/m808/gdp_zero10.py arm=$I base=$BASE > $O/gdp_zero10.txt 2>&1; echo "gdpz rc=$?"
bash scripts/instruments/ng.sh $I $ARM D:/PoB_work/c87mt_work/ng_$ARM rd:measurements/ng968_20261108:m968 > $O/ng.txt 2>&1; echo "ng rc=$?"
python -X utf8 -I scripts/instruments/tools/keepsum.py $I $O/keepsum.tsv > $O/keepsum.log 2>&1; echo "keepsum rc=$?"
if [ "$SB" = 1 ]; then python -X utf8 scripts/instruments/tools/nullgate.py $W/b10_${ARM}${SUF}.txt measurements/b10_20261108_m968_ins measurements/b10_20261108_m968_n1_ins measurements/b10_20261108_m968_n2_ins > $O/nullgate.txt 2>&1; echo "nullgate rc=$?"; fi
if [ "$SB" = 1 ]; then python -X utf8 -I scripts/instruments/brk693/home_break.py arm=$I base=$BASE n1=$K/n1 n2=$K/n2 --test arm > $O/home_break.txt 2>&1; echo "home rc=$?"; fi
echo READSDONE
