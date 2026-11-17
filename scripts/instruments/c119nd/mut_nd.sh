#!/usr/bin/env bash
# (1193) c119nd 突然変異（使い捨ての木で 1 本ずつ当てて T1・T2 を回し、戻す）。
#   使い方: bash mut_nd.sh <使い捨ての木（段 B のコミット）> <出力フォルダ>
#   M1 床を国民意識の条件なしで戻す → T1 赤／M2 分離の圧 ×0.5 → T2 分離 赤／M3 戦時の徴兵の割合 ×0.95 → T2 徴兵 赤／
#   M4 暴力の政体の変化で政治意識を +0.25×(1−v) → T2 革命の後 赤。
export TEMP=D:/PoB_work/c119nd_work/tmp TMP=D:/PoB_work/c119nd_work/tmp PYTHONUTF8=1
T=${1:?木}; OUT=${2:?出力}; mkdir -p "$OUT"
cd "$T" || exit 2
mut() {   # mut <名> <ファイル> <置き換え前> <置き換え後>
  local name=$1 f=$2
  cp "$f" "$OUT/$name.orig"
  python -X utf8 - "$f" "$3" "$4" <<'PY'
import sys
p, o, n = sys.argv[1], sys.argv[2].encode('utf-8'), sys.argv[3].encode('utf-8')
b = open(p, 'rb').read()
assert b.count(o) == 1, (p, b.count(o))
open(p, 'wb').write(b.replace(o, n))
PY
  dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path D:/PoB_work/c119nd_artx --filter "FullyQualifiedName~NationalConsciousnessRemovedTests" --logger "console;verbosity=normal" > "$OUT/$name.log" 2>&1
  echo "$name rc=$?" | tee -a "$OUT/summary.txt"
  grep -E "^\s+(失敗|成功|Failed|Passed) PriceOfBread|NationalConsciousnessRemovedTests\.[A-Za-z0-9_]+ \[|合計|失敗!|成功!" "$OUT/$name.log" | sed "s/^/  /" >> "$OUT/summary.txt"
  cp "$OUT/$name.orig" "$f"; rm -f "$OUT/$name.orig"
}
: > "$OUT/summary.txt"
mut M0none PriceOfBread.SimCore/Politics.cs "        double orderFloor = 1.0 - hope;" "        double orderFloor = 1.0 - hope;"
mut M1 PriceOfBread.SimCore/Politics.cs "        double legFloor = orderFloor * (1.0 - PoliticalQuantities.Clamp01(w.DefeatShock));" "        double legFloor = Math.Max(w.DynasticMandate >= 0.6 && w.Polity == Polity.ConstitutionalMonarchy && w.Crown != CrownForm.None ? 0.6 * w.DynasticMandate : 0, orderFloor) * (1.0 - PoliticalQuantities.Clamp01(w.DefeatShock));"
mut M2 PriceOfBread.SimCore/Politics.cs "            pressure *= 1.0 - PoliticalQuantities.Clamp01(RegionAvgSuffrageInclusion(w, r));" "            pressure *= 1.0 - PoliticalQuantities.Clamp01(RegionAvgSuffrageInclusion(w, r)); pressure *= 0.5;"
mut M3 PriceOfBread.SimCore/Military.cs "        if (atWar && w.Mobilized) conscriptFrac = Math.Max(conscriptFrac, c.MobilizationConscriptFrac.Value);" "        if (atWar && w.Mobilized) conscriptFrac = Math.Max(conscriptFrac, c.MobilizationConscriptFrac.Value); conscriptFrac *= 0.95;"
mut M4 PriceOfBread.SimCore/Politics.cs "            if (IsSeizure(target)) w.RuptureWindowQuarters" "            foreach (var p in w.AllPops()) p.Consciousness = PoliticalQuantities.Clamp01(p.Consciousness + 0.25 * (1.0 - p.Consciousness));
            if (IsSeizure(target)) w.RuptureWindowQuarters"
git status --short | head -5 >> "$OUT/summary.txt"
echo MUTDONE >> "$OUT/summary.txt"
