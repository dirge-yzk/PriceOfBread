#!/usr/bin/env bash
# (1171) 合流（m1157/kit1157.sh の写し・作業フォルダを m1171_work へ）
# (1157) 合流（m1150/kit1150.sh の写し・木と作業フォルダを wt_m1157・m1171_work へ）
# (1150) 合流（m1143/kit1143.sh の写し・作業フォルダを m1171_work へ）
# (1143) 合流：テストプレイの道具箱 D:/PoB_play/kit を最後の main の木で建て直す（m1136/kit1136.sh の写し・作業フォルダを m1171_work）
H=${1:?コミット}; W=D:/PoB_work/m1171_work/kitsrc
rm -rf $W && mkdir -p $W && (cd C:/Users/dirge/Documents/PriceOfBread && git archive $H PriceOfBread.Runner PriceOfBread.SimCore PriceOfBread.Display) | tar -x -C $W || exit 1
dotnet build $W/PriceOfBread.Runner -c Release --artifacts-path D:/PoB_play/kit/build -p:UseSharedCompilation=false 2>&1 | tail -3
rm -rf D:/PoB_play/kit/build/obj
ls D:/PoB_play/kit/build/bin/PriceOfBread.Runner/release/PriceOfBread.Display.dll && echo DISPLAYDLL
echo KITDONE
