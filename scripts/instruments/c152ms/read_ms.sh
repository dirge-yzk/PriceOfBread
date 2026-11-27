#!/usr/bin/env bash
# (1423) c152ms 腕 B の読み手（c151lk/read_lk.sh の写しから基線と基準を m1418 へ・門を ms_gate.py へ・進行不能の線は lk_gate.py の --wars-only）。
#   基線の生 keep_1418/base_ins・ヌル腕の生 keep_1418/n1 と旧の keep_1411/n2（名指し）・NG の基準 ng1418_20261127。
#   stoplines906・1330 の基線とヌル腕は measurements/m1418_20261127/readM の stop906_base・_n1・_n2 と stop1330_base・_n1・_n2（同じ台本・同じ生）を読む＝腕だけ回す。
#   使い方: bash read_lk.sh <ins（D:/ の形）> <成績表（D:/ の形）> <タグ> <出力フォルダ（D:/ の形）>
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
INS=${1:?ins}; CARD=${2:?成績表}; TAG=${3:?タグ}; OUT=${4:?出力}
K=D:/PoB_work/keep_1418; K2=D:/PoB_work/keep_1411; BASE=$K/base_ins; BC=b10_20261127_m1418_ins
mkdir -p "$OUT"
cd "$R"
python -X utf8 -I $S/c152ms/ms_gate.py $INS > "$OUT/gate_ms.txt" 2>&1; echo "gate rc=$?"
python -X utf8 -I $S/c151lk/lk_gate.py $INS 10 --wars-only > "$OUT/wars.txt" 2>&1; echo "wars rc=$?"
python -X utf8 -I $S/m1017/popjump1017.py B=$INS base=$BASE N1=$K/n1 N2=$K2/n2 > "$OUT/popjump.txt" 2>&1; echo "popjump rc=$?"
bash "$S/c78gz/stoplines906.sh" "$INS" "$OUT/stop906_arm.txt" 1 > "$OUT/stop906_arm.log" 2>&1; echo "stop906 rc=$?"
bash "$S/c139sl/stoplines1330.sh" "$INS" "$OUT/stop1330_arm.txt" 1 > "$OUT/stop1330_arm.log" 2>&1; echo "stop1330 rc=$?"
python -X utf8 -I $S/c11fl/pxjump.py arm=$INS base=$BASE n1=$K/n1 n2=$K2/n2 > "$OUT/pxjump.txt" 2>&1; echo "pxjump rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$INS" -v4 > "$OUT/hol_v4.txt" 2>&1; echo "hol rc=$?"
python -X utf8 -I "$S/m808/gdp_zero10.py" arm=$INS base=$BASE > "$OUT/gdp_zero10.txt" 2>&1; echo "gdpz rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$INS" > "$OUT/plvl.txt" 2>&1; echo "plvl rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$INS" 1 > "$OUT/ukdef.txt" 2>&1; echo "ukdef rc=$?"
python -X utf8 "$S/tools/nullgate.py" "$CARD" "$R/measurements/$BC" "$R/measurements/b10_20261127_m1418_n1_ins" "$R/measurements/b10_20261127_m1411_n2_ins" > "$OUT/nullgate.txt" 2>&1; echo "nullgate rc=$?"
[ -f "$CARD.cost" ] && grep -h "推定" "$CARD.cost" > "$OUT/cost.txt" 2>&1
diff <(grep -v SOURCE-HASH "$CARD") <(grep -v SOURCE-HASH "$R/measurements/$BC") | grep -c '^[<>]' > "$OUT/card_difflines.txt"
python -X utf8 -I "$S/brk693/home_break.py" arm=$INS base=$BASE n1=$K/n1 --test arm > "$OUT/home_break.txt" 2>&1; echo "home rc=$?"
bash "$S/ng.sh" "$INS" "$TAG" "$OUT/ng" rd:$R/measurements/ng1418_20261127:m1418 > "$OUT/ng.txt" 2>&1; echo "ng rc=$?"
echo READDONE
