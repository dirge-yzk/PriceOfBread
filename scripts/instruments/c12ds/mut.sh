#!/usr/bin/env bash
# (804) 突然変異：使い捨ての木（c12ds の先の detached worktree）で 1 本ずつ変えて、新しい純関数の試験が赤になるかを見る。
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/dotnet:$PATH" AVALONIA_TELEMETRY_OPTOUT=1 PYTHONUTF8=1
W=/d/PoB_work/c12ds_804/wt_mut; A=/d/PoB_work/c12ds_804/art_mut; OUT=/d/PoB_work/c12ds_804/mutations.txt
cd "$W" || exit 1
: > "$OUT"
run() { # 名前 ファイル 旧 新 フィルタ
  python -I - "$2" "$3" "$4" <<'P' || { echo "$1: 置換に失敗" >> "$OUT"; return; }
import sys
p,old,new=sys.argv[1:4]
b=open(p,'rb').read().decode('utf-8')
assert b.count(old)==1,(old,b.count(old))
open(p,'wb').write(b.replace(old,new).encode('utf-8'))
P
  dotnet build PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path "$A" --disable-build-servers > /d/PoB_work/c12ds_804/mut_build.txt 2>&1 || { echo "$1: ビルド失敗" >> "$OUT"; git checkout -q -- .; return; }
  R=$(dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path "$A" --filter "$5" 2>&1 | grep -E "^(Passed|Failed)!|Failed:|Passed:|失敗|合格" | tail -2 | tr '\n' ' ')
  echo "$1: $R" >> "$OUT"
  git checkout -q -- .
}
F1="FullyQualifiedName~行政の距離の割増の純関数"
F2="FullyQualifiedName~内陸の鉄道の減衰の純関数"
run "元の木（変更なし）" PriceOfBread.SimCore/Fiscal.cs "internal static double AdminDistanceMulOfKm" "internal static double AdminDistanceMulOfKm" "$F1|$F2"
run "MB6-1 減衰の重みを落とす" PriceOfBread.SimCore/Fiscal.cs "1.0 + decay * km / Diplomacy.DistanceNormKm : 1.0;" "1.0 + km / Diplomacy.DistanceNormKm : 1.0;" "$F1"
run "MB6-2 IsFinite を外す" PriceOfBread.SimCore/Fiscal.cs "=> km > 0 && double.IsFinite(km) ? 1.0 + decay" "=> km > 0 ? 1.0 + decay" "$F1"
run "MB6-3 km>0 を外す" PriceOfBread.SimCore/Fiscal.cs "=> km > 0 && double.IsFinite(km) ? 1.0 + decay" "=> double.IsFinite(km) ? 1.0 + decay" "$F1"
run "MB6-4 ÷1万を ×1万にする" PriceOfBread.SimCore/Fiscal.cs "1.0 + decay * km / Diplomacy.DistanceNormKm : 1.0;" "1.0 + decay * km * Diplomacy.DistanceNormKm : 1.0;" "$F1"
run "MB11-1 1.0+ を 0.5+ にする" PriceOfBread.SimCore/BilateralMarket.cs "=> railToPort / (1.0 + nearestPortKm" "=> railToPort / (0.5 + nearestPortKm" "$F2"
run "MB11-2 ÷1万を落とす" PriceOfBread.SimCore/BilateralMarket.cs "(1.0 + nearestPortKm / Diplomacy.DistanceNormKm);" "(1.0 + nearestPortKm);" "$F2"
echo done >> "$OUT"
