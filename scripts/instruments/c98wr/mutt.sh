#!/usr/bin/env bash
# (1038) 突然変異の木ごとに番人 WarEntryUnifyTests を回し、合否の行を 1 本のファイルへ。呼び方：bash mutt.sh <mut のフォルダ> <出力> <木...>
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/dotnet:$PATH" AVALONIA_TELEMETRY_OPTOUT=1
D=$1; OUT=$2; shift 2; : > $OUT
for m in "$@"; do
  dotnet build "$D/$m/PriceOfBread.Tests/PriceOfBread.Tests.csproj" -c Release --artifacts-path $D/art_$m --disable-build-servers > $D/build_$m.log 2>&1; echo "$m build rc=$?" >> $OUT
  dotnet test "$D/$m/PriceOfBread.Tests/PriceOfBread.Tests.csproj" -c Release --no-build --artifacts-path $D/art_$m --filter "FullyQualifiedName~WarEntryUnifyTests" > $D/test_$m.log 2>&1
  grep -E "^\s+失敗 |合格:" $D/test_$m.log | sed "s/^/$m /" >> $OUT
done
