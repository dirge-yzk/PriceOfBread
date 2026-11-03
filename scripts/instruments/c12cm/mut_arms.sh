#!/usr/bin/env bash
# (805) c12cm 突然変異の腕（門 G を落とす誤りを入れた使い捨ての木・種 1〜2・計器つき・全長）。木そのものは変えない。
#   使い方: bash scripts/instruments/c12cm/mut_arms.sh prep <測る木>    各腕の写しを取って突然変異を当てる（短い）
#           bash scripts/instruments/c12cm/mut_arms.sh run  <測る木>    腕を同時に回す（長い＝Bash を run_in_background で。腕が満杯なら ARMFULL で止まる）
#   出力は D:/PoB_work/c12cm_mut/out_<腕>_2/ins（生は合流の判断まで残す）。読み手は  python -X utf8 -I scripts/instruments/c12cm/gates_expect.py <ins>
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH"
export POB_WORK=D:/PoB_work/c12cm_mut PYTHONUTF8=1 AVALONIA_TELEMETRY_OPTOUT=1 MEASURE_CACHE=0
CMD="${1:?prep か run}"; T="${2:?木}"; D="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"; export POB_REPO="$T"
ARMS="${MUARMS:-Mu6 MuCut MuTp MuR}"
mkdir -p $POB_WORK
case "$CMD" in
  prep) for m in $ARMS; do MU=$m bash $D/../batch.sh prep c12cm$m "dir:$T" plain $D/mutate.py || exit 1; done; echo MUTPREPDONE ;;
  run)  printf '%s\n' $ARMS | xargs -P 4 -I{} bash -c 'INS=1 bash '"$D"'/../batch.sh run c12cm{} 2' ; echo MUTRUNDONE ;;
esac
