#!/usr/bin/env bash
# (787) c9op 腕の駆動：木 $2 から腕 $1 を準備し、計器つきで種 $3 本回して、終わったら印 D:/PoB_work/c9op/marks/$1.done に rc を書く。
#   bash scripts/instruments/c9op/run_arm.sh <腕> <木> <種の数>      （run_in_background で起動する・待ちは印の waitmark.sh か完了の通知）
# 腕が満杯（rc 3）のときは印に rc=3 を書いて終わる＝呼び手が空いてから呼び直す。
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH"
export POB_WORK=D:/PoB_work/c9op PYTHONUTF8=1 AVALONIA_TELEMETRY_OPTOUT=1 MEASURE_CACHE=0
ARM="$1"; TREE="$2"; N="$3"; REPO_SCRIPTS="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
export POB_REPO="$TREE"
mkdir -p $POB_WORK/marks; rm -f $POB_WORK/marks/$ARM.done
bash "$REPO_SCRIPTS/batch.sh" prep "$ARM" "dir:$TREE" plain || { echo "rc=9 prep end=$(date '+%F %T')" > $POB_WORK/marks/$ARM.done; exit 0; }
INS=1 bash "$REPO_SCRIPTS/batch.sh" run "$ARM" "$N"
echo "rc=$? end=$(date '+%F %T')" > $POB_WORK/marks/$ARM.done
