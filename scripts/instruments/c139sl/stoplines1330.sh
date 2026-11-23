#!/usr/bin/env bash
# (1330) c139sl：stoplines1002.sh をそのまま呼び（旧の節はバイトで同じ）、出力の末尾に〈100 倍の跳びの記録の区分〉の節を足す。採否に使わない（結果の後に組んだ区分）。
#   使い方: bash stoplines1330.sh <ins フォルダ（D:/ の形）> <出力ファイル> [種の起点 1|11]
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
cd "$(dirname "$0")/../../.."; S=scripts/instruments
M=${1:?ins}; OUT=${2:?出力}; SB=${3:-1}
bash $S/c92gf/stoplines1002.sh "$M" "$OUT" "$SB" > /dev/null || exit $?
{ echo "== 記録（1330・100 倍の跳びの区分・採否に使わない・結果の後に組んだ区分）：gdp_jump1330 --rows"; python -X utf8 -I $S/c139sl/gdp_jump1330.py --rows M=$M; } >> "$OUT" 2>&1 || exit $?
echo STOPDONE
