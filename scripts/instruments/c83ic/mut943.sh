#!/usr/bin/env bash
# (943) c83ic 突然変異の試験：使い捨ての木 D:/PoB_work/c83ic_work/wt_mut（器のコミットの detached）に M1〜M6 を 1 本ずつ当て、ImposeCrownFromLoserTests の赤と緑を数える。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/c83ic_work/mut.mark bash scripts/instruments/c83ic/mut943.sh <コミット>
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH"
SP="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"; REPO="$(cd "$SP/../../.." && pwd)"
C="${1:?コミット}"; T=D:/PoB_work/c83ic_work/wt_mut; O=D:/PoB_work/c83ic_work/mutation.txt
git -C "$REPO" worktree add --detach "$T" "$C" > /dev/null 2>&1 || exit 1
: > "$O"
for m in BASE M1 M2 M3 M4 M5 M6; do
  if [ "$m" = BASE ]; then python "$SP/mut943.py" "$T" RESET; else python "$SP/mut943.py" "$T" "$m"; fi || exit 1
  dotnet test "$T/PriceOfBread.Tests/PriceOfBread.Tests.csproj" -c Release --artifacts-path D:/PoB_work/c83ic_mutart --filter "FullyQualifiedName~ImposeCrownFromLoserTests" > "$T.$m.log" 2>&1
  echo "== $m rc=$?" >> "$O"
  grep -E '^\s+失敗 |成功!|失敗!' "$T.$m.log" | sed 's/^ *//' | cut -c1-200 >> "$O"
done
python "$SP/mut943.py" "$T" RESET > /dev/null
git -C "$REPO" worktree remove --force "$T"
