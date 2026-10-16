#!/bin/bash
# ce5 反証役 bv 中#1 の処置：旗 1（ConsumptionLayersEnabled=1;ConsumptionOpeningLevel=1）で 3 本を 2 つの木で回す
REPO=/c/Users/dirge/Documents/PriceOfBread
W=D:/PoB_work/merge_372
OUTF=$W/ce5_retest.txt
: > $OUTF
for C in 5459c41a f1a2d03d; do
  T=$W/t_$C
  rm -rf $T && mkdir -p $T
  git -C $REPO archive $C | tar -xf - -C $T
  PYTHONUTF8=1 python - "$T/PriceOfBread.SimCore/Coefficients.cs" <<'PY'
import sys
p=sys.argv[1]; b=open(p,'rb').read()
for k in [b'public int ConsumptionLayersEnabled = 0;', b'public int ConsumptionOpeningLevel = 0;']:
    assert b.count(k)==1, k
    b=b.replace(k, k.replace(b'= 0;', b'= 1;'))
open(p,'wb').write(b)
PY
  {
    echo "=== 木 $(git -C $REPO rev-parse $C)（git archive の写し $T）"
    echo "POB_COEF 相当＝既定を書き換えた写し：ConsumptionLayersEnabled=1;ConsumptionOpeningLevel=1"
    grep -n "public int ConsumptionLayersEnabled\|public int ConsumptionOpeningLevel" $T/PriceOfBread.SimCore/Coefficients.cs
  } >> $OUTF
  "/c/Program Files/dotnet/dotnet" test "$T/PriceOfBread.Tests/PriceOfBread.Tests.csproj" -c Release --artifacts-path "$W/art_$C" \
    --filter "FullyQualifiedName~FranceTests.工業化はゆるやかに進む|FullyQualifiedName~InstrumentsTests.符号2の構造と飢え|FullyQualifiedName~InstrumentsTests.段の法の固定の理由" \
    --logger "console;verbosity=normal" > $W/ce5_retest_$C.log 2>&1
  echo "rc=$?" >> $OUTF
  grep -E "^\s+(成功|失敗|Passed|Failed) |合計|エラー メッセージ|^\s{3}\S" $W/ce5_retest_$C.log | grep -v "warning" | head -40 >> $OUTF
done
echo DONE >> $OUTF
