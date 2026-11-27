#!/usr/bin/env bash
# (1423) c152ms：旗 1 の木の絞った試験（㉜：差分で変えた名 MilitarismStrengthBonus・Quality・militarismMul・ArmyProfessionalism を Tests/ で grep して当たったクラス＋新しい試験＋指示の束＋㉘ の束）と savetest・leakhunt。
#   bash narrow.sh <作業フォルダ（D:/…）> <名>
WK=${1:?作業フォルダ}; N=${2:?名}; mkdir -p $WK/tmp
export TEMP=$WK/tmp TMP=$WK/tmp
cd "$(dirname "$0")/../../.."; A=$WK/art
dotnet build PriceOfBread.Tests -c Release --artifacts-path $A -v q -nologo > $WK/build_$N.txt 2>&1 || { echo BUILDFAIL; exit 1; }
C="MilitarismBonusRemovalTests InstrumentsArmsTests TechLevelRemovalTests HistoricalReferenceTests MilitaryTests MilitaryWarMultContactTests WarInstrumentTests DormantIdentityTests IndustrializationReaderTests UnitTypeTests InstrumentsTests WriterRatchetTests ValueCountRatchetTests DerivedValueRatchetTests"
F=""; for c in $C; do F="$F${F:+|}FullyQualifiedName~$c"; done
dotnet test PriceOfBread.Tests -c Release --artifacts-path $A --no-build --filter "$F" --logger "trx;LogFileName=$N.trx" --results-directory $WK/trx > $WK/t_$N.txt 2>&1
RC=$?; grep -E "合計|失敗 |Failed |成功" $WK/t_$N.txt | head -20
if [ "${NOSAVE:-}" != 1 ]; then
  dotnet build PriceOfBread.Runner -c Release --artifacts-path $A -v q -nologo > $WK/buildr_$N.txt 2>&1 || { echo RUNNERBUILDFAIL; exit 1; }
  R=$(ls $A/bin/PriceOfBread.Runner/release/PriceOfBread.Runner.exe 2>/dev/null | head -1)
  "$R" savetest > $WK/savetest_$N.txt 2>&1; echo "savetest rc=$? $(tail -1 $WK/savetest_$N.txt)"
  "$R" leakhunt > $WK/leakhunt_$N.txt 2>&1; echo "leakhunt rc=$? $(tail -1 $WK/leakhunt_$N.txt)"
fi
exit $RC
