#!/usr/bin/env bash
# (1179) c117k1 ㉘ 走査の番人の束（器の木・旗 0）＋TechLevelRemovalTests＋SecessionInclusionOnceTests＋分離を読む試験 6 本＋RulePowerTests。
#   bash scripts/instruments/c117k1/scan28.sh <作業フォルダ（D:/…）>  ＝この台本のある木を隔離ビルド（--artifacts-path）で回す。結果は <作業フォルダ>/scan28.txt
WK=${1:?作業フォルダ}; mkdir -p $WK/tmp
export TEMP=$WK/tmp TMP=$WK/tmp AVALONIA_TELEMETRY_OPTOUT=1
cd "$(dirname "$0")/../../.."; R=$(pwd); A=$WK/art; O=$WK/scan28.txt
echo "# (1179) scan28 木=$R HEAD=$(git rev-parse --short HEAD) $(date '+%F %T')" > $O
dotnet build PriceOfBread.Tests -c Release --artifacts-path $A -v q -nologo > $WK/build_t.txt 2>&1 || { echo "BUILDFAIL Tests" >> $O; exit 1; }
F="FullyQualifiedName~IndustrializationReaderTests|FullyQualifiedName~UnitTypeTests|FullyQualifiedName~InstrumentsTests|FullyQualifiedName~WriterRatchetTests|FullyQualifiedName~DormantIdentityTests|FullyQualifiedName~TechLevelRemovalTests|FullyQualifiedName~SecessionInclusionOnceTests|FullyQualifiedName~NationIdentityTests|FullyQualifiedName~RevoltIgnitionTests|FullyQualifiedName~RuleOnsetInstrumentTests|FullyQualifiedName~RuleOrderP1Tests|FullyQualifiedName~RuleSharesFormTests|FullyQualifiedName~SecessionLaborLawInstrumentTests|FullyQualifiedName~RulePowerTests"
dotnet test PriceOfBread.Tests -c Release --artifacts-path $A --no-build --filter "$F" > $WK/t_scan.txt 2>&1; RC=$?
echo "scan28 rc=$RC $(grep -E '合計' $WK/t_scan.txt | tail -1)" >> $O
grep -E "^\s+失敗 " $WK/t_scan.txt >> $O
echo "SCANDONE $(date '+%F %T')" >> $O
exit $RC
