#!/usr/bin/env bash
# (1138) c111tp 腕 B の読み手（c109rc/read_rc.sh の型・基準は INSRAW keep_1136/base_ins とヌル腕 keep_1122/n1・n2・成績表 m1122・NG ng1122）
#   使い方: bash read_tp.sh <ins のフォルダ（D:/ の形）> <成績表のファイル（D:/ の形）> <タグ> <出力フォルダ（D:/ の形）> [旗 0 の tport_s のフォルダ]
#   止める線の 100 倍と 1e10 倍は、基準とヌル腕も同じ台本で数え直す（stop1002_base・_n1・_n2）。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
INS=${1:?ins}; CARD=${2:?成績表}; TAG=${3:?タグ}; OUT=${4:?出力}; F0=${5:-D:/PoB_work/c111tp_work/flag0_tport}
K=D:/PoB_work/keep_1122; BASE=D:/PoB_work/keep_1136/base_ins
mkdir -p "$OUT"
python -X utf8 -I "$D0/count_tport.py" "$INS" 1 10 > "$OUT/gates_tp.txt" 2>&1; echo "gates rc=$?"
python -X utf8 -I "$D0/count_tport.py" "$F0" 0 10 > "$OUT/gates_flag0.txt" 2>&1; echo "gates0 rc=$?"
python -X utf8 -I $S/m1017/popjump1017.py B=$INS base=$BASE N1=$K/n1 N2=$K/n2 > "$OUT/popjump.txt" 2>&1; echo "popjump rc=$?"
bash "$S/c92gf/stoplines1002.sh" "$INS" "$OUT/stop1002.txt" 1 > "$OUT/stop1002.log" 2>&1; echo "stop1002 rc=$?"
for k in base:$BASE n1:$K/n1 n2:$K/n2; do bash "$S/c92gf/stoplines1002.sh" "${k#*:}" "$OUT/stop1002_${k%%:*}.txt" 1 > "$OUT/stop1002_${k%%:*}.log" 2>&1; echo "stop1002 ${k%%:*} rc=$?"; done
python -X utf8 "$S/hol/an_h2.py" "$INS" -v4 > "$OUT/hol_v4.txt" 2>&1; echo "hol rc=$?"
python -X utf8 -I "$S/m808/gdp_zero10.py" arm=$INS base=$BASE > "$OUT/gdp_zero10.txt" 2>&1; echo "gdpz rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$INS" > "$OUT/plvl.txt" 2>&1; echo "plvl rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$INS" 1 > "$OUT/ukdef.txt" 2>&1; echo "ukdef rc=$?"
python -X utf8 "$S/tools/nullgate.py" "$CARD" "$R/measurements/b10_20261114_m1122_ins" "$R/measurements/b10_20261114_m1122_n1_ins" "$R/measurements/b10_20261114_m1122_n2_ins" > "$OUT/nullgate.txt" 2>&1; echo "nullgate rc=$?"
python -X utf8 -I "$S/brk693/home_break.py" arm=$INS base=$BASE n1=$K/n1 n2=$K/n2 --test arm > "$OUT/home_break.txt" 2>&1; echo "home rc=$?"
bash "$S/ng.sh" "$INS" "$TAG" "$OUT/ng" rd:$R/measurements/ng1122_20261114:m1122 > "$OUT/ng.txt" 2>&1; echo "ng rc=$?"
[ -f "$CARD.cost" ] && grep -h "推定" "$CARD.cost" > "$OUT/cost.txt" 2>&1
echo READDONE
