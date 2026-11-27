#!/usr/bin/env bash
# (1407) c150rp 腕 B の読み手（c149pm/read_pm.sh の写しから基線と基準を m1404 へ）。基線の生 keep_1404/base_ins・ヌル腕の生 keep_1404/n1・n2・NG の基準 ng1404_20261127。
#   使い方: bash read_pm.sh <ins（D:/ の形）> <成績表（D:/ の形）> <タグ> <出力フォルダ（D:/ の形）>
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
INS=${1:?ins}; CARD=${2:?成績表}; TAG=${3:?タグ}; OUT=${4:?出力}
K=D:/PoB_work/keep_1404; BASE=D:/PoB_work/keep_1404/base_ins; BC=b10_20261127_m1404_ins
mkdir -p "$OUT"
cd "$R"
python -X utf8 -I $S/m1017/popjump1017.py B=$INS base=$BASE N1=$K/n1 N2=$K/n2 > "$OUT/popjump.txt" 2>&1; echo "popjump rc=$?"
for x in arm:$INS base:$BASE n1:$K/n1 n2:$K/n2; do n=${x%%:*}; d=${x#*:}
  bash "$S/c78gz/stoplines906.sh" "$d" "$OUT/stop906_$n.txt" 1 > "$OUT/stop906_$n.log" 2>&1; echo "stop906 $n rc=$?"
  bash "$S/c139sl/stoplines1330.sh" "$d" "$OUT/stop1330_$n.txt" 1 > "$OUT/stop1330_$n.log" 2>&1; echo "stop1330 $n rc=$?"
done
python -X utf8 -I $S/c11fl/pxjump.py arm=$INS base=$BASE n1=$K/n1 n2=$K/n2 > "$OUT/pxjump.txt" 2>&1; echo "pxjump rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$INS" -v4 > "$OUT/hol_v4.txt" 2>&1; echo "hol rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$BASE" -v4 > "$OUT/hol_v4_base.txt" 2>&1; echo "hol base rc=$?"
python -X utf8 -I "$S/m808/gdp_zero10.py" arm=$INS base=$BASE > "$OUT/gdp_zero10.txt" 2>&1; echo "gdpz rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$INS" > "$OUT/plvl.txt" 2>&1; echo "plvl rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$INS" 1 > "$OUT/ukdef.txt" 2>&1; echo "ukdef rc=$?"
python -X utf8 "$S/tools/nullgate.py" "$CARD" "$R/measurements/$BC" "$R/measurements/b10_20261127_m1404_n1_ins" "$R/measurements/b10_20261127_m1404_n2_ins" > "$OUT/nullgate.txt" 2>&1; echo "nullgate rc=$?"
[ -f "$CARD.cost" ] && grep -h "推定" "$CARD.cost" > "$OUT/cost.txt" 2>&1
diff <(grep -v SOURCE-HASH "$CARD") <(grep -v SOURCE-HASH "$R/measurements/$BC") | grep -c '^[<>]' > "$OUT/card_difflines.txt"
python -X utf8 -I "$S/brk693/home_break.py" arm=$INS base=$BASE n1=$K/n1 --test arm > "$OUT/home_break.txt" 2>&1; echo "home rc=$?"
bash "$S/ng.sh" "$INS" "$TAG" "$OUT/ng" rd:$R/measurements/ng1404_20261127:m1404 > "$OUT/ng.txt" 2>&1; echo "ng rc=$?"
python -X utf8 -I "$S/c150rp/gate_rp.py" arm=$INS base=$BASE > "$OUT/gate.txt" 2>&1; echo "gate rc=$?"
python -X utf8 -I "$S/c150rp/paper_rp.py" "$INS" 1-10 > "$OUT/paper_arm.txt" 2>&1; echo "paper rc=$?"
echo READDONE
