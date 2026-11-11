#!/usr/bin/env bash
# (981) 合流（m975 の台本の写し・木と作業フォルダを m981 にした）
# (975) 合流（(968) suite968.sh の写し・作業フォルダだけ m981_work・番人の組に波87 の線の試験〔BasisxInstrument・MoTrendInit・MilitaryWarMultContact・CivilWarArmsDemand〕を足した）
# (968) 合流：c86lt/suite964.sh の写し（作業フォルダを D:/PoB_work/m968_work/suite だけ替えた）。(964) c86lt：m961/suite961.sh の写し（作業フォルダを D:/PoB_work/c86lt_work/suite・saveleak.sh は測る木の写しの物を使う・中身の並びは同じ）。
#   （波88 の線の試験 LandShareCrown・LandOwnershipData・SbBargain を番人の組に足した）
#   bash scripts/instruments/m981/suite981.sh <コミット> [full|guards|ratchet]
export PATH="/c/Program Files/dotnet:/usr/bin:/c/Program Files/Git/usr/bin:$PATH" AVALONIA_TELEMETRY_OPTOUT=1
H=${1:?コミット}; MODE=${2:-full}; W=D:/PoB_work/m981_work/suite; S=$W/suitesrc
rm -rf $S && mkdir -p $S && (cd C:/Users/dirge/Documents/PriceOfBread && git archive $H) | tar -x -C $S || exit 1
cd $S
dotnet build PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path $W/art_suite --disable-build-servers > $W/suite_build.txt 2>&1 || { echo BUILDFAIL; tail -20 $W/suite_build.txt; exit 1; }
for f in "Category!=FullLength:nonfull" "Category=FullLength:full"; do
  dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $W/art_suite --list-tests --filter "${f%%:*}" > $W/list_${f##*:}.txt 2>&1
  echo "list ${f##*:} $(grep -c '^    ' $W/list_${f##*:}.txt)"
done
if [ "$MODE" = ratchet ]; then
  T0=$(date +%s); dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $W/art_suite --filter "FullyQualifiedName~WriterRatchetTests" > $W/ratchet.txt 2>&1; echo "ratchet rc=$? $(( $(date +%s) - T0 ))s"
  grep -h "合計\|中止\|Total\|Passed!\|Failed!\|\[FAIL\]" $W/ratchet.txt | tail -12
  echo RATCHETDONE; exit 0
fi
if [ "$MODE" = guards ]; then
  F="FullyQualifiedName~Instrument|FullyQualifiedName~BasisxInstrumentTests|FullyQualifiedName~MoTrendInitTests|FullyQualifiedName~MilitaryWarMultContactTests|FullyQualifiedName~CivilWarArmsDemandTests|FullyQualifiedName~MilitaryTests|FullyQualifiedName~RuleInstrumentTests|FullyQualifiedName~IndustrializationReaderTests|FullyQualifiedName~UnitTypeTests|FullyQualifiedName~InstrumentsTests|FullyQualifiedName~WriterRatchetTests|FullyQualifiedName~DormantIdentityTests|FullyQualifiedName~GoodIdGuardTests|FullyQualifiedName~CapitalCouponWriteDownTests|FullyQualifiedName~CearnInstrumentTests|FullyQualifiedName~SilverParityDemandWeightedTests|FullyQualifiedName~SilverstInstrumentTests|FullyQualifiedName~SilverParityInstrumentTests|FullyQualifiedName~MultiNationTests|FullyQualifiedName~CoefficientRelationTests|FullyQualifiedName~WarVerdictTests|FullyQualifiedName~HabitTests|FullyQualifiedName~HabitLogisticTests|FullyQualifiedName~NavalCedeFleetReturnTests|FullyQualifiedName~DiscoveryEngineerTests|FullyQualifiedName~MilitaryTests|FullyQualifiedName~FiscalTests|FullyQualifiedName~SubstInstrumentTests|FullyQualifiedName~CornintInstrumentTests|FullyQualifiedName~CollapsePeaceTests|FullyQualifiedName~ForcedPeaceBlankTests|FullyQualifiedName~RuleInstrumentTests|FullyQualifiedName~TechLevelRemovalTests|FullyQualifiedName~FullChainTests|FullyQualifiedName~InstrumentsMonTests|FullyQualifiedName~TreasuryLedgerTests|FullyQualifiedName~LaborClosedTests|FullyQualifiedName~ServiceClosedTests|FullyQualifiedName~StateEduDemandTests|FullyQualifiedName~EmptyPopEstateTests|FullyQualifiedName~SubsistenceStage2Tests|FullyQualifiedName~FoodLedgerTests|FullyQualifiedName~UrbanFlowRealTests|FullyQualifiedName~IdeaNeedBoardTests|FullyQualifiedName~RuleOrderP1Tests|FullyQualifiedName~CrimeTests|FullyQualifiedName~RuleSharesFormTests|FullyQualifiedName~FuelOperatingCostTests|FullyQualifiedName~CearnTradeColumnsTests|FullyQualifiedName~SubsistenceNoBoughtTechTests|FullyQualifiedName~TradeClearingTests|FullyQualifiedName~TradeClearingBoardTests|FullyQualifiedName~ImportPayerLiveTests|FullyQualifiedName~RouteKmTests|FullyQualifiedName~CedeRuleInstrumentTests|FullyQualifiedName~CrownPcInstrumentTests|FullyQualifiedName~LandbaseInstrumentTests|FullyQualifiedName~LandShareTests|FullyQualifiedName~SummonByInterestShareTests|FullyQualifiedName~CedeRuleToLocalTests|FullyQualifiedName~CivilWarArmsDemandTests|FullyQualifiedName~OpeningColonyDominanceTests|FullyQualifiedName~OpeningProvinceHeldTests|FullyQualifiedName~LandTaxFixedBasisTests|FullyQualifiedName~LandfixInstrumentTests|FullyQualifiedName~SecessionLaborLawInstrumentTests|FullyQualifiedName~OpeningBakeTests|FullyQualifiedName~LandShareCrownTests|FullyQualifiedName~LandOwnershipDataTests|FullyQualifiedName~SbBargainTests|FullyQualifiedName~CornintInstrumentTests"
  T0=$(date +%s); dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $W/art_suite --filter "$F" > $W/guards.txt 2>&1; echo "guards rc=$? $(( $(date +%s) - T0 ))s"
  grep -h "合計\|中止\|Total\|Passed!\|Failed!\|\[FAIL\]" $W/guards.txt | tail -12
  echo GUARDSDONE; exit 0
fi
T0=$(date +%s); dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $W/art_suite --filter "Category!=FullLength" > $W/suite_nonfull.txt 2>&1; echo "nonfull rc=$? $(( $(date +%s) - T0 ))s"
T0=$(date +%s); dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $W/art_suite --filter "Category=FullLength" > $W/suite_full.txt 2>&1; echo "full rc=$? $(( $(date +%s) - T0 ))s"
grep -h "合計\|中止\|Total tests\|aborted\|\[FAIL\]" $W/suite_nonfull.txt $W/suite_full.txt | tail -8
grep -h -A2 "^\s*失敗 " $W/suite_nonfull.txt $W/suite_full.txt | head -40
echo SUITEDONE
T0=$(date +%s); POB_WORK=$W SAVELEAK_SRC=$S bash $S/scripts/instruments/saveleak.sh > $W/saveleak.txt 2>&1; echo "saveleak rc=$? $(( $(date +%s) - T0 ))s"
tail -3 $W/savetest.txt; tail -3 $W/leakhunt.txt; tail -2 $W/saveleak.log
echo SAVELEAKDONE
