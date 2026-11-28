#!/usr/bin/env bash
# (1435) c154sd ㉜ の改めの絞った試験（旗 1 の木・腕 B の前）：差分で変えた名（FactionFloorByRegion・FactionStrengthByMilitancy・SatReadersToDiscontent・FightHeads・
#   FactionStrength・MinSizeFraction・AvgMilitancy・ComputeClusters・PrdsCount・facfloor2_s）を Tests/ で grep して当たったクラス＋㉘ の束＋㊴ の計器の束＋FullLength＋Tests.Ui の 3 クラス＋savetest・leakhunt。
#   bash narrow.sh <作業フォルダ（D:/…）> <名>   結果は <作業フォルダ>/narrow_<名>.txt
WK=${1:?作業フォルダ}; N=${2:?名}; mkdir -p $WK/tmp $WK/trx
export TEMP=$WK/tmp TMP=$WK/tmp AVALONIA_TELEMETRY_OPTOUT=1
D0="$(cd "$(dirname "$0")" && pwd)"; cd "$D0/../../.."; R=$(pwd); A=$WK/art; O=$WK/narrow_$N.txt
C="${CLASSES:-FactionFloorRegionTests SatReadersToDiscontentTests RevoltIgnitionTests PrdsShadowTests PoliticsUnificationC148poTests DiscontentReaderTests PartyTests WarThemeTests LawstepInstrumentTests AiFirstWaveTests SbBargainTests SaveRoundTripTests
CreditTests DiscontentSplitTests FiscalTests IdeaNeedBoardTests PolicyPoliticsTests UnderconsumptionTests InstrumentHookClosureTests InstrumentColumnTests
IndustrializationReaderTests UnitTypeTests InstrumentsTests WriterRatchetTests DormantIdentityTests DerivedValueRatchetTests ValueCountRatchetTests Instrument}"
F=$(for c in $C; do printf 'FullyQualifiedName~%s|' $c; done); F=${F%|}
echo "# (1435) narrow $N HEAD=$(git rev-parse --short HEAD) 汚れ=$(git status --porcelain | wc -l) $(date '+%F %T')" > $O
dotnet build PriceOfBread.Tests -c Release --artifacts-path $A -v q -nologo > $WK/build_n$N.txt 2>&1 || { echo "BUILDFAIL" >> $O; exit 1; }
dotnet test PriceOfBread.Tests -c Release --artifacts-path $A --no-build --filter "$F" --logger "trx;LogFileName=n_$N.trx" --results-directory $WK/trx > $WK/t_n$N.txt 2>&1; R1=$?
echo "Tests 絞り rc=$R1 $(grep -E '合計' $WK/t_n$N.txt | tail -1)" >> $O
grep -E "^\s+失敗 " $WK/t_n$N.txt >> $O
if [ "${NOFULL:-0}" = 1 ]; then echo "NOFULL=1：FullLength は腕 B と並べない＝フルスイートで回す" > $WK/t_nf$N.txt; R3=0; else dotnet test PriceOfBread.Tests -c Release --artifacts-path $A --no-build --filter "Category=FullLength" --logger "trx;LogFileName=nf_$N.trx" --results-directory $WK/trx > $WK/t_nf$N.txt 2>&1; R3=$?; fi
echo "FullLength rc=$R3 $(grep -E '合計' $WK/t_nf$N.txt | tail -1)" >> $O
grep -E "^\s+失敗 " $WK/t_nf$N.txt >> $O
dotnet build PriceOfBread.Tests.Ui -c Release --artifacts-path $A -v q -nologo > $WK/build_u$N.txt 2>&1
dotnet test PriceOfBread.Tests.Ui -c Release --artifacts-path $A --no-build --filter "FullyQualifiedName~PlayHarnessTests|FullyQualifiedName~LaborMarketFormTests|FullyQualifiedName~ObservationFixTests" --logger "trx;LogFileName=nu_$N.trx" --results-directory $WK/trx > $WK/t_nu$N.txt 2>&1; R4=$?
echo "Ui 3 クラス rc=$R4 $(grep -E '合計' $WK/t_nu$N.txt | tail -1)" >> $O
grep -E "^\s+失敗 " $WK/t_nu$N.txt >> $O
dotnet build PriceOfBread.Runner -c Release --artifacts-path $A -v q -nologo > $WK/build_r$N.txt 2>&1 || { echo RUNNERBUILDFAIL >> $O; exit 1; }
RX=$(ls $A/bin/PriceOfBread.Runner/release/PriceOfBread.Runner.exe 2>/dev/null | head -1)
"$RX" savetest > $WK/savetest_$N.txt 2>&1; echo "savetest rc=$? $(tail -1 $WK/savetest_$N.txt)" >> $O
"$RX" leakhunt > $WK/leakhunt_$N.txt 2>&1; echo "leakhunt rc=$? $(tail -1 $WK/leakhunt_$N.txt)" >> $O
echo "NARROWDONE $(date '+%F %T')" >> $O
[ $R1 = 0 ] && [ $R3 = 0 ] && [ $R4 = 0 ]
