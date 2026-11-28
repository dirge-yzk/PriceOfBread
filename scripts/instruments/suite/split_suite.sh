#!/usr/bin/env bash
# (1426) c153tp：フルスイートの Category!=FullLength を重い組 H と残り R の 2 本の dotnet test に分けて同時に流す（suite_m1425.sh と同じ順・同じ口）。
#   bash scripts/instruments/suite/split_suite.sh <作業フォルダ（D:/…）>        結果は <作業フォルダ>/suite.txt・trx は <作業フォルダ>/trx（tests_H.trx・tests_R.trx・ui.trx）
#   口：SKIPINS=1（㊴ の束を飛ばす）・SKIPSCAN=0（走査の束を先に回す）・SKIPFULL=1（FullLength と savetest・leakhunt を飛ばす）・FILTERS_SH=<filters.sh の代わり>・ART=<既にビルドした artifacts>（無ければ <作業フォルダ>/art）
# H と R は --no-build で artifacts を共有し、TEMP・TMP と --results-directory を分ける（Instruments の静的な状態はプロセスごと）。
WK=${1:?作業フォルダ}; mkdir -p $WK/tmpH $WK/tmpR $WK/tmp $WK/trx
export AVALONIA_TELEMETRY_OPTOUT=1 TEMP=$WK/tmp TMP=$WK/tmp
cd "$(dirname "$0")/../../.."; R=$(pwd); A=${ART:-$WK/art}; O=$WK/suite.txt
. "${FILTERS_SH:-$R/scripts/instruments/suite/filters.sh}"   # FILTERS_SH＝突然変異の台本が差し替える口
echo "# (1426) split suite 木=$R HEAD=$(git rev-parse --short HEAD) $(date '+%F %T')" > $O
dotnet build PriceOfBread.Tests -c Release --artifacts-path $A -v q -nologo > $WK/build_t.txt 2>&1 || { echo "BUILDFAIL Tests" >> $O; exit 1; }
dotnet build PriceOfBread.Tests.Ui -c Release --artifacts-path $A -v q -nologo > $WK/build_u.txt 2>&1 || { echo "BUILDFAIL Tests.Ui" >> $O; exit 1; }
LT=$(dotnet test PriceOfBread.Tests -c Release --artifacts-path $A --no-build --list-tests --filter "Category!=FullLength" 2>/dev/null | grep -c "^    ")
LH=$(dotnet test PriceOfBread.Tests -c Release --artifacts-path $A --no-build --list-tests --filter "$FILTER_H" 2>/dev/null | grep -c "^    ")
LR=$(dotnet test PriceOfBread.Tests -c Release --artifacts-path $A --no-build --list-tests --filter "$FILTER_R" 2>/dev/null | grep -c "^    ")
LF=$(dotnet test PriceOfBread.Tests -c Release --artifacts-path $A --no-build --list-tests --filter "Category=FullLength" 2>/dev/null | grep -c "^    ")
LU=$(dotnet test PriceOfBread.Tests.Ui -c Release --artifacts-path $A --no-build --list-tests 2>/dev/null | grep -c "^    ")
echo "list-tests Tests(Category!=FullLength)=$LT H=$LH R=$LR H+R=$((LH+LR)) Tests.Ui=$LU 和=$((LT+LU)) FullLength=$LF" >> $O
if [ $((LH+LR)) != $LT ] || [ $LH = 0 ]; then echo "STOP H+R が list-tests と合わない（または H が空）" >> $O; exit 1; fi
if [ "${SKIPSCAN:-1}" != 1 ]; then dotnet test PriceOfBread.Tests -c Release --artifacts-path $A --no-build --filter "FullyQualifiedName~IndustrializationReaderTests|FullyQualifiedName~UnitTypeTests|FullyQualifiedName~InstrumentsTests|FullyQualifiedName~WriterRatchetTests|FullyQualifiedName~DormantIdentityTests|FullyQualifiedName~TechLevelRemovalTests|FullyQualifiedName~OnDoubleReadScanTests|FullyQualifiedName~NextCapitalHomeFirstTests|FullyQualifiedName~TerritoryTests|FullyQualifiedName~RevoltIgnitionTests" > $WK/t_scan.txt 2>&1; RS=$?
echo "㉘ 走査の番人＋T1・T2 rc=$RS $(grep -E '合計' $WK/t_scan.txt | tail -1)" >> $O
if [ $RS != 0 ]; then grep -E "^\s+失敗 " $WK/t_scan.txt >> $O; echo "STOP 走査の番人が赤" >> $O; exit 1; fi
fi
if [ "${SKIPINS:-0}" = 1 ]; then echo "㊴ は先に別に回した（SKIPINS=1）" > $WK/t_ins.txt; RC=0; else dotnet test PriceOfBread.Tests -c Release --artifacts-path $A --no-build --filter "FullyQualifiedName~Instrument|FullyQualifiedName~Famine|FullyQualifiedName~Fiscal|FullyQualifiedName~Military|FullyQualifiedName~Revolt|FullyQualifiedName~WriterRatchet|FullyQualifiedName~SecessionTreasuryShare" > $WK/t_ins.txt 2>&1; RC=$?; fi
echo "㊴ 計器の束 rc=$RC $(grep -E '合計' $WK/t_ins.txt | tail -1)" >> $O
if [ $RC != 0 ]; then grep -E "^\s+失敗 " $WK/t_ins.txt >> $O; echo "STOP 計器の束が赤" >> $O; exit 1; fi
# H と R を同時に（プロセスを 2 本・TEMP を分ける）
T0=$(date +%s)
printf 'H
R
' | xargs -P 2 -I{} bash -c 'if [ "$1" = H ]; then F="$2"; else F="$3"; fi; bash "$4/scripts/instruments/suite/split_one.sh" "$1" "$5" "$6" "$F"' _ {} "$FILTER_H" "$FILTER_R" "$R" "$WK" "$A"
T1=$(date +%s)
R1H=$(cat $WK/rc_H.txt); R1R=$(cat $WK/rc_R.txt); R1=$((R1H+R1R))
echo "Tests H rc=$R1H $(grep -E '合計' $WK/t_H.txt | tail -1) 壁 $(( $(cat $WK/end_H.txt)-T0 )) 秒" >> $O
echo "Tests R rc=$R1R $(grep -E '合計' $WK/t_R.txt | tail -1) 壁 $(( $(cat $WK/end_R.txt)-T0 )) 秒" >> $O
echo "Tests(H と R) 壁 $((T1-T0)) 秒" >> $O
grep -E "^\s+失敗 " $WK/t_H.txt $WK/t_R.txt >> $O
dotnet test PriceOfBread.Tests.Ui -c Release --artifacts-path $A --no-build --logger "trx;LogFileName=ui.trx" --results-directory $WK/trx > $WK/t_ui.txt 2>&1; R2=$?
echo "Tests.Ui rc=$R2 $(grep -E '合計' $WK/t_ui.txt | tail -1)" >> $O
grep -E "^\s+失敗 " $WK/t_ui.txt >> $O
if [ "${SKIPFULL:-0}" = 1 ]; then echo "SKIPFULL=1：同じ SimCore の木で先に回した" > $WK/t_full.txt; R3=0; else dotnet test PriceOfBread.Tests -c Release --artifacts-path $A --no-build --filter "Category=FullLength" > $WK/t_full.txt 2>&1; R3=$?; fi
echo "FullLength rc=$R3 $(grep -E '合計' $WK/t_full.txt | tail -1)" >> $O
grep -E "^\s+失敗 " $WK/t_full.txt >> $O
[ "${SKIPFULL:-0}" = 1 ] || POB_WORK=$WK SAVELEAK_SRC=$R bash $R/scripts/instruments/saveleak.sh > /dev/null 2>&1
echo "savetest: $(grep -h 'savetest' $WK/savetest.txt 2>/dev/null | tail -1)" >> $O
echo "leakhunt: $(tail -3 $WK/leakhunt.txt 2>/dev/null | tr '\n' ' ')" >> $O
PYTHONUTF8=1 python "$R/scripts/instruments/suite/trx_sum.py" --trx $WK/trx/tests_H.trx $WK/trx/tests_R.trx --ui $WK/trx/ui.trx --list-total $((LT+LU)) --out $WK/trx_sum.txt >> $O 2>&1; RS2=$?
echo "SUITEDONE $(date '+%F %T')" >> $O
[ $R1 = 0 ] && [ $R2 = 0 ] && [ $R3 = 0 ] && [ $RS2 = 0 ]
