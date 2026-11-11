#!/usr/bin/env bash
# (846) c69id：変異を 1 本ずつ使い捨ての写しへ当て、該当の試験の組だけを回し、結果を出力のフォルダに残す。
#   bash mutrun846.sh <出力のフォルダ> <ID>...
#   写し＝D:/PoB_work/c69id_mut（git archive）・元＝D:/PoB_work/wt_c69id（コミット済みの木）・artifacts＝D:/PoB_work/c69id_mutart
WT=D:/PoB_work/wt_c69id; MUT=${MUT:-D:/PoB_work/c69id_mut}; ART=${ART:-D:/PoB_work/c69id_mutart}
OUT=${1:?出力のフォルダ}; shift
mkdir -p "$OUT"
filter_of() {
  case "$1" in
    ML1) echo "FullyQualifiedName~LaborClosedTests" ;;
    ML2) echo "FullyQualifiedName~LaborClosedTests|FullyQualifiedName~WriterRatchetTests" ;;
    MS1) echo "FullyQualifiedName~ServiceClosedTests" ;;
    MS2) echo "FullyQualifiedName~ServiceClosedTests|FullyQualifiedName~WriterRatchetTests" ;;
    ME1|ME2|ME3|ME4|ME5) echo "FullyQualifiedName~StateEduDemandTests" ;;
    MR1|MR2|MR3) echo "FullyQualifiedName~RuleInstrumentTests" ;;
    MF1|MF2|MF3) echo "FullyQualifiedName~FullChainTests" ;;
    *) echo "FullyQualifiedName~NONE" ;;
  esac
}
for ID in "$@"; do
  F=$(python "$WT/scripts/instruments/c69id/mut846.py" files $ID | tr -d '\r')
  python "$WT/scripts/instruments/c69id/mut846.py" restore $ID "$MUT" "$WT" > "$OUT/mut_$ID.txt" 2>&1
  python "$WT/scripts/instruments/c69id/mut846.py" apply $ID "$MUT" >> "$OUT/mut_$ID.txt" 2>&1 || { echo "APPLYFAIL $ID" >> "$OUT/mut_$ID.txt"; continue; }
  echo "filter: $(filter_of $ID)" >> "$OUT/mut_$ID.txt"
  bash "$WT/scripts/instruments/c69id/t846.sh" "$MUT" "$ART" "$(filter_of $ID)" >> "$OUT/mut_$ID.txt" 2>&1
  cp "$ART.test.txt" "$OUT/mut_${ID}_testlog.txt" 2>/dev/null
  python "$WT/scripts/instruments/c69id/mut846.py" restore $ID "$MUT" "$WT" >> "$OUT/mut_$ID.txt" 2>&1
done
echo ALLDONE > "$OUT/alldone_$(basename $MUT).txt"
