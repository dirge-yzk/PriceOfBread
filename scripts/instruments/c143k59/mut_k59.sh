#!/usr/bin/env bash
# (1356) c143k59 突然変異（事前登録の〈突然変異ごとの門の赤〉の表・㉚・c140hf/mut_hf.sh の写し）：bash mut_k59.sh <コミット> <出力フォルダ（D:/…）>
#   コミットを git archive で使い捨ての木へ出し、μ ごとに Simulation.cs か LaborSupply.cs を 1 か所だけ書き換え、FemalePartLawFreeTests・GenderTests を回す。
#   試験は旗を盤面で置く（旗の既定はコミットの値のまま）。
REV=${1:?}; OUT=${2:?}; R=D:/PoB_work/wt_c143k59; W=D:/PoB_work/c143k59_work/mut
export TEMP=D:/PoB_work/c143k59_work/tmp TMP=D:/PoB_work/c143k59_work/tmp DOTNET_CLI_TELEMETRY_OPTOUT=1
mkdir -p $OUT $TEMP
for MU in M1 M2 M3; do
  T=$W/$MU; rm -rf $T; mkdir -p $T
  git -C $R archive $REV PriceOfBread.SimCore PriceOfBread.Tests | tar -x -C $T || { echo ARCHFAIL; exit 1; }
  python -I - "$T/PriceOfBread.SimCore" $MU <<'PY'
import sys
d, mu = sys.argv[1], sys.argv[2]
SI = d + '/Simulation.cs'; LS = d + '/LaborSupply.cs'
R = {'M1': (SI, [(b'World.Coef.FemalePartLawFree > 0 ? World.Coef.FemalePartBase :', b'false ? World.Coef.FemalePartBase :')]),            # 旧の式へ戻す（旗を読まない）
     'M2': (SI, [(b'World.Coef.FemalePartLawFree > 0 ? World.Coef.FemalePartBase :', b'World.Coef.FemalePartLawFree > 0 ? World.Coef.FemalePartProperty :')]),   # 旗 1 の枝が FemalePartProperty を読む
     'M3': (LS, [(b'* Need(supplyFill) * r.WomenWageRetention)', b'* Need(supplyFill))')])}                                                      # 困窮の項から賃金保持を外す
p, subs = R[mu]
b = open(p, 'rb').read()
for o, n in subs:
    assert b.count(o) == 1, (mu, o)
    b = b.replace(o, n)
open(p, 'wb').write(b); print('mutated', mu)
PY
  dotnet build $T/PriceOfBread.Tests -c Release --artifacts-path $T/art -v q -nologo > $OUT/build_$MU.txt 2>&1 || { echo "BUILDFAIL $MU" | tee $OUT/mut_$MU.txt; continue; }
  dotnet test $T/PriceOfBread.Tests -c Release --artifacts-path $T/art --no-build --filter "FullyQualifiedName~FemalePartLawFreeTests|FullyQualifiedName~GenderTests" --logger "console;verbosity=normal" > $OUT/raw_$MU.txt 2>&1
  { echo "# $MU（$REV）"; grep -E "^\s+(失敗|Failed) |合計:|Total" $OUT/raw_$MU.txt; } > $OUT/mut_$MU.txt
  rm -rf $T
done
echo MUTDONE > $OUT/mut.mark
