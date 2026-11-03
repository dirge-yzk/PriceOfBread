#!/usr/bin/env bash
# (805) c12cm 試験の突然変異：測る木の写しに誤りを 1 つずつ入れ、足した試験が赤になることを確かめる（木そのものは変えない）。
#   使い方: bash scripts/instruments/c12cm/mut_tests.sh <測る木> <出力ファイル>   （長い＝Bash を run_in_background で）
#   各突然変異の前に、突然変異が触る 4 本のソースを測る木から写し直す（前の突然変異を持ち越さない）。最後に何も入れない状態の試験（対照＝全部緑）も回す。
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH" PYTHONUTF8=1 AVALONIA_TELEMETRY_OPTOUT=1
T="${1:?木}"; OUT="${2:?出力}"; D="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
M=D:/PoB_work/c12cm/mut_tree; ART=D:/PoB_work/c12cm/art_mut
rm -rf $M && mkdir -p $M && tar -cf - -C $T --exclude=bin --exclude=obj --exclude=.git --exclude=docs --exclude=measurements --exclude=uishot --exclude=.vs . | tar -xf - -C $M || exit 1
: > $OUT
F_HOUSING="FullyQualifiedName~HousingGateSharedTests|FullyQualifiedName~UrbanFlowRealTests"
F_SAT="FullyQualifiedName~HousingGateSharedTests|FullyQualifiedName~FactoryEquipmentTests|FullyQualifiedName~ModernizeReturnAndScoreTests"
F_CP="FullyQualifiedName~CpeaceColumnsTests"
run() {  # <名前> <filter>
  local name="$1" filt="$2"
  for f in Diplomacy InstrumentsCpeace PopDecision EconomyStep; do cp $T/PriceOfBread.SimCore/$f.cs $M/PriceOfBread.SimCore/$f.cs; done
  if [ "$name" != "ctrl" ]; then MU=$name python -X utf8 -I $D/mutate.py $M > /dev/null || { echo "$name 当たらず" >> $OUT; return; }; fi
  dotnet build $M/PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path $ART --disable-build-servers > $ART.build.$name.txt 2>&1 || { echo "$name ビルド失敗" >> $OUT; return; }
  local r; r=$(dotnet test $M/PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $ART --filter "$filt" 2>&1 | grep -E "^(成功|失敗)!|  失敗 PriceOfBread" | head -8)
  echo "== $name（filter=$filt）" >> $OUT; echo "$r" >> $OUT
}
run ctrl "$F_HOUSING|$F_SAT|$F_CP"
run Mu1 "$F_HOUSING"; run Mu2 "$F_HOUSING"; run Mu3 "$F_HOUSING"
run Mu4 "$F_SAT"; run Mu5 "$F_SAT"
run Mu7 "$F_CP"; run Mu8 "$F_CP"; run Mu9 "$F_CP"; run Mu10 "$F_CP"
echo MUTTESTSDONE >> $OUT
