#!/usr/bin/env bash
# (1342) c141k59 ㉘ 走査の番人（5 クラス＋TechLevelRemovalTests＋OnDoubleReadScanTests）＋この線の試験（FemalePartLawFreeTests・GenderTests・LaborWeightTests・MobilityTests・RuleSharesFormTests）を隔離ビルドで（c139nc/scan_nc.sh の写し）。
#   bash scan_k59.sh <作業フォルダ（D:/…）> [絞り込み]   結果は <作業フォルダ>/scan.txt
WK=${1:?作業フォルダ}; mkdir -p $WK/tmp
export TEMP=$WK/tmp TMP=$WK/tmp
cd "$(dirname "$0")/../../.."; A=$WK/art; O=$WK/scan.txt
F=${2:-"FullyQualifiedName~IndustrializationReaderTests|FullyQualifiedName~UnitTypeTests|FullyQualifiedName~InstrumentsTests|FullyQualifiedName~WriterRatchetTests|FullyQualifiedName~DormantIdentityTests|FullyQualifiedName~TechLevelRemovalTests|FullyQualifiedName~OnDoubleReadScanTests|FullyQualifiedName~FemalePartLawFreeTests|FullyQualifiedName~GenderTests|FullyQualifiedName~LaborWeightTests|FullyQualifiedName~MobilityTests|FullyQualifiedName~RuleSharesFormTests"}
echo "# (1342) scan HEAD=$(git rev-parse --short HEAD) 汚れ=$(git status --porcelain | wc -l) $(date '+%F %T')" > $O
echo "# filter=$F" >> $O
dotnet build PriceOfBread.Tests -c Release --artifacts-path $A -v q -nologo > $WK/build_scan.txt 2>&1 || { echo "BUILDFAIL" >> $O; grep -E "error" $WK/build_scan.txt | head -20 >> $O; echo SCANDONE >> $O; exit 1; }
dotnet test PriceOfBread.Tests -c Release --artifacts-path $A --no-build --filter "$F" --logger "trx;LogFileName=scan.trx" --results-directory $WK/trx > $WK/t_scan.txt 2>&1; RS=$?
echo "scan rc=$RS $(grep -E '合計|Total|成功!|失敗!' $WK/t_scan.txt | tail -2 | tr '\n' ' ')" >> $O
grep -E "^\s+失敗 |^\s+Failed " $WK/t_scan.txt >> $O
echo "SCANDONE $(date '+%F %T')" >> $O
exit $RS
