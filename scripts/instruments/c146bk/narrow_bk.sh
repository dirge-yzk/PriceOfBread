#!/usr/bin/env bash
# (1378) c146bk 旗 1 の木の絞った試験（㉜）：差分の名を Tests/ で grep して当たったクラス＋㉘ の束＋FullLength（trx で赤を見る）→ savetest・leakhunt。
#   bash scripts/instruments/c146bk/narrow_bk.sh <作業フォルダ（D:/…）>
WK=${1:?作業フォルダ}; mkdir -p $WK/tmp $WK/trx; export TEMP=$WK/tmp TMP=$WK/tmp
cd "$(dirname "$0")/../../.."; R=$(pwd); A=$WK/art; O=$WK/narrow.txt
echo "# narrow HEAD=$(git rev-parse --short HEAD) start $(date '+%F %T')" > $O
dotnet test PriceOfBread.Tests -c Release --artifacts-path $A --filter "(FullyQualifiedName~AiBuildOutlayGateTests|FullyQualifiedName~AiFirstWaveTests|FullyQualifiedName~BankruptReentryShortageTests|FullyQualifiedName~BuyShadowInstrumentTests|FullyQualifiedName~ClearingFillInstrumentTests|FullyQualifiedName~ClearingScopeTests|FullyQualifiedName~DomesticServiceRationTests|FullyQualifiedName~DormantIdentityTests|FullyQualifiedName~EconomyTests|FullyQualifiedName~FactoryEquipmentTests|FullyQualifiedName~FactoryIdeologyInstrumentTests|FullyQualifiedName~FiscalTests|FullyQualifiedName~InpFillTests|FullyQualifiedName~InstrumentColumnTests|FullyQualifiedName~InstrumentsMonTests|FullyQualifiedName~LandbaseInstrumentTests|FullyQualifiedName~MinimumWageAiFloorTests|FullyQualifiedName~MobilityTests|FullyQualifiedName~OpeningFiscalAlignTests|FullyQualifiedName~PhantomProductionTests|FullyQualifiedName~RecruitVacancyTests|FullyQualifiedName~SecessionLaborLawInstrumentTests|FullyQualifiedName~StateEduDemandTests|FullyQualifiedName~StateFactoryTests|FullyQualifiedName~SteelMarketAiTests|FullyQualifiedName~SubsistenceStage2Tests|FullyQualifiedName~SubsistenceTechShareTests|FullyQualifiedName~TradeBilateralInstrumentTests|FullyQualifiedName~TradeClearingTests|FullyQualifiedName~TsregInstrumentTests|FullyQualifiedName~UnitTypeTests|FullyQualifiedName~UrxInstrumentTests|FullyQualifiedName~WriterRatchetTests|FullyQualifiedName~ZeroPopSkipTests|FullyQualifiedName~IndustrializationReaderTests|FullyQualifiedName~InstrumentsTests|FullyQualifiedName~ValueCountRatchetTests|FullyQualifiedName~DerivedValueRatchetTests)|Category=FullLength" --logger "trx;LogFileName=narrow.trx" --results-directory $WK/trx > $WK/t_narrow.txt 2>&1; RC=$?
echo "narrow rc=$RC $(grep -E '(失敗|成功)!' $WK/t_narrow.txt | tail -1)" >> $O
grep -E "^\s+失敗 " $WK/t_narrow.txt >> $O
POB_WORK=$WK SAVELEAK_SRC=$R bash $R/scripts/instruments/saveleak.sh > $WK/saveleak.log 2>&1
echo "savetest: $(grep -h 'savetest' $WK/savetest.txt 2>/dev/null | tail -1)" >> $O
echo "leakhunt: $(tail -3 $WK/leakhunt.txt 2>/dev/null | tr '\n' ' ')" >> $O
echo "NARROWDONE $(date '+%F %T')" >> $O
