#!/usr/bin/env bash
# (895) c76ls 突然変異（試験 LandShareTests・WriterRatchetTests と門の台本 gate895.py）：使い捨ての worktree で 1 本ずつ壊し、どの試験が赤になるかを表にする。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/c76ls_work/mut.mark bash scripts/instruments/c76ls/mut895.sh
#   出力＝D:/PoB_work/c76ls_work/mut895.txt（線が measurements/m895_20261106/mutation.txt へ写す）。使い捨ての worktree は終わりに外す。
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH" PYTHONUTF8=1
SP="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"; REPO="$(cd "$SP/../../.." && pwd)"
W=D:/PoB_work/c76ls_work; MW=$W/mutwt; ART=$W/mut_art; OUT=$W/mut895.txt
cd "$REPO" || exit 9
HEAD=$(git rev-parse HEAD)
git worktree remove --force "$MW" > /dev/null 2>&1
git worktree add --detach "$MW" "$HEAD" > /dev/null 2>&1 || { echo "worktree add failed"; exit 8; }
: > "$OUT"
echo "木 $HEAD（使い捨て $MW）・試験の組 LandShareTests＋WriterRatchetTests" >> "$OUT"
run_case() {   # run_case <番号>
  local name="$1"
  git -C "$MW" checkout -- . > /dev/null 2>&1
  if [ "$name" != "M0" ]; then
    python -X utf8 "$SP/mutapply895.py" "$MW" "$name" >> "$OUT" 2>&1 || { echo "$name 置き換え失敗" >> "$OUT"; return; }
  fi
  dotnet build "$MW/PriceOfBread.Tests/PriceOfBread.Tests.csproj" -c Release --artifacts-path "$ART" --disable-build-servers > "$W/mut_build_$name.txt" 2>&1 || { echo "$name	ビルド失敗" >> "$OUT"; return; }
  dotnet test "$MW/PriceOfBread.Tests/PriceOfBread.Tests.csproj" -c Release --no-build --artifacts-path "$ART" --filter "FullyQualifiedName~LandShareTests|FullyQualifiedName~WriterRatchetTests" > "$W/mut_test_$name.txt" 2>&1
  local red; red=$(grep -E "^\s*(失敗|Failed) " "$W/mut_test_$name.txt" | grep -oE "(LandShareTests|WriterRatchetTests)\.[^ (\[]*" | sed 's/^LandShareTests\.//' | sort -u | tr '\n' ' ')
  local summ; summ=$(grep -E "合格:|Passed!|Failed!|失敗!|成功!" "$W/mut_test_$name.txt" | tail -1)
  echo "$name	赤: ${red:-なし}	$summ" >> "$OUT"
}
for m in M0 M1a M1b M2 M3 M4 M5 M6 M7 M8 M9 M10b M11 M12 M13 M14 M15 M16 M17 M19 M20 M21 M22 M23 M24 M25 M26 M27; do run_case "$m"; done
git -C "$MW" checkout -- . > /dev/null 2>&1
# M10 門の台本の比べを 1e-12 から 1 へ緩める＝自分の試し（人工のずれ 1e-6）が落とせない
cp "$SP/gate895.py" "$W/gate895_m10.py"
python -X utf8 - "$W/gate895_m10.py" <<'PY'
import sys
p = sys.argv[1]
b = open(p, 'rb').read()
a = b'TOL = 1e-12\n'
assert b.count(a) == 1
open(p, 'wb').write(b.replace(a, b'TOL = 1\n'))
PY
python -X utf8 "$W/gate895_m10.py" --selftest > "$W/mut_gate_m10.txt" 2>&1; RC10=$?
python -X utf8 "$SP/gate895.py" --selftest > "$W/mut_gate_m0.txt" 2>&1; RC0=$?
echo "M10	門の台本 TOL=1 の自分の試し rc=$RC10（0 なら緑＝穴・1 なら赤）・元の台本 rc=$RC0	$(tail -1 "$W/mut_gate_m10.txt")" >> "$OUT"
git worktree remove --force "$MW" > /dev/null 2>&1
rm -rf "$ART"
echo "おわり" >> "$OUT"
cat "$OUT"
