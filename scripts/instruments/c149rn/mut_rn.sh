#!/usr/bin/env bash
# (1401) c149rn 突然変異（事前登録 [5] の表・c146wy/mut_wy.sh の写し）：bash mut_rn.sh <コミット> <出力フォルダ（D:/…）>
#   コミットを git archive で使い捨ての木へ出し、μ ごとに 1 か所だけ当てて、この線の試験を回す（旗は盤面で置く＝旗 1 の側が赤になるか）。M0 は当てない対照（全て緑）。
REV=${1:?}; OUT=${2:?}; R=D:/PoB_work/wt_c149rn; W=D:/PoB_work/c149rn_work/mut
export TEMP=D:/PoB_work/c149rn_work/tmp TMP=D:/PoB_work/c149rn_work/tmp DOTNET_CLI_TELEMETRY_OPTOUT=1
mkdir -p $OUT $TEMP
for MU in M0 M1 M2 M3 M4 M5; do
  T=$W/$MU; rm -rf $T; mkdir -p $T
  git -C $R archive $REV PriceOfBread.SimCore PriceOfBread.Tests | tar -x -C $T || { echo ARCHFAIL; exit 1; }
  python -I - "$T/PriceOfBread.SimCore" $MU <<'PY'
import sys
d, mu = sys.argv[1], sys.argv[2]
DI = d + '/Diplomacy.cs'; FA = d + '/Factions.cs'; SP = d + '/SelfPlay.cs'; SI = d + '/Simulation.cs'
R = {'M0': [],
     'M1': [(DI, b'(c.RandomOnlyClimateEpidemic > 0 || rng.NextDouble() < chance)', b'(rng.NextDouble() < chance)', 1)],
     'M2': [(SI, b'if (!(hc.RandomOnlyClimateEpidemic > 0))', b'if (true)', 1)],
     'M3': [(FA, b' && !(c.RandomOnlyClimateEpidemic > 0))', b')', 1)],
     'M4': [(SP, b' || w.Coef.RandomOnlyClimateEpidemic > 0 || w.AiRng', b' || w.AiRng', 1)],
     'M5': [(DI, b'(c.RandomOnlyClimateEpidemic > 0 || rng.NextDouble() < colonizeChance)', b'(rng.NextDouble() < colonizeChance)', 1)]}
for p, o, n, k in R[mu]:
    b = open(p, 'rb').read()
    assert b.count(o) == k, (mu, o, b.count(o))
    open(p, 'wb').write(b.replace(o, n))
print('mutated', mu)
PY
  dotnet build $T/PriceOfBread.Tests -c Release --artifacts-path $T/art -v q -nologo > $OUT/build_$MU.txt 2>&1 || { echo "BUILDFAIL $MU" | tee $OUT/mut_$MU.txt; continue; }
  dotnet test $T/PriceOfBread.Tests -c Release --artifacts-path $T/art --no-build --filter "FullyQualifiedName~RandomOnlyClimateEpidemicTests" --logger "console;verbosity=normal" > $OUT/raw_$MU.txt 2>&1
  { echo "# $MU（$REV）"; grep -E "^\s+(失敗|Failed|成功|Passed) |合計:|Total" $OUT/raw_$MU.txt; } > $OUT/mut_$MU.txt
  rm -rf $T
done
echo MUTDONE > $OUT/mut.mark
