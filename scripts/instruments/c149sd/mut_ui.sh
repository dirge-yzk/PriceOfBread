#!/usr/bin/env bash
# (1398) c149sd 突然変異（Tests.Ui の書き換えた番人 PlayHarnessTests 不満度の本体…）：μ1 声の項を戻す→赤の見込み・M0 対照。bash mut_ui.sh <コミット> <出力（D:/…）>
REV=${1:?}; OUT=${2:?}; R=D:/PoB_work/wt_c149sd; W=D:/PoB_work/c149sd_work/mutui
export TEMP=D:/PoB_work/c149sd_work/tmp TMP=D:/PoB_work/c149sd_work/tmp DOTNET_CLI_TELEMETRY_OPTOUT=1 AVALONIA_TELEMETRY_OPTOUT=1
mkdir -p $OUT $TEMP
for MU in M0 M1; do
  T=$W/$MU; rm -rf $T; mkdir -p $T
  F=$(git -C $R ls-tree --name-only $REV | grep -v -E '^(measurements|docs|scripts|bgm-preview)$' | tr '\n' ' ')
  git -C $R archive $REV $F | tar -x -C $T || { echo ARCHFAIL; exit 1; }
  if [ $MU = M1 ]; then python -I - "$T/PriceOfBread.SimCore/Politics.cs" <<'PY'
import sys
p = sys.argv[1]; b = open(p, 'rb').read()
o = b'if (c.SatReadersToDiscontent > 0)   // (1371)'
assert b.count(o) == 1
open(p, 'wb').write(b.replace(o, b'if (c.SatReadersToDiscontent > 9)   // (1371)'))
PY
  fi
  dotnet build $T/PriceOfBread.Tests.Ui -c Release --artifacts-path $T/art -v q -nologo > $OUT/build_ui_$MU.txt 2>&1 || { echo "BUILDFAIL $MU" | tee $OUT/mutui_$MU.txt; continue; }
  dotnet test $T/PriceOfBread.Tests.Ui -c Release --artifacts-path $T/art --no-build --filter "FullyQualifiedName~PlayHarnessTests.不満度の本体" > $OUT/rawui_$MU.txt 2>&1
  { echo "# $MU（$REV）"; grep -E "^\s+(失敗|Failed) |合計:|Total" $OUT/rawui_$MU.txt; } > $OUT/mutui_$MU.txt
  rm -rf $T
done
echo MUTDONE > $OUT/mutui.mark
