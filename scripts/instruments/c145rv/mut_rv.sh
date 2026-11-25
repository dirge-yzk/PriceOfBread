#!/usr/bin/env bash
# (1371) c145rv 突然変異（事前登録 [3] の表）：bash mut_rv.sh <コミット> <出力フォルダ（D:/…）>
#   コミットを git archive で使い捨ての木へ出し、μ ごとに旗の分岐を 1 か所だけ false にして戻し、新しい試験 2 クラスを回す（旗は盤面で置く）。
REV=${1:?}; OUT=${2:?}; R=D:/PoB_work/wt_c145rv; W=D:/PoB_work/c145rv_work/mut
export TEMP=D:/PoB_work/c145rv_work/tmp TMP=D:/PoB_work/c145rv_work/tmp DOTNET_CLI_TELEMETRY_OPTOUT=1
mkdir -p $OUT $TEMP
for MU in M1 M2 M3 M4 M5 M6; do
  T=$W/$MU; rm -rf $T; mkdir -p $T
  git -C $R archive $REV PriceOfBread.SimCore PriceOfBread.Tests | tar -x -C $T || { echo ARCHFAIL; exit 1; }
  python -I - "$T/PriceOfBread.SimCore" $MU <<'PY'
import sys
d, mu = sys.argv[1], sys.argv[2]
PO = d + '/Politics.cs'; FS = d + '/FactionSnapshot.cs'; ID = d + '/Ideology.cs'
R = {'M1': (PO, b'double warDrain = c.WarWearyPoliticsCut > 0 ? 1.0', b'double warDrain = false ? 1.0'),
     'M2': (PO, b'double warMilTerm = c.WarWearyPoliticsCut > 0 ? 0.0', b'double warMilTerm = false ? 0.0'),
     'M3': (PO, b'double dmWar = c.WarWearyPoliticsCut > 0 ? 0.0', b'double dmWar = false ? 0.0'),
     'M4': (PO, b'if (c.SatReadersToDiscontent > 0)   // (1371)', b'if (false)   // (1371)'),
     'M5': (FS, b'if (w.Coef.SatReadersToDiscontent > 0) return d;', b'if (false) return d;'),
     'M6': (ID, b'mil = w.Coef.SatReadersToDiscontent > 0 ? disc : p.Militancy;', b'mil = false ? disc : p.Militancy;')}
p, o, n = R[mu]
b = open(p, 'rb').read()
assert b.count(o) == 1, (mu, o)
open(p, 'wb').write(b.replace(o, n)); print('mutated', mu)
PY
  dotnet build $T/PriceOfBread.Tests -c Release --artifacts-path $T/art -v q -nologo > $OUT/build_$MU.txt 2>&1 || { echo "BUILDFAIL $MU" | tee $OUT/mut_$MU.txt; continue; }
  dotnet test $T/PriceOfBread.Tests -c Release --artifacts-path $T/art --no-build --filter "FullyQualifiedName~WarWearyPoliticsCutTests|FullyQualifiedName~SatReadersToDiscontentTests" --logger "console;verbosity=normal" > $OUT/raw_$MU.txt 2>&1
  { echo "# $MU（$REV）"; grep -E "^\s+(失敗|Failed) |合計:|Total" $OUT/raw_$MU.txt; } > $OUT/mut_$MU.txt
  rm -rf $T
done
echo MUTDONE > $OUT/mut.mark
