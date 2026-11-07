#!/usr/bin/env bash
# (951) c84lb 旗 1 の確かめ（使い捨ての worktree・5b109157 を当てる・腕 B は回さない）：LandTaxFixedBasisTests・LandfixInstrumentTests・InstrumentsTests の :2119 の試験。
#   使い方: bash scripts/instruments/c84lb/flag1_951.sh <コミット> <出力ファイル>
set -u
C=$1; OUT=$2
WT=D:/PoB_work/c84lb_work/wt_f1; ART=D:/PoB_work/c84lb_work/art_f1
REPO=C:/Users/dirge/Documents/PriceOfBread
git -C "$REPO" worktree add --detach "$WT" "$C" > /dev/null 2>&1 || { echo "worktree の作成に失敗"; exit 3; }
git -C "$WT" cherry-pick --no-commit 5b109157 > /dev/null 2>&1 || { echo "5b109157 が当たらない"; git -C "$REPO" worktree remove --force "$WT"; exit 3; }
grep -n "public double LandTaxFixedBasis" "$WT/PriceOfBread.SimCore/Coefficients.cs" | cut -c1-70 > "$OUT"
dotnet test "$WT/PriceOfBread.Tests" -c Release --artifacts-path "$ART" --filter "FullyQualifiedName~LandTaxFixedBasisTests|FullyQualifiedName~LandfixInstrumentTests|FullyQualifiedName~InstrumentsTests.建設の州の表と専売の取り分が別の積算器に閉じる" > "$OUT.log" 2>&1
rc=$?
echo "rc=$rc" >> "$OUT"
grep -E "^\s+失敗 |^Actual|テストの合計数|成功:|失敗:|スキップ:" "$OUT.log" >> "$OUT"
dotnet test "$WT/PriceOfBread.Tests" -c Release --artifacts-path "$ART" --no-build --filter "FullyQualifiedName~LandTaxFixedBasisTests|FullyQualifiedName~LandfixInstrumentTests|FullyQualifiedName~InstrumentsTests.建設の州の表と専売の取り分が別の積算器に閉じる" --list-tests 2>/dev/null | grep -c "^    " | sed 's/^/list-tests の数 /' >> "$OUT"
rm -f "$OUT.log"
git -C "$REPO" worktree remove --force "$WT"
rm -rf "$ART"
exit $rc
