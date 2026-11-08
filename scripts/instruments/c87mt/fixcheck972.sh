#!/usr/bin/env bash
# (972) 追記 1 の確かめ：直した T4 を旗 1 の木と旗 0 の使い捨ての木で回す
export PATH="/usr/bin:/c/Program Files/dotnet:$PATH"
for t in "D:/PoB_work/wt_c87mt:D:/PoB_work/c87mt_art:flag1" "D:/PoB_work/c87mt_work/f0_tree:D:/PoB_work/c87mt_work/f0_art:flag0"; do
  IFS=: read d1 d2 a1 a2 nm <<<"$t"; T="$d1:$d2"; A="$a1:$a2"
  echo "== $nm 木 $T"
  dotnet build "$T/PriceOfBread.Tests/PriceOfBread.Tests.csproj" -c Release --artifacts-path "$A" -v q --disable-build-servers > /dev/null 2>&1 || { echo build-fail; continue; }
  dotnet test "$T/PriceOfBread.Tests/PriceOfBread.Tests.csproj" -c Release --no-build --artifacts-path "$A" --filter "FullyQualifiedName~MoTrendInitTests" 2>&1 | grep -E "成功!|失敗!|^ *失敗 "
done
