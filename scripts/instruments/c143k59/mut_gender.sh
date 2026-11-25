#!/usr/bin/env bash
# (1356) c143k59 書き換えを外す突然変異（★(743) の改め）：旗 1 のコミットで GenderTests の新しい Assert を旧の InRange(0.5, 0.7) へ戻し、GenderTests が赤になることを確かめる。
#   bash mut_gender.sh <コミット> <出力フォルダ（D:/…）>
REV=${1:?}; OUT=${2:?}; R=D:/PoB_work/wt_c143k59; T=D:/PoB_work/c143k59_work/mutg
export TEMP=D:/PoB_work/c143k59_work/tmp TMP=D:/PoB_work/c143k59_work/tmp DOTNET_CLI_TELEMETRY_OPTOUT=1
mkdir -p $OUT $TEMP; rm -rf $T; mkdir -p $T
git -C $R archive $REV PriceOfBread.SimCore PriceOfBread.Tests | tar -x -C $T || { echo ARCHFAIL; exit 1; }
python -I - "$T/PriceOfBread.Tests/GenderTests.cs" <<'PY'
import sys
p = sys.argv[1]; b = open(p, 'rb').read()
o = b'        Assert.Equal(0.4, reformed.World.Region.FemaleParticipation, 2);\n'
n = b'        Assert.InRange(reformed.World.Region.FemaleParticipation, 0.5, 0.7 + 1e-9);\n'
assert b.count(o) == 1
open(p, 'wb').write(b.replace(o, n)); print('mutated M4')
PY
dotnet build $T/PriceOfBread.Tests -c Release --artifacts-path $T/art -v q -nologo > $OUT/build_M4.txt 2>&1 || { echo "BUILDFAIL M4" | tee $OUT/mut_M4.txt; exit 1; }
dotnet test $T/PriceOfBread.Tests -c Release --artifacts-path $T/art --no-build --filter "FullyQualifiedName~GenderTests.女性財産権法は労働供給を増やす" --logger "console;verbosity=normal" > $OUT/raw_M4.txt 2>&1
{ echo "# M4（$REV）"; grep -E "^\s+(失敗|Failed) |合計:|Total" $OUT/raw_M4.txt; } > $OUT/mut_M4.txt
rm -rf $T
echo MUTDONE > $OUT/mutg.mark
