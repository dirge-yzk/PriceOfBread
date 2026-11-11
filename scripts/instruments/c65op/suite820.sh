#!/usr/bin/env bash
# (820) c65op 旗 1 の木のフルスイート（Release・Category!=FullLength と =FullLength を分けて回す）と savetest・leakhunt（㉜・腕 B の前）。10 種のバッチと並べない（README §1-2）。
#   bash suite820.sh <コミット>   worktree D:/PoB_work/wt_c65op の git archive の写しで回す（c14fl/suite815.sh の写し＝作業フォルダと木だけ c65op へ）
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH" AVALONIA_TELEMETRY_OPTOUT=1
H=${1:?コミット}; W=D:/PoB_work/c65op_work/suite; S=$W/suitesrc
rm -rf $S && mkdir -p $S && (cd D:/PoB_work/wt_c65op && git archive $H) | tar -x -C $S || exit 1
cd $S
dotnet build PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path $W/art_suite --disable-build-servers > $W/suite_build.txt 2>&1 || { echo BUILDFAIL; exit 1; }
dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $W/art_suite --list-tests --filter "Category!=FullLength" > $W/list_nonfull.txt 2>&1
dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $W/art_suite --list-tests --filter "Category=FullLength" > $W/list_full.txt 2>&1
T0=$(date +%s); dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $W/art_suite --filter "Category!=FullLength" > $W/suite_nonfull.txt 2>&1; echo "nonfull rc=$? $(( $(date +%s) - T0 ))s"
T0=$(date +%s); dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $W/art_suite --filter "Category=FullLength" > $W/suite_full.txt 2>&1; echo "full rc=$? $(( $(date +%s) - T0 ))s"
echo "list nonfull $(grep -c '^    ' $W/list_nonfull.txt) full $(grep -c '^    ' $W/list_full.txt)"
grep -h "合計\|中止\|Total tests\|aborted" $W/suite_nonfull.txt $W/suite_full.txt | tail -4
grep -h "^\s*失敗 PriceOfBread" $W/suite_nonfull.txt $W/suite_full.txt | head -20
mkdir -p $W/sl; POB_WORK=$W/sl SAVELEAK_SRC=$S bash D:/PoB_work/wt_c65op/scripts/instruments/saveleak.sh; echo "saveleak rc=$?"
grep -h "OK\|NG\|違反\|PASS\|FAIL" $W/sl/savetest.txt $W/sl/leakhunt.txt 2>/dev/null | tail -6
echo SUITEDONE
