#!/usr/bin/env bash
# (937) 使い捨て：InstrumentsTests の建設の閉じが旗 1 の軌道で赤になった件の帰属＝旗 0 の木でヌル腕の揺らし（SupplySlope 0.50000001・0.49999999）でも赤になるか。
export PATH="/c/Program Files/dotnet:/usr/bin:/c/Program Files/Git/usr/bin:$PATH"
SRC=D:/PoB_work/wt_c82lt; B=D:/PoB_work/c82lt_work/np
F='FullyQualifiedName~InstrumentsTests.建設の州の表と専売の取り分が別の積算器に閉じる'
for v in "0.50000001:n1" "0.49999999:n2" "flag1n1:f1n1"; do
  tag=${v##*:}; val=${v%%:*}; T=$B/tree_$tag; rm -rf $T; mkdir -p $T
  (cd $SRC && git archive HEAD PriceOfBread.SimCore PriceOfBread.Tests PriceOfBread.App PriceOfBread.Runner goods.json) | tar -x -C $T
  python -X utf8 - "$T" "$val" <<'PY'
import sys
t, v = sys.argv[1], sys.argv[2]
p = t + '/PriceOfBread.SimCore/Coefficients.cs'
b = open(p, 'rb').read().decode('utf-8')
o1 = 'public double SupplySlope = 0.5;'
o2 = 'public double LandTaxFixedBasis = 1.0;'
assert b.count(o1) == 1 and b.count(o2) == 1
if v == 'flag1n1':
    b = b.replace(o1, 'public double SupplySlope = 0.50000001;')
else:
    b = b.replace(o1, 'public double SupplySlope = ' + v + ';').replace(o2, 'public double LandTaxFixedBasis = 0.0;')
open(p, 'wb').write(b.encode('utf-8'))
PY
  dotnet build $T/PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path $B/art_$tag -v q > $B/build_$tag.txt 2>&1 || { echo "$tag BUILDFAIL"; continue; }
  dotnet test $T/PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $B/art_$tag --filter "$F" > $B/test_$tag.txt 2>&1
  echo "$tag rc=$? $(grep -h '合計:' $B/test_$tag.txt | head -1)"
  grep -h -A1 'エラー メッセージ' $B/test_$tag.txt | tail -1
  rm -rf $B/art_$tag $T
done
echo NPDONE
