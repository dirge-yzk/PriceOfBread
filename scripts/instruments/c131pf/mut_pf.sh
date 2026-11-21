#!/usr/bin/env bash
# (1277) c131pf 突然変異（c126pf/mut_pf.sh の写し・事前登録 [3] の表）：bash mut_pf.sh <コミット> <出力フォルダ（D:/…）>
#   コミットを git archive で使い捨ての木へ出し、μ ごとに BilateralMarket.cs を 1 か所だけ書き換えて CapSkipSellerPotentialTests を回す。
REV=${1:?}; OUT=${2:?}; R=/c/Users/dirge/Documents/PriceOfBread; W=D:/PoB_work/c131pf_work/mut
export TEMP=D:/PoB_work/c131pf_work/tmp TMP=D:/PoB_work/c131pf_work/tmp DOTNET_CLI_TELEMETRY_OPTOUT=1
mkdir -p $OUT $TEMP
for MU in mu1 mu4 mu5 mu6; do
  T=$W/$MU; rm -rf $T; mkdir -p $T
  git -C $R archive $REV PriceOfBread.SimCore PriceOfBread.Tests | tar -x -C $T || { echo ARCHFAIL; exit 1; }
  python -I - "$T/PriceOfBread.SimCore/BilateralMarket.cs" $MU <<'PY'
import sys
p, mu = sys.argv[1], sys.argv[2]
b = open(p, 'rb').read()
R = {'mu1': (b'addW = qS * Wg * potW;', b'addW = 0.0 * Wg * potW;'),
     'mu4': (b'Math.Max(0, xS - unmetQ - skipQ)', b'Math.Max(0, xS)'),
     'mu5': (b'double newUnmetS = skipQ > 0 ? Math.Min(newUnmet, Math.Max(0, X - doneQ - unmetQ - skipQ)) : newUnmet;', b'double newUnmetS = newUnmet;'),
     'mu6': (b'bool priced = sel.Price * (cheapAfter is not null && cheapAfter[s2] ? lbMulCheap : lbMul) < lp;', b'bool priced = true;')}
o, n = R[mu]; assert b.count(o) == 1, mu
open(p, 'wb').write(b.replace(o, n)); print('mutated', mu)
PY
  dotnet build $T/PriceOfBread.Tests -c Release --artifacts-path $T/art -v q -nologo > $OUT/build_$MU.txt 2>&1 || { echo "BUILDFAIL $MU" | tee $OUT/mut_$MU.txt; continue; }
  dotnet test $T/PriceOfBread.Tests -c Release --artifacts-path $T/art --no-build --filter "FullyQualifiedName~CapSkipSellerPotentialTests" --logger "console;verbosity=normal" > $OUT/raw_$MU.txt 2>&1
  { echo "# $MU（$REV）"; grep -E "^\s+(失敗|成功|合格) |合計:" $OUT/raw_$MU.txt; } > $OUT/mut_$MU.txt
  rm -rf $T/art
done
echo MUTDONE
