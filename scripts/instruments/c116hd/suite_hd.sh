#!/usr/bin/env bash
# (1174) c116hd 旗 1 の木の試験（c115sv/suite_sv.sh の写し・㊴ 計器の束→㉜ フルスイート〔2 プロジェクト・trx・--list-tests の和〕→FullLength→savetest・leakhunt）。
#   bash scripts/instruments/c116hd/suite_hd.sh <作業フォルダ（D:/…）>   ＝この台本のある木を隔離ビルド（--artifacts-path）で回す。結果は <作業フォルダ>/suite.txt
WK=${1:?作業フォルダ}; mkdir -p $WK/tmp $WK/trx
export TEMP=$WK/tmp TMP=$WK/tmp AVALONIA_TELEMETRY_OPTOUT=1
cd "$(dirname "$0")/../../.."; R=$(pwd); A=$WK/art; O=$WK/suite.txt
echo "# (1174) suite 木=$R HEAD=$(git rev-parse --short HEAD) $(date '+%F %T')" > $O
dotnet build PriceOfBread.Tests -c Release --artifacts-path $A -v q -nologo > $WK/build_t.txt 2>&1 || { echo "BUILDFAIL Tests" >> $O; exit 1; }
dotnet build PriceOfBread.Tests.Ui -c Release --artifacts-path $A -v q -nologo > $WK/build_u.txt 2>&1 || { echo "BUILDFAIL Tests.Ui" >> $O; exit 1; }
LT=$(dotnet test PriceOfBread.Tests -c Release --artifacts-path $A --no-build --list-tests --filter "Category!=FullLength" 2>/dev/null | grep -c "^    ")
LF=$(dotnet test PriceOfBread.Tests -c Release --artifacts-path $A --no-build --list-tests --filter "Category=FullLength" 2>/dev/null | grep -c "^    ")
LU=$(dotnet test PriceOfBread.Tests.Ui -c Release --artifacts-path $A --no-build --list-tests 2>/dev/null | grep -c "^    ")
echo "list-tests Tests(Category!=FullLength)=$LT Tests.Ui=$LU 和=$((LT+LU)) FullLength=$LF" >> $O
dotnet test PriceOfBread.Tests -c Release --artifacts-path $A --no-build --filter "FullyQualifiedName~Instrument" > $WK/t_ins.txt 2>&1; RC=$?
echo "㊴ 計器の束 rc=$RC $(grep -E '合計' $WK/t_ins.txt | tail -1)" >> $O
if [ $RC != 0 ]; then grep -E "^\s+失敗 " $WK/t_ins.txt >> $O; echo "STOP 計器の束が赤" >> $O; exit 1; fi
dotnet test PriceOfBread.Tests -c Release --artifacts-path $A --no-build --filter "Category!=FullLength" --logger "trx;LogFileName=tests.trx" --results-directory $WK/trx > $WK/t_main.txt 2>&1; R1=$?
echo "Tests(Category!=FullLength) rc=$R1 $(grep -E '合計' $WK/t_main.txt | tail -1)" >> $O
grep -E "^\s+失敗 " $WK/t_main.txt >> $O
dotnet test PriceOfBread.Tests.Ui -c Release --artifacts-path $A --no-build --logger "trx;LogFileName=ui.trx" --results-directory $WK/trx > $WK/t_ui.txt 2>&1; R2=$?
echo "Tests.Ui rc=$R2 $(grep -E '合計' $WK/t_ui.txt | tail -1)" >> $O
grep -E "^\s+失敗 " $WK/t_ui.txt >> $O
dotnet test PriceOfBread.Tests -c Release --artifacts-path $A --no-build --filter "Category=FullLength" > $WK/t_full.txt 2>&1; R3=$?
echo "FullLength rc=$R3 $(grep -E '合計' $WK/t_full.txt | tail -1)" >> $O
grep -E "^\s+失敗 " $WK/t_full.txt >> $O
POB_WORK=$WK SAVELEAK_SRC=$R bash $R/scripts/instruments/saveleak.sh > /dev/null 2>&1
echo "savetest: $(grep -h 'savetest' $WK/savetest.txt 2>/dev/null | tail -1)" >> $O
echo "leakhunt: $(tail -3 $WK/leakhunt.txt 2>/dev/null | tr '\n' ' ')" >> $O
echo "SUITEDONE $(date '+%F %T')" >> $O
[ $R1 = 0 ] && [ $R2 = 0 ] && [ $R3 = 0 ]
