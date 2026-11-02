#!/usr/bin/env bash
# (774) c7sw 腕 B／S11 の走り：木を写し、計器つきで N 種を回す（旗は木の既定）。
#   bash scripts/instruments/c7sw/armrun.sh <腕> <種の数> <種の起点> <木> [係数]   出力＝/d/PoB_work/c7sw/w/out_<腕>_<N>[_s<起点>]/ins
ARM=${1:?腕}; N=${2:?種の数}; SB=${3:?種の起点}; TREE=${4:?木}; COEF=${5-}
export POB_WORK=/d/PoB_work/c7sw/w; mkdir -p "$POB_WORK"
SP="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
bash "$SP/batch.sh" prep "$ARM" "dir:$TREE" plain || exit 1
printf '%s' "$COEF" > "$POB_WORK/coef_$ARM.txt"
INS=1 bash "$SP/batch.sh" run "$ARM" "$N" "$SB"
