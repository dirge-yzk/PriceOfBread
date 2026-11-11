#!/usr/bin/env bash
# (826)（(818) の写し・作業フォルダだけ m826・git archive の元は wt_m826） フルスイート（Release・Category!=FullLength と =FullLength を分けて回す）。10 種のバッチと並べない（README §1-2）。
#   bash suite826.sh <コミット> [guards]   guards を付けると ㉘ の走査の番人と線の試験の組だけを回す（--list-tests も数える）
export PATH="/c/Program Files/dotnet:$PATH" AVALONIA_TELEMETRY_OPTOUT=1
H=${1:?コミット}; MODE=${2:-full}; W=D:/PoB_work/m826; S=$W/suitesrc
rm -rf $S && mkdir -p $S && (cd D:/PoB_work/wt_m826 && git archive $H) | tar -x -C $S || exit 1
cd $S
dotnet build PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path $W/art_suite --disable-build-servers > $W/suite_build.txt 2>&1 || { echo BUILDFAIL; tail -20 $W/suite_build.txt; exit 1; }
for f in "Category!=FullLength:nonfull" "Category=FullLength:full"; do
  dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $W/art_suite --list-tests --filter "${f%%:*}" > $W/list_${f##*:}.txt 2>&1
  echo "list ${f##*:} $(grep -c '^    ' $W/list_${f##*:}.txt)"
done
if [ "$MODE" = guards ]; then
  F="FullyQualifiedName~IndustrializationReaderTests|FullyQualifiedName~UnitTypeTests|FullyQualifiedName~InstrumentsTests|FullyQualifiedName~WriterRatchetTests|FullyQualifiedName~DormantIdentityTests|FullyQualifiedName~GoodIdGuardTests|FullyQualifiedName~FiscalTests|FullyQualifiedName~CapitalCouponWriteDownTests|FullyQualifiedName~HabitLogisticTests|FullyQualifiedName~HabitTests|FullyQualifiedName~CearnInstrumentTests|FullyQualifiedName~SilverstInstrumentTests|FullyQualifiedName~ForcedPeaceBlankTests|FullyQualifiedName~CollapsePeaceTests|FullyQualifiedName~NavalPortProcurementTests|FullyQualifiedName~DiscoveryEngineerTests|FullyQualifiedName~TechLevelRemovalTests|FullyQualifiedName~InstitutionAnchorTests|FullyQualifiedName~MilitaryTests"
  T0=$(date +%s); dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $W/art_suite --filter "$F" > $W/guards.txt 2>&1; echo "guards rc=$? $(( $(date +%s) - T0 ))s"
  grep -h "合計\|成功\|失敗\|中止\|Total\|Passed!\|Failed!\|\[FAIL\]" $W/guards.txt | tail -12
  echo GUARDSDONE; exit 0
fi
T0=$(date +%s); dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $W/art_suite --filter "Category!=FullLength" > $W/suite_nonfull.txt 2>&1; echo "nonfull rc=$? $(( $(date +%s) - T0 ))s"
T0=$(date +%s); dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $W/art_suite --filter "Category=FullLength" > $W/suite_full.txt 2>&1; echo "full rc=$? $(( $(date +%s) - T0 ))s"
grep -h "合計\|中止\|Total tests\|aborted\|\[FAIL\]" $W/suite_nonfull.txt $W/suite_full.txt | tail -8
echo SUITEDONE
