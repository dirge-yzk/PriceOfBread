#!/usr/bin/env bash
# (921) 突然変異（木の中を 1 か所ずつ壊して戻す＝実行中はその木にコミットしない）。出力＝D:/PoB_work/c80sm_work/mutation.txt
T=D:/PoB_work/wt_c80sm; O=D:/PoB_work/c80sm_work
python -I $T/scripts/instruments/c80sm/mutate921.py $T $O/mutation.txt "FullyQualifiedName~CoupGateNanMarkTests" \
 "M1_暴力かどうかを見ない@@PriceOfBread.SimCore/InstrumentsCoup.cs@@route == 5 && !violent ? 1 : 0@@route == 5 ? 1 : 0" \
 "M2_試験の経路を 4 にする@@PriceOfBread.Tests/CoupGateNanMarkTests.cs@@Assert.Equal(\"1\", Mark(5, false));@@Assert.Equal(\"1\", Mark(4, false));" \
 "M5_経路 5 を 4 に取り違える@@PriceOfBread.SimCore/InstrumentsCoup.cs@@route == 5 && !violent ? 1 : 0@@route == 4 && !violent ? 1 : 0"
