#!/usr/bin/env bash
# (996) c92gf：stoplines906.sh をそのまま呼び（旧の節はバイトで同じ）、出力の末尾に〈州の出入り〉の記録の節を足す。採否に使わない（m988 の盤面を見た後に作った区分）。
#   使い方: bash stoplines1002.sh <ins フォルダ（D:/ の形）> <出力ファイル> [種の起点 1|11]
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
cd "$(dirname "$0")/../../.."; S=scripts/instruments
M=${1:?ins}; OUT=${2:?出力}; SB=${3:-1}
bash $S/c78gz/stoplines906.sh "$M" "$OUT" "$SB" > /dev/null || exit $?
{ echo "== 記録（1002・州の出入り・採否に使わない・m988 の盤面を見た後に作った区分）：gdp_jump1002 --rows"; python -X utf8 -I $S/c92gf/gdp_jump1002.py --rows M=$M; } >> "$OUT" 2>&1 || exit $?
echo STOPDONE
