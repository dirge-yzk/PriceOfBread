#!/usr/bin/env bash
# (1283) c132fg 突然変異（事前登録 [5] の表）：bash mut_fg.sh <コミット> <出力フォルダ（D:/…）>
#   コミットを git archive で使い捨ての木へ出し、μ ごとに PopDecision.cs を 1 か所だけ書き換え、旗 1 にして FamineGateReturnsRuralTests と UrxInstrumentTests を回す。
#   旗の既定はコミットの値のまま（試験は旗を盤面で置く）。
REV=${1:?}; OUT=${2:?}; R=/c/Users/dirge/Documents/PriceOfBread; W=D:/PoB_work/c132fg_work/mut
export TEMP=D:/PoB_work/c132fg_work/tmp TMP=D:/PoB_work/c132fg_work/tmp DOTNET_CLI_TELEMETRY_OPTOUT=1
mkdir -p $OUT $TEMP
for MU in mu1 mu2 mu3 mu4; do
  T=$W/$MU; rm -rf $T; mkdir -p $T
  git -C $R archive $REV PriceOfBread.SimCore PriceOfBread.Tests | tar -x -C $T || { echo ARCHFAIL; exit 1; }
  python -I - "$T/PriceOfBread.SimCore/PopDecision.cs" $MU <<'PY'
import sys
p, mu = sys.argv[1], sys.argv[2]
b = open(p, 'rb').read()
G = b'                if (g < 0) flow = coef.Mobility * g * urban.Size;'
R = {'mu1': (G, b'                if (g > 0) flow = coef.Mobility * g * urban.Size;'),          # g の符号を逆
     'mu2': (b'        if (flow > 0) Move(rural, urban, flow);',                                  # 門の外でも戻す
             b'        if (coef.FamineGateReturnsRural > 0) { double g2 = MigrationValues(region, coef, rural, urban).Gsym; if (g2 < 0) flow = coef.Mobility * g2 * urban.Size; }\n        if (flow > 0) Move(rural, urban, flow);'),
     'mu3': (G, b'                if (g < 0) flow = coef.Mobility * g * rural.Size;'),          # 都市の規模→農村の規模
     'mu4': (G, b'                if (g < 0) flow = 0;')}                                         # 戻さない（旗 1 が何もしない）
o, n = R[mu]; assert b.count(o) == 1, mu
open(p, 'wb').write(b.replace(o, n)); print('mutated', mu)
PY
  dotnet build $T/PriceOfBread.Tests -c Release --artifacts-path $T/art -v q -nologo > $OUT/build_$MU.txt 2>&1 || { echo "BUILDFAIL $MU" | tee $OUT/mut_$MU.txt; continue; }
  dotnet test $T/PriceOfBread.Tests -c Release --artifacts-path $T/art --no-build --filter "FullyQualifiedName~FamineGateReturnsRuralTests|FullyQualifiedName~UrxInstrumentTests" --logger "console;verbosity=normal" > $OUT/raw_$MU.txt 2>&1
  { echo "# $MU（$REV）"; grep -E "^\s+(失敗|成功|合格) |合計:" $OUT/raw_$MU.txt; } > $OUT/mut_$MU.txt
  rm -rf $T
done
echo MUTDONE
