#!/usr/bin/env bash
# (1332) c139nc 突然変異（事前登録の〈突然変異ごとの門の赤〉の表・㉚・c135nc/mut_nc.sh の写し）：bash mut_nc.sh <コミット> <出力フォルダ（D:/…）>
#   コミットを git archive で使い捨ての木へ出し、M ごとに Territory.cs（M0〜M5）か InstrumentsCapCol.cs（M6）を 1 か所だけ書き換え、NextCapitalHomeFirstTests と TerritoryTests を回す。
#   試験は旗を盤面で置く（旗の既定はコミットの値のまま）。
REV=${1:?}; OUT=${2:?}; R=D:/PoB_work/wt_c139nc; W=D:/PoB_work/c139nc_work/mut
export TEMP=D:/PoB_work/c139nc_work/tmp TMP=D:/PoB_work/c139nc_work/tmp DOTNET_CLI_TELEMETRY_OPTOUT=1
mkdir -p $OUT $TEMP
for MU in M0 M1 M2 M3 M4 M5 M6; do
  T=$W/$MU; rm -rf $T; mkdir -p $T
  git -C $R archive $REV PriceOfBread.SimCore PriceOfBread.Tests | tar -x -C $T || { echo ARCHFAIL; exit 1; }
  python -I - "$T/PriceOfBread.SimCore" $MU <<'PY'
import sys
d, mu = sys.argv[1], sys.argv[2]
TE = d + '/Territory.cs'; CC = d + '/Instruments/InstrumentsCapCol.cs'
FLAG = b'bool coreFirst = from.Coef.NextCapitalCoreFirst > 0;'
FILT = b'if (coreFirst ? q.Tier != top : (homeOnly && q.Tier == RegionTier.Colonial)) continue;'
TOP = b'if (!(leaving is not null && leaving.Contains(q)) && q.Tier < top) top = q.Tier;'
LEAVE = b'            if ((leaving is not null && leaving.Contains(q))) continue;\n            if (coreFirst'
TIE = b'if (g > capBest || (g == capBest && (cap is null || q.Id < cap.Id))) { capBest = g; cap = q; }'
R = {'M0': (TE, [(FLAG, b'bool coreFirst = false;')]),                                                         # 旗 0 の実装（何もしない）
     'M1': (TE, [(FILT, b'if (coreFirst ? false : (homeOnly && q.Tier == RegionTier.Colonial)) continue;')]),  # 格の順を外す（leaving の外の全ての州）
     'M2': (TE, [(TOP, b'if (!(leaving is not null && leaving.Contains(q)) && (q.Tier == RegionTier.Integrated ? -1 : (int)q.Tier) < (top == RegionTier.Integrated ? -1 : (int)top)) top = q.Tier;')]),   # Integrated を Core の上へ
     'M3': (TE, [(TOP, b'if (q.Tier < top) top = q.Tier;')]),                                                  # 最も高い格の判定が leaving を無視
     'M4': (TE, [(LEAVE, b'            if (coreFirst')]),                                                      # 候補の走査が leaving を無視
     'M5': (TE, [(TIE, TIE.replace(b'q.Id < cap.Id', b'q.Id > cap.Id'))]),                                   # 同点で Id の大きい方
     'M6': (CC, [(b'            if (CcRank(ar.Tier) > top) s.TierGap++;\n', b'')])}                           # 計器が格の下の付け替えを数えない
p, subs = R[mu]
b = open(p, 'rb').read()
for o, n in subs:
    assert b.count(o) == 1, (mu, o)
    b = b.replace(o, n)
open(p, 'wb').write(b); print('mutated', mu)
PY
  dotnet build $T/PriceOfBread.Tests -c Release --artifacts-path $T/art -v q -nologo > $OUT/build_$MU.txt 2>&1 || { echo "BUILDFAIL $MU" | tee $OUT/mut_$MU.txt; continue; }
  dotnet test $T/PriceOfBread.Tests -c Release --artifacts-path $T/art --no-build --filter "FullyQualifiedName~NextCapitalHomeFirstTests|FullyQualifiedName~TerritoryTests" --logger "console;verbosity=normal" > $OUT/raw_$MU.txt 2>&1
  { echo "# $MU（$REV）"; grep -E "^\s+(失敗|成功|合格|Passed|Failed) |合計:|Total" $OUT/raw_$MU.txt; } > $OUT/mut_$MU.txt
  rm -rf $T
done
echo MUTDONE
