#!/usr/bin/env bash
# (959) c85mw 旗 1 の木のフルスイート（㉜）：Category!=FullLength と =FullLength を分けて回し、--list-tests の数と突き合わせる材料を <出力> へ。
#   使い方: bash suite959.sh <木> <出力フォルダ> <隔離ビルドのフォルダ>
T="${1:?木}"; O="${2:?出力}"; A="${3:?隔離ビルド}"
DOTNET="/c/Program Files/dotnet/dotnet"
mkdir -p "$O"; cd "$T" || exit 2
"$DOTNET" build PriceOfBread.Tests -c Release --artifacts-path "$A" > "$O/build.log" 2>&1 || exit 3
"$DOTNET" test PriceOfBread.Tests -c Release --artifacts-path "$A" --no-build --list-tests --filter "Category!=FullLength" > "$O/list_nonfull.txt" 2>&1
"$DOTNET" test PriceOfBread.Tests -c Release --artifacts-path "$A" --no-build --list-tests --filter "Category=FullLength" > "$O/list_full.txt" 2>&1
"$DOTNET" test PriceOfBread.Tests -c Release --artifacts-path "$A" --no-build --filter "Category!=FullLength" --logger "trx;LogFileName=nonfull.trx" --results-directory "$(cygpath -w "$O")" > "$O/nonfull.log" 2>&1
echo "nonfull rc=$?" >> "$O/rc.txt"
"$DOTNET" test PriceOfBread.Tests -c Release --artifacts-path "$A" --no-build --filter "Category=FullLength" --logger "trx;LogFileName=full.trx" --results-directory "$(cygpath -w "$O")" > "$O/full.log" 2>&1
echo "full rc=$?" >> "$O/rc.txt"
for k in nonfull full; do
  grep -o 'testName="[^"]*" [^>]*outcome="Failed"' "$O/$k.trx" 2>/dev/null | sed 's/testName="\([^"]*\)".*/\1/' > "$O/${k}_failed.txt"
done
exit 0
