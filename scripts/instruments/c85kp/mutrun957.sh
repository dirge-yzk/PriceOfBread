#!/usr/bin/env bash
# (957) c85kp 突然変異の試験（c84kp/mutrun952.sh の型）：使い捨ての木 D:/PoB_work/c85kp_work/mut（c85kp の T3′ のコミットの detached worktree＝点火の形）で、
#   形そのもの（ign）と M1〜M6 ごとに OpeningColonyDominanceTests（T1〜T4・T3′）を回し、試験ごとの合否を書く。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/c85kp_work/mut.mark bash scripts/instruments/c85kp/mutrun957.sh
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH"
SP="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
T=D:/PoB_work/c85kp_work/mut; O=D:/PoB_work/c85kp_work/mut_out; mkdir -p "$O"; [ -z "$MUTS" ] && rm -f "$O/mut957_rc.txt"
for M in ${MUTS:-ign M1 M2 M3 M4 M5 M6}; do   # MUTS="M6" で一部だけ回し直す
  git -C "$T" checkout -q -- . || exit 9
  if [ "$M" != ign ]; then python -X utf8 "$SP/mutate957.py" "$T" "$M" || exit 9; fi
  git -C "$T" diff --stat > "$O/mut957_$M.diffstat"
  dotnet test "$T/PriceOfBread.Tests/PriceOfBread.Tests.csproj" -c Release --artifacts-path D:/PoB_work/c85kp_work/mut_art --disable-build-servers \
    --filter "FullyQualifiedName~OpeningColonyDominanceTests" --logger "console;verbosity=normal" > "$O/mut957_$M.log" 2>&1
  echo "$M rc=$?" >> "$O/mut957_rc.txt"
  grep -E "^\s+(成功|失敗|合格|Passed|Failed) " "$O/mut957_$M.log" | sed -E 's/\[[^]]*\]$//' > "$O/mut957_$M.txt"
done
git -C "$T" checkout -q -- .
