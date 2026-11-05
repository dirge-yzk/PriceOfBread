#!/usr/bin/env bash
# (861) c71id（(854) の mutrun854.sh の写し・作業フォルダの名だけ c71id。変異の台本 mut854.py は c70id のを使う）：変異を 1 本ずつ使い捨ての写しへ当て、該当の試験の組だけを回し、結果を出力のフォルダに残す。
#   bash mutrun854.sh <出力のフォルダ> <ID>...
#   写し＝D:/PoB_work/c71id_mut（git archive のコミット済みの木）・元＝D:/PoB_work/wt_c71id・artifacts＝D:/PoB_work/c71id_mutart
#   使う前：写しを作る＝ git archive HEAD | tar -x -C D:/PoB_work/c71id_mut（コミット済みの木だけ）
WT=D:/PoB_work/wt_c71id; MUT=${MUT:-D:/PoB_work/c71id_mut}; ART=${ART:-D:/PoB_work/c71id_mutart}
OUT=${1:?出力のフォルダ}; shift
mkdir -p "$OUT"
for ID in "$@"; do
  python "$WT/scripts/instruments/c70id/mut854.py" restore $ID "$MUT" "$WT" > "$OUT/mut_$ID.txt" 2>&1
  python "$WT/scripts/instruments/c70id/mut854.py" apply $ID "$MUT" >> "$OUT/mut_$ID.txt" 2>&1 || { echo "APPLYFAIL $ID" >> "$OUT/mut_$ID.txt"; continue; }
  FLT=$(python "$WT/scripts/instruments/c70id/mut854.py" filter $ID | tr -d '\r')
  echo "filter: $FLT" >> "$OUT/mut_$ID.txt"
  bash "$WT/scripts/instruments/c70id/t854.sh" "$MUT" "$ART" "$FLT" >> "$OUT/mut_$ID.txt" 2>&1
  cp "$ART.test.txt" "$OUT/mut_${ID}_testlog.txt" 2>/dev/null
  python "$WT/scripts/instruments/c70id/mut854.py" restore $ID "$MUT" "$WT" >> "$OUT/mut_$ID.txt" 2>&1
done
echo ALLDONE > "$OUT/alldone_$(basename $MUT).txt"
