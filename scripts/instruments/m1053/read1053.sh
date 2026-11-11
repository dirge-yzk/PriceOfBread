#!/usr/bin/env bash
# (1053) 合流の読み手（m1044/read1044.sh と c99db/read1048.sh の写し・種の起点と基準の生を引数にした・門は c99db/gates_db.py を変えずに・記録は c99db/record_db.py）
#   使い方: bash read1053.sh <腕の作業フォルダ（D:/ の形）> <タグ> <出力フォルダ（D:/ の形）> <種の起点> <基準の生（D:/ の形）>
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
W=${1:?作業}; TAG=${2:?タグ}; OUT=${3:?出力}; SB=${4:?種の起点}; BASE=${5:?基準}
INS=$W/out_$TAG/ins; CARD=$W/b10_$TAG.txt; K=D:/PoB_work/keep_1044
mkdir -p "$OUT"
python -X utf8 -I "$S/c99db/gates_db.py" "$INS" > "$OUT/gates_db.txt" 2>&1; echo "gates rc=$?"
python -X utf8 -I "$S/c99db/record_db.py" "$BASE" "$INS" "$K/n1" "$K/n2" > "$OUT/record_db.txt" 2>&1; echo "record rc=$?"
python -X utf8 -I $S/m1017/popjump1017.py B=$INS base=$BASE N1=$K/n1 N2=$K/n2 > "$OUT/popjump.txt" 2>&1; echo "popjump rc=$?"
bash "$S/c92gf/stoplines1002.sh" "$INS" "$OUT/stop1002.txt" "$SB" > "$OUT/stop1002.log" 2>&1; echo "stop1002 rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$INS" -v4 > "$OUT/hol_v4.txt" 2>&1; echo "hol rc=$?"
python -X utf8 -I "$S/m808/gdp_zero10.py" arm=$INS base=$BASE > "$OUT/gdp_zero10.txt" 2>&1; echo "gdpz rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$INS" > "$OUT/plvl.txt" 2>&1; echo "plvl rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$INS" "$SB" > "$OUT/ukdef.txt" 2>&1; echo "ukdef rc=$?"
python -X utf8 "$S/tools/nullgate.py" "$CARD" "$R/measurements/b10_20261111_m1044_ins" "$R/measurements/b10_20261111_m1044_n1_ins" "$R/measurements/b10_20261111_m1044_n2_ins" > "$OUT/nullgate.txt" 2>&1; echo "nullgate rc=$?"
python -X utf8 -I "$S/brk693/home_break.py" arm=$INS base=$BASE n1=$K/n1 n2=$K/n2 --test arm > "$OUT/home_break.txt" 2>&1; echo "home rc=$?"
bash "$S/ng.sh" "$INS" "$TAG" "$OUT/ng" rd:$R/measurements/ng1044_20261111:m1044 > "$OUT/ng.txt" 2>&1; echo "ng rc=$?"
grep -h "推定" "$CARD.cost" > "$OUT/cost.txt" 2>&1
echo READDONE
