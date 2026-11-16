#!/usr/bin/env bash
# (1175) c116cl 使い捨て：突然変異を 1 本ずつ当てて試験を回し、戻す（bg.sh で走らせる）
cd /d/PoB_work/wt_c116cl
export TEMP=D:/PoB_work/c116cl_work TMP=D:/PoB_work/c116cl_work
W=D:/PoB_work/c116cl_work
run() { # ラベル プロジェクト フィルタ 戻すファイル
  L=$1; P=$2; F=$3; X=$4
  python -X utf8 -I scripts/instruments/c116cl/mutate.py $L > $W/mut_$L.txt 2>&1
  dotnet test $P -c Release --artifacts-path D:/PoB_work/c116cl_art2 --filter "$F" >> $W/mut_$L.txt 2>&1
  git checkout -- "$X"
}
# 変更前の緑（番人が今の盤面で緑）
dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path D:/PoB_work/c116cl_art2 --filter "FullyQualifiedName~TradeClearingTests|FullyQualifiedName~MintregInstrumentTests" > $W/mut_green.txt 2>&1
run M2  PriceOfBread.Tests/PriceOfBread.Tests.csproj "FullyQualifiedName~TradeClearingTests" PriceOfBread.SimCore/MarketFlow.cs
run M3a PriceOfBread.Tests.Ui/PriceOfBread.Tests.Ui.csproj "FullyQualifiedName~LaborMarketFormTests" PriceOfBread.SimCore/MarketFlow.cs
run M5  PriceOfBread.Tests/PriceOfBread.Tests.csproj "FullyQualifiedName~MintregInstrumentTests" PriceOfBread.SimCore/Simulation.cs
echo done
