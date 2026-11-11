#!/usr/bin/env bash
# (964) c86lt 腕の読み（read958.sh の写し・作業フォルダを c86lt_work・基準を m961〔keep_961〕・門は gate964.py）。(958) c85lt 腕の読み（(945) read945.sh の写し・作業フォルダを c85lt_work・基準を m947・門は gate958.py・止める線は stoplines949.sh〔pair・4 区分〕）（読むだけ）。
#   bash scripts/instruments/tools/bg.sh <印> bash scripts/instruments/c86lt/read964.sh <腕の名 c86ltB|c86ltS> <種の起点 1|11>
#   比べる基準：$KEEP（既定 keep_961）/base_ins・S11 は $KEEP/a11・ヌル腕は $KEEP/n1・n2・止める線のヌル腕は measurements/m961_20261108/null/stop906_n{1,2}.txt。
#   足した読み：G2 の新（paper951.py に region_s と landfix_s）・旧（why945.py）・限界 ⑮・名目 GDP 0 の続き（gdp_zero10.py）・議会の支持（sup945.py）。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
SP="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"; REPO="$(cd "$SP/../../.." && pwd)"; cd "$REPO" || exit 9
ARM=${1:?腕}; SB=${2:-1}; W=D:/PoB_work/c86lt_work/arm_$ARM; SUF=_10; [ "$SB" = 11 ] && SUF=_10_s11; I=$W/out_${ARM}${SUF}/ins; O=D:/PoB_work/c86lt_work/reads_$ARM; mkdir -p $O
K=${KEEP:-D:/PoB_work/keep_961}; if [ "$SB" = 11 ]; then BASE=$K/a11; else BASE=$K/base_ins; fi
[ -d "$I" ] || { echo "ins が無い：$I"; exit 2; }
python -X utf8 scripts/instruments/c86lt/gate964.py $I $SB > $O/gate.txt 2>&1; echo "gate rc=$?"
python -X utf8 scripts/instruments/c82lt/tab937.py $I $BASE $SB $SB > $O/tab.txt 2>&1; echo "tab rc=$?"
python -X utf8 scripts/instruments/c83lt/sup945.py $I $BASE > $O/sup.txt 2>&1; echo "sup rc=$?"
python -X utf8 scripts/instruments/c84lb/paper951.py $I $I > $O/g2_new.txt 2>&1; echo "g2new rc=$?"
python -X utf8 scripts/instruments/c83lt/why945.py $I > $O/g2_old_why.txt 2>&1; echo "why rc=$?"
RULE=pair NULLS="measurements/m961_20261108/null/stop906_n1.txt measurements/m961_20261108/null/stop906_n2.txt" bash scripts/instruments/c84gz/stoplines949.sh $I $O/stop949.txt $SB; echo "stop949 rc=$?"
python -X utf8 scripts/instruments/hol/an_h2.py $I -v4 > $O/hol.txt 2>&1; echo "hol rc=$?"
python -X utf8 -I scripts/instruments/m808/gdp_zero10.py arm=$I base=$BASE > $O/gdp_zero10.txt 2>&1; echo "gdpz rc=$?"
bash scripts/instruments/ng.sh $I $ARM D:/PoB_work/c86lt_work/ng_$ARM rd:measurements/ng961_20261108:m961 > $O/ng.txt 2>&1; echo "ng rc=$?"
python -X utf8 -I scripts/instruments/tools/keepsum.py $I $O/keepsum.tsv > $O/keepsum.log 2>&1; echo "keepsum rc=$?"
if [ "$SB" = 1 ]; then python -X utf8 scripts/instruments/tools/nullgate.py $W/b10_${ARM}_10.txt measurements/b10_20261108_m961_ins measurements/b10_20261108_m961_n1_ins measurements/b10_20261108_m961_n2_ins > $O/nullgate.txt 2>&1; echo "nullgate rc=$?"; fi
if [ "$SB" = 1 ]; then python -X utf8 -I scripts/instruments/brk693/home_break.py arm=$I base=$BASE n1=$K/n1 n2=$K/n2 --test arm > $O/home_break.txt 2>&1; echo "home rc=$?"; fi
echo READSDONE
