#!/usr/bin/env bash
# (1357) c143sp ㉜ 旗 1 の木の絞った試験（腕 B の前）：差分で変えた名（StateProcureAsSold・Procure(・BuyUpkeepGood・LastExport・LastSold・StProc）を Tests/ で grep して当たったクラス＋㉘ の束＋Ui の当たった 3 クラス＋savetest・leakhunt。trx で赤を見る。
#   bash narrow_sp.sh <作業フォルダ（D:/…）>
WK=${1:?}; mkdir -p $WK/tmp $WK/trx
export TEMP=$WK/tmp TMP=$WK/tmp AVALONIA_TELEMETRY_OPTOUT=1 DOTNET_CLI_TELEMETRY_OPTOUT=1
cd D:/PoB_work/wt_c143sp; R=$(pwd); A=$WK/art; O=$WK/narrow.txt
echo "# (1357) narrow HEAD=$(git rev-parse --short HEAD) 汚れ=$(git status --porcelain | wc -l) $(date '+%F %T')" > $O
CL=$(grep -rlE "StateProcureAsSold|Procure\(|BuyUpkeepGood|LastExport|LastSold|StProc" PriceOfBread.Tests --include=*.cs | xargs -n1 basename | sed 's/\.cs$//' | sort -u)
CL="$CL IndustrializationReaderTests UnitTypeTests InstrumentsTests WriterRatchetTests DormantIdentityTests TechLevelRemovalTests OnDoubleReadScanTests"
F=$(for c in $(echo $CL | tr ' ' '\n' | sort -u); do printf 'FullyQualifiedName~%s.|' "$c"; done); F=${F%|}
echo "classes $(echo $CL | tr ' ' '\n' | sort -u | wc -l)" >> $O
dotnet build PriceOfBread.Tests -c Release --artifacts-path $A -v q -nologo > $WK/build_t.txt 2>&1 || { echo "BUILDFAIL Tests" >> $O; exit 1; }
dotnet build PriceOfBread.Tests.Ui -c Release --artifacts-path $A -v q -nologo > $WK/build_u.txt 2>&1 || { echo "BUILDFAIL Tests.Ui" >> $O; exit 1; }
dotnet test PriceOfBread.Tests -c Release --artifacts-path $A --no-build --filter "$F" --logger "trx;LogFileName=narrow.trx" --results-directory $WK/trx > $WK/t_narrow.txt 2>&1; R1=$?
echo "Tests narrow rc=$R1 $(grep -E '合計' $WK/t_narrow.txt | tail -1)" >> $O
grep -E "^\s+失敗 " $WK/t_narrow.txt >> $O
dotnet test PriceOfBread.Tests.Ui -c Release --artifacts-path $A --no-build --filter "FullyQualifiedName~LaborMarketFormTests|FullyQualifiedName~ProductionDiagTests|FullyQualifiedName~ProductionMarketTablesTests" --logger "trx;LogFileName=ui_narrow.trx" --results-directory $WK/trx > $WK/t_ui.txt 2>&1; R2=$?
echo "Ui narrow rc=$R2 $(grep -E '合計' $WK/t_ui.txt | tail -1)" >> $O
grep -E "^\s+失敗 " $WK/t_ui.txt >> $O
POB_WORK=$WK SAVELEAK_SRC=$R bash $R/scripts/instruments/saveleak.sh > /dev/null 2>&1
echo "savetest: $(grep -h 'savetest' $WK/savetest.txt 2>/dev/null | tail -1)" >> $O
echo "leakhunt: $(tail -3 $WK/leakhunt.txt 2>/dev/null | tr '\n' ' ')" >> $O
echo "NARROWDONE $(date '+%F %T')" >> $O
[ $R1 = 0 ] && [ $R2 = 0 ]
