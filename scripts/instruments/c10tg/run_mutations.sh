#!/usr/bin/env bash
# (792) c10tg の突然変異を使い捨ての木で順に回す。使い方：bash run_mutations.sh <使い捨ての木> <成果物の置き場> <出力ファイル>   （長い＝Bash を run_in_background で）
# 使い捨ての木は c10tg の HEAD の git worktree。各モードの前に Fiscal.cs と Simulation.cs を git checkout で戻し、mutate_tg.py で 1 行を書き換え、ビルドして該当の試験だけ回す。
export PYTHONUTF8=1
T="$1"; ART="$2"; OUT="$3"
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
: > "$OUT"
for M in M1 M2 M3 M4 Mb; do
  git -C "$T" checkout -- PriceOfBread.SimCore/Fiscal.cs PriceOfBread.SimCore/Simulation.cs
  python -I "$HERE/mutate_tg.py" "$T" "$M" >> "$OUT" 2>&1 || { echo "## $M 失敗（突然変異が当たらない）" >> "$OUT"; continue; }
  echo "## $M" >> "$OUT"
  dotnet build "$T/PriceOfBread.Tests/PriceOfBread.Tests.csproj" -c Release --artifacts-path "$ART" --disable-build-servers > "$ART/build_$M.txt" 2>&1 || { echo "ビルド失敗 $M" >> "$OUT"; continue; }
  dotnet test "$T/PriceOfBread.Tests/PriceOfBread.Tests.csproj" -c Release --no-build --artifacts-path "$ART" --filter "FullyQualifiedName~FiscalTests|FullyQualifiedName~PortByLandTests" 2>&1 | grep -E "^ *(失敗|Failed)|^(成功|失敗)!|Passed|b＝|期待" >> "$OUT"
done
git -C "$T" checkout -- PriceOfBread.SimCore/Fiscal.cs PriceOfBread.SimCore/Simulation.cs
echo "rc=0 end=$(date '+%F %T')" > "$OUT.done"
