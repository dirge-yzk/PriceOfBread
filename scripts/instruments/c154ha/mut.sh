#!/usr/bin/env bash
# (1436) c154ha 突然変異：使い捨ての木で Territory.cs を 1 か所ずつ壊し、ReceiveCapRoomTests の赤と緑を数える。
#   bash mut.sh <使い捨ての木（D:/…）> <出力の表（D:/…）>
T=${1:?木}; OUT=${2:?出力}; A=D:/PoB_work/c154ha_mutart; F=PriceOfBread.SimCore/Territory.cs
cd "$T" || exit 1
mkdir -p "$(dirname "$OUT")"; : > "$OUT"
run() {
  local name="$1" a="$2" b="$3"
  git checkout -q -- $F
  python -X utf8 -c "import sys;p=sys.argv[1];s=open(p,'rb').read();a=sys.argv[2].encode();b=sys.argv[3].encode();assert s.count(a)==1,('not unique',a);open(p,'wb').write(s.replace(a,b))" "$F" "$a" "$b" || { echo "$name 置き換え失敗" >> "$OUT"; return; }
  dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path $A --filter "FullyQualifiedName~ReceiveCapRoomTests" --logger "trx;LogFileName=$name.trx" --results-directory "$(dirname "$OUT")/mut_trx" > "$(dirname "$OUT")/mut_$name.log" 2>&1
  local red=$(grep -oE "\[FAIL\]|失敗 [A-Za-z_.]*ReceiveCapRoomTests\.[^ ]*" "$(dirname "$OUT")/mut_$name.log" | grep -oE "T[0-9a-z]+_[^ ]*" | sed 's/_.*//' | sort -u | tr '\n' ' ')
  echo "$name 赤: ${red:-なし} | $(grep -E "合計" "$(dirname "$OUT")/mut_$name.log" | tail -1)" >> "$OUT"
}
run M0 "double cap = c.ReceiveCapByRoom > 0" "double cap = c.ReceiveCapByRoom > 0"
run M1 "double cap = c.ReceiveCapByRoom > 0" "double cap = c.ReceiveCapByRoom > 99"
run M2 "r.ArableLand - r.SumRuralPopulation())) + JobRoom(r)" "r.ArableLand - r.SumPopulation())) + JobRoom(r)"
run M3 "Math.Max(0.0, r.ArableLand - r.SumRuralPopulation())" "(r.ArableLand - r.SumRuralPopulation())"
run M4 "* ReceiveRoom(dst) * AbsorbFoodHousing(dst, c)" "* ReceiveRoom(dst) * AbsorbCap(dst, c)"
run M5 ")) + JobRoom(r);" ")) + 0.0 * JobRoom(r);"
git checkout -q -- $F
echo MUTDONE >> "$OUT"
