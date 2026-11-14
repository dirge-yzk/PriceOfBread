#!/usr/bin/env bash
# (1129) 合流（m1122/kit1122.sh の写し・作業フォルダを m1129_work）
# (1122) 合流（m1116/kit1116.sh の写し・作業フォルダを m1129_work）
# (1116) 合流（m1110/kit1110.sh の写し・作業フォルダを m1116_work・(1111) c107di で Runner が PriceOfBread.Display を参照する＝git archive に PriceOfBread.Display を足した〔反証役 v の中〕）
# (1110) 合流（m1103/kit1103.sh の写し・作業フォルダを m1110_work）
# (1103) 合流（m1096/kit1096.sh の写し・作業フォルダを m1103_work）
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
H=${1:?コミット}; W=D:/PoB_work/m1129_work/kitsrc
rm -rf $W && mkdir -p $W && (cd C:/Users/dirge/Documents/PriceOfBread && git archive $H PriceOfBread.Runner PriceOfBread.SimCore PriceOfBread.Display) | tar -x -C $W || exit 1
dotnet build $W/PriceOfBread.Runner -c Release --artifacts-path D:/PoB_play/kit/build -p:UseSharedCompilation=false 2>&1 | tail -3
rm -rf D:/PoB_play/kit/build/obj
ls D:/PoB_play/kit/build/bin/PriceOfBread.Runner/release/PriceOfBread.Display.dll && echo DISPLAYDLL
echo KITDONE
