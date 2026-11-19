#!/usr/bin/env bash
# (1236) c125b1（(1208) の写し）：使い捨ての写しの木（worktree の HEAD を git archive＋未コミットの試験を写す）に置き換えを当て、隔離ビルドで試験の絞りを回す（段0・突然変異）。
#   bash scratch.sh <名前> <絞り（dotnet test の --filter）> [置き換えの組 ファイル::旧::新 ...]
#   特別な組：PROBE＝scripts/instruments/r1041/probe_ledger.cs.txt を Tests/ProbeEbLedger.cs として置く（コミットしない）。
#   出力 D:/PoB_work/c121b1_work/<名前>/test.txt（結果の要約は sum.txt）。写しの木は終わりに消す。
NM=${1:?名前}; F=${2:?絞り}; shift 2
WT=D:/PoB_work/wt_c125b1; W=D:/PoB_work/c125b1_work/$NM; S=$W/src
export TEMP=$W/tmp TMP=$W/tmp AVALONIA_TELEMETRY_OPTOUT=1 PYTHONUTF8=1
rm -rf $W && mkdir -p $S $W/tmp
(cd $WT && git archive HEAD) | tar -x -C $S || exit 1
(cd $WT && git ls-files --others --exclude-standard PriceOfBread.Tests PriceOfBread.SimCore) | while read -r f; do mkdir -p "$S/$(dirname "$f")"; cp "$WT/$f" "$S/$f"; done
for spec in "$@"; do
  if [ "$spec" = PROBE ]; then cp $WT/scripts/instruments/r1041/probe_ledger.cs.txt $S/PriceOfBread.Tests/ProbeEbLedger.cs; echo "PROBE 置いた" >> $W/sum.txt; continue; fi
  file=${spec%%::*}; rest=${spec#*::}; old=${rest%%::*}; new=${rest#*::}
  python -I $WT/scripts/instruments/tools/replace1.py "$S/$file" "$old" "$new" >> $W/sum.txt 2>&1 || { echo "REPLACEFAIL $file" >> $W/sum.txt; exit 1; }
  echo "置き換え $file" >> $W/sum.txt
done
cd $S
dotnet build PriceOfBread.Tests -c Release --artifacts-path $W/art -v q -nologo > $W/build.txt 2>&1 || { echo BUILDFAIL >> $W/sum.txt; tail -20 $W/build.txt >> $W/sum.txt; exit 1; }
dotnet test PriceOfBread.Tests -c Release --artifacts-path $W/art --no-build --filter "$F" --logger "console;verbosity=detailed" > $W/test.txt 2>&1; RC=$?
echo "test rc=$RC $(grep -E '合計|Total tests' $W/test.txt | tail -1)" >> $W/sum.txt
grep -E "^\s+(失敗|Failed) " $W/test.txt >> $W/sum.txt
cd $W && rm -rf $S $W/art $W/tmp
echo "SCRATCHDONE $(date '+%F %T')" >> $W/sum.txt
exit $RC
