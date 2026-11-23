#!/usr/bin/env bash
# (1324) c138df ㉘ 走査の番人（IndustrializationReader・UnitType・InstrumentsTests・WriterRatchet・DormantIdentity・TechLevelRemoval・OnDoubleReadScan）＋この線の試験（ArrearsOnePredicateTests）と本位の停止・不履行の累計を名指す試験（ArrearsOpenedRoomTests・FiscalTests・TreasuryLedgerTests・ArrearsUnderLimitPrevMonthTests）（c137ak/scan_ak.sh の写し）。
#   既定は走査の番人＋この線の試験（数分）。
#   bash scan_df.sh <作業フォルダ（D:/…）>   結果は <作業フォルダ>/scan.txt
WK=${1:?作業フォルダ}; mkdir -p $WK/tmp
export TEMP=$WK/tmp TMP=$WK/tmp
cd "$(dirname "$0")/../../.."; A=$WK/art; O=$WK/scan.txt
echo "# (1324) scan HEAD=$(git rev-parse --short HEAD) 汚れ=$(git status --porcelain | wc -l) $(date '+%F %T')" > $O
dotnet build PriceOfBread.Tests -c Release --artifacts-path $A -v q -nologo > $WK/build_scan.txt 2>&1 || { echo "BUILDFAIL" >> $O; grep -E "error" $WK/build_scan.txt | head -20 >> $O; exit 1; }
dotnet test PriceOfBread.Tests -c Release --artifacts-path $A --no-build --filter "FullyQualifiedName~IndustrializationReaderTests|FullyQualifiedName~UnitTypeTests|FullyQualifiedName~InstrumentsTests|FullyQualifiedName~WriterRatchetTests|FullyQualifiedName~DormantIdentityTests|FullyQualifiedName~OnDoubleReadScanTests|FullyQualifiedName~TechLevelRemovalTests|FullyQualifiedName~ArrearsOnePredicateTests|FullyQualifiedName~ArrearsOpenedRoomTests|FullyQualifiedName~FiscalTests|FullyQualifiedName~TreasuryLedgerTests|FullyQualifiedName~ArrearsUnderLimitPrevMonthTests${SCANMORE:+|$SCANMORE}" > $WK/t_scan.txt 2>&1; RS=$?
echo "scan rc=$RS $(grep -E '合計|Total|成功!|失敗!' $WK/t_scan.txt | tail -2 | tr '\n' ' ')" >> $O
grep -E "^\s+失敗 |^\s+Failed " $WK/t_scan.txt >> $O
echo "SCANDONE $(date '+%F %T')" >> $O
exit $RS
