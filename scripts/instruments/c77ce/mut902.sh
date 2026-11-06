#!/usr/bin/env bash
# (902) c77ce 突然変異（機構の試験 CedeRuleToLocalTests）：使い捨ての worktree で 1 本ずつ壊し、どの試験が赤になるかを表にする（c75ce/mut888.sh の型）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/c77ce_work/mut.mark bash scripts/instruments/c77ce/mut902.sh
#   出力＝D:/PoB_work/c77ce_work/mut902.txt（線が measurements/m902_20261106/ へ写す）。使い捨ての worktree は終わりに外す。
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH" PYTHONUTF8=1
SP="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"; REPO="$(cd "$SP/../../.." && pwd)"
W=D:/PoB_work/c77ce_work; MW=$W/mutwt; ART=$W/mut_art; OUT=$W/mut902.txt
cd "$REPO" || exit 9
HEAD=$(git rev-parse HEAD)
git worktree remove --force "$MW" > /dev/null 2>&1
git worktree add --detach "$MW" "$HEAD" > /dev/null 2>&1 || { echo "worktree add failed"; exit 8; }
: > "$OUT"
echo "木 $HEAD（使い捨て $MW）" >> "$OUT"
run_case() {   # run_case <名>（none＝変えない）
  local name="$1"
  git -C "$MW" checkout -- . > /dev/null 2>&1
  if [ "$name" != "M0" ]; then
    python -X utf8 -I "$SP/mutapply902.py" "$MW/PriceOfBread.SimCore/Territory.cs" "$name" >> "$OUT" 2>&1 || { echo "$name 置き換え失敗" >> "$OUT"; return; }
  fi
  dotnet build "$MW/PriceOfBread.Tests/PriceOfBread.Tests.csproj" -c Release --artifacts-path "$ART" --disable-build-servers > "$W/mut_build_$name.txt" 2>&1 || { echo "$name ビルド失敗" >> "$OUT"; return; }
  dotnet test "$MW/PriceOfBread.Tests/PriceOfBread.Tests.csproj" -c Release --no-build --artifacts-path "$ART" --filter "FullyQualifiedName~CedeRuleToLocalTests" > "$W/mut_test_$name.txt" 2>&1
  local red; red=$(grep -oE "CedeRuleToLocalTests\.T[0-9]+_[^ (]*(\(flag: [0-9.]+\))?" "$W/mut_test_$name.txt" | sed 's/CedeRuleToLocalTests\.//; s/_[^(]*//' | sort -u | tr '\n' ' ')
  local summ; summ=$(grep -E "合格:|Passed!|Failed!|失敗!|成功!" "$W/mut_test_$name.txt" | tail -1)
  echo "$name	赤: ${red:-なし}	$summ" >> "$OUT"
}
for m in M0 M1 M2 M3 M4 M5 M6 M7 M8 M9 M10 M11 M12; do run_case $m; done
git -C "$MW" checkout -- . > /dev/null 2>&1
git worktree remove --force "$MW" > /dev/null 2>&1
echo "おわり" >> "$OUT"
