#!/usr/bin/env bash
# (819) c65fl 突然変異（使い捨ての木）：規則 C の番人の Theory（FiscalTests）と CapitalCouponWriteDownTests が突然変異ごとに赤になるかを読む（事前登録 measurements/m819_20261104_prereg_fl.txt [4]）。
#   使い方: bash mut_unit65.sh <コミット> <出力> Mu1 Mu2 ...（Bash の run_in_background で）
#   写し＝<コミット> の git archive を D:/PoB_work/c65fl_work/mutu/src_<Mu> へ・Mu1〜Mu4 は scripts/instruments/c14fl/mutate.py・Mu5 はこの台本（旗の枝ごと通らない＝全く落とさない）。
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH" AVALONIA_TELEMETRY_OPTOUT=1 PYTHONUTF8=1
H=${1:?コミット}; OUT=${2:?出力}; shift 2
R=D:/PoB_work/wt_c65fl; W=D:/PoB_work/c65fl_work/mutu
mkdir -p $W
for m in "$@"; do
  T=$W/src_$m; rm -rf $T; mkdir -p $T
  (cd $R && git archive $H) | tar -x -C $T || exit 1
  if [ "$m" = Mu5 ]; then
    python -I -c "
import sys
p=sys.argv[1]; a='if (c.CapitalCouponUnpaidWriteDown > 0) {'; s=open(p,'rb').read().decode('utf-8')
assert s.count(a)==1
open(p,'wb').write(s.replace(a,'if (false /* MUTANT */) {').encode('utf-8'))
print('mutated Mu5')" $T/PriceOfBread.SimCore/CapitalMarket.cs
  else
    python -I $R/scripts/instruments/c14fl/mutate.py $T $m
  fi
  echo "## $m" >> $OUT
  dotnet test $T/PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path $W/art_$m --disable-build-servers \
    --filter "FullyQualifiedName~CapitalCouponWriteDownTests|FullyQualifiedName~FiscalTests.外資の配当と逃避は投資家までしか払わない" 2>&1 \
    | grep -E -A1 "^\s+(失敗|Failed) |エラー メッセージ|合計:|Total tests" | grep -v "^--$" >> $OUT
done
echo MUTUNITDONE >> $OUT
