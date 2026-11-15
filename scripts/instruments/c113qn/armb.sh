#!/usr/bin/env bash
# (1153) c113qn 腕 B の読み（門・止める線・空洞化・名目 GDP 0・NG・nullgate・周辺の点検）。読むだけ。
#   bash scripts/instruments/c113qn/armb.sh <腕 B の出力フォルダ（out_<腕>_10・D:/ の形）> <成績表 measurements/b10_…_ins> <旗 0 の colqn_s の写し> <結果のフォルダ>
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
cd "$(dirname "$0")/../../.."; S=scripts/instruments
O=${1:?出力}; B=${2:?成績表}; F0=${3:?旗0}; R=${4:?結果}; mkdir -p "$R"
python -X utf8 -I $S/c113qn/gates.py . "$O/ins" --flag0 "$F0" > "$R/gates_armB.txt" 2>&1
bash $S/c92gf/stoplines1002.sh "$O/ins" "$R/armB_stop_B.txt" 1 > /dev/null 2>&1
python -X utf8 -I $S/hol/an_h2.py "$O/ins" -v4 > "$R/armB_hol.txt" 2>&1
python -X utf8 -I $S/m808/gdp_zero10.py B="$O/ins" > "$R/armB_gdpzero.txt" 2>&1
bash $S/ng.sh "$O/ins" c113qnB D:/PoB_work/c113qn_work/ng rd:measurements/ng1150_20261115:m1150 > "$R/armB_ng.txt" 2>&1
python -X utf8 -I $S/tools/nullgate.py "$B" measurements/b10_20261115_m1150_ins measurements/b10_20261115_m1150_n1_ins measurements/b10_20261115_m1150_n2_ins > "$R/armB_nullgate.txt" 2>&1
python -X utf8 -I $S/c113qn/periph.py . B="$O/ins" > "$R/armB_periph.txt" 2>&1
echo ARMBDONE
