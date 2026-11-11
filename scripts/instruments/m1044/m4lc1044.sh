#!/usr/bin/env bash
# (1044) 合流：c98lc の反証役の高 2〈M4（xPrice と xNeed の取り違え）が赤になることは確かめていない〉を晴らす。
#   合わせた木の HEAD を git archive で 2 つ写し、元のまま（緑の見込み）と M4（BilateralMarket.cs の理由の取り違え）で TradeSkipC4Tests を全て回し、試験の名と結果を全文で残す。
#   bash m4lc1044.sh <コミット>
export PATH="/c/Program Files/dotnet:/usr/bin:/c/Program Files/Git/usr/bin:$PATH"
H=${1:?コミット}; W=D:/PoB_work/m1044_work/m4; rm -rf $W; mkdir -p $W
for v in orig m4; do
  S=$W/src_$v; mkdir -p $S; (cd D:/PoB_work/wt_m1044 && git archive $H) | tar -x -C $S || exit 1
  if [ $v = m4 ]; then
    python -X utf8 -I - "$S/PriceOfBread.SimCore/BilateralMarket.cs" <<'PY'
import sys
p=sys.argv[1]; b=open(p,'rb').read()
o='xPrice <= xNeed ? Instruments.TsExit.XPriceSmall'.encode(); n='xPrice >= xNeed ? Instruments.TsExit.XPriceSmall'.encode()
assert b.count(o)==1, b.count(o)
open(p,'wb').write(b.replace(o,n,1)); print('M4 applied')
PY
  fi
  (cd $S && dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path $W/art_$v --disable-build-servers --filter "FullyQualifiedName~TradeSkipC4Tests" --logger "console;verbosity=normal" > $W/test_$v.txt 2>&1); echo "$v rc=$?"
done
echo M4DONE
