#!/usr/bin/env bash
# (1343) c141wd 突然変異（事前登録の〈突然変異ごとの門の赤〉の表・㉚・c140hf/mut_hf.sh の写し）：bash mut_wd.sh <コミット> <出力フォルダ（D:/…）>
#   コミットを git archive で使い捨ての木へ出し、μ ごとに Simulation.cs を 1 か所だけ書き換え、WarmupDecisionSymmetryTests を回す（旗は試験が盤面で置く）。
REV=${1:?}; OUT=${2:?}; R=D:/PoB_work/wt_c141wd; W=D:/PoB_work/c141wd_work/mut
export TEMP=D:/PoB_work/c141wd_work/tmp TMP=D:/PoB_work/c141wd_work/tmp DOTNET_CLI_TELEMETRY_OPTOUT=1
mkdir -p $OUT $TEMP
for MU in M0 M1 M2 M3; do
  T=$W/$MU; rm -rf $T; mkdir -p $T
  git -C $R archive $REV PriceOfBread.SimCore PriceOfBread.Tests | tar -x -C $T || { echo ARCHFAIL; exit 1; }
  rm -f $T/PriceOfBread.Tests/ZzProbeWd0.cs
  python -I - "$T/PriceOfBread.SimCore/Simulation.cs" $MU <<'PY'
import sys
p, mu = sys.argv[1], sys.argv[2]
G = b'bool decideNow = !(World.WarmupFrozen && World.Coef.WarmupNoDecisions > 0);'
R = {'M0': [(G, b'bool decideNow = true;')],                                                                       # 旗を読まない（旧の規則）
     'M1': [(b'if (decideNow && World.SelfPower is { } self)', b'if (World.SelfPower is { } self)')],              # mu1 DecideQuarter だけ止める（近代化の率と海軍の配分は助走でも走る）
     'M2': [(G, b'bool decideNow = !(!World.WarmupFrozen && World.Coef.WarmupNoDecisions > 0);')],                 # mu2 WarmupFrozen の向きを逆にする
     'M3': [(G, b'bool decideNow = !(World.WarmupFrozen && World.Coef.WarmupNoDecisions > 0 && World.SelfPower?.IsPlayer == true);')]}   # mu3 IsPlayer の国だけ止める
b = open(p, 'rb').read()
for o, n in R[mu]:
    assert b.count(o) == 1, (mu, o)
    b = b.replace(o, n)
open(p, 'wb').write(b); print('mutated', mu)
PY
  dotnet build $T/PriceOfBread.Tests -c Release --artifacts-path $T/art -v q -nologo > $OUT/build_$MU.txt 2>&1 || { echo "BUILDFAIL $MU" | tee $OUT/mut_$MU.txt; continue; }
  dotnet test $T/PriceOfBread.Tests -c Release --artifacts-path $T/art --no-build --filter "FullyQualifiedName~WarmupDecisionSymmetryTests" --logger "console;verbosity=normal" > $OUT/raw_$MU.txt 2>&1
  { echo "# $MU（$REV）"; grep -E "^\s+(失敗|Failed) |合計:|Total" $OUT/raw_$MU.txt; } > $OUT/mut_$MU.txt
  rm -rf $T
done
echo MUTDONE > $OUT/mut.mark
