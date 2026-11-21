#!/usr/bin/env bash
# (1287) 合流（波132・m1280 の写しから・m1274/kit1274.sh の写し・作業フォルダを m1287_work へ）。系譜は m1247/kit1247.sh の頭の注を読む。
H=${1:?コミット}; W=D:/PoB_work/m1287_work/kitsrc
rm -rf $W && mkdir -p $W && (cd C:/Users/dirge/Documents/PriceOfBread && git archive $H PriceOfBread.Runner PriceOfBread.SimCore PriceOfBread.Display) | tar -x -C $W || exit 1
dotnet build $W/PriceOfBread.Runner -c Release --artifacts-path D:/PoB_play/kit/build -p:UseSharedCompilation=false 2>&1 | tail -3
rm -rf D:/PoB_play/kit/build/obj
ls D:/PoB_play/kit/build/bin/PriceOfBread.Runner/release/PriceOfBread.Display.dll && echo DISPLAYDLL
echo KITDONE
