#!/usr/bin/env bash
# (1324) c138df 腕 B の読み手（c137ak/read_ak.sh と c132df/read_df.sh の写し・基線の生は INSRAW keep_1321/base_ins・ヌル腕は keep_1321/n1・n2・NG の基準は ng1321_20261123）。
#   基準とヌル腕の側の読みは pre_base.sh（腕に依らない・同じ台本）。
#   使い方: bash read_df.sh <ins（D:/ の形）> <成績表（D:/ の形・無ければ -）> <タグ> <出力フォルダ（D:/ の形）> <種の起点 1|11>
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
INS=${1:?ins}; CARD=${2:?成績表}; TAG=${3:?タグ}; OUT=${4:?出力}; SB=${5:?種の起点}
K=D:/PoB_work/keep_1321; BASE=$K/base_ins
mkdir -p "$OUT"
cd "$R"
python -X utf8 -I $S/m1017/popjump1017.py B=$INS base=$BASE N1=$K/n1 > "$OUT/popjump.txt" 2>&1; echo "popjump rc=$?"
bash "$S/c78gz/stoplines906.sh" "$INS" "$OUT/stop906.txt" "$SB" > "$OUT/stop906.log" 2>&1; echo "stop906 rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$INS" -v4 > "$OUT/hol_v4.txt" 2>&1; echo "hol rc=$?"
python -X utf8 -I "$S/m808/gdp_zero10.py" arm=$INS base=$BASE > "$OUT/gdp_zero10.txt" 2>&1; echo "gdpz rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$INS" > "$OUT/plvl.txt" 2>&1; echo "plvl rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$INS" "$SB" > "$OUT/ukdef.txt" 2>&1; echo "ukdef rc=$?"
if [ "$CARD" != "-" ]; then
  python -X utf8 "$S/tools/nullgate.py" "$CARD" "$R/measurements/b10_20261123_m1321_ins" "$R/measurements/b10_20261123_m1321_n1_ins" "$R/measurements/b10_20261123_m1321_n2_ins" > "$OUT/nullgate.txt" 2>&1; echo "nullgate rc=$?"
  [ -f "$CARD.cost" ] && grep -h "推定" "$CARD.cost" > "$OUT/cost.txt" 2>&1
  diff <(grep -v SOURCE-HASH "$CARD") <(grep -v SOURCE-HASH "$R/measurements/b10_20261123_m1321_ins") | grep -c '^[<>]' > "$OUT/card_difflines.txt"
fi
python -X utf8 -I "$S/brk693/home_break.py" arm=$INS base=$BASE n1=$K/n1 --test arm > "$OUT/home_break.txt" 2>&1; echo "home rc=$?"
bash "$S/ng.sh" "$INS" "$TAG" "$OUT/ng" rd:$R/measurements/ng1321_20261123:m1321 > "$OUT/ng.txt" 2>&1; echo "ng rc=$?"
python -X utf8 -I $S/c132df/gate_df.py "$INS" $SB $((SB+9)) armB > "$OUT/gate.txt" 2>&1; echo "gate rc=$?"
python -X utf8 -I "$S/c132df/arrears_sum.py" "$INS" $SB $((SB+9)) > "$OUT/arrears.txt" 2>&1; echo "arrears rc=$?"
python -X utf8 "$D0/tiny.py" "$INS" $SB $((SB+9)) > "$OUT/tiny.txt" 2>&1; echo "tiny rc=$?"
python -X utf8 -I "$S/r1313/q12_born.py" "$OUT/q12_arm" $SB-$((SB+9)) arm=$INS > "$OUT/q12_arm.log" 2>&1; echo "q12 rc=$?"
echo READDONE
