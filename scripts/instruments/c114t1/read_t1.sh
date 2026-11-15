#!/usr/bin/env bash
# (1159) c114t1 腕の読み手（c112t1/read_t1.sh の型・基準は INSRAW keep_1157/base_ins とヌル腕 keep_1157/n1・n2〔絞った表〕・成績表 m1157・NG ng1157）
#   使い方: bash read_t1.sh <ins のフォルダ（D:/ の形）> <成績表のファイル（D:/ の形）> <タグ> <出力フォルダ（D:/ の形）> <旗0 の ins> <W2(a)′ の床>
#   止める線の 100 倍と 1e10 倍は基準とヌル腕も同じ台本で数え直した物（measurements/m1157_20261115/stopbase/）と並べる。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
INS=${1:?ins}; CARD=${2:?成績表}; TAG=${3:?タグ}; OUT=${4:?出力}; F0=${5:?旗0}; FLOOR=${6:?床}
K=D:/PoB_work/keep_1157; BASE=$K/base_ins
mkdir -p "$OUT"
python -X utf8 -I "$D0/t1gates.py" 腕=$INS 旗0=$F0 --floor $FLOOR > "$OUT/gates_t1buy.txt" 2>&1; echo "t1gates rc=$?"
python -X utf8 -I "$S/c112t1/u1awb.py" 基線=$BASE N1=$K/n1 N2=$K/n2 腕=$INS > "$OUT/w1_record.txt" 2>&1; echo "w1 rc=$?"
python -X utf8 -I "$S/c112t1/newold.py" 旧=$BASE 新=$INS > "$OUT/newold.txt" 2>&1; echo "newold rc=$?"
python -X utf8 -I "$S/c112t1/tbsum.py" $INS > "$OUT/tbsum.txt" 2>&1; echo "tbsum rc=$?"
python -X utf8 -I "$D0/silver_emp.py" arm=$INS > "$OUT/silver_emp.txt" 2>&1; echo "silver rc=$?"
bash "$S/c92gf/stoplines1002.sh" "$INS" "$OUT/stop1002.txt" 1 > "$OUT/stop1002.log" 2>&1; echo "stop1002 rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$INS" -v4 > "$OUT/hol_v4.txt" 2>&1; echo "hol rc=$?"
python -X utf8 -I "$S/m808/gdp_zero10.py" arm=$INS base=$BASE > "$OUT/gdp_zero10.txt" 2>&1; echo "gdpz rc=$?"
python -X utf8 -I "$S/m609/plvl_all.py" "$INS" 10 "$TAG" > "$OUT/plvl_all.txt" 2>&1; echo "plvl rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$INS" 1 > "$OUT/ukdef.txt" 2>&1; echo "ukdef rc=$?"
python -X utf8 "$S/tools/nullgate.py" "$CARD" "$R/measurements/b10_20261115_m1157_ins" "$R/measurements/b10_20261115_m1157_n1_ins" "$R/measurements/b10_20261115_m1157_n2_ins" > "$OUT/nullgate.txt" 2>&1; echo "nullgate rc=$?"
python -X utf8 -I "$S/brk693/home_break.py" arm=$INS base=$BASE n1=$K/n1 n2=$K/n2 --test arm > "$OUT/home_break.txt" 2>&1; echo "home rc=$?"
bash "$S/ng.sh" "$INS" "$TAG" "$OUT/ng" rd:$R/measurements/ng1157_20261115:m1157 > "$OUT/ng.txt" 2>&1; echo "ng rc=$?"
[ -f "$CARD.cost" ] && grep -h "推定\|A-1\|着値の充填\|見た買い手行" "$CARD.cost" > "$OUT/cost.txt" 2>&1
echo READDONE
