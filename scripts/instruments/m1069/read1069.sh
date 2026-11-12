#!/usr/bin/env bash
# (1069) 合流の読み手（m1053/read1053.sh の写し・門は c101vs/gate_vs.py を変えずに・記録は c101vs/rec_vs.py・ヌル腕と NG の基準は m1053）
#   使い方: bash read1069.sh <腕の作業フォルダ（D:/ の形）> <タグ> <出力フォルダ（D:/ の形）> <種の起点> <基準の生（D:/ の形）> <旗 0 の生（dep_s あり・D:/ の形）か ->
#   S11（種の起点 11）は旗 0 の種 11〜20 が無い＝旗 0 と旗 1 の両方に S11 の生を渡し、G1・G1b の〈旗 1 の側〉だけを読む（予言 [4]）。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
W=${1:?作業}; TAG=${2:?タグ}; OUT=${3:?出力}; SB=${4:?種の起点}; BASE=${5:?基準}; F0=${6:--}
INS=$W/out_$TAG/ins; CARD=$W/b10_$TAG.txt; K=D:/PoB_work/keep_1053
mkdir -p "$OUT"
if [ "$F0" != "-" ]; then
  python -X utf8 -I "$S/c101vs/gate_vs.py" gate "$F0" "$INS" "$K/base_ins" "$K/n1" "$K/n2" > "$OUT/gate_vs.txt" 2>&1; echo "gate rc=$?"
  python -X utf8 -I "$S/c101vs/rec_vs.py" "$F0" "$INS" "$BASE" "$K/n1" "$K/n2" > "$OUT/rec_vs.txt" 2>&1; echo "rec rc=$?"
fi
python -X utf8 -I $S/m1017/popjump1017.py B=$INS base=$BASE N1=$K/n1 N2=$K/n2 > "$OUT/popjump.txt" 2>&1; echo "popjump rc=$?"
bash "$S/c92gf/stoplines1002.sh" "$INS" "$OUT/stop1002.txt" "$SB" > "$OUT/stop1002.log" 2>&1; echo "stop1002 rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$INS" -v4 > "$OUT/hol_v4.txt" 2>&1; echo "hol rc=$?"
python -X utf8 -I "$S/m808/gdp_zero10.py" arm=$INS base=$BASE > "$OUT/gdp_zero10.txt" 2>&1; echo "gdpz rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$INS" > "$OUT/plvl.txt" 2>&1; echo "plvl rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$INS" "$SB" > "$OUT/ukdef.txt" 2>&1; echo "ukdef rc=$?"
python -X utf8 "$S/tools/nullgate.py" "$CARD" "$R/measurements/b10_20261112_m1053_ins" "$R/measurements/b10_20261112_m1053_n1_ins" "$R/measurements/b10_20261112_m1053_n2_ins" > "$OUT/nullgate.txt" 2>&1; echo "nullgate rc=$?"
python -X utf8 -I "$S/brk693/home_break.py" arm=$INS base=$BASE n1=$K/n1 n2=$K/n2 --test arm > "$OUT/home_break.txt" 2>&1; echo "home rc=$?"
bash "$S/ng.sh" "$INS" "$TAG" "$OUT/ng" rd:$R/measurements/ng1053_20261112:m1053 > "$OUT/ng.txt" 2>&1; echo "ng rc=$?"
grep -h "推定" "$CARD.cost" > "$OUT/cost.txt" 2>&1
echo READDONE
