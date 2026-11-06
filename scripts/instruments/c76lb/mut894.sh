#!/usr/bin/env bash
# (894) c76lb 突然変異（計器の試験 LandbaseInstrumentTests）：使い捨ての worktree で 1 本ずつ壊し、どの試験が赤になるかを表にする（c75ce の mut888.sh の型を写した）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/c76lb_work/mut.mark bash scripts/instruments/c76lb/mut894.sh
#   出力＝D:/PoB_work/c76lb_work/mut894.txt（線が measurements/m894_20261106/ へ写す）。使い捨ての worktree は終わりに外す。
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH" PYTHONUTF8=1
SP="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"; REPO="$(cd "$SP/../../.." && pwd)"
W=D:/PoB_work/c76lb_work; MW=$W/mutwt; ART=$W/mut_art; OUT=$W/mut894.txt
cd "$REPO" || exit 9
HEAD=$(git rev-parse HEAD)
git worktree remove --force "$MW" > /dev/null 2>&1
git worktree add --detach "$MW" "$HEAD" > /dev/null 2>&1 || { echo "worktree add failed"; exit 8; }
: > "$OUT"
echo "木 $HEAD（使い捨て $MW）" >> "$OUT"
F=PriceOfBread.SimCore/InstrumentsLandbase.cs
run_case() {   # run_case <名> <番号|none>
  local name="$1" idx="$2"
  git -C "$MW" checkout -- . > /dev/null 2>&1
  if [ "$idx" != "none" ]; then
    python -X utf8 "$SP/mutapply894.py" "$MW/$F" "$idx" >> "$OUT" 2>&1 || { echo "$name 置き換え失敗" >> "$OUT"; return; }
  fi
  dotnet build "$MW/PriceOfBread.Tests/PriceOfBread.Tests.csproj" -c Release --artifacts-path "$ART" --disable-build-servers > "$W/mut_build_$name.txt" 2>&1 || { echo "$name ビルド失敗" >> "$OUT"; return; }
  dotnet test "$MW/PriceOfBread.Tests/PriceOfBread.Tests.csproj" -c Release --no-build --artifacts-path "$ART" --filter "FullyQualifiedName~LandbaseInstrumentTests" > "$W/mut_test_$name.txt" 2>&1
  local red; red=$(grep -oE "LandbaseInstrumentTests\.T[0-9]+_" "$W/mut_test_$name.txt" | sed 's/LandbaseInstrumentTests\.//; s/_$//' | sort -u -V | tr '\n' ' ')
  local summ; summ=$(grep -E "合格:|Passed!|Failed!|失敗!|成功!" "$W/mut_test_$name.txt" | tail -1)
  echo "$name	赤: ${red:-なし}	$summ" >> "$OUT"
}
run_case U0_変えない none
run_case U1_免税を落とす U1
run_case U2_Rのmaxを外す U2
run_case U3_Rに輸出額を足す U3
run_case U4_遅れを3四半期 U4
run_case U5_0の州の判定を小なりへ U5
run_case U6_地代を0へ書き戻す U6
run_case U7_取り分の上界でrを1 U7
run_case U8_総額の上界にrを掛ける U8
run_case U9_不足を値段で割らない U9
run_case U10_KOverをK1000へ U10
run_case U11_十分の一税を落とす U11
run_case U12_持ち主の空の判定を反転 U12
run_case U13_照合の許しを1へ U13
run_case U14_値段の重みを1へ U14
run_case U15_州の請求に免税を掛けない U15
git -C "$MW" checkout -- . > /dev/null 2>&1
git worktree remove --force "$MW" > /dev/null 2>&1
rm -rf "$ART"
echo "おわり" >> "$OUT"
cat "$OUT"
