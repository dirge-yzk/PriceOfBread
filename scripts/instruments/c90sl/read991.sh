#!/usr/bin/env bash
# (991) c90sl 腕の読み手（読むだけ）：門（gates991）・止める線（stoplines949 pair・NULLS は m988）・空洞化（an_h2 -v4）・NG（記録）・nullgate（記録・種 1〜10 だけ）・.cost。
#   使い方: bash read991.sh <腕の作業フォルダ（D:/ の形）> <タグ（out_<タグ>・b10_<タグ>.txt）> <種の起点 1|11> <出力フォルダ（D:/ の形）>
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments   # R は D:/ の形（ng.sh の比べの空振りを避ける）
W=${1:?作業}; TAG=${2:?タグ}; SB=${3:-1}; OUT=${4:?出力}
INS=$W/out_$TAG/ins; CARD=$W/b10_$TAG.txt
mkdir -p "$OUT"
python -X utf8 "$D0/gates991.py" "$INS" 10 "$SB" > "$OUT/gates991.txt" 2>&1
RULE=pair NULLS="$R/measurements/m988_20261109/null/stop906_n1.txt $R/measurements/m988_20261109/null/stop906_n2.txt" bash "$S/c84gz/stoplines949.sh" "$INS" "$OUT/stop949.txt" "$SB" > /dev/null 2>&1; echo "stop949 rc=$?" > "$OUT/stop949.rc"
python -X utf8 "$S/hol/an_h2.py" "$INS" -v4 > "$OUT/hol_v4.txt" 2>&1
bash "$S/ng.sh" "$INS" "$TAG" "$OUT/ng" rd:$R/measurements/ng988_20261109:m988 > "$OUT/ng.txt" 2>&1
if [ "$SB" = "1" ]; then python -X utf8 "$S/tools/nullgate.py" "$CARD" "$R/measurements/b10_20261109_m988_ins" "$R/measurements/b10_20261109_m988_n1_ins" "$R/measurements/b10_20261109_m988_n2_ins" > "$OUT/nullgate.txt" 2>&1; fi
grep -h "推定" "$CARD.cost" > "$OUT/cost.txt" 2>&1
echo READDONE
