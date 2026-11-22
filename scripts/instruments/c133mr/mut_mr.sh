#!/usr/bin/env bash
# (1290) c133mr 突然変異（事前登録の〈突然変異ごとの門の赤〉の表・㉚）：bash mut_mr.sh <コミット> <出力フォルダ（D:/…）>
#   コミットを git archive で使い捨ての木へ出し、M ごとに Diplomacy.cs の格の順の枝を 1 か所だけ書き換え、MajorityLabelTierOrderTests と ColonyLabelsFromDataTests を回す。
#   試験は旗を盤面で置く（旗の既定はコミットの値のまま）。
REV=${1:?}; OUT=${2:?}; R=D:/PoB_work/wt_c133mr; W=D:/PoB_work/c133mr_work/mut
export TEMP=D:/PoB_work/c133mr_work/tmp TMP=D:/PoB_work/c133mr_work/tmp DOTNET_CLI_TELEMETRY_OPTOUT=1
mkdir -p $OUT $TEMP
for MU in M1 M2 M3 M4 M5; do
  T=$W/$MU; rm -rf $T; mkdir -p $T
  git -C $R archive $REV PriceOfBread.SimCore PriceOfBread.Tests | tar -x -C $T || { echo ARCHFAIL; exit 1; }
  python -I - "$T/PriceOfBread.SimCore/Diplomacy.cs" $MU <<'PY'
import sys
p, mu = sys.argv[1], sys.argv[2]
b = open(p, 'rb').read()
C = b'w.Coef.MajorityLabelTierOrder > 0 && (ct.Count == 0 || rt.Count == 0))'
N = b'bool needC = ct.Count == 0, needR = rt.Count == 0;'
AC = b'if (needC && r.Culture.Length > 0) ct[r.Culture] = ct.GetValueOrDefault(r.Culture) + pop;'
AR = b'if (needR && r.Religion.Length > 0) rt[r.Religion] = rt.GetValueOrDefault(r.Religion) + pop;'
R = {'M1': [(C, b'w.Coef.MajorityLabelTierOrder > 0)'), (N, b'bool needC = true, needR = true;')],                    # 植民地格を常に足す
     'M2': [(AC, b'if (needC && w.Region.Culture.Length > 0) ct[w.Region.Culture] = 1;'),                                 # 首都の札へ逃がす
            (AR, b'if (needR && w.Region.Religion.Length > 0) rt[w.Region.Religion] = 1;')],
     'M3': [(C, b'w.Coef.MajorityLabelTierOrder > 9 && (ct.Count == 0 || rt.Count == 0))')],                               # 逃げ道を外す
     'M4': [(C, b'w.Coef.MajorityLabelTierOrder > 0 && !w.Regions.Exists(x => x.Tier != RegionTier.Colonial))'),           # 本国格の州の数で選ぶ
            (N, b'bool needC = true, needR = true;')],
     'M5': [(N, b'bool needC = ct.Count == 0, needR = false;')]}                                                             # 宗教だけ逃がさない
for o, n in R[mu]:
    assert b.count(o) == 1, (mu, o)
    b = b.replace(o, n)
open(p, 'wb').write(b); print('mutated', mu)
PY
  dotnet build $T/PriceOfBread.Tests -c Release --artifacts-path $T/art -v q -nologo > $OUT/build_$MU.txt 2>&1 || { echo "BUILDFAIL $MU" | tee $OUT/mut_$MU.txt; continue; }
  dotnet test $T/PriceOfBread.Tests -c Release --artifacts-path $T/art --no-build --filter "FullyQualifiedName~MajorityLabelTierOrderTests|FullyQualifiedName~ColonyLabelsFromDataTests" --logger "console;verbosity=normal" > $OUT/raw_$MU.txt 2>&1
  { echo "# $MU（$REV）"; grep -E "^\s+(失敗|成功|合格|Passed|Failed) |合計:|Total" $OUT/raw_$MU.txt; } > $OUT/mut_$MU.txt
  rm -rf $T
done
echo MUTDONE
