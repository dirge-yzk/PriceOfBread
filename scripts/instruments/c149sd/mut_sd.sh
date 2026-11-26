#!/usr/bin/env bash
# (1398) c149sd 突然変異（事前登録 m1398 [5]）：bash mut_sd.sh <コミット> <出力フォルダ（D:/…）>
#   MUS・FILT で選ぶ（追記：μ4 参加の max を戻す・μ5 思想の闘争度を戻す）
#   μ1 Politics.Discontent の旗 1 の枝を外す（声の項を戻す）・μ2 UnrestOf を旧の式へ・μ3 Credit の dBar を逆に。c147wb/mut_wb.sh の写し。
REV=${1:?}; OUT=${2:?}; R=D:/PoB_work/wt_c149sd; W=D:/PoB_work/c149sd_work/mut
export TEMP=D:/PoB_work/c149sd_work/tmp TMP=D:/PoB_work/c149sd_work/tmp DOTNET_CLI_TELEMETRY_OPTOUT=1
mkdir -p $OUT $TEMP
MUS=${MUS:-M0 M1 M2 M3}; FILT=${FILT:-FullyQualifiedName~SatReadersToDiscontentTests}
for MU in $MUS; do
  T=$W/$MU; rm -rf $T; mkdir -p $T
  git -C $R archive $REV PriceOfBread.SimCore PriceOfBread.Tests | tar -x -C $T || { echo ARCHFAIL; exit 1; }
  python -I - "$T/PriceOfBread.SimCore" $MU <<'PY'
import sys
d, mu = sys.argv[1], sys.argv[2]
PO = d + '/Politics.cs'; SP = d + '/SelfPlay.cs'; CR = d + '/Credit.cs'; FS = d + '/FactionSnapshot.cs'; ID = d + '/Ideology.cs'
R = {'M0': [],
     'M1': [(PO, b'if (c.SatReadersToDiscontent > 0)   // (1371)', b'if (c.SatReadersToDiscontent > 9)   // (1371)', 1)],
     'M2': [(SP, b'=> w.Coef.SatReadersToDiscontent > 0 ? Politics.AvgMassDiscontent(w)', b'=> w.Coef.SatReadersToDiscontent > 9 ? Politics.AvgMassDiscontent(w)', 1)],
     'M4': [(FS, b'if (w.Coef.SatReadersToDiscontent > 0) return d;', b'if (w.Coef.SatReadersToDiscontent > 9) return d;', 1)],
     'M5': [(ID, b'mil = w.Coef.SatReadersToDiscontent > 0 ? disc : p.Militancy;', b'mil = w.Coef.SatReadersToDiscontent > 9 ? disc : p.Militancy;', 1)],
     'M3': [(CR, b'bool dBar = c.SatReadersToDiscontent > 0;', b'bool dBar = !(c.SatReadersToDiscontent > 0);', 1)]}
for p, o, n, k in R[mu]:
    b = open(p, 'rb').read()
    assert b.count(o) == k, (mu, o, b.count(o))
    open(p, 'wb').write(b.replace(o, n))
print('mutated', mu)
PY
  dotnet build $T/PriceOfBread.Tests -c Release --artifacts-path $T/art -v q -nologo > $OUT/build_$MU.txt 2>&1 || { echo "BUILDFAIL $MU" | tee $OUT/mut_$MU.txt; continue; }
  dotnet test $T/PriceOfBread.Tests -c Release --artifacts-path $T/art --no-build --filter "$FILT" > $OUT/raw_$MU.txt 2>&1
  { echo "# $MU（$REV）"; grep -E "^\s+(失敗|Failed) |合計:|Total" $OUT/raw_$MU.txt; } > $OUT/mut_$MU.txt
  rm -rf $T
done
echo MUTDONE > $OUT/mut.mark
