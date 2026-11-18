#!/usr/bin/env bash
# (1223) c123iv 腕 B の読み手（c122nr/read_nr.sh の写し・基線の生は INSRAW keep_1219/base_ins・ヌル腕は keep_1212/n1・NG は ng1198〔生が同じ〕）。
#   門＝止める線（stoplines1002・plvl_seeds・popjump1017・an_h2 -v4・.cost）＋史実の桁 H1・H2（c123iv/magn_iv.py）＋狙い W0・W0b（c123iv/w0.py）。他は記録。
#   使い方: bash read_iv.sh <ins（D:/ の形）> <成績表（D:/ の形・無ければ -）> <タグ> <出力フォルダ（D:/ の形）> <種の起点 1|11>
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
INS=${1:?ins}; CARD=${2:?成績表}; TAG=${3:?タグ}; OUT=${4:?出力}; SB=${5:?種の起点}
K=D:/PoB_work/keep_1212; BASE=D:/PoB_work/keep_1219/base_ins
mkdir -p "$OUT"
cd "$R"
python -X utf8 -I "$S/c123iv/w0.py" "$INS" 1-10 > "$OUT/w0.txt" 2>&1; echo "w0 rc=$?"
python -X utf8 -I "$S/c123iv/w0.py" "$BASE" 1-10 > "$OUT/w0_base.txt" 2>&1; echo "w0 base rc=$?"
python -X utf8 -I "$S/c123iv/magn_iv.py" "$INS" 1-10 "$BASE" > "$OUT/magn_iv.txt" 2>&1; echo "magn_iv rc=$?"
python -X utf8 -I "$S/c123iv/shadow.py" "$INS" 1-10 "$OUT/shadow_arm_f0.txt" 0 > /dev/null 2>&1; echo "shadow arm rc=$?"
python -X utf8 -I $S/m1017/popjump1017.py B=$INS base=$BASE N1=$K/n1 > "$OUT/popjump.txt" 2>&1; echo "popjump rc=$?"
bash "$S/c92gf/stoplines1002.sh" "$INS" "$OUT/stop1002.txt" "$SB" > "$OUT/stop1002.log" 2>&1; echo "stop1002 rc=$?"
bash "$S/c92gf/stoplines1002.sh" "$BASE" "$OUT/stop1002_base.txt" "$SB" > "$OUT/stop1002_base.log" 2>&1; echo "stop1002 base rc=$?"
bash "$S/c92gf/stoplines1002.sh" "$K/n1" "$OUT/stop1002_n1.txt" "$SB" > "$OUT/stop1002_n1.log" 2>&1; echo "stop1002 n1 rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$INS" -v4 > "$OUT/hol_v4.txt" 2>&1; echo "hol rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$BASE" -v4 > "$OUT/hol_v4_base.txt" 2>&1; echo "hol base rc=$?"
python -X utf8 -I "$S/m808/gdp_zero10.py" arm=$INS base=$BASE > "$OUT/gdp_zero10.txt" 2>&1; echo "gdpz rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$INS" > "$OUT/plvl.txt" 2>&1; echo "plvl rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$BASE" > "$OUT/plvl_base.txt" 2>&1; echo "plvl base rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$K/n1" > "$OUT/plvl_n1.txt" 2>&1; echo "plvl n1 rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$INS" "$SB" > "$OUT/ukdef.txt" 2>&1; echo "ukdef rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$BASE" "$SB" > "$OUT/ukdef_base.txt" 2>&1; echo "ukdef base rc=$?"
if [ "$CARD" != "-" ]; then
  python -X utf8 "$S/tools/nullgate.py" "$CARD" "$R/measurements/b10_20261118_m1212_ins" "$R/measurements/b10_20261118_m1212_n1_ins" > "$OUT/nullgate.txt" 2>&1; echo "nullgate rc=$?"
  [ -f "$CARD.cost" ] && grep -h "推定" "$CARD.cost" > "$OUT/cost.txt" 2>&1
fi
python -X utf8 -I "$S/brk693/home_break.py" arm=$INS base=$BASE n1=$K/n1 --test arm > "$OUT/home_break.txt" 2>&1; echo "home rc=$?"
bash "$S/ng.sh" "$INS" "$TAG" "$OUT/ng" rd:$R/measurements/ng1198_20261118:m1198 > "$OUT/ng.txt" 2>&1; echo "ng rc=$?"
echo READDONE
