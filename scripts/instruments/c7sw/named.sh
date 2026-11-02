#!/usr/bin/env bash
# (774) c7sw 名指しの確かめ・突然変異の走り：使い捨ての木を写し、係数を渡し、計器つきで 1 種だけ全長で回す。
#   bash scripts/instruments/c7sw/named.sh <腕> <種> <係数（POB_COEF の形 A=1;B=2）> <木> [編集の python（写しの木の根を引数に取る）...]
#   例：N2 の種 3 の旗 1  bash named.sh c7swN2s3 3 "SupplySlope=0.49999999;InheritedStateIntentDrop=1" /d/PoB_work/wt_c7sw
#   出力＝/d/PoB_work/c7sw/w/out_<腕>_1_s<種>/ins（計器の生）。作業フォルダは D: に限る（C: に作らない）。長い＝Bash を run_in_background で（tools/bg.sh の印を付ける）。
ARM=${1:?腕}; SEED=${2:?種}; COEF=${3-}; TREE=${4:?木}; shift 4
export POB_WORK=/d/PoB_work/c7sw/w; mkdir -p "$POB_WORK"
SP="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
bash "$SP/batch.sh" prep "$ARM" "dir:$TREE" plain "$@" || exit 1
printf '%s' "$COEF" > "$POB_WORK/coef_$ARM.txt"
INS=1 bash "$SP/batch.sh" run "$ARM" 1 "$SEED"
