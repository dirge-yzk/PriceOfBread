#!/usr/bin/env bash
# (1302) 突然変異（㉘ は scan28.txt に済み） Ma・Mb・Mc・Md（出力は measurements/m1302_20261122/）
cd D:/PoB_work/wt_c135tl || exit 2
OUT=measurements/m1302_20261122
ART=D:/PoB_work/c135tl_art
T() { dotnet test PriceOfBread.Tests -c Release --artifacts-path $ART --filter "$1" 2>&1 | grep -E "^\s*失敗 |成功!|失敗!" | cut -c1-200; }
F=PriceOfBread.SimCore/Instruments/InstrumentsTreasLedger.cs
M=scripts/instruments/c135tl/mutate.py
: > $OUT/mutations.txt
run() { name=$1; shift; python $M "$@"; echo "== $name" >> $OUT/mutations.txt; T "FullyQualifiedName~TreasLedgerInstrumentTests" >> $OUT/mutations.txt; git checkout -- $F PriceOfBread.SimCore/Instruments/Instruments.cs; }
run "Ma _bornTreasury を書かない" $F 'if (!s.Have) rows.Add(head + "_bornTreasury' 'if (false) rows.Add(head + "_bornTreasury'
run "Mb Peek を空にしない" $F 'sk.Peek.Clear();' ';'
run "Mc LedgerStep の符号を逆にする" $F 'double x = led[k] - prev[k];' 'double x = prev[k] - led[k];'
echo done >> $OUT/mutations.txt
touch D:/PoB_work/c135tl_work/mut.mark
