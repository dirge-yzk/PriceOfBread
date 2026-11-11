#!/usr/bin/env bash
# (859)（(852) kit852.sh の写し・作業フォルダと git archive の元だけ m859） # (852)（(845) の写し・作業フォルダと git archive の元だけ m852） # (839)（(818) の kit818.sh の写し・作業フォルダだけ m839・git archive の元は wt_m845） テストプレイの道具箱 D:/PoB_play/kit/build を main の木で建て直す（Runner と SimCore だけ・--artifacts-path・Release）。
#   bash kit832.sh <コミット>   ⚠建て直しの前に D:/PoB_play の Runner が動いていないことを確かめる（呼び手）。D:/PoB_play の他の物には書かない。
H=${1:?コミット}; W=D:/PoB_work/m859_work/kitsrc
rm -rf $W && mkdir -p $W && (cd C:/Users/dirge/Documents/PriceOfBread && git archive $H PriceOfBread.Runner PriceOfBread.SimCore) | tar -x -C $W || exit 1
dotnet build $W/PriceOfBread.Runner -c Release --artifacts-path D:/PoB_play/kit/build 2>&1 | tail -3
rm -rf D:/PoB_play/kit/build/obj
echo KITDONE
