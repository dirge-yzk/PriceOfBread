#!/usr/bin/env bash
# (1096) 合流（m1087/kit1087.sh の写し・作業フォルダを m1096_work）
# (1087) 合流（m1078/kit1078.sh の写し・作業フォルダを m1096_work）
# (1078) 合流（m1069/kit1069.sh の写し・作業フォルダを m1078_work）
# (1053) 合流（m1044/kit1044.sh の写し・作業フォルダを m1069_work）
# (1044) 合流（m1035/kit1035.sh の写し・作業フォルダを m1044_work）
# (1035) 合流（m1027/kit1027.sh の写し・作業フォルダを m1035_work）
# (1027) 合流（kit1022.sh の写し・作業フォルダを m1035_work）
# (1022) 合流（kit1017.sh の写し・作業フォルダを m1035_work）
# (1017) 合流（kit1011.sh の写し・作業フォルダを m1017_work にした）
# (1011) 合流（(1006) kit1006.sh の写し・作業フォルダを m1011_work にした）＝D:/PoB_play/kit を合流の最後の main の木で建て直す。
#   bash kit1011.sh <コミット>   ⚠建て直しの前に D:/PoB_play の Runner が動いていないことを確かめる（呼び手）。D:/PoB_play の他の物には書かない。
H=${1:?コミット}; W=D:/PoB_work/m1096_work/kitsrc
rm -rf $W && mkdir -p $W && (cd C:/Users/dirge/Documents/PriceOfBread && git archive $H PriceOfBread.Runner PriceOfBread.SimCore) | tar -x -C $W || exit 1
dotnet build $W/PriceOfBread.Runner -c Release --artifacts-path D:/PoB_play/kit/build -p:UseSharedCompilation=false 2>&1 | tail -3
rm -rf D:/PoB_play/kit/build/obj
echo KITDONE
