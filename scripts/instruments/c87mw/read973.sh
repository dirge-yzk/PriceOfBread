#!/usr/bin/env bash
# (973) c87mw 腕の読み（(964) read964.sh の型・基準を m968〔keep_968〕・門は gates973.py）。
#   bash scripts/instruments/tools/bg.sh <印> bash scripts/instruments/c87mw/read973.sh <腕の名 c87mwB|c87mwS> <種の起点 1|11> [出力の根]
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
SP="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"; REPO="$(cd "$SP/../../.." && pwd)"; cd "$REPO" || exit 9
ARM=${1:?腕}; SB=${2:-1}; R=${3:-D:/PoB_work/c87mw_work}; W=$R/arm_$ARM; SUF=_10; [ "$SB" = 11 ] && SUF=_10_s11; I=$W/out_${ARM}${SUF}/ins; O=$R/reads_$ARM; mkdir -p $O
K=${KEEP:-D:/PoB_work/keep_968}; if [ "$SB" = 11 ]; then BASE=$K/a11; else BASE=$K/base_ins; fi
[ -d "$I" ] || { echo "ins が無い：$I"; exit 2; }
SB=$SB python -X utf8 scripts/instruments/c87mw/gates973.py $I $BASE > $O/gate.txt 2>&1; echo "gate rc=$?"
RULE=pair NULLS="measurements/m968_20261108/null/stop906_n1.txt measurements/m968_20261108/null/stop906_n2.txt" bash scripts/instruments/c84gz/stoplines949.sh $I $O/stop949.txt $SB; echo "stop949 rc=$?"
python -X utf8 scripts/instruments/hol/an_h2.py $I -v4 > $O/hol.txt 2>&1; echo "hol rc=$?"
python -X utf8 -I scripts/instruments/m808/gdp_zero10.py arm=$I base=$BASE > $O/gdp_zero10.txt 2>&1; echo "gdpz rc=$?"
bash scripts/instruments/ng.sh $I $ARM $R/ng_$ARM rd:measurements/ng968_20261108:m968 > $O/ng.txt 2>&1; echo "ng rc=$?"
python -X utf8 -I scripts/instruments/tools/keepsum.py $I $O/keepsum.tsv > $O/keepsum.log 2>&1; echo "keepsum rc=$?"
if [ "$SB" = 1 ]; then python -X utf8 scripts/instruments/tools/nullgate.py $W/b10_${ARM}_10.txt measurements/b10_20261108_m968_ins measurements/b10_20261108_m968_n1_ins measurements/b10_20261108_m968_n2_ins > $O/nullgate.txt 2>&1; echo "nullgate rc=$?"; fi
if [ "$SB" = 1 ]; then python -X utf8 -I scripts/instruments/brk693/home_break.py arm=$I base=$BASE n1=$K/n1 n2=$K/n2 --test arm > $O/home_break.txt 2>&1; echo "home rc=$?"; fi
echo READSDONE
