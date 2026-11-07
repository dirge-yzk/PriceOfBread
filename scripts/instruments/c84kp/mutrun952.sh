#!/usr/bin/env bash
# (952) c84kp 突然変異の試験：使い捨ての木で、点火の形（ign）と M1〜M5 ごとに OpeningColonyDominanceTests を回し、試験ごとの合否を書く。
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH"
T=D:/PoB_work/c84kp_work/mut; O=D:/PoB_work/c84kp_work/mut_out; mkdir -p "$O"
for M in ign M1 M2 M3 M4 M5; do
  git -C "$T" checkout -q -- . || exit 9
  python -X utf8 D:/PoB_work/c84kp_work/mutate952.py "$T" ign || exit 9
  if [ "$M" != ign ]; then python -X utf8 D:/PoB_work/c84kp_work/mutate952.py "$T" "$M" || exit 9; fi
  git -C "$T" diff --stat > "$O/mut952_$M.diffstat"
  dotnet test "$T/PriceOfBread.Tests/PriceOfBread.Tests.csproj" -c Release --artifacts-path D:/PoB_work/c84kp_work/mut_art \
    --filter "FullyQualifiedName~OpeningColonyDominanceTests" --logger "console;verbosity=normal" > "$O/mut952_$M.log" 2>&1
  echo "$M rc=$?" >> "$O/mut952_rc.txt"
done
git -C "$T" checkout -q -- .
