#!/usr/bin/env bash
# (888) c75ce 突然変異（計器の試験 CedeRuleInstrumentTests）：使い捨ての worktree で 1 本ずつ壊し、どの試験が赤になるかを表にする。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/c75ce_work/mut.mark bash scripts/instruments/c75ce/mut888.sh
#   出力＝D:/PoB_work/c75ce_work/mut888.txt（合流が measurements/m888_20261106/ へ写す・線が自分で写す）。使い捨ての worktree は終わりに外す。
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH" PYTHONUTF8=1
SP="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"; REPO="$(cd "$SP/../../.." && pwd)"
W=D:/PoB_work/c75ce_work; MW=$W/mutwt; ART=$W/mut_art; OUT=$W/mut888.txt
cd "$REPO" || exit 9
HEAD=$(git rev-parse HEAD)
git worktree remove --force "$MW" > /dev/null 2>&1
git worktree add --detach "$MW" "$HEAD" > /dev/null 2>&1 || { echo "worktree add failed"; exit 8; }
: > "$OUT"
echo "木 $HEAD（使い捨て $MW）" >> "$OUT"
run_case() {   # run_case <名> <ファイル> <python の置き換えの式の番号>
  local name="$1" file="$2" idx="$3"
  git -C "$MW" checkout -- . > /dev/null 2>&1
  if [ "$idx" != "none" ]; then
    python -X utf8 "$SP/mutapply888.py" "$MW/$file" "$idx" >> "$OUT" 2>&1 || { echo "$name 置き換え失敗" >> "$OUT"; return; }
  fi
  dotnet build "$MW/PriceOfBread.Tests/PriceOfBread.Tests.csproj" -c Release --artifacts-path "$ART" --disable-build-servers > "$W/mut_build_$name.txt" 2>&1 || { echo "$name ビルド失敗" >> "$OUT"; return; }
  dotnet test "$MW/PriceOfBread.Tests/PriceOfBread.Tests.csproj" -c Release --no-build --artifacts-path "$ART" --filter "FullyQualifiedName~CedeRuleInstrumentTests" > "$W/mut_test_$name.txt" 2>&1
  local red; red=$(grep -oE "CedeRuleInstrumentTests\.T[0-9]_[^ ]*" "$W/mut_test_$name.txt" | grep -B0 "" | sort -u | sed 's/CedeRuleInstrumentTests\.//; s/_.*//' | sort -u | tr '\n' ' ')
  local summ; summ=$(grep -E "合格:|Passed!|Failed!|失敗!|成功!" "$W/mut_test_$name.txt" | tail -1)
  echo "$name	赤: ${red:-なし}	$summ" >> "$OUT"
}
run_case M0_変えない none none
run_case M1_鍵をNameだけ PriceOfBread.SimCore/InstrumentsCede.cs 1
run_case M2_印を外す PriceOfBread.SimCore/InstrumentsCede.cs 2
run_case M3_movedAgainを持ち主だけ PriceOfBread.SimCore/InstrumentsCede.cs 3
run_case M4_yearをQuarterの時 PriceOfBread.SimCore/InstrumentsCede.cs 4
run_case M5_助走を捨てない PriceOfBread.SimCore/InstrumentsCede.cs 5
run_case M6_門の許しを1へ PriceOfBread.SimCore/InstrumentsCede.cs 6
run_case M7_前をRehomeの後に読む PriceOfBread.SimCore/Territory.cs 7
run_case M8_差に持分を入れない PriceOfBread.SimCore/InstrumentsCede.cs 8
git -C "$MW" checkout -- . > /dev/null 2>&1
git worktree remove --force "$MW" > /dev/null 2>&1
rm -rf "$ART"
echo "おわり" >> "$OUT"
cat "$OUT"
