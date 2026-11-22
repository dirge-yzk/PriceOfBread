#!/usr/bin/env bash
# (1304) c135nc 旗 1 の木の試験の残り（suite_nc.sh の ㊴ の束が緑で終わった後に、束と重ならない残りだけを回す＝束を二度回さない・効率化）。
#   Tests(Category!=FullLength) の母集合＝㊴ の束（suite_nc.sh の filter）＋残り（この台本の filter）＝和を --list-tests と突き合わせる。Tests.Ui・FullLength・savetest・leakhunt は suite_nc.sh と同じ。
#   bash scripts/instruments/c135nc/suite2_nc.sh <作業フォルダ（suite_nc.sh と同じ・art を使う）>
WK=${1:?作業フォルダ}; mkdir -p $WK/tmp $WK/trx
export TEMP=$WK/tmp TMP=$WK/tmp AVALONIA_TELEMETRY_OPTOUT=1
cd "$(dirname "$0")/../../.."; R=$(pwd); A=$WK/art; O=$WK/suite2.txt
B="FullyQualifiedName~Instrument|FullyQualifiedName~Famine|FullyQualifiedName~Fiscal|FullyQualifiedName~Military|FullyQualifiedName~Revolt|FullyQualifiedName~WriterRatchet|FullyQualifiedName~NextCapitalHomeFirst"
C="Category!=FullLength&FullyQualifiedName!~Instrument&FullyQualifiedName!~Famine&FullyQualifiedName!~Fiscal&FullyQualifiedName!~Military&FullyQualifiedName!~Revolt&FullyQualifiedName!~WriterRatchet&FullyQualifiedName!~NextCapitalHomeFirst"
echo "# (1304) suite2 木=$R HEAD=$(git rev-parse --short HEAD) $(date '+%F %T')" > $O
LT=$(dotnet test PriceOfBread.Tests -c Release --artifacts-path $A --no-build --list-tests --filter "Category!=FullLength" 2>/dev/null | grep -c "^    ")
LB=$(dotnet test PriceOfBread.Tests -c Release --artifacts-path $A --no-build --list-tests --filter "$B" 2>/dev/null | grep -c "^    ")
LC=$(dotnet test PriceOfBread.Tests -c Release --artifacts-path $A --no-build --list-tests --filter "$C" 2>/dev/null | grep -c "^    ")
LBF=$(dotnet test PriceOfBread.Tests -c Release --artifacts-path $A --no-build --list-tests --filter "Category=FullLength&($B)" 2>/dev/null | grep -c "^    ")
echo "list-tests Category!=FullLength=$LT 束=$LB（うち FullLength $LBF） 残り=$LC 束−束の FullLength＋残り=$((LB-LBF+LC))" >> $O
dotnet test PriceOfBread.Tests -c Release --artifacts-path $A --no-build --filter "$C" --logger "trx;LogFileName=tests_rest.trx" --results-directory $WK/trx > $WK/t_rest.txt 2>&1; R1=$?
echo "Tests(残り) rc=$R1 $(grep -E '合計' $WK/t_rest.txt | tail -1)" >> $O
grep -E "^\s+失敗 " $WK/t_rest.txt >> $O
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
