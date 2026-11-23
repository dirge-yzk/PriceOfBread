#!/usr/bin/env bash
# (1318) c137ak ㉘ 走査の番人（5 クラス＋OnDoubleReadScanTests）＋この線の試験（SecessionTreasuryShareTests）と分離を名指す試験（TreasLedgerInstrumentTests・NationIdentityTests・ConservationTests・RevoltIgnitionTests・PopCloseInstrumentTests・CedeRuleInstrumentTests・InheritIntentTests）を隔離ビルドで（c135nc/scan_nc.sh の写し）。
#   既定は走査の番人＋この線の試験だけ（数分）。分離を名指す重い試験は SCANMORE に名を入れる（例：SCANMORE="FullyQualifiedName~ConservationTests"）＝フルスイートが含む。
#   bash scan_nc.sh <作業フォルダ（D:/…）>   結果は <作業フォルダ>/scan.txt
WK=${1:?作業フォルダ}; mkdir -p $WK/tmp
export TEMP=$WK/tmp TMP=$WK/tmp
cd "$(dirname "$0")/../../.."; A=$WK/art; O=$WK/scan.txt
echo "# (1318) scan HEAD=$(git rev-parse --short HEAD) 汚れ=$(git status --porcelain | wc -l) $(date '+%F %T')" > $O
dotnet build PriceOfBread.Tests -c Release --artifacts-path $A -v q -nologo > $WK/build_scan.txt 2>&1 || { echo "BUILDFAIL" >> $O; grep -E "error" $WK/build_scan.txt | head -20 >> $O; exit 1; }
dotnet test PriceOfBread.Tests -c Release --artifacts-path $A --no-build --filter "FullyQualifiedName~IndustrializationReaderTests|FullyQualifiedName~UnitTypeTests|FullyQualifiedName~InstrumentsTests|FullyQualifiedName~WriterRatchetTests|FullyQualifiedName~DormantIdentityTests|FullyQualifiedName~OnDoubleReadScanTests|FullyQualifiedName~SecessionTreasuryShareTests${SCANMORE:+|$SCANMORE}" > $WK/t_scan.txt 2>&1; RS=$?
echo "scan rc=$RS $(grep -E '合計|Total|成功!|失敗!' $WK/t_scan.txt | tail -2 | tr '\n' ' ')" >> $O
grep -E "^\s+失敗 |^\s+Failed " $WK/t_scan.txt >> $O
echo "SCANDONE $(date '+%F %T')" >> $O
exit $RS
