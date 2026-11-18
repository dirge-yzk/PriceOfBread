#!/usr/bin/env bash
# (1201) c120fm 突然変異の腕（使い捨ての木 D:/PoB_work/c120fm_work/mut_<M>・git archive のコミットから・FormationGateTests だけを回す）。
#   bash mutrun1201.sh <コミット> <出力フォルダ（D:/…）> [M1 M2 ...]（既定は M1〜M5 と M0〔変異なし〕）
export TEMP=D:/PoB_work/c120fm_work/tmp TMP=D:/PoB_work/c120fm_work/tmp DOTNET_CLI_TELEMETRY_OPTOUT=1
H=${1:?コミット}; OUT=${2:?出力}; shift 2; MS=${*:-M0 M1 M2 M3 M4 M5}
S="$(cd "$(dirname "$0")" && pwd)"; REPO="$(cd "$S/../../.." && pwd)"; W=D:/PoB_work/c120fm_work
mkdir -p "$OUT"
for M in $MS; do
  T=$W/mut_$M; rm -rf $T $W/mut_art_$M && mkdir -p $T
  (cd $REPO && git archive $H PriceOfBread.SimCore PriceOfBread.Tests) | tar -x -C $T || { echo "$M ARCHIVEFAIL" >> $OUT/mut_summary.txt; continue; }
  if [ "$M" != M0 ]; then python -X utf8 -I $S/mut1201.py $T $M > $OUT/mut_$M.txt 2>&1 || { echo "$M MUTFAIL" >> $OUT/mut_summary.txt; continue; }; fi
  dotnet build $T/PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path $W/mut_art_$M --disable-build-servers > $OUT/mut_${M}_build.txt 2>&1 || { echo "$M BUILDFAIL" >> $OUT/mut_summary.txt; continue; }
  dotnet test $T/PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $W/mut_art_$M --filter "FullyQualifiedName~FormationGateTests" --logger "console;verbosity=normal" >> $OUT/mut_$M.txt 2>&1
  RC=$?
  FAILED=$(grep -E "^\s+(失敗|Failed) " $OUT/mut_$M.txt | sed -E 's/^\s+(失敗|Failed) +//; s/ \[.*//' | sed -E 's/^PriceOfBread\.Tests\.FormationGateTests\.//' | tr '\n' ';')
  echo "$M rc=$RC 赤=[$FAILED] $(grep -E '合計|Total tests|成功|Passed!|失敗!|Failed!' $OUT/mut_$M.txt | tail -1)" >> $OUT/mut_summary.txt
  rm -rf $T $W/mut_art_$M
done
echo MUTDONE
