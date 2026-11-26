#!/usr/bin/env bash
# (1401) c149rn 試験の直しの確かめ（事前登録 [10]）：F1＝そのまま（旗 1）・F0＝旗の既定を 0 に戻した木・MX＝直しを外した木（EvalRowOwn→EvalRow・旗 1）。WarInstrumentTests と WriterRatchetTests を回す。
#   bash fixcheck_rn.sh <コミット> <出力フォルダ（D:/…）>
REV=${1:?}; OUT=${2:?}; R=D:/PoB_work/wt_c149rn; W=D:/PoB_work/c149rn_work/fix
export TEMP=D:/PoB_work/c149rn_work/tmp TMP=D:/PoB_work/c149rn_work/tmp DOTNET_CLI_TELEMETRY_OPTOUT=1
mkdir -p $OUT $TEMP
for MU in F1 F0 MX; do
  T=$W/$MU; rm -rf $T; mkdir -p $T
  git -C $R archive $REV PriceOfBread.SimCore PriceOfBread.Tests | tar -x -C $T || { echo ARCHFAIL; exit 1; }
  python -I - "$T" $MU <<'PY'
import sys
d, mu = sys.argv[1], sys.argv[2]
CO = d + '/PriceOfBread.SimCore/Coefficients.p05_port_insurance.cs'; WT = d + '/PriceOfBread.Tests/WarInstrumentTests.cs'
R = {'F1': [], 'F0': [(CO, b'public double RandomOnlyClimateEpidemic = 1.0;', b'public double RandomOnlyClimateEpidemic = 0.0;', 1)],
     'MX': [(WT, b'var r = EvalRowOwn(', b'var r = EvalRow(', 2)]}
for p, o, n, k in R[mu]:
    b = open(p, 'rb').read(); assert b.count(o) == k, (mu, o, b.count(o)); open(p, 'wb').write(b.replace(o, n))
print('variant', mu)
PY
  dotnet build $T/PriceOfBread.Tests -c Release --artifacts-path $T/art -v q -nologo > $OUT/build_$MU.txt 2>&1 || { echo "BUILDFAIL $MU" | tee $OUT/fix_$MU.txt; continue; }
  dotnet test $T/PriceOfBread.Tests -c Release --artifacts-path $T/art --no-build --filter "FullyQualifiedName~WarInstrumentTests|FullyQualifiedName~WriterRatchetTests" --logger "console;verbosity=normal" > $OUT/raw_$MU.txt 2>&1
  { echo "# $MU（$REV）"; grep -E "^\s+失敗 |合計:|Total|成功!|失敗!" $OUT/raw_$MU.txt; } > $OUT/fix_$MU.txt
  rm -rf $T
done
echo FIXDONE > $OUT/fix.mark
