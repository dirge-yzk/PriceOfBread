#!/usr/bin/env bash
# (1406) c150sd（c149sd/base_sd.sh の写し・基準を m1404 へ） 読み手の基準とヌル腕の側を先に数える（read_sd.sh は出力があれば飛ばす）。bash base_sd.sh <出力フォルダ（D:/…）> <種の起点>
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
OUT=${1:?}; SB=${2:?}; K=D:/PoB_work/keep_1404; BASE=D:/PoB_work/keep_1404/base_ins
mkdir -p "$OUT"; cd "$R"
bash "$S/c78gz/stoplines906.sh" "$BASE" "$OUT/stop906_base.txt" "$SB" > "$OUT/stop906_base.log" 2>&1; echo "stop906base rc=$?"
bash "$S/c78gz/stoplines906.sh" "$K/n1" "$OUT/stop906_n1.txt" "$SB" > "$OUT/stop906_n1.log" 2>&1; echo "stop906n1 rc=$?"
bash "$S/c139sl/stoplines1330.sh" "$BASE" "$OUT/stop1330_base.txt" "$SB" > "$OUT/stop1330_base.log" 2>&1; echo "stop1330base rc=$?"
bash "$S/c139sl/stoplines1330.sh" "$K/n1" "$OUT/stop1330_n1.txt" "$SB" > "$OUT/stop1330_n1.log" 2>&1; echo "stop1330n1 rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$BASE" -v4 > "$OUT/hol_v4_base.txt" 2>&1; echo "holbase rc=$?"
echo BASEDONE
