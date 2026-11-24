#!/usr/bin/env bash
cd D:/PoB_work/wt_c141cn_mut || exit 1
F=PriceOfBread.SimCore/Politics.cs
OUT=D:/PoB_work/c141cn_work/mut.out; : > $OUT
run() {
  python -I - "$1" "$2" <<'P'
import sys
a,b=sys.argv[1],sys.argv[2]
f='PriceOfBread.SimCore/Politics.cs'
d=open(f,'rb').read().decode('utf-8')
assert d.count(a)==1,a
open(f,'wb').write(d.replace(a,b).encode('utf-8'))
P
  dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path D:/PoB_work/c141cn_mut_art --filter "FullyQualifiedName~CoercionNowTests" 2>&1 | grep -E "^(失敗|成功|Failed|Passed)|テストの合計|合計:|error" | head -8 >> $OUT
  git checkout -q -- $F
}
echo "== M1 支払い率を落とす" >> $OUT
run "w.BaseCoercion * w.Legitimacy * w.FiscalFundingRatio.Value * warDrain" "w.BaseCoercion * w.Legitimacy * warDrain"
echo "== M2 warDrain を落とす" >> $OUT
run "w.BaseCoercion * w.Legitimacy * w.FiscalFundingRatio.Value * warDrain" "w.BaseCoercion * w.Legitimacy * w.FiscalFundingRatio.Value"
echo "== M3 Legitimacy を落とす" >> $OUT
run "w.BaseCoercion * w.Legitimacy * w.FiscalFundingRatio.Value * warDrain" "w.BaseCoercion * w.FiscalFundingRatio.Value * warDrain"
echo "== 戻した木で緑" >> $OUT
dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path D:/PoB_work/c141cn_mut_art --filter "FullyQualifiedName~CoercionNowTests" 2>&1 | grep -E "^(失敗|成功|Failed|Passed)|テストの合計|合計:|error" | head -8 >> $OUT
