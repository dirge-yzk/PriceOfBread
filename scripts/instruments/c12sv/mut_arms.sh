#!/usr/bin/env bash
# (807) c12sv 突然変異の腕の準備：測る木（旗の既定 1 の木）の写しに M1〜M3 を順に当てて prep（写しを取る）する。木そのものは変えない。
#   使い方: bash scripts/instruments/c12sv/mut_arms.sh <測る木>
#   腕は別に  POB_WORK=D:/PoB_work/c12sv_mut POB_REPO=<測る木> INS=1 bash scripts/instruments/batch.sh run c12svM1 2  のように回す（run_in_background）。
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH"
export POB_WORK=D:/PoB_work/c12sv_mut PYTHONUTF8=1 AVALONIA_TELEMETRY_OPTOUT=1 MEASURE_CACHE=0
T="${1:?木}"; D="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"; export POB_REPO="$T"
mkdir -p $POB_WORK
for m in M1 M2 M3; do
  cat > $POB_WORK/mut_$m.py <<PY
import subprocess, sys
sys.exit(subprocess.call([sys.executable, '-I', r'$D/mutate.py', sys.argv[1], '$m']))
PY
  bash $D/../batch.sh prep c12sv$m "dir:$T" plain $POB_WORK/mut_$m.py || exit 1
done
echo MUTPREPDONE
