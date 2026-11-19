#!/usr/bin/env bash
# (1235) c125rk 突然変異 5 本（使い捨ての木で 1 本ずつ適用→ビルド→RkInstrumentTests→結果を書く→戻す）。使い方：bash mut.sh <使い捨ての木> <出力>
T="$1"; OUT="$2"; ED="python -I /d/PoB_work/c125rk_work/ed.py"; : > "$OUT"
run() {   # run <名前> <ファイル> <旧> <新>
  local N="$1" F="$2"
  printf '<<<<\n%s\n====\n%s\n>>>>\n' "$3" "$4" | $ED "$T/$F" > /dev/null || { echo "$N: 適用失敗" >> "$OUT"; return; }
  dotnet build "$T/PriceOfBread.Tests/PriceOfBread.Tests.csproj" -c Release --artifacts-path D:/PoB_work/c125rk_mutart --disable-build-servers > "$OUT.$N.build.txt" 2>&1
  R=$(dotnet test "$T/PriceOfBread.Tests/PriceOfBread.Tests.csproj" -c Release --no-build --artifacts-path D:/PoB_work/c125rk_mutart --filter "FullyQualifiedName~RkInstrumentTests" 2>&1)
  echo "$R" > "$OUT.$N.test.txt"
  echo "$N: $(echo "$R" | grep -E '^\s*(失敗|Failed) ' | sed 's/ \[.*//' | cut -c1-100 | tr '\n' ';') | $(echo "$R" | grep -E '合計:' | cut -c1-80)" >> "$OUT"
  git -C "$T" checkout -- "$F"
}
run mu1 PriceOfBread.SimCore/Instruments/InstrumentsRk.cs 'return f / (f + nominalGdpPerYear);' 'return f / nominalGdpPerYear;'
run mu2 PriceOfBread.SimCore/Instruments/InstrumentsRk.cs 'return RulePowerReach(km * (1.0 - cover * (1.0 - fr)));' 'return RulePowerReach(km * (1.0 + cover * (1.0 - fr)));'
run mu3 PriceOfBread.SimCore/Instruments/InstrumentsRulePower.cs 'if (!On || !firstClearingOfQuarter) return;' 'if (!firstClearingOfQuarter) return;'
run mu4 PriceOfBread.SimCore/Instruments/InstrumentsPaysale.cs 'if (!(q % 4 == 3 || IsPicked(w, S(w)))) continue;' '// 門を外した突然変異'
run mu5 PriceOfBread.SimCore/Instruments/InstrumentsRk.cs '(e.Score > 0 ? (b.A, b.B) : (b.B, b.A))' '(e.Score > 0 ? (b.B, b.A) : (b.A, b.B))'
echo done >> "$OUT"
