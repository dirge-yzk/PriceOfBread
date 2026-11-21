#!/usr/bin/env bash
# (1284) c132df ㉘ 走査の番人（4 クラス＋DormantIdentityTests＋OnDoubleReadScanTests）＋足した試験＋近い番人（ArrearsOpenedRoomTests・FiscalTests）を隔離ビルドで回す。
#   bash scan_df.sh <作業フォルダ（D:/…）>  ＝この台本のある木。結果は <作業フォルダ>/scan.txt
WK=${1:?作業フォルダ}; mkdir -p $WK/tmp; export TEMP=$WK/tmp TMP=$WK/tmp DOTNET_CLI_TELEMETRY_OPTOUT=1
cd "$(dirname "$0")/../../.."; A=$WK/art; O=$WK/scan.txt
echo "# (1284) scan 木=$(pwd) HEAD=$(git rev-parse --short HEAD) dirty=$(git status --porcelain | wc -l) $(date '+%F %T')" > $O
dotnet build PriceOfBread.Tests -c Release --artifacts-path $A -v q -nologo > $WK/build_scan.txt 2>&1 || { echo "BUILDFAIL" >> $O; grep -E "error" $WK/build_scan.txt | head -20 >> $O; exit 1; }
F="FullyQualifiedName~IndustrializationReaderTests|FullyQualifiedName~UnitTypeTests|FullyQualifiedName~InstrumentsTests|FullyQualifiedName~WriterRatchetTests|FullyQualifiedName~DormantIdentityTests|FullyQualifiedName~OnDoubleReadScanTests|FullyQualifiedName~ArrearsOnePredicateTests|FullyQualifiedName~ArrearsInstrumentTests|FullyQualifiedName~ArrearsOpenedRoomTests|FullyQualifiedName~FiscalTests"
dotnet test PriceOfBread.Tests -c Release --artifacts-path $A --no-build --filter "$F" > $WK/t_scan.txt 2>&1; RC=$?
echo "scan rc=$RC $(grep -E '合計|Total|成功|失敗' $WK/t_scan.txt | tail -2 | tr '\n' ' ')" >> $O
grep -E "^\s+失敗 |^\s+Failed " $WK/t_scan.txt >> $O
echo SCANDONE >> $O
exit $RC
