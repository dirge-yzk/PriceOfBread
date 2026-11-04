#!/usr/bin/env bash
# (819) c65fl フルスイート（Release・Category!=FullLength と =FullLength を分けて回す）・続けて savetest と leakhunt（saveleak.sh）。10 種のバッチと並べない（README §1-2）。
#   bash suite819.sh <コミット>   worktree D:/PoB_work/wt_c65fl の git archive の写しで回す（c14fl/suite815.sh の写し＝作業フォルダと木だけ c65fl へ・saveleak を足した）
export PATH="/c/Program Files/dotnet:/usr/bin:/c/Program Files/Git/usr/bin:$PATH" AVALONIA_TELEMETRY_OPTOUT=1
H=${1:?コミット}; W=D:/PoB_work/c65fl_work/suite; S=$W/suitesrc
rm -rf $S && mkdir -p $S && (cd D:/PoB_work/wt_c65fl && git archive $H) | tar -x -C $S || exit 1
cd $S
dotnet build PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path $W/art_suite --disable-build-servers > $W/suite_build.txt 2>&1 || { echo BUILDFAIL; exit 1; }
dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $W/art_suite --list-tests --filter "Category!=FullLength" > $W/list_nonfull.txt 2>&1
dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $W/art_suite --list-tests --filter "Category=FullLength" > $W/list_full.txt 2>&1
T0=$(date +%s); dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $W/art_suite --filter "Category!=FullLength" > $W/suite_nonfull.txt 2>&1; echo "nonfull rc=$? $(( $(date +%s) - T0 ))s"
T0=$(date +%s); dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $W/art_suite --filter "Category=FullLength" > $W/suite_full.txt 2>&1; echo "full rc=$? $(( $(date +%s) - T0 ))s"
echo "list nonfull $(grep -c '^    ' $W/list_nonfull.txt) full $(grep -c '^    ' $W/list_full.txt)"
grep -h "合計\|中止\|Total tests\|aborted" $W/suite_nonfull.txt $W/suite_full.txt | tail -4
grep -h -A2 "^\s*失敗 " $W/suite_nonfull.txt $W/suite_full.txt | head -40
echo SUITEDONE
T0=$(date +%s); POB_WORK=$W SAVELEAK_SRC=$S bash D:/PoB_work/wt_c65fl/scripts/instruments/saveleak.sh > $W/saveleak.txt 2>&1; echo "saveleak rc=$? $(( $(date +%s) - T0 ))s"
tail -3 $W/savetest.txt; tail -3 $W/leakhunt.txt; tail -2 $W/saveleak.log
echo SAVELEAKDONE
