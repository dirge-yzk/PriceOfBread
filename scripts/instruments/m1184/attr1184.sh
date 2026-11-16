#!/usr/bin/env bash
# (1184) 合流（m1178/attr1178.sh の写し・作業フォルダを m1184_work へ）
# (1178) 合流：番人の束（合わせた木 920a655a1）の InstrumentsTests の赤 3 本の帰属を、同じビルドで POB_COEF の旗を 1 本ずつ 0 に戻して読む（試験は書き換えない）。
export TEMP=D:/PoB_work/m1184_work/tmp TMP=D:/PoB_work/m1184_work/tmp
W=D:/PoB_work/m1184_work/suite; cd $W/suitesrc || exit 2
F="FullyQualifiedName~InstrumentsTests.軍事費の施行の一歩の内訳|FullyQualifiedName~InstrumentsTests.史実比較の桁の入力の表|FullyQualifiedName~InstrumentsTests.行政の請求の2つの式"
for c in "all:" "noT1:TradeBuyBySupplyCurve=0" "noK1:SecessionInclusionOnce=0" "noHD:StockpilePanicWeight=0.5" "none:TradeBuyBySupplyCurve=0;SecessionInclusionOnce=0;StockpilePanicWeight=0.5" "onlyT1:SecessionInclusionOnce=0;StockpilePanicWeight=0.5" "onlyK1:TradeBuyBySupplyCurve=0;StockpilePanicWeight=0.5" "onlyHD:TradeBuyBySupplyCurve=0;SecessionInclusionOnce=0"; do
  n=${c%%:*}; v=${c#*:}
  POB_COEF="$v" dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $W/art_suite --filter "$F" > $W/attr_$n.txt 2>&1
  echo "$n [$v] $(grep -E '成功!|失敗!' $W/attr_$n.txt | tail -1 | cut -c1-60) $(grep -E '^\s*失敗 ' $W/attr_$n.txt | sed 's/.*InstrumentsTests\.//' | cut -c1-12 | tr '\n' ' ')"
done
echo ATTRDONE
