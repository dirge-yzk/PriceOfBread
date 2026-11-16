#!/usr/bin/env bash
# (1172) c116t1 腕 B の読み手（c115sv/read_sv.sh の型・基準は m1171〔keep_1171/base_ins・n1・n2・ng1171・b10_20261116_m1171_ins〕・門は c116t1/t1gates3.py）
#   使い方: bash read_t1.sh <ins（D:/ の形）> <成績表（D:/ の形・無ければ -）> <タグ> <出力フォルダ（D:/ の形）> <種の起点 1|11> <floorSum> <floorN>
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
INS=${1:?ins}; CARD=${2:?成績表}; TAG=${3:?タグ}; OUT=${4:?出力}; SB=${5:?種の起点}; FS=${6:?floorSum}; FN=${7:?floorN}
SE=$((SB+9)); K=D:/PoB_work/keep_1171; BASE=$K/base_ins
mkdir -p "$OUT"
cd "$R"
python -X utf8 -I "$S/c116t1/t1gates3.py" "$INS" "$SB" "$SE" "$FS" "$FN" > "$OUT/gates_t1.txt" 2>&1; echo "gates rc=$?"
python -X utf8 -I "$S/c116t1/impratio.py" "$INS" "$BASE" "$K/n1" "$K/n2" > "$OUT/impratio.txt" 2>&1; echo "impratio rc=$?"
python -X utf8 -I $S/m1017/popjump1017.py B=$INS base=$BASE N1=$K/n1 N2=$K/n2 > "$OUT/popjump.txt" 2>&1; echo "popjump rc=$?"
bash "$S/c92gf/stoplines1002.sh" "$INS" "$OUT/stop1002.txt" "$SB" > "$OUT/stop1002.log" 2>&1; echo "stop1002 rc=$?"
bash "$S/c92gf/stoplines1002.sh" "$BASE" "$OUT/stop1002_base.txt" "$SB" > "$OUT/stop1002_base.log" 2>&1; echo "stop1002 base rc=$?"
bash "$S/c92gf/stoplines1002.sh" "$K/n1" "$OUT/stop1002_n1.txt" "$SB" > "$OUT/stop1002_n1.log" 2>&1; echo "stop1002 n1 rc=$?"
bash "$S/c92gf/stoplines1002.sh" "$K/n2" "$OUT/stop1002_n2.txt" "$SB" > "$OUT/stop1002_n2.log" 2>&1; echo "stop1002 n2 rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$INS" -v4 > "$OUT/hol_v4.txt" 2>&1; echo "hol rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$BASE" -v4 > "$OUT/hol_v4_base.txt" 2>&1; echo "hol base rc=$?"
python -X utf8 -I "$S/m808/gdp_zero10.py" arm=$INS base=$BASE > "$OUT/gdp_zero10.txt" 2>&1; echo "gdpz rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$INS" > "$OUT/plvl.txt" 2>&1; echo "plvl rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$INS" "$SB" > "$OUT/ukdef.txt" 2>&1; echo "ukdef rc=$?"
if [ "$CARD" != "-" ]; then
  python -X utf8 "$S/tools/nullgate.py" "$CARD" "$R/measurements/b10_20261116_m1171_ins" "$R/measurements/b10_20261116_m1171_n1_ins" "$R/measurements/b10_20261116_m1171_n2_ins" > "$OUT/nullgate.txt" 2>&1; echo "nullgate rc=$?"
  [ -f "$CARD.cost" ] && cp "$CARD.cost" "$OUT/arm.cost"
fi
python -X utf8 -I "$S/brk693/home_break.py" arm=$INS base=$BASE n1=$K/n1 n2=$K/n2 --test arm > "$OUT/home_break.txt" 2>&1; echo "home rc=$?"
bash "$S/ng.sh" "$INS" "$TAG" "$OUT/ng" rd:$R/measurements/ng1171_20261116:m1171 > "$OUT/ng.txt" 2>&1; echo "ng rc=$?"
echo READDONE
