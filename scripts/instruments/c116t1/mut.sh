#!/usr/bin/env bash
# (1172) c116t1 の突然変異を使い捨ての木で 1 本ずつ：bash mut.sh <コミット> <作業フォルダ> <出力> <名:試験の絞り込み>...
#   木は git archive で作る（共有の木で checkout しない）。名ごとに元のファイルへ戻してから mutate.py で置き換え、隔離ビルドで試験を回し、合格と失敗の数を 1 行ずつ書く。
set -u
C="$1"; W="$2"; OUT="$3"; shift 3
SRC="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../.." && pwd)"
export TEMP="$W/tmp" TMP="$W/tmp" PYTHONUTF8=1
mkdir -p "$W/tree" "$W/tmp"
rm -rf "$W/tree"/* ; git -C "$SRC" archive "$C" | tar -x -C "$W/tree"
cp "$W/tree/PriceOfBread.SimCore/Instruments/InstrumentsBuyShadow.cs" "$W/ins.orig"
cp "$W/tree/PriceOfBread.SimCore/BilateralMarket.cs" "$W/bm.orig"
echo "突然変異（コミット $C の写しの木）" > "$OUT"
for spec in "$@"; do
  name="${spec%%:*}"; filt="${spec#*:}"
  cp "$W/ins.orig" "$W/tree/PriceOfBread.SimCore/Instruments/InstrumentsBuyShadow.cs"
  cp "$W/bm.orig" "$W/tree/PriceOfBread.SimCore/BilateralMarket.cs"
  python -X utf8 -I "$SRC/scripts/instruments/c116t1/mutate.py" "$W/tree" "$name" >> "$OUT" || continue
  dotnet test "$W/tree/PriceOfBread.Tests/PriceOfBread.Tests.csproj" -c Release --artifacts-path "$W/art" --filter "$filt" > "$W/$name.log" 2>&1
  rc=$?
  s=$(grep -E "合格:|Passed!|Failed!" "$W/$name.log" | tail -1)
  fails=$(grep -E "^\s+(失敗|Failed) " "$W/$name.log" | sed 's/^\s*//' | cut -c1-160 | tr '\n' ';')
  echo "$name rc=$rc 絞り込み=$filt | $s | 赤の試験: $fails" >> "$OUT"
done
echo "おわり" >> "$OUT"
