#!/usr/bin/env bash
# (1304) c135nc 突然変異（事前登録の〈突然変異ごとの門の赤〉の表・㉚・c133mr/mut_mr.sh の写し）：bash mut_nc.sh <コミット> <出力フォルダ（D:/…）>
#   コミットを git archive で使い捨ての木へ出し、M ごとに Territory.cs（M1〜M6）か InstrumentsCapCol.cs（M7）を 1 か所だけ書き換え、NextCapitalHomeFirstTests と TerritoryTests を回す。
#   試験は旗を盤面で置く（旗の既定はコミットの値のまま）。
REV=${1:?}; OUT=${2:?}; R=D:/PoB_work/wt_c135nc; W=D:/PoB_work/c135nc_work/mut
export TEMP=D:/PoB_work/c135nc_work/tmp TMP=D:/PoB_work/c135nc_work/tmp DOTNET_CLI_TELEMETRY_OPTOUT=1
mkdir -p $OUT $TEMP
for MU in M1 M2 M3 M4 M5 M6 M7; do
  T=$W/$MU; rm -rf $T; mkdir -p $T
  git -C $R archive $REV PriceOfBread.SimCore PriceOfBread.Tests | tar -x -C $T || { echo ARCHFAIL; exit 1; }
  python -I - "$T/PriceOfBread.SimCore" $MU <<'PY'
import sys
d, mu = sys.argv[1], sys.argv[2]
TE = d + '/Territory.cs'; CC = d + '/Instruments/InstrumentsCapCol.cs'
SKIP = b'            if (homeOnly && q.Tier == RegionTier.Colonial) continue;'
LEAVE = b'            if ((leaving is not null && leaving.Contains(q))) continue;\n            if (homeOnly'
HOMECHK = b'if (q.Tier != RegionTier.Colonial && !(leaving is not null && leaving.Contains(q))) { homeOnly = true; break; }'
TIE = b'if (g > capBest || (g == capBest && (cap is null || q.Id < cap.Id))) { capBest = g; cap = q; }'
R = {'M1': (TE, [(SKIP, b'            if (false) continue;')]),                                                   # 格の条件を外す（旧の規則）
     'M2': (TE, [(SKIP, b'            if (homeOnly && q.Tier != RegionTier.Colonial) continue;')]),              # 条件を逆に（植民地格だけ）
     'M3': (TE, [(SKIP, b'            if (from.Coef.NextCapitalHomeFirst > 0 && !homeOnly) return null;\n' + SKIP)]),   # 本国格が無いとき null
     'M4': (TE, [(TIE, TIE.replace(b'q.Id < cap.Id', b'q.Id > cap.Id'))]),                                     # 同点で Id の大きい方
     'M5': (TE, [(LEAVE, b'            if (homeOnly')]),                                                        # 候補の leaving を無視
     'M6': (TE, [(HOMECHK, b'if (q.Tier != RegionTier.Colonial) { homeOnly = true; break; }')]),                # 本国格の有無の判定が leaving を無視
     'M7': (CC, [(b'        s.Picks++;\n', b'')])}                                                               # 計器の受け口が付け替えを数えない
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
