#!/usr/bin/env bash
# (1350) c142lr 突然変異（事前登録の〈突然変異ごとの門の赤〉の表・㉚・c140hf/mut_hf.sh の写し）：bash mut_lr.sh <コミット> <出力フォルダ（D:/…）>
#   コミットを git archive で使い捨ての木へ出し、μ ごとに Government.cs か Simulation.cs を 1 か所だけ書き換え、ProtectedLawReversalTests を回す。
#   試験は旗を盤面で置く（旗の既定はコミットの値のまま）。M0＝書き換えなし（対照）。
REV=${1:?}; OUT=${2:?}; R=D:/PoB_work/wt_c142lr; W=D:/PoB_work/c142lr_work/mut
export TEMP=D:/PoB_work/c142lr_work/tmp TMP=D:/PoB_work/c142lr_work/tmp DOTNET_CLI_TELEMETRY_OPTOUT=1
mkdir -p $OUT $TEMP
for MU in ${MUS:-M0 M1 M2 M3 M4}; do
  T=$W/$MU; rm -rf $T; mkdir -p $T
  git -C $R archive $REV PriceOfBread.SimCore PriceOfBread.Tests | tar -x -C $T || { echo ARCHFAIL; exit 1; }
  python -I - "$T/PriceOfBread.SimCore" $MU <<'PY'
import sys
d, mu = sys.argv[1], sys.argv[2]
GV = d + '/Government.cs'; SM = d + '/Simulation.cs'
L07 = b'        if (a is PolicyAxis.SerfEmancipation or PolicyAxis.LandReform) return true;\n'
FL = b'        if (c.ProtectedLawsReversible > 0) return false;'
R = {'M0': (GV, []),
     'M1': (GV, [(FL, b'        if (false) return false;')]),                                                   # BlockedByIrreversibility の旗を読まない（旧のまま）
     'M2': (GV, [(L07, b'')]),                                                                               # L07 を旗の行より後へ＝旗 1 で農奴解放・土地改革も外れる
     'M3': (SM, [(b'if (World.Coef.ProtectedLawsReversible > 0 || !(', b'if (!(')]),                       # 児童労働の門の旗を読まない（旧のまま）
     'M4': (GV, [(b'if (c.ProtectedLawsReversible > 0 || !regressive', b'if (!regressive')]),             # ReversalCostMul の旗を読まない（旧のまま）
     'F0': (d + '/Coefficients.p06_rank_discovery.cs', [(b'public double ProtectedLawsReversible = 1;', b'public double ProtectedLawsReversible = 0;')])}   # 旗の既定 0 の木（試験の直しを旗 0 でも確かめる）
p, subs = R[mu]
b = open(p, 'rb').read()
for o, n in subs:
    assert b.count(o) == 1, (mu, o)
    b = b.replace(o, n)
if mu == 'M2':
    o = FL + b'   // (1344)'
    assert b.count(o) == 1
    i = b.index(o); j = b.index(b'\n', i) + 1
    b = b[:j] + L07 + b[j:]
open(p, 'wb').write(b); print('mutated', mu)
PY
  dotnet build $T/PriceOfBread.Tests -c Release --artifacts-path $T/art -v q -nologo > $OUT/build_$MU.txt 2>&1 || { echo "BUILDFAIL $MU" | tee $OUT/mut_$MU.txt; continue; }
  dotnet test $T/PriceOfBread.Tests -c Release --artifacts-path $T/art --no-build --filter "${FILT:-FullyQualifiedName~ProtectedLawReversalTests}" --logger "console;verbosity=normal" > $OUT/raw_$MU.txt 2>&1
  { echo "# $MU（$REV）"; grep -E "^\s+(失敗|Failed) |合計:|Total" $OUT/raw_$MU.txt; } > $OUT/mut_$MU.txt
  rm -rf $T
done
echo MUTDONE > $OUT/mut.mark
