#!/usr/bin/env bash
# (797) テストプレイの道具箱 D:/PoB_play/kit/build を main の木で建て直す（Runner と SimCore だけ・git archive・--artifacts-path・Release）。
#   bash kit.sh <コミット>   ⚠建て直しの前に D:/PoB_play の Runner が動いていないことを確かめる（呼び手）。D:/PoB_play の他の物には書かない。
H=${1:?コミット}; W=D:/PoB_work/m797/kitsrc
rm -rf $W && mkdir -p $W && (cd D:/PoB_work/wt_m797 && git archive $H PriceOfBread.Runner PriceOfBread.SimCore) | tar -x -C $W || exit 1
dotnet build $W/PriceOfBread.Runner -c Release --artifacts-path D:/PoB_play/kit/build 2>&1 | tail -3
rm -rf D:/PoB_play/kit/build/obj
echo KITDONE
