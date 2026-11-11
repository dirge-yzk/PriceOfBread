#!/usr/bin/env bash
# (820) c65op 突然変異の腕：測る木（旗 0 の器の木＝mutate.py が旗を 1 にする）の写しに Mu を当てて prep し、計器つき 2 種（種 1〜2・全長）で回し、門の台本と機構の試験を回す。木そのものは変えない。
#   使い方: bash scripts/instruments/c65op/mut_arms.sh <測る木> <Mu1 Mu2 ...>（Bash の run_in_background で）
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH"
export POB_WORK=D:/PoB_work/c65op_work/mut PYTHONUTF8=1 AVALONIA_TELEMETRY_OPTOUT=1 MEASURE_CACHE=0
export POB_INSTRUMENTS_NATIONS="${POB_INSTRUMENTS_NATIONS:-清,ロシア,オスマン,フランス,イギリス,プロイセン,オーストリア,日本,アメリカ}"
T="${1:?木}"; shift; D="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"; export POB_REPO="$T"
mkdir -p $POB_WORK
for m in "$@"; do
  MU=$m bash $D/../batch.sh prep c65op$m "dir:$T" plain $D/mutate.py || exit 1
done
printf '%s\n' "$@" | xargs -P 6 -I{} bash -c 'INS=1 bash '"$D"'/../batch.sh run c65op{} 2 > '"$POB_WORK"'/run_{}.log 2>&1'
for m in "$@"; do echo "== $m"; tail -n 3 $POB_WORK/run_$m.log; done
echo MUTDONE   # 機構の試験は mut_unit.sh（腕の前に回した）
