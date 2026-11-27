#!/usr/bin/env bash
# (1426) c153tp：フルスイートを 2 本の dotnet test に分ける時の、重い組の絞り方（split_suite.sh と ident.sh の腕 t が読む 1 か所）。
# 重い組＝Collection("Instruments")（DisableParallelization）の中で最も長い 2 クラス（選んだ理由は measurements/m1426_*_prereg_tp.txt）。
# クラスの名は末尾に . を付けて頭が同じ別のクラスに当たらないようにする。FullLength は H にも R にも入れない（別に回す）。
HEAVY_CLASSES="InstrumentsTests InstrumentHookClosureTests"
_h=""; _r=""
for c in $HEAVY_CLASSES; do _h="${_h:+$_h|}FullyQualifiedName~PriceOfBread.Tests.$c."; _r="$_r&FullyQualifiedName!~PriceOfBread.Tests.$c."; done
FILTER_H="Category!=FullLength&($_h)"
FILTER_R="Category!=FullLength$_r"
