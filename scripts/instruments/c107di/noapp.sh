#!/usr/bin/env bash
# (1111) エンジンの試験のビルドが App・Display・Runner を参照しないことの確かめ。
#   bash noapp.sh <隔離ビルドの artifacts-path>   obj/PriceOfBread.Tests/project.assets.json に PriceOfBread.App/Display/Runner が 0 件なら rc=0。
A="${1:?artifacts-path}/obj/PriceOfBread.Tests/project.assets.json"
[ -f "$A" ] || { echo "無い: $A"; exit 2; }
n=$(grep -c -E "PriceOfBread\.(App|Display|Runner)" "$A")
u=$(grep -c "PriceOfBread.App" "${1}/obj/PriceOfBread.Tests.Ui/project.assets.json" 2>/dev/null)
echo "エンジンの試験の assets 内の App・Display・Runner の行 = $n（0 が合格）／画面の試験の assets 内の App の行 = ${u:-無し}（対照・1 以上）"
[ "$n" = 0 ] && [ "${u:-0}" -ge 1 ]
