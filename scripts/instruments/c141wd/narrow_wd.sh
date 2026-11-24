#!/usr/bin/env bash
# (1343) c141wd ㉜ の改めの絞った試験（旗 1 の木・腕 B の前）：差分の名（WarmupNoDecisions・DecideQuarter・AutoModernizationFrac・SetNavalUpkeepShare・ModernizationFrac・WarmupEconomy・StartWarmup）を
#   Tests/ で grep して当たったファイルのクラス＋㉘ の束（scan_wd.sh の既定）＋FullLength＋savetest・leakhunt。赤は trx で見る（c140hf/narrow_hf.sh の写し）。
#   bash narrow_wd.sh <作業フォルダ（D:/…）>
WK=${1:?作業フォルダ}; mkdir -p $WK/tmp $WK/trx
export TEMP=$WK/tmp TMP=$WK/tmp
D0="$(cd "$(dirname "$0")" && pwd)"; cd "$D0/../../.."; R=$(pwd); A=$WK/art
CL=$(grep -rlE "WarmupNoDecisions|DecideQuarter|AutoModernizationFrac|SetNavalUpkeepShare|ModernizationFrac|WarmupEconomy|StartWarmup" PriceOfBread.Tests --include=*.cs | xargs grep -ohE "^\s*(public |internal )?(static )?(sealed )?(partial )?class [A-Za-z0-9_]+" | awk '{print $NF}' | sort -u)
echo "$CL" > $WK/narrow_classes.txt
F="FullyQualifiedName~IndustrializationReaderTests|FullyQualifiedName~UnitTypeTests|FullyQualifiedName~InstrumentsTests|FullyQualifiedName~WriterRatchetTests|FullyQualifiedName~DormantIdentityTests|FullyQualifiedName~TechLevelRemovalTests|FullyQualifiedName~OnDoubleReadScanTests"
for c in $CL; do F="$F|FullyQualifiedName~PriceOfBread.Tests.$c."; done
F="($F)&Category!=FullLength"
bash $D0/scan_wd.sh $WK "$F"; RS=$?
O=$WK/scan.txt
dotnet test PriceOfBread.Tests -c Release --artifacts-path $A --no-build --filter "Category=FullLength" --logger "trx;LogFileName=full.trx" --results-directory $WK/trx > $WK/t_full.txt 2>&1; R3=$?
echo "FullLength rc=$R3 $(grep -E '合計' $WK/t_full.txt | tail -1)" >> $O
grep -E "^\s+失敗 " $WK/t_full.txt >> $O
POB_WORK=$WK SAVELEAK_SRC=$R bash $R/scripts/instruments/saveleak.sh > /dev/null 2>&1
echo "savetest: $(grep -h 'savetest' $WK/savetest.txt 2>/dev/null | tail -1)" >> $O
echo "leakhunt: $(tail -3 $WK/leakhunt.txt 2>/dev/null | tr '\n' ' ')" >> $O
echo "NARROWDONE $(date '+%F %T')" >> $O
[ $RS = 0 ] && [ $R3 = 0 ]
