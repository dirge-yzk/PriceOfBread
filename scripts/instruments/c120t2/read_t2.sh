#!/usr/bin/env bash
# (1202) c120t2 腕 B の読み手（c119nd/read_nd.sh の写し・基線とヌル腕と NG の基準は m1198〔keep_1198/base_ins・n1・ng1198〕・ヌル腕は N1 の 1 本）。
#   門＝止める線（stoplines1002・plvl_seeds・popjump1017・S2b gread.py px・an_h2 -v4・warcount の 10 倍・.cost）＋史実の桁 H（paper.py）。他は記録（G-能・G-産 gread.py g）。
#   使い方: bash read_t2.sh <ins（D:/ の形）> <成績表（D:/ の形・無ければ -）> <タグ> <出力フォルダ（D:/ の形）> <種の起点 1|11> <A0 の生（D:/ の形）>
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
INS=${1:?ins}; CARD=${2:?成績表}; TAG=${3:?タグ}; OUT=${4:?出力}; SB=${5:?種の起点}; A0=${6:?A0}
SE=$((SB+9)); K=D:/PoB_work/keep_1198; BASE=$K/base_ins
mkdir -p "$OUT"
cd "$R"
python -X utf8 -I "$S/c120t2/paper.py" "$BASE" $SB $SE "$INS" > "$OUT/paper_H.txt" 2>&1; echo "paper rc=$?"
python -X utf8 -I "$S/c120t2/gread.py" g "$A0" "$INS" $SB $SE > "$OUT/g_records.txt" 2>&1; echo "g rc=$?"
for x in B:$INS M:$BASE N1:$K/n1; do python -X utf8 -I "$S/c120t2/gread.py" px "${x#*:}" $SB $SE > "$OUT/s2b_${x%%:*}.txt" 2>&1; echo "s2b ${x%%:*} rc=$?"; done
python -X utf8 -I "$S/c118nc/warcount.py" M=$BASE N1=$K/n1 B=$INS > "$OUT/warcount.txt" 2>&1; echo "warcount rc=$?"
python -X utf8 -I $S/m1017/popjump1017.py B=$INS base=$BASE N1=$K/n1 > "$OUT/popjump.txt" 2>&1; echo "popjump rc=$?"
bash "$S/c92gf/stoplines1002.sh" "$INS" "$OUT/stop1002.txt" "$SB" > "$OUT/stop1002.log" 2>&1; echo "stop1002 rc=$?"
bash "$S/c92gf/stoplines1002.sh" "$BASE" "$OUT/stop1002_base.txt" "$SB" > "$OUT/stop1002_base.log" 2>&1; echo "stop1002 base rc=$?"
bash "$S/c92gf/stoplines1002.sh" "$K/n1" "$OUT/stop1002_n1.txt" "$SB" > "$OUT/stop1002_n1.log" 2>&1; echo "stop1002 n1 rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$INS" -v4 > "$OUT/hol_v4.txt" 2>&1; echo "hol rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$BASE" -v4 > "$OUT/hol_v4_base.txt" 2>&1; echo "hol base rc=$?"
python -X utf8 -I "$S/m808/gdp_zero10.py" arm=$INS base=$BASE > "$OUT/gdp_zero10.txt" 2>&1; echo "gdpz rc=$?"
for x in B:$INS M:$BASE N1:$K/n1; do python -X utf8 -I "$S/brk693/plvl_seeds.py" "${x#*:}" > "$OUT/plvl_${x%%:*}.txt" 2>&1; echo "plvl ${x%%:*} rc=$?"; done
python -X utf8 -I "$S/m975/ukdef975.py" "$INS" "$SB" > "$OUT/ukdef.txt" 2>&1; echo "ukdef rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$BASE" "$SB" > "$OUT/ukdef_base.txt" 2>&1; echo "ukdef base rc=$?"
if [ "$CARD" != "-" ]; then
  python -X utf8 "$S/tools/nullgate.py" "$CARD" "$R/measurements/b10_20261118_m1198_ins" "$R/measurements/b10_20261118_m1198_n1_ins" "$R/measurements/b10_20261118_m1198_n1_ins" > "$OUT/nullgate.txt" 2>&1; echo "nullgate rc=$?"
  [ -f "$CARD.cost" ] && grep -h "推定" "$CARD.cost" > "$OUT/cost.txt" 2>&1
fi
python -X utf8 -I "$S/brk693/home_break.py" arm=$INS base=$BASE n1=$K/n1 --test arm > "$OUT/home_break.txt" 2>&1; echo "home rc=$?"
bash "$S/ng.sh" "$INS" "$TAG" "$OUT/ng" rd:$R/measurements/ng1198_20261118:m1198 > "$OUT/ng.txt" 2>&1; echo "ng rc=$?"
echo READDONE
