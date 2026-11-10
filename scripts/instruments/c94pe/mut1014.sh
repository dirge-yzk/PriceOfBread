#!/usr/bin/env bash
# (1014) c94pe 突然変異（使い捨ての木・c93ms/mutate.sh の型）：渡したコミットの木を D:/PoB_work/c94pe_work/mut/<名> へ写し、Meritocracy.cs を 1 か所だけ書き換え、
#   ColonyPettyEntryTests と MerchInstrumentTests を回す。出力の全文を measurements/m1014_20261110/mut_<名>.txt に残し、日本語の出力（成功!・失敗:・合格:）と英語の出力で絞って 1 行を出す。
#   使い方：bash mut1014.sh <コミット>   （M0〜M6 を順に回す）
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH" AVALONIA_TELEMETRY_OPTOUT=1 PYTHONUTF8=1
H=${1:?コミット}
R="$(cd "$(dirname "$0")/../../.." && pwd)"; MUTW=/d/PoB_work/c94pe_work/mut; OUTD=$R/measurements/m1014_20261110
mkdir -p "$MUTW" "$OUTD"
one() {
  NAME=$1; A=$2; B=$3; DST=$MUTW/$NAME
  rm -rf "$DST"; mkdir -p "$DST"
  (cd "$R" && git archive "$H") | tar -x -C "$DST" || { echo "$NAME ARCHIVEFAIL"; return; }
  if [ -n "$A" ]; then
    python - "$DST/PriceOfBread.SimCore/Meritocracy.cs" "$A" "$B" <<'P' || { echo "$NAME EDITFAIL"; return; }
import sys
p, a, b = sys.argv[1:4]
d = open(p, 'rb').read(); a = a.encode(); b = b.encode()
assert d.count(a) == 1, d.count(a)
open(p, 'wb').write(d.replace(a, b))
P
  fi
  (cd "$DST" && dotnet test PriceOfBread.Tests -c Release --artifacts-path "$MUTW/art_$NAME" --disable-build-servers \
     --filter "FullyQualifiedName~ColonyPettyEntryTests|FullyQualifiedName~MerchInstrumentTests") > "$OUTD/mut_$NAME.txt" 2>&1
  echo "$NAME rc=$? $(grep -E "成功!|失敗!|Passed!|Failed!" "$OUTD/mut_$NAME.txt" | head -1)  赤の試験: $(grep -E "^\s+失敗 |^\s+Failed " "$OUTD/mut_$NAME.txt" | sed 's/^ *//' | tr '\n' ' ')"
  rm -rf "$DST" "$MUTW/art_$NAME"
}
one M0 "" ""
one M1 'if (t.Size <= Eps && !(c.ColonyPettyEntryEmptyTarget > 0 && idx == r.MerchantPopIndex)) continue;' 'if (t.Size <= Eps) continue;'
one M2 'double attract = Math.Max(0, t.EarnedPerMonth.Value) / Math.Max(Eps, t.Size) / Math.Max(Eps, fromIncome * c.PromotionPremium);
                    if (attract > 1.0)' 'double attract = t.Size <= Eps ? 0 : Math.Max(0, t.EarnedPerMonth.Value) / Math.Max(Eps, t.Size) / Math.Max(Eps, fromIncome * c.PromotionPremium);
                    if (attract > 1.0)'
one M3 'from.Size * Math.Min(attract - 1.0, 1.0), from.Size);' 'from.Size * (attract - 1.0), from.Size);'
one M4 '&& from.Savings.Value / from.Size >= avgPerCap)' '&& (c.ColonyPettyEntryEmptyTarget > 0 || from.Savings.Value / from.Size >= avgPerCap))'
one M5 'if (t.Size <= Eps) continue;   // 完全に空の階級は' 'if (t.Size <= Eps && !(c.ColonyPettyEntryEmptyTarget > 0)) continue;   // 完全に空の階級は'
one M6 '&& !(c.ColonyPettyEntryEmptyTarget > 0 && idx == r.MerchantPopIndex)) continue;' '&& !(c.ColonyPettyEntryEmptyTarget > 0)) continue;'
echo MUTDONE
