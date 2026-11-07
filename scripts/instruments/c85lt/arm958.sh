#!/usr/bin/env bash
# (958) c85lt 腕を起こす（(945) arm958.sh の写し・作業フォルダだけ c85lt_work へ）：計器つき 10 種（INS=1）。腕 B＝種 1〜10・S11＝種 11〜20（呼び手が POB_CEARN=0 を渡す）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/c85lt_work/armB.mark bash scripts/instruments/c85lt/arm958.sh c85ltB 10 1
#   POB_CEARN=0 bash scripts/instruments/tools/bg.sh D:/PoB_work/c85lt_work/armS.mark bash scripts/instruments/c85lt/arm958.sh c85ltS 10 11
#   作業フォルダ D:/PoB_work/c85lt_work/arm_<腕>（POB_WORK）・木＝この台本のある木の HEAD（batch.sh prep commit:）。終了コードは batch.sh run のまま（0 完走・2 計器の国が空・3 ARMFULL・4 BATCHFAIL）。
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH"
SP="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"; REPO="$(cd "$SP/../../.." && pwd)"
ARM="${1:?腕}"; N="${2:-10}"; SB="${3:-1}"
export POB_WORK="D:/PoB_work/c85lt_work/arm_$ARM"; mkdir -p "$POB_WORK"
H=$(git -C "$REPO" rev-parse HEAD)
echo "木 $H 腕 $ARM 種 $SB〜$((SB+N-1)) POB_CEARN=${POB_CEARN:-（既定）}"
bash "$REPO/scripts/instruments/batch.sh" prep "$ARM" "commit:$H" plain || exit 1
INS=1 bash "$REPO/scripts/instruments/batch.sh" run "$ARM" "$N" "$SB"
