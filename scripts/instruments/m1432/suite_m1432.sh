#!/usr/bin/env bash
# (1432) 合流の採った木のフルスイート（m1425/suite_m1425.sh の写し・走査の束はフルスイートが含む＝SKIPSCAN=1 の既定のまま）。
#   bash scripts/instruments/m1432/suite_m1432.sh <作業フォルダ（D:/…）>   ＝この台本のある木を隔離ビルド（--artifacts-path）で回す。結果は <作業フォルダ>/suite.txt
WK=${1:?作業フォルダ}; mkdir -p $WK/tmp $WK/trx
export TEMP=$WK/tmp TMP=$WK/tmp AVALONIA_TELEMETRY_OPTOUT=1
cd "$(dirname "$0")/../../.."; R=$(pwd); A=$WK/art; O=$WK/suite.txt
echo "# (1432) suite 木=$R HEAD=$(git rev-parse --short HEAD) $(date '+%F %T')" > $O
dotnet build PriceOfBread.Tests -c Release --artifacts-path $A -v q -nologo > $WK/build_t.txt 2>&1 || { echo "BUILDFAIL Tests" >> $O; exit 1; }
dotnet build PriceOfBread.Tests.Ui -c Release --artifacts-path $A -v q -nologo > $WK/build_u.txt 2>&1 || { echo "BUILDFAIL Tests.Ui" >> $O; exit 1; }
LT=$(dotnet test PriceOfBread.Tests -c Release --artifacts-path $A --no-build --list-tests --filter "Category!=FullLength" 2>/dev/null | grep -c "^    ")
LF=$(dotnet test PriceOfBread.Tests -c Release --artifacts-path $A --no-build --list-tests --filter "Category=FullLength" 2>/dev/null | grep -c "^    ")
LU=$(dotnet test PriceOfBread.Tests.Ui -c Release --artifacts-path $A --no-build --list-tests 2>/dev/null | grep -c "^    ")
echo "list-tests Tests(Category!=FullLength)=$LT Tests.Ui=$LU 和=$((LT+LU)) FullLength=$LF" >> $O
if [ "${SKIPSCAN:-1}" != 1 ]; then dotnet test PriceOfBread.Tests -c Release --artifacts-path $A --no-build --filter "FullyQualifiedName~IndustrializationReaderTests|FullyQualifiedName~UnitTypeTests|FullyQualifiedName~InstrumentsTests|FullyQualifiedName~WriterRatchetTests|FullyQualifiedName~DormantIdentityTests|FullyQualifiedName~TechLevelRemovalTests|FullyQualifiedName~OnDoubleReadScanTests|FullyQualifiedName~NextCapitalHomeFirstTests|FullyQualifiedName~TerritoryTests|FullyQualifiedName~RevoltIgnitionTests" > $WK/t_scan.txt 2>&1; RS=$?
echo "㉘ 走査の番人＋T1・T2 rc=$RS $(grep -E '合計' $WK/t_scan.txt | tail -1)" >> $O
if [ $RS != 0 ]; then grep -E "^\s+失敗 " $WK/t_scan.txt >> $O; echo "STOP 走査の番人が赤" >> $O; exit 1; fi
fi   # SKIPSCAN=1 は走査の束を飛ばす（フルスイートが同じ試験を含む）
if [ "${SKIPINS:-0}" = 1 ]; then echo "㊴ は先に別に回した（SKIPINS=1）" > $WK/t_ins.txt; RC=0; else dotnet test PriceOfBread.Tests -c Release --artifacts-path $A --no-build --filter "FullyQualifiedName~Instrument|FullyQualifiedName~Famine|FullyQualifiedName~Fiscal|FullyQualifiedName~Military|FullyQualifiedName~Revolt|FullyQualifiedName~WriterRatchet|FullyQualifiedName~SecessionTreasuryShare" > $WK/t_ins.txt 2>&1; RC=$?; fi
echo "㊴ 計器の束 rc=$RC $(grep -E '合計' $WK/t_ins.txt | tail -1)" >> $O
if [ $RC != 0 ]; then grep -E "^\s+失敗 " $WK/t_ins.txt >> $O; echo "STOP 計器の束が赤" >> $O; exit 1; fi
dotnet test PriceOfBread.Tests -c Release --artifacts-path $A --no-build --filter "Category!=FullLength" --logger "trx;LogFileName=tests.trx" --results-directory $WK/trx > $WK/t_main.txt 2>&1; R1=$?
echo "Tests(Category!=FullLength) rc=$R1 $(grep -E '合計' $WK/t_main.txt | tail -1)" >> $O
grep -E "^\s+失敗 " $WK/t_main.txt >> $O
dotnet test PriceOfBread.Tests.Ui -c Release --artifacts-path $A --no-build --logger "trx;LogFileName=ui.trx" --results-directory $WK/trx > $WK/t_ui.txt 2>&1; R2=$?
echo "Tests.Ui rc=$R2 $(grep -E '合計' $WK/t_ui.txt | tail -1)" >> $O
grep -E "^\s+失敗 " $WK/t_ui.txt >> $O
if [ "${SKIPFULL:-0}" = 1 ]; then echo "SKIPFULL=1：同じ SimCore の木で先に回した" > $WK/t_full.txt; R3=0; else dotnet test PriceOfBread.Tests -c Release --artifacts-path $A --no-build --filter "Category=FullLength" > $WK/t_full.txt 2>&1; R3=$?; fi
echo "FullLength rc=$R3 $(grep -E '合計' $WK/t_full.txt | tail -1)" >> $O
grep -E "^\s+失敗 " $WK/t_full.txt >> $O
[ "${SKIPFULL:-0}" = 1 ] || POB_WORK=$WK SAVELEAK_SRC=$R bash $R/scripts/instruments/saveleak.sh > /dev/null 2>&1
echo "savetest: $(grep -h 'savetest' $WK/savetest.txt 2>/dev/null | tail -1)" >> $O
echo "leakhunt: $(tail -3 $WK/leakhunt.txt 2>/dev/null | tr '\n' ' ')" >> $O
echo "SUITEDONE $(date '+%F %T')" >> $O
[ $R1 = 0 ] && [ $R2 = 0 ] && [ $R3 = 0 ]
