#!/usr/bin/env bash
# (930) c81la：(909) mut909.sh の写し（作業フォルダを c81la_work/mut909 へ・置き換えの台本は c78ls/mutapply909.py のまま）。元の注：(909) c78ls 使い捨ての木の突然変異（試験だけ・計算の腕は回さない）：事前登録 [5] の M0・M1・M7b・M8・M3・M4 を 1 本ずつ当て、
#   LandOwnershipDataTests と LandShareTests.T5 の赤と緑を数える。出力 $W/mutation.txt（合流の判断まで残す・measurements/m909_20261107/mutation.txt へ写す）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/c81la_work/mut909.mark bash scripts/instruments/c81la/mut930.sh <コミット>
export PATH="/c/Program Files/dotnet:/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:$PATH" AVALONIA_TELEMETRY_OPTOUT=1 PYTHONUTF8=1
SP="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"; REPO="$(cd "$SP/../../.." && pwd)"
C=${1:?コミット}; W=D:/PoB_work/c81la_work/mut909; mkdir -p $W; T=$W/mut_tree; A=$W/art_mut; OUT=$W/mutation.txt
git -C "$REPO" worktree add --detach "$T" "$C" > /dev/null 2>&1 || { echo WTFAIL; exit 9; }
echo "# (930)〔(909) の M0・M1・M7b・M8・M3・M4〕突然変異（使い捨ての木 $T・コミット $C）" > $OUT
for M in M0 M1 M7b M8 M3 M4; do
  git -C "$T" checkout -- . > /dev/null 2>&1
  python -X utf8 "$SP/../c78ls/mutapply909.py" "$T" $M >> $OUT 2>&1 || { echo "$M APPLYFAIL" >> $OUT; continue; }
  dotnet build "$T/PriceOfBread.Tests/PriceOfBread.Tests.csproj" -c Release --artifacts-path $A --disable-build-servers > $W/mut_build_$M.txt 2>&1 || { echo "$M BUILDFAIL" >> $OUT; tail -5 $W/mut_build_$M.txt >> $OUT; continue; }
  dotnet test "$T/PriceOfBread.Tests/PriceOfBread.Tests.csproj" -c Release --no-build --artifacts-path $A --filter "FullyQualifiedName~LandOwnershipDataTests|FullyQualifiedName~LandShareTests.T5_" > $W/mut_test_$M.txt 2>&1
  echo "== $M rc=$?" >> $OUT
  grep -h "合格:\|^\s*失敗 " $W/mut_test_$M.txt | sed 's/^ *//' >> $OUT
done
git -C "$T" checkout -- . > /dev/null 2>&1
git -C "$REPO" worktree remove --force "$T"
rm -rf $A
echo MUTDONE >> $OUT
