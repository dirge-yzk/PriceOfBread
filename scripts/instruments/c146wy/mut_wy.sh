#!/usr/bin/env bash
# (1377) c146wy 突然変異（事前登録 [4] の表・c145rv/mut_rv.sh の写し）：bash mut_wy.sh <コミット> <出力フォルダ（D:/…）>
#   コミットを git archive で使い捨ての木へ出し、μ ごとに 1 か所だけ当てて、旗の試験 3 クラスを回す（旗は盤面で置く・旗 1 の側が赤になるか）。
REV=${1:?}; OUT=${2:?}; R=D:/PoB_work/wt_c146wy; W=D:/PoB_work/c146wy_work/mut
export TEMP=D:/PoB_work/c146wy_work/tmp TMP=D:/PoB_work/c146wy_work/tmp DOTNET_CLI_TELEMETRY_OPTOUT=1
mkdir -p $OUT $TEMP
for MU in M0 M1 M2 M3 M4; do
  T=$W/$MU; rm -rf $T; mkdir -p $T
  git -C $R archive $REV PriceOfBread.SimCore PriceOfBread.Tests | tar -x -C $T || { echo ARCHFAIL; exit 1; }
  python -I - "$T/PriceOfBread.SimCore" $MU <<'PY'
import sys
d, mu = sys.argv[1], sys.argv[2]
PO = d + '/Politics.cs'; IS = d + '/Instruments/InstrumentsScalars.cs'
R = {'M0': [(PO, b'c.WarWearyPoliticsCut > 0', b'c.WarWearyPoliticsCut > 9', 3), (IS, b'c.WarWearyPoliticsCut > 0', b'c.WarWearyPoliticsCut > 9', 1)],
     'M1': [(PO, b'double warDrain = c.WarWearyPoliticsCut > 0 ? 1.0', b'double warDrain = false ? 1.0', 1)],
     'M2': [(PO, b'double warMilTerm = c.WarWearyPoliticsCut > 0 ? 0.0', b'double warMilTerm = false ? 0.0', 1)],
     'M3': [(PO, b'double dmWar = c.WarWearyPoliticsCut > 0 ? 0.0', b'double dmWar = false ? 0.0', 1)],
     'M4': [(IS, b'F(c.WarWearyPoliticsCut > 0 ? 0.0 : -c.DynMandateWarWeight', b'F(false ? 0.0 : -c.DynMandateWarWeight', 1)]}
for p, o, n, k in R[mu]:
    b = open(p, 'rb').read()
    assert b.count(o) == k, (mu, o, b.count(o))
    open(p, 'wb').write(b.replace(o, n))
print('mutated', mu)
PY
  dotnet build $T/PriceOfBread.Tests -c Release --artifacts-path $T/art -v q -nologo > $OUT/build_$MU.txt 2>&1 || { echo "BUILDFAIL $MU" | tee $OUT/mut_$MU.txt; continue; }
  dotnet test $T/PriceOfBread.Tests -c Release --artifacts-path $T/art --no-build --filter "FullyQualifiedName~WarWearyPoliticsCutTests|FullyQualifiedName~CoercionNowTests|FullyQualifiedName~ScalarDeletionTests" --logger "console;verbosity=normal" > $OUT/raw_$MU.txt 2>&1
  { echo "# $MU（$REV）"; grep -E "^\s+(失敗|Failed) |合計:|Total" $OUT/raw_$MU.txt; } > $OUT/mut_$MU.txt
  rm -rf $T
done
echo MUTDONE > $OUT/mut.mark
