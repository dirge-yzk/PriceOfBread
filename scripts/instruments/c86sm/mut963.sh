#!/usr/bin/env bash
# (963) c86sm 突然変異：使い捨ての木（git worktree --detach）に 1 本ずつ当て、該当の試験の組を回して赤か緑かを書く。出力は measurements/m963_20261108_mut.txt へ写す。
#   bash scripts/instruments/c86sm/mut963.sh <リポの木> <使い捨ての木> <artifacts>
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH" AVALONIA_TELEMETRY_OPTOUT=1
R=${1:?}; W=${2:?}; ART=${3:?}
HB="FullyQualifiedName~HeirDebtAfterSettleColumnsTests|FullyQualifiedName~BillBlockedColumnsTests"
UB="FullyQualifiedName~U4InstrumentTests"
for M in M1:"$HB" M2:"$HB" M3:"$HB" M5:"$UB" M6:"$UB" M7:"$UB"; do
  N=${M%%:*}; F=${M#*:}
  cd "$W" && git checkout -q -- . || exit 9
  python -I -X utf8 "$R/scripts/instruments/c86sm/mut963.py" "$W" "$N" || { echo "$N 適用失敗"; continue; }
  dotnet test PriceOfBread.Tests -c Release --artifacts-path "$ART" --disable-build-servers --filter "$F" > "$ART.$N.txt" 2>&1
  echo "$N rc=$? $(grep -E '成功!|失敗!|error CS' "$ART.$N.txt" | head -2 | tr '\n' ' ')"
  grep -E "^\s+失敗 " "$ART.$N.txt" | head -4
done
cd "$W" && git checkout -q -- .
# M4：ident_check の cpop_s の枝を外す（写しで）→ 道具の試験 a・b・f が赤
cp "$R/scripts/instruments/ident/ident_check.py" "$ART.m4.py"
python -I -X utf8 -c "
import sys
p=sys.argv[1]; b=open(p,'rb').read().decode('utf-8'); o=\"if t == 'cpop_s':\"; assert b.count(o)==1
open(p,'wb').write(b.replace(o,'if False:').encode('utf-8'))" "$ART.m4.py"
echo "M4 $(python -I -X utf8 "$R/scripts/instruments/c86sm/cpop_route_test.py" "$ART.m4work" "$ART.m4.py" 2>&1 | grep -E '^✗|GATE' | tr '\n' ' ' | cut -c1-600)"
