#!/usr/bin/env bash
# (1298) c134k49 突然変異（事前登録 §4 の〈突然変異ごとの門の赤〉の表・㉚・c133mr/mut_mr.sh の写し）：bash mut_k49.sh <コミット> <出力フォルダ（D:/…）>
#   コミットを git archive で使い捨ての木へ出し、μ ごとに 1 か所だけ書き換え、InvestMomentumDeletionTests を回す。
#   μ0＝既定を 0.3 に戻す／μ1＝勢いを利幅の倍率へ移す（0.3 を literal で）／μ2＝勢いを労働を買わない生産者にも（係数で）／μ3＝目標の利幅を半分に（勢いは消したまま）。
REV=${1:?}; OUT=${2:?}; R=D:/PoB_work/wt_c134k49; W=D:/PoB_work/c134k49_work/mut
export TEMP=D:/PoB_work/c134k49_work/tmp TMP=D:/PoB_work/c134k49_work/tmp DOTNET_CLI_TELEMETRY_OPTOUT=1
mkdir -p $OUT $TEMP
for MU in M0 M1 M2 M3; do
  T=$W/$MU; rm -rf $T; mkdir -p $T
  git -C $R archive $REV PriceOfBread.SimCore PriceOfBread.Tests | tar -x -C $T || { echo ARCHFAIL; exit 1; }
  python -I - "$T/PriceOfBread.SimCore" $MU <<'PY'
import sys
d, mu = sys.argv[1], sys.argv[2]
E = d + '/EconomyStep.cs'; P = d + '/Coefficients.p08_audit_soldiers.cs'
X = b'Math.Clamp(1.0 + margin + (s.BuysLabor ? coef.InvestExtrapolationWeight * (margin - s.SmoothedMargin) : 0.0), floor, catMax)'
R = {'M0': [(P, b'public double InvestExtrapolationWeight = 0;', b'public double InvestExtrapolationWeight = 0.3;')],
     'M1': [(E, X, b'Math.Clamp(1.0 + margin * (1.0 + (s.BuysLabor ? 0.3 * (margin - s.SmoothedMargin) : 0.0)), floor, catMax)')],
     'M2': [(E, X, b'Math.Clamp(1.0 + margin + coef.InvestExtrapolationWeight * (margin - s.SmoothedMargin), floor, catMax)')],
     'M3': [(E, X, b'Math.Clamp(1.0 + 0.5 * margin + (s.BuysLabor ? coef.InvestExtrapolationWeight * (margin - s.SmoothedMargin) : 0.0), floor, catMax)')]}
for p, o, n in R[mu]:
    b = open(p, 'rb').read()
    assert b.count(o) == 1, (mu, o)
    open(p, 'wb').write(b.replace(o, n))
print('mutated', mu)
PY
  dotnet build $T/PriceOfBread.Tests -c Release --artifacts-path $T/art -v q -nologo > $OUT/build_$MU.txt 2>&1 || { echo "BUILDFAIL $MU" | tee $OUT/mut_$MU.txt; continue; }
  dotnet test $T/PriceOfBread.Tests -c Release --artifacts-path $T/art --no-build --filter "FullyQualifiedName~InvestMomentumDeletionTests" --logger "console;verbosity=normal" > $OUT/raw_$MU.txt 2>&1
  { echo "# $MU（$REV）"; grep -E "^\s+(失敗|成功|合格|Passed|Failed) |合計:|Total" $OUT/raw_$MU.txt; } > $OUT/mut_$MU.txt
  rm -rf $T
done
echo MUTDONE
