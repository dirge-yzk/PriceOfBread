#!/usr/bin/env bash
# (1357) c143sp ㉘ 走査の番人（5 クラス＋TechLevelRemovalTests＋OnDoubleReadScanTests）＋この線の試験（NextCapitalHomeFirstTests・TerritoryTests・RevoltIgnitionTests）を隔離ビルドで（c134k49/scan_k49.sh の写し）。
#   bash scan_nc.sh <作業フォルダ（D:/…）>   結果は <作業フォルダ>/scan.txt
WK=${1:?作業フォルダ}; mkdir -p $WK/tmp
export TEMP=$WK/tmp TMP=$WK/tmp
cd "$(dirname "$0")/../../.."; A=$WK/art; O=$WK/scan.txt
echo "# (1357) scan HEAD=$(git rev-parse --short HEAD) 汚れ=$(git status --porcelain | wc -l) $(date '+%F %T')" > $O
dotnet build PriceOfBread.Tests -c Release --artifacts-path $A -v q -nologo > $WK/build_scan.txt 2>&1 || { echo "BUILDFAIL" >> $O; grep -E "error" $WK/build_scan.txt | head -20 >> $O; exit 1; }
dotnet test PriceOfBread.Tests -c Release --artifacts-path $A --no-build --filter "FullyQualifiedName~IndustrializationReaderTests|FullyQualifiedName~UnitTypeTests|FullyQualifiedName~InstrumentsTests|FullyQualifiedName~WriterRatchetTests|FullyQualifiedName~DormantIdentityTests|FullyQualifiedName~TechLevelRemovalTests|FullyQualifiedName~OnDoubleReadScanTests|FullyQualifiedName~StateProcureLedgerTests|FullyQualifiedName~TradeClearingTests" > $WK/t_scan.txt 2>&1; RS=$?
echo "scan rc=$RS $(grep -E '合計|Total|成功!|失敗!' $WK/t_scan.txt | tail -2 | tr '\n' ' ')" >> $O
grep -E "^\s+失敗 |^\s+Failed " $WK/t_scan.txt >> $O
echo "SCANDONE $(date '+%F %T')" >> $O
exit $RS
