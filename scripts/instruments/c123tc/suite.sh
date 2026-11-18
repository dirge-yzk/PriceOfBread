#!/bin/bash
# (1221) c123tc：フルスイート（2 プロジェクト・Release・隔離ビルド）。出力先 $1・木 $2・ビルド先 $3
OUT=$1; TREE=$2; ART=$3
mkdir -p "$OUT"; cd "$TREE" || exit 1
export TEMP=D:/PoB_work/c123tc_work/tmp TMP=D:/PoB_work/c123tc_work/tmp; mkdir -p "$TEMP"
echo "# $(git rev-parse HEAD)" > "$OUT/suite_log.txt"
for P in PriceOfBread.Tests PriceOfBread.Tests.Ui; do
  dotnet build $P -c Release --artifacts-path "$ART" > "$OUT/build_$P.txt" 2>&1
  dotnet test $P -c Release --no-build --artifacts-path "$ART" --list-tests --filter "Category!=FullLength" > "$OUT/list_${P}_nonfull.txt" 2>&1
  dotnet test $P -c Release --no-build --artifacts-path "$ART" --list-tests --filter "Category=FullLength" > "$OUT/list_${P}_full.txt" 2>&1
done
for P in PriceOfBread.Tests PriceOfBread.Tests.Ui; do
  S=$(date +%s)
  dotnet test $P -c Release --no-build --artifacts-path "$ART" --filter "Category!=FullLength" --logger "trx;LogFileName=nonfull_$P.trx" --results-directory "$OUT" > "$OUT/suite_nonfull_$P.txt" 2>&1
  echo "nonfull $P rc=$? $(( $(date +%s) - S ))s" >> "$OUT/suite_log.txt"
done
S=$(date +%s)
dotnet test PriceOfBread.Tests -c Release --no-build --artifacts-path "$ART" --filter "Category=FullLength" --logger "trx;LogFileName=full_PriceOfBread.Tests.trx" --results-directory "$OUT" > "$OUT/suite_full_PriceOfBread.Tests.txt" 2>&1
echo "full PriceOfBread.Tests rc=$? $(( $(date +%s) - S ))s" >> "$OUT/suite_log.txt"
echo done > "$OUT/suite.mark"
