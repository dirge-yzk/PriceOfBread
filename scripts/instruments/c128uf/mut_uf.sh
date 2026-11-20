#!/usr/bin/env bash
# (1258) c128uf 突然変異の試験（使い捨ての木 D:/PoB_work/wt_c128uf_mut・隔離ビルド D:/PoB_work/c128uf_mutart）。
#   使い方: bash mut_uf.sh <コミット> <出力フォルダ>
C=${1:?コミット}; OUT=${2:?出力}; T=D:/PoB_work/wt_c128uf_mut; D0="$(cd "$(dirname "$0")" && pwd)"
mkdir -p "$OUT"
[ -d $T ] || git -C "$D0/../../.." worktree add --detach $T $C
git -C $T checkout --detach $C
for m in ${MUTS:-μ0 μ1 μ2 μ3 μ4}; do
  git -C $T checkout -- PriceOfBread.SimCore/Formations.cs PriceOfBread.SimCore/Diplomacy.cs
  python -X utf8 "$D0/mut_uf.py" $T $m > "$OUT/$m.txt" 2>&1 || { echo "$m APPLYFAIL"; continue; }
  dotnet test $T/PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path D:/PoB_work/c128uf_mutart --filter "FullyQualifiedName~UnificationSkipFormedTests" --logger "console;verbosity=normal" >> "$OUT/$m.txt" 2>&1
  echo "$m rc=$? $(grep -E '合計:' "$OUT/$m.txt" | tail -1)"
  grep -E '^\s+(失敗|成功) PriceOfBread' "$OUT/$m.txt" | sed 's/\[.*//'
done
git -C $T checkout -- PriceOfBread.SimCore/Formations.cs PriceOfBread.SimCore/Diplomacy.cs
echo MUTDONE
