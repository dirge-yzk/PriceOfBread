#!/usr/bin/env bash
# (1350) c142lr ㉜ の改めの絞った試験（旗 1 の木・腕 B の前・c140hf/narrow_hf.sh の写し）：差分の名（ProtectedLawsReversible・IrreversibleLaws・ProtectedLaw・BlockedByIrreversibility・ReversalCostMul・RuptureWindow・不可逆・後退不能・戦時のみ緩和可・ChildLaborLegal）を Tests/ で grep して当たったクラス
#   （ProtectedLawReversalTests・DormantIdentityTests・Lawgate2InstrumentTests・MinimumWageAiFloorTests・GenderTests・PolicyPoliticsTests・AiFirstWaveTests・PlayTextTests・RevoltIgnitionTests・StartWarmupTests・InstrumentsTests）＋㉘ の束＋FullLength＋savetest・leakhunt。赤は trx で見る。
#   bash narrow_lr.sh <作業フォルダ（D:/…）>
WK=${1:?作業フォルダ}; mkdir -p $WK/tmp $WK/trx
export TEMP=$WK/tmp TMP=$WK/tmp
D0="$(cd "$(dirname "$0")" && pwd)"; cd "$D0/../../.."; R=$(pwd); A=$WK/art
bash $D0/scan_lr.sh $WK "FullyQualifiedName~IndustrializationReaderTests|FullyQualifiedName~UnitTypeTests|FullyQualifiedName~InstrumentsTests|FullyQualifiedName~WriterRatchetTests|FullyQualifiedName~DormantIdentityTests|FullyQualifiedName~TechLevelRemovalTests|FullyQualifiedName~OnDoubleReadScanTests|FullyQualifiedName~ProtectedLawReversalTests|FullyQualifiedName~Lawgate2InstrumentTests|FullyQualifiedName~MinimumWageAiFloorTests|FullyQualifiedName~GenderTests|FullyQualifiedName~PolicyPoliticsTests|FullyQualifiedName~AiFirstWaveTests|FullyQualifiedName~PlayTextTests|FullyQualifiedName~RevoltIgnitionTests|FullyQualifiedName~StartWarmupTests|FullyQualifiedName~RuleSharesFormTests"; RS=$?
O=$WK/scan.txt
dotnet test PriceOfBread.Tests -c Release --artifacts-path $A --no-build --filter "Category=FullLength" --logger "trx;LogFileName=full.trx" --results-directory $WK/trx > $WK/t_full.txt 2>&1; R3=$?
echo "FullLength rc=$R3 $(grep -E '合計' $WK/t_full.txt | tail -1)" >> $O
grep -E "^\s+失敗 " $WK/t_full.txt >> $O
POB_WORK=$WK SAVELEAK_SRC=$R bash $R/scripts/instruments/saveleak.sh > /dev/null 2>&1
echo "savetest: $(grep -h 'savetest' $WK/savetest.txt 2>/dev/null | tail -1)" >> $O
echo "leakhunt: $(tail -3 $WK/leakhunt.txt 2>/dev/null | tr '\n' ' ')" >> $O
echo "NARROWDONE $(date '+%F %T')" >> $O
[ $RS = 0 ] && [ $R3 = 0 ]
