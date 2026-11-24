#!/usr/bin/env bash
# (1335) c140hf 突然変異（事前登録の〈突然変異ごとの門の赤〉の表・㉚・c139nc/mut_nc.sh の写し）：bash mut_hf.sh <コミット> <出力フォルダ（D:/…）>
#   コミットを git archive で使い捨ての木へ出し、μ ごとに Territory.cs か MultiNationSim.cs を 1 か所だけ書き換え、HungerFlightTests・TerritoryTests・ConservationTests を回す。
#   試験は旗を盤面で置く（旗の既定はコミットの値のまま）。
REV=${1:?}; OUT=${2:?}; R=D:/PoB_work/wt_c140hf; W=D:/PoB_work/c140hf_work/mut
export TEMP=D:/PoB_work/c140hf_work/tmp TMP=D:/PoB_work/c140hf_work/tmp DOTNET_CLI_TELEMETRY_OPTOUT=1
mkdir -p $OUT $TEMP
for MU in M0 M1 M2 M3; do
  T=$W/$MU; rm -rf $T; mkdir -p $T
  git -C $R archive $REV PriceOfBread.SimCore PriceOfBread.Tests | tar -x -C $T || { echo ARCHFAIL; exit 1; }
  python -I - "$T/PriceOfBread.SimCore" $MU <<'PY'
import sys
d, mu = sys.argv[1], sys.argv[2]
TE = d + '/Territory.cs'; MN = d + '/MultiNationSim.cs'
R = {'M0': (TE, [(b'stay[i] = c.DomesticStayFoodFill > 0 ?', b'stay[i] = false ?')]),                                          # 旗を読まない（旧の規則）
     'M1': (TE, [(b'=> opportunity * Holding(r, c) * r.FoodFill;', b'=> opportunity * Holding(r, c);')]),                   # StayValue から食の充足を外す
     'M2': (TE, [(b'=> opportunity * Holding(r, c) * r.FoodFill;', b'=> opportunity * Holding(r, c) * r.FoodFill * r.FoodFill;')]),   # 食の充足の 2 乗
     'M3': (MN, [(b'double push = Territory.StayValue(r, _coef);', b'double push = Territory.Opportunity(r, _coef) * Territory.Holding(r, _coef);')])}   # 越境の側だけ食の項なしの式へ
p, subs = R[mu]
b = open(p, 'rb').read()
for o, n in subs:
    assert b.count(o) == 1, (mu, o)
    b = b.replace(o, n)
open(p, 'wb').write(b); print('mutated', mu)
PY
  dotnet build $T/PriceOfBread.Tests -c Release --artifacts-path $T/art -v q -nologo > $OUT/build_$MU.txt 2>&1 || { echo "BUILDFAIL $MU" | tee $OUT/mut_$MU.txt; continue; }
  dotnet test $T/PriceOfBread.Tests -c Release --artifacts-path $T/art --no-build --filter "FullyQualifiedName~HungerFlightTests|FullyQualifiedName~TerritoryTests|FullyQualifiedName~ConservationTests" --logger "console;verbosity=normal" > $OUT/raw_$MU.txt 2>&1
  { echo "# $MU（$REV）"; grep -E "^\s+(失敗|Failed) |合計:|Total" $OUT/raw_$MU.txt; } > $OUT/mut_$MU.txt
  rm -rf $T
done
echo MUTDONE > $OUT/mut.mark
