#!/usr/bin/env bash
# (927) c81sm 突然変異の一式（木の中を 1 か所ずつ壊して戻す＝実行中はその木にコミットしない）。出力＝D:/PoB_work/c81sm_work/mut927.txt
#   使い方：bash scripts/instruments/c81sm/mut927.sh   （長い＝run_in_background）
T=D:/PoB_work/wt_c81sm; O=D:/PoB_work/c81sm_work
PY="python -I $T/scripts/instruments/c81sm/mutate927.py $T"
$PY $O/mut927_i.txt "FullyQualifiedName~LandShareTests" \
 "LsMax_TierRule列の最大を取らない@@PriceOfBread.SimCore/InstrumentsLandshare.cs@@            LsMax(ref a.MaxSlotTierRule, LsRelDiff(expTier, act));@@            "
$PY $O/mut927_ii.txt "FullyQualifiedName~HeirDebtSettledMovedTests" \
 "ii_差でなくbackを足す旧の形@@PriceOfBread.SimCore/BilateralMarket.cs@@Instruments.CearnBillSettle(r2, p2, debt0 - p2.Debt.Value);@@Instruments.CearnBillSettle(r2, p2, back);"
$PY $O/mut927_iii.txt "FullyQualifiedName~EmptyPopDebtFirstTests|FullyQualifiedName~CearnTradeColumnsTests" \
 "iii_尾に列を1本足した写し@@PriceOfBread.SimCore/InstrumentsCearn.cs@@estateDebtMovedPerQuarter\theirDebtSettledPerQuarter\";@@estateDebtMovedPerQuarter\theirDebtSettledPerQuarter\textraCol\";"
$PY $O/mut927_iv.txt "FullyQualifiedName~OnDoubleReadScanTests" \
 "iv_Discovery.StepにOnの読みを1本足す@@PriceOfBread.SimCore/Discovery.cs@@bool insOn = Instruments.On;@@bool insOn = Instruments.On; bool insOn2 = Instruments.On;"
cat $O/mut927_i.txt $O/mut927_ii.txt $O/mut927_iii.txt $O/mut927_iv.txt > $O/mut927.txt
