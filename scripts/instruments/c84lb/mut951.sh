#!/usr/bin/env bash
# (951) c84lb 突然変異 M1〜M3（使い捨ての worktree・試験 LandfixInstrumentTests だけ）。出力は measurements/m951_20261108/mut/M*.txt（印は bg.sh が書く）。
#   使い方: bash scripts/instruments/c84lb/mut951.sh <コミット> <出力フォルダ>
#   M1 捨てるのをやめる・M2 毎月全ての写しを捨てる・M3 捨てるのを締め月（四半期の最後の月）だけにする。
set -u
C=$1; OUT=$2
WT=D:/PoB_work/c84lb_work/wt_mut; ART=D:/PoB_work/c84lb_work/art_mut
REPO=C:/Users/dirge/Documents/PriceOfBread
mkdir -p "$OUT"
git -C "$REPO" worktree add --detach "$WT" "$C" > /dev/null 2>&1 || { echo "worktree の作成に失敗"; exit 3; }
F="$WT/PriceOfBread.SimCore/InstrumentsLandbase.cs"
for M in M1 M2 M3; do
  git -C "$WT" checkout -- PriceOfBread.SimCore/InstrumentsLandbase.cs
  python -X utf8 - "$F" "$M" <<'PY'
import sys
p, m = sys.argv[1], sys.argv[2]
b = open(p, 'rb').read().decode('utf-8')
call = "        if (a.Fixed && a.Held.Count > 0) LbDropLeft(w, a);"
keep = "            if (here.Contains(s)) { a.Held[k++] = s; continue; }\n"
if m == 'M1': old, new = call, "        // M1 捨てない"
elif m == 'M2': old, new = keep, ""
else: old, new = call, "        if (a.Fixed && a.Held.Count > 0 && w.IsQuarterEndMonth) LbDropLeft(w, a);"
assert b.count(old) == 1, m
open(p, 'wb').write(b.replace(old, new).encode('utf-8'))
PY
  dotnet test "$WT/PriceOfBread.Tests" -c Release --artifacts-path "$ART" --filter "FullyQualifiedName~LandfixInstrumentTests" > "$OUT/$M.log" 2>&1
  echo "rc=$?" > "$OUT/$M.txt"
  grep -E "^\s+(成功|失敗) |^Actual|テストの合計数|成功:|失敗:" "$OUT/$M.log" >> "$OUT/$M.txt"
  rm -f "$OUT/$M.log"
done
git -C "$WT" checkout -- PriceOfBread.SimCore/InstrumentsLandbase.cs
git -C "$REPO" worktree remove --force "$WT"
rm -rf "$ART"
echo done
