#!/usr/bin/env bash
# (1349) c142ds 突然変異（事前登録の〈突然変異ごとの門の赤〉の表・㉚・c139nc/mut_nc.sh の写し）：bash mut_ds.sh <コミット> <出力フォルダ（D:/…）>
#   コミットを git archive で使い捨ての木へ出し、μ ごとに Territory.cs か MultiNationSim.cs を書き換え、DestFoodSymmetryTests・HungerFlightTests・TerritoryTests を回す。
#   試験は旗を盤面で置く（旗の既定はコミットの値のまま）。
REV=${1:?}; OUT=${2:?}; R=D:/PoB_work/wt_c142ds; W=D:/PoB_work/c142ds_work/mut
export TEMP=D:/PoB_work/c142ds_work/tmp TMP=D:/PoB_work/c142ds_work/tmp DOTNET_CLI_TELEMETRY_OPTOUT=1
mkdir -p $OUT $TEMP
for MU in M0 M1 M2 M3 M4; do
  T=$W/$MU; rm -rf $T; mkdir -p $T
  git -C $R archive $REV PriceOfBread.SimCore PriceOfBread.Tests | tar -x -C $T || { echo ARCHFAIL; exit 1; }
  python -I - "$T/PriceOfBread.SimCore" $MU <<'PY'
import sys
d, mu = sys.argv[1], sys.argv[2]
TE = d + '/Territory.cs'; MN = d + '/MultiNationSim.cs'
R = {'M0': [(TE, b'dv[i] = c.DestFoodSymmetric > 0 ?', b'dv[i] = false ?'), (MN, b'double ov = _coef.DestFoodSymmetric > 0 ?', b'double ov = false ?')],   # 旗を読まない（両側とも旧の規則）
     'M1': [(TE, b'dv[i] = c.DestFoodSymmetric > 0 ?', b'dv[i] = false ?')],                                                     # 国内だけ旧
     'M2': [(MN, b'double ov = _coef.DestFoodSymmetric > 0 ?', b'double ov = false ?')],                                       # 越境だけ旧
     'M3': [(TE, b'* AbsorbCap(dst, c)).Value;', b'* AbsorbCap(dst, c) * (c.DestFoodSymmetric > 0 ? dst.FoodFill : 1.0)).Value;')],   # 旗 1 で国内の上限にも食を掛ける
     'M4': [(TE, b'=> opportunity * r.FoodFill;', b'=> opportunity * r.FoodFill * r.FoodFill;')]}                               # 食の 2 乗
for p, o, n in R[mu]:
    b = open(p, 'rb').read()
    assert b.count(o) == 1, (mu, o)
    open(p, 'wb').write(b.replace(o, n))
print('mutated', mu)
PY
  dotnet build $T/PriceOfBread.Tests -c Release --artifacts-path $T/art -v q -nologo > $OUT/build_$MU.txt 2>&1 || { echo "BUILDFAIL $MU" | tee $OUT/mut_$MU.txt; continue; }
  dotnet test $T/PriceOfBread.Tests -c Release --artifacts-path $T/art --no-build --filter "FullyQualifiedName~DestFoodSymmetryTests|FullyQualifiedName~HungerFlightTests|FullyQualifiedName~TerritoryTests" --logger "console;verbosity=normal" > $OUT/raw_$MU.txt 2>&1
  { echo "# $MU（$REV）"; grep -E "^\s+(失敗|Failed) |合計:|Total" $OUT/raw_$MU.txt; } > $OUT/mut_$MU.txt
  rm -rf $T
done
echo MUTDONE > $OUT/mut.mark
