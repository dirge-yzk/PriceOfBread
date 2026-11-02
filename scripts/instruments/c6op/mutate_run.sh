#!/usr/bin/env bash
# (771) c6op 突然変異の走り：使い捨ての木 $1 で M1〜M6 を 1 つずつ当て、HabitTests（と M6 は計器なし＝計器つき）を回して赤を確かめる。結果は $2 へ。
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH"
TREE="$1"; OUT="$2"; SP="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"; : > "$OUT"
for m in M1 M2 M3 M4 M5 M6; do
  python "$SP/mutate.py" "$TREE" reset || exit 1
  python "$SP/mutate.py" "$TREE" $m || { echo "$m: 変異を当てられない" >> "$OUT"; continue; }
  dotnet test "$TREE/PriceOfBread.Tests/PriceOfBread.Tests.csproj" -c Release --nologo --filter "FullyQualifiedName~HabitTests" > "$OUT.$m.log" 2>&1
  echo "$m: $(grep -E '^(失敗|成功|Failed|Passed)!|合計|失敗:' "$OUT.$m.log" | tail -2 | tr '\n' ' ') 赤のテスト: $(grep -E '^\s*(失敗|Failed) ' "$OUT.$m.log" | sed 's/ \[.*//' | tr '\n' ',' )" >> "$OUT"
done
python "$SP/mutate.py" "$TREE" reset
echo done >> "$OUT"
