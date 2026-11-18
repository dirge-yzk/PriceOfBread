#!/usr/bin/env bash
# (1219) 合流（m1212/kit1212.sh の写し・作業フォルダだけ m1219）
# (1212) 合流（m1205/kit1205.sh の写し・作業フォルダだけ m1212）
# (1205) 合流（m1198/kit1198.sh の写し・作業フォルダだけ m1205）
# (1198) 合流（m1191/kit1191.sh の写し・作業フォルダだけ m1198）
# (1191) 合流（m1184/kit1184.sh の写し・木と作業フォルダを wt_m1191・m1191_work へ）
# (1184) 合流（m1178/kit1178.sh の写し・作業フォルダを m1191_work へ）
# (1178) 合流（m1171/kit1171.sh の写し・作業フォルダを m1191_work へ）
# (1171) 合流（m1157/kit1157.sh の写し・作業フォルダを m1191_work へ）
# (1157) 合流（m1150/kit1150.sh の写し・木と作業フォルダを wt_m1157・m1191_work へ）
# (1150) 合流（m1143/kit1143.sh の写し・作業フォルダを m1191_work へ）
# (1143) 合流：テストプレイの道具箱 D:/PoB_play/kit を最後の main の木で建て直す（m1136/kit1136.sh の写し・作業フォルダを m1191_work）
H=${1:?コミット}; W=D:/PoB_work/m1219_work/kitsrc
rm -rf $W && mkdir -p $W && (cd C:/Users/dirge/Documents/PriceOfBread && git archive $H PriceOfBread.Runner PriceOfBread.SimCore PriceOfBread.Display) | tar -x -C $W || exit 1
dotnet build $W/PriceOfBread.Runner -c Release --artifacts-path D:/PoB_play/kit/build -p:UseSharedCompilation=false 2>&1 | tail -3
rm -rf D:/PoB_play/kit/build/obj
ls D:/PoB_play/kit/build/bin/PriceOfBread.Runner/release/PriceOfBread.Display.dll && echo DISPLAYDLL
echo KITDONE
