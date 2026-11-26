#!/usr/bin/env bash
# (1385) c147wb 突然変異（事前登録 [3]）：bash mut_wb.sh <コミット> <出力フォルダ（D:/…）>
#   μ1a 闘争度の厭戦の項を戻す・μ1b 保持力の戦時の乗数を戻す・μ1c 天命の厭戦の項を戻す・μ2 D̄ を満足へ戻す（旗 1 でも旧の式）・μ3 Credit の旗の判定を外す（常に D̄）
REV=${1:?}; OUT=${2:?}; R=D:/PoB_work/wt_c147wb; W=D:/PoB_work/c147wb_work/mut
export TEMP=D:/PoB_work/c147wb_work/tmp TMP=D:/PoB_work/c147wb_work/tmp DOTNET_CLI_TELEMETRY_OPTOUT=1
mkdir -p $OUT $TEMP
for MU in M1a M1b M1c M2 M3; do
  T=$W/$MU; rm -rf $T; mkdir -p $T
  git -C $R archive $REV PriceOfBread.SimCore PriceOfBread.Tests | tar -x -C $T || { echo ARCHFAIL; exit 1; }
  python -I - "$T/PriceOfBread.SimCore" $MU <<'PY'
import sys
d, mu = sys.argv[1], sys.argv[2]
PO = d + '/Politics.cs'; SP = d + '/SelfPlay.cs'; CR = d + '/Credit.cs'
R = {'M1a': [(PO, b'double sum = voice + minorityMil + famineMil;', b'double sum = voice + minorityMil + 0.35 * (w.SelfPower?.WarExhaustion ?? 0) + famineMil;', 1)],
     'M1b': [(PO, b'return w.BaseCoercion * w.Legitimacy * w.FiscalFundingRatio.Value;', b'return w.BaseCoercion * w.Legitimacy * w.FiscalFundingRatio.Value * (1.0 - 0.15 * (w.SelfPower?.WarExhaustion ?? 0));', 1)],
     'M1c': [(PO, b'double dynPerf = dmSat + dmFood;', b'double dynPerf = dmSat + dmFood - 0.4 * (w.SelfPower?.WarExhaustion ?? 0);', 1)],
     'M2': [(SP, b'w.Coef.SatReadersToDiscontent > 0 ? Politics.AvgMassDiscontent(w)', b'w.Coef.SatReadersToDiscontent > 9 ? Politics.AvgMassDiscontent(w)', 1), (CR, b'bool dBar = c.SatReadersToDiscontent > 0;', b'bool dBar = c.SatReadersToDiscontent > 9;', 1)],
     'M3': [(CR, b'bool dBar = c.SatReadersToDiscontent > 0;', b'bool dBar = true;', 1)]}
for p, o, n, k in R[mu]:
    b = open(p, 'rb').read().replace(b'\r\n', b'\n')
    assert b.count(o) == k, (mu, o, b.count(o))
    open(p, 'wb').write(b.replace(o, n))
print('mutated', mu)
PY
  dotnet build $T/PriceOfBread.Tests -c Release --artifacts-path $T/art -v q -nologo > $OUT/build_$MU.txt 2>&1 || { echo "BUILDFAIL $MU" | tee $OUT/mut_$MU.txt; continue; }
  dotnet test $T/PriceOfBread.Tests -c Release --artifacts-path $T/art --no-build --filter "FullyQualifiedName~SatReadersToDiscontentTests|FullyQualifiedName~WarWearyPoliticsCutTests|FullyQualifiedName~CoercionNowTests|FullyQualifiedName~ScalarDeletionTests|FullyQualifiedName~RevoltIgnitionTests" --logger "console;verbosity=normal" > $OUT/raw_$MU.txt 2>&1
  { echo "# $MU（$REV）"; grep -E "^\s+(失敗|Failed) |合計:|Total" $OUT/raw_$MU.txt; } > $OUT/mut_$MU.txt
  rm -rf $T
done
echo MUTDONE > $OUT/mut.mark
