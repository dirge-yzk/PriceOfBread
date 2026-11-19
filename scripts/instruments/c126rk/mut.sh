#!/usr/bin/env bash
# (1241) c126rk 突然変異 5 本（使い捨ての木で 1 本ずつ適用→ビルド→RkInstrumentTests→結果を書く→戻す）。使い方：bash mut.sh <使い捨ての木> <出力>
T="$1"; OUT="$2"; : > "$OUT"
run() {   # run <名前> <ファイル> <旧> <新>
  local N="$1" F="$2"
  python -I -c "
import sys
p,o,n=sys.argv[1:4]
b=open(p,'rb').read().decode('utf-8')
assert b.count(o)==1, 'count=%d'%b.count(o)
open(p,'wb').write(b.replace(o,n).encode('utf-8'))" "$T/$F" "$3" "$4" || { echo "$N: 適用失敗" >> "$OUT"; return; }
  dotnet build "$T/PriceOfBread.Tests/PriceOfBread.Tests.csproj" -c Release --artifacts-path D:/PoB_work/c126rk_mutart --disable-build-servers > "$OUT.$N.build.txt" 2>&1
  R=$(dotnet test "$T/PriceOfBread.Tests/PriceOfBread.Tests.csproj" -c Release --no-build --artifacts-path D:/PoB_work/c126rk_mutart --filter "FullyQualifiedName~RkInstrumentTests" 2>&1)
  echo "$R" > "$OUT.$N.test.txt"
  echo "$N: $(echo "$R" | grep -E '^\s*(失敗|Failed) ' | cut -d'[' -f1 | cut -c1-100 | tr '\n' ';') | $(echo "$R" | grep -E '合計:' | cut -c1-80)" >> "$OUT"
  git -C "$T" checkout -- "$F"
}
I=PriceOfBread.SimCore/Instruments
run mu1 $I/InstrumentsPaysale.cs 'PaysaleColNames);' 'PaysaleColNames.Where(x => x != "diffAll"));'
run mu2 $I/InstrumentsPaysale.cs 'PaysaleStake(stateUnits, privateUnits) * diff;' 'diff;'
run mu3 $I/InstrumentsRk.cs 'snap.TerrCost[a] = HorseCostOf(snap.Tr[a].r, c); snap.TerrCost[b] = HorseCostOf(snap.Tr[b].r, c);' ''
run mu4 $I/InstrumentsRk.cs 'double owner = snap.Get(a, b);' 'double owner = snap.Get(a, b) * (1.0 - 0.5 * (1.0 - 0.5));'
run mu5 $I/InstrumentsRulePower.cs 'if (!On || !firstClearingOfQuarter) return;' 'if (!firstClearingOfQuarter) return;'
echo done >> "$OUT"
