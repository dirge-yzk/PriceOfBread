#!/usr/bin/env bash
# (1319) c137k49 突然変異（事前登録 §4 の〈突然変異ごとの門の赤〉の表・㉚・c134k49/mut_k49.sh の写し）：bash mut_k49.sh <コミット> <出力フォルダ（D:/…）>
#   コミットを git archive で使い捨ての木へ出し、μ ごとに 1 か所だけ書き換え、InvestMomentumDeletionTests と C3 の番人（PolicyPoliticsTests の Theory）を回す。
#   μ0＝既定を 0.3 に戻す／μ2＝勢いを労働を買わない生産者にも（係数で）／μ4＝項の符号を返す（係数のまま）／μ5＝項の符号を返し重みを 0.3 の literal で。
REV=${1:?}; OUT=${2:?}; R=D:/PoB_work/wt_c137k49; W=D:/PoB_work/c137k49_work/mut
export TEMP=D:/PoB_work/c137k49_work/tmp TMP=D:/PoB_work/c137k49_work/tmp DOTNET_CLI_TELEMETRY_OPTOUT=1
mkdir -p $OUT $TEMP
for MU in ${MUS:-M0 M2 M4 M5}; do
  T=$W/$MU; rm -rf $T; mkdir -p $T
  git -C $R archive $REV PriceOfBread.SimCore PriceOfBread.Tests | tar -x -C $T || { echo ARCHFAIL; exit 1; }
  python -I - "$T/PriceOfBread.SimCore" $MU <<'PY'
import sys
d, mu = sys.argv[1], sys.argv[2]
E = d + '/EconomyStep.cs'; P = d + '/Coefficients.p08_audit_soldiers.cs'
X = b'Math.Clamp(1.0 + margin + (s.BuysLabor ? coef.InvestExtrapolationWeight * (margin - s.SmoothedMargin) : 0.0), floor, catMax)'
R = {'M0': [(P, b'public double InvestExtrapolationWeight = 0;', b'public double InvestExtrapolationWeight = 0.3;')],
     'M2': [(E, X, b'Math.Clamp(1.0 + margin + coef.InvestExtrapolationWeight * (margin - s.SmoothedMargin), floor, catMax)')],
     'M4': [(E, X, b'Math.Clamp(1.0 + margin + (s.BuysLabor ? coef.InvestExtrapolationWeight * (s.SmoothedMargin - margin) : 0.0), floor, catMax)')],
     'M5': [(E, X, b'Math.Clamp(1.0 + margin + (s.BuysLabor ? 0.3 * (s.SmoothedMargin - margin) : 0.0), floor, catMax)')]}
for p, o, n in R[mu]:
    b = open(p, 'rb').read()
    assert b.count(o) == 1, (mu, o)
    open(p, 'wb').write(b.replace(o, n))
print('mutated', mu)
PY
  dotnet build $T/PriceOfBread.Tests -c Release --artifacts-path $T/art -v q -nologo > $OUT/build_$MU.txt 2>&1 || { echo "BUILDFAIL $MU" | tee $OUT/mut_$MU.txt; continue; }
  dotnet test $T/PriceOfBread.Tests -c Release --artifacts-path $T/art --no-build --filter "FullyQualifiedName~InvestMomentumDeletionTests|FullyQualifiedName~PolicyPoliticsTests.実税が重い" --logger "console;verbosity=normal" > $OUT/raw_$MU.txt 2>&1
  { echo "# $MU（$REV）"; grep -E "^\s+(失敗|成功|合格|Passed|Failed) |合計:|Total" $OUT/raw_$MU.txt; } > $OUT/mut_$MU.txt
  rm -rf $T
done
echo MUTDONE
