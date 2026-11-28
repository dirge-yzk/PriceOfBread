#!/usr/bin/env bash
# (1435) c154sd（c152sd/scan_sd.sh の写し） ㉘ 走査の番人（5 クラス＋DerivedValueRatchetTests・ValueCountRatchetTests）＋この線の試験を隔離ビルドで。
#   bash scan.sh <作業フォルダ（D:/…）> [絞り込み] [名]   結果は <作業フォルダ>/scan_<名>.txt・trx は <作業フォルダ>/trx/<名>.trx
WK=${1:?作業フォルダ}; N=${3:-scan}; mkdir -p $WK/tmp $WK/trx
export TEMP=$WK/tmp TMP=$WK/tmp
cd "$(dirname "$0")/../../.."; A=$WK/art; O=$WK/scan_$N.txt
F=${2:-"FullyQualifiedName~IndustrializationReaderTests|FullyQualifiedName~UnitTypeTests|FullyQualifiedName~InstrumentsTests|FullyQualifiedName~WriterRatchetTests|FullyQualifiedName~DormantIdentityTests|FullyQualifiedName~DerivedValueRatchetTests|FullyQualifiedName~ValueCountRatchetTests|FullyQualifiedName~FactionFloorRegionTests|FullyQualifiedName~InstrumentHookClosureTests|FullyQualifiedName~InstrumentColumnTests"}
echo "# (1435) $N HEAD=$(git rev-parse --short HEAD) 汚れ=$(git status --porcelain | wc -l) $(date '+%F %T')" > $O
dotnet build PriceOfBread.Tests -c Release --artifacts-path $A -v q -nologo > $WK/build_$N.txt 2>&1 || { echo "BUILDFAIL" >> $O; grep -E "error" $WK/build_$N.txt | head -20 >> $O; exit 1; }
dotnet test PriceOfBread.Tests -c Release --artifacts-path $A --no-build --filter "$F" --logger "trx;LogFileName=$N.trx" --results-directory $WK/trx > $WK/t_$N.txt 2>&1; RS=$?
echo "$N rc=$RS $(grep -E '合計|Total' $WK/t_$N.txt | tail -1 | tr '\n' ' ')" >> $O
grep -E "^\s+失敗 |^\s+Failed " $WK/t_$N.txt >> $O
echo "SCANDONE $(date '+%F %T')" >> $O
exit $RS
