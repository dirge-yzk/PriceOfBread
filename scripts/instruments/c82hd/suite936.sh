#!/usr/bin/env bash
# (936) c82hd suite930.sh の写し（作業フォルダだけ D:/PoB_work/c82hd_work/suite）。元：(930) c81la フルスイート（(909) suite909.sh の写し・作業フォルダだけ SUITE_W〔既定 D:/PoB_work/c81la_work/suite〕へ）。元の注：(909) c78ls フルスイート（(895) suite895.sh の写し・木は worktree そのまま・作業フォルダ c78ls_work）：Release・Category!=FullLength と =FullLength を分けて回し、--list-tests の数と突き合わせる。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/c82hd_work/suite.mark bash scripts/instruments/c82hd/suite936.sh
#   savetest と leakhunt はこの台本の終わりで saveleak.sh を回す。10 種のバッチと並べない。
export PATH="/c/Program Files/dotnet:/usr/bin:/c/Program Files/Git/usr/bin:$PATH" AVALONIA_TELEMETRY_OPTOUT=1
SP="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"; REPO="$(cd "$SP/../../.." && pwd)"
W="${SUITE_W:-D:/PoB_work/c82hd_work/suite}"; A=$W/art_suite; mkdir -p "$W"
cd "$REPO" || exit 9
echo "木 $(git rev-parse HEAD)"
dotnet build PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path $A --disable-build-servers > $W/suite_build.txt 2>&1 || { echo BUILDFAIL; tail -20 $W/suite_build.txt; exit 1; }
for f in "Category!=FullLength:nonfull" "Category=FullLength:full"; do
  dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $A --list-tests --filter "${f%%:*}" > $W/list_${f##*:}.txt 2>&1
  echo "list ${f##*:} $(grep -c '^    ' $W/list_${f##*:}.txt)"
done
T0=$(date +%s); dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $A --filter "Category!=FullLength" > $W/suite_nonfull.txt 2>&1; echo "nonfull rc=$? $(( $(date +%s) - T0 ))s"
T0=$(date +%s); dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $A --filter "Category=FullLength" > $W/suite_full.txt 2>&1; echo "full rc=$? $(( $(date +%s) - T0 ))s"
grep -h "合計\|中止\|Total tests\|aborted\|\[FAIL\]\|成功!\|失敗!" $W/suite_nonfull.txt $W/suite_full.txt | tail -8
grep -h -A2 "^\s*失敗 " $W/suite_nonfull.txt $W/suite_full.txt | head -40
rm -rf $A
T0=$(date +%s); POB_WORK=$W SAVELEAK_SRC="$REPO" bash "$REPO/scripts/instruments/saveleak.sh"; echo "saveleak rc=$? $(( $(date +%s) - T0 ))s"
grep -h "savetest\|leakhunt\|世界の台帳\|終了コード" $W/savetest.txt $W/leakhunt.txt 2>/dev/null | head -8
echo SUITEDONE
