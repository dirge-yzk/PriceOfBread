#!/usr/bin/env bash
# (1201) c120fm：旗 1 の木のフルスイート（㉜・㊴）。m1198/suite1198.sh の型。使い捨ての木に <コミット> を写し、旗 2 本の既定を 1 に置き換えてから建てる。
#   bash suite1201.sh <コミット> <旗の組 ab|a|b> [full|bundle|tests:<filter>]
#   ab＝FormationNatioGate と FormationOwnDirectOnly を両方 1（赤は試験の名で帰属）。順＝㊴ 計器の試験の束 → 2 プロジェクトの Category!=FullLength・=FullLength（trx）→ savetest・leakhunt。
export TEMP=D:/PoB_work/c120fm_work/tmp TMP=D:/PoB_work/c120fm_work/tmp
export PATH="/c/Program Files/dotnet:/usr/bin:/c/Program Files/Git/usr/bin:$PATH" AVALONIA_TELEMETRY_OPTOUT=1 DOTNET_CLI_TELEMETRY_OPTOUT=1
H=${1:?コミット}; FL=${2:?旗の組}; MODE=${3:-full}; W=D:/PoB_work/c120fm_work/suite_$FL; S=$W/suitesrc
R="$(cd "$(dirname "$0")/../../.." && pwd)"
rm -rf $S && mkdir -p $S $W/trx && (cd $R && git archive $H) | tar -x -C $S || exit 1
C=$S/PriceOfBread.SimCore/Coefficients.p09_kingship_tax.cs
case $FL in *a*) python -X utf8 -I $R/scripts/instruments/tools/replace1.py $C "public double FormationNatioGate = 0.0;" "public double FormationNatioGate = 1.0;" || exit 1 ;; esac
case $FL in *b*) python -X utf8 -I $R/scripts/instruments/tools/replace1.py $C "public double FormationOwnDirectOnly = 0.0;" "public double FormationOwnDirectOnly = 1.0;" || exit 1 ;; esac
cd $S
: > $W/suite_build.txt
for P in PriceOfBread.Tests PriceOfBread.Tests.Ui; do dotnet build $P/$P.csproj -c Release --artifacts-path $W/art_suite --disable-build-servers >> $W/suite_build.txt 2>&1 || { echo BUILDFAIL $P; tail -20 $W/suite_build.txt; exit 1; }; done
for P in PriceOfBread.Tests PriceOfBread.Tests.Ui; do for f in "Category!=FullLength:nonfull" "Category=FullLength:full"; do
  dotnet test $P/$P.csproj -c Release --no-build --artifacts-path $W/art_suite --list-tests --filter "${f%%:*}" > $W/list_${P}_${f##*:}.txt 2>&1
  echo "list $P ${f##*:} $(grep -c '^    ' $W/list_${P}_${f##*:}.txt)"
done; done
case $MODE in tests:*)
  T0=$(date +%s); dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $W/art_suite --filter "${MODE#tests:}" --logger "console;verbosity=normal" > $W/tests.txt 2>&1; echo "tests rc=$? $(( $(date +%s) - T0 ))s"
  grep -h "合計\|Total\|Passed!\|Failed!\|^\s*失敗 " $W/tests.txt | tail -20; echo TESTSDONE; exit 0 ;;
esac
# ㊴ 計器の試験の束を先に
T0=$(date +%s); dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $W/art_suite --filter "FullyQualifiedName~Instrument" > $W/bundle.txt 2>&1; echo "bundle rc=$? $(( $(date +%s) - T0 ))s"
grep -h "合計\|中止\|Total\|Passed!\|Failed!\|^\s*失敗 " $W/bundle.txt | tail -8
[ "$MODE" = bundle ] && { echo BUNDLEDONE; exit 0; }
for P in PriceOfBread.Tests PriceOfBread.Tests.Ui; do
T0=$(date +%s); dotnet test $P/$P.csproj -c Release --no-build --artifacts-path $W/art_suite --filter "Category!=FullLength" --logger "trx;LogFileName=$W/trx/nonfull_$P.trx" > $W/suite_nonfull_$P.txt 2>&1; echo "nonfull $P rc=$? $(( $(date +%s) - T0 ))s"
T0=$(date +%s); dotnet test $P/$P.csproj -c Release --no-build --artifacts-path $W/art_suite --filter "Category=FullLength" --logger "trx;LogFileName=$W/trx/full_$P.trx" > $W/suite_full_$P.txt 2>&1; echo "full $P rc=$? $(( $(date +%s) - T0 ))s"
done
grep -h "合計\|中止\|Total tests\|aborted\|\[FAIL\]\|一致するテストがありません\|No test matches" $W/suite_nonfull_*.txt $W/suite_full_*.txt | tail -12
grep -h -A2 "^\s*失敗 " $W/suite_nonfull_*.txt $W/suite_full_*.txt | head -40
echo SUITEDONE
T0=$(date +%s); POB_WORK=$W SAVELEAK_SRC=$S bash $S/scripts/instruments/saveleak.sh > $W/saveleak.txt 2>&1; echo "saveleak rc=$? $(( $(date +%s) - T0 ))s"
grep -h "OK\|NG\|違反\|一致\|不一致\|FAIL" $W/savetest.txt $W/leakhunt.txt 2>/dev/null | tail -8
echo SAVELEAKDONE
