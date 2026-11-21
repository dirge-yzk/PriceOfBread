#!/usr/bin/env bash
# (1284) c132df 腕に依らない基準の側の読み（★(1254)：止める線の台本を基線の生で先に回す・腕 B と同じ台本で数え直す＝⑰）：bash pre_base.sh <出力フォルダ（D:/ の形）>
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
OUT=${1:?出力}; K=D:/PoB_work/keep_1280; BASE=$K/base_ins
mkdir -p "$OUT"; cd "$R"
bash "$S/c78gz/stoplines906.sh" "$BASE" "$OUT/stop906_base.txt" 1 > "$OUT/stop906_base.log" 2>&1; echo "stop906 base rc=$?"
bash "$S/c78gz/stoplines906.sh" "$K/n1" "$OUT/stop906_n1.txt" 1 > "$OUT/stop906_n1.log" 2>&1; echo "stop906 n1 rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$BASE" -v4 > "$OUT/hol_v4_base.txt" 2>&1; echo "hol base rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$BASE" 1 > "$OUT/ukdef_base.txt" 2>&1; echo "ukdef base rc=$?"
python -X utf8 -I "$S/c132df/paper_df.py" "$BASE" 1 10 m1280 > "$OUT/paper_base.txt" 2>&1; echo "paper base rc=$?"
python -X utf8 -I "$S/c132df/paper_df.py" "$K/n1" 1 10 m1280_n1 > "$OUT/paper_n1.txt" 2>&1; echo "paper n1 rc=$?"
echo PREDONE
