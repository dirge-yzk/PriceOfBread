#!/usr/bin/env bash
# (1435) c154sd 突然変異（c152sd/mut_sd.sh の写し）：bash mut.sh <コミット> <出力フォルダ（D:/…）>  事前登録 m1435 [6] の μ1〜μ6 と M0（対照）。
#   μ1 Floor が旗を読まない（φN）・μ2 足切りの分母を参加の総量 P_n にする・μ3 :49（見えたかの側）だけ φN・μ4 強さと AvgMilitancy が旗を読まない（Size×AvgMilitancy）・μ5 σ を 1 に固定・μ6 結束の伸びの 0.5 を消す
REV=${1:?}; OUT=${2:?}; R=D:/PoB_work/wt_c154sd; W=D:/PoB_work/c154sd_work/mutw
export TEMP=D:/PoB_work/c154sd_work/tmp TMP=D:/PoB_work/c154sd_work/tmp DOTNET_CLI_TELEMETRY_OPTOUT=1
mkdir -p $OUT $TEMP
MUS=${MUS:-M0 M1 M2 M3 M4 M5 M6}; FILT=${FILT:-FullyQualifiedName~FactionFloorRegionTests}
for MU in $MUS; do
  T=$W/$MU; rm -rf $T; mkdir -p $T
  git -C $R archive $REV PriceOfBread.SimCore PriceOfBread.Tests | tar -x -C $T || { echo ARCHFAIL; exit 1; }
  python -I - "$T/PriceOfBread.SimCore" $MU <<'PY'
import sys
d, mu = sys.argv[1], sys.argv[2]
FS = d + '/FactionSnapshot.cs'; FA = d + '/Factions.cs'; PO = d + '/Politics.cs'
R = {'M0': [],
     'M1': [(FS, '=> w.Coef.FactionFloorByRegion > 0 ? MinSizeFraction * regionPop : MinSizeFraction * totalPop;', '=> MinSizeFraction * totalPop;')],
     'M2': [(FS, 'extras?.Add((fight, fightSq, regionPop[key.Region]));', 'extras?.Add((fight, fightSq, System.Linq.Enumerable.Sum(clusters.Values, v => v.Size)));')],
     'M3': [(FA, 'size >= FactionSnapshot.Floor(w, extras[ci].RegionPop, totalPop)) { seen = true; break; }', 'size >= FactionSnapshot.MinSizeFraction * totalPop) { seen = true; break; }')],
     'M4': [(PO, 'double heads = c.FactionStrengthByMilitancy > 0 ? f.FightHeads : f.Size;', 'double heads = f.Size;'),
            (FA, 'if (c.FactionStrengthByMilitancy > 0) st.AvgMilitancy = fight > 0 ? extras[ci].FightSq / fight : 0.0;', '')],
     'M5': [(PO, 'double silenced = 1.0 - voice;', 'double silenced = 1.0;')],
     'M6': [(FA, 'c.FactionCohesionGrowth * (0.5 + consc)', 'c.FactionCohesionGrowth * (consc)')]}
for p, o, n in R[mu]:
    b = open(p, 'rb').read(); ob = o.encode('utf-8')
    assert b.count(ob) == 1, (mu, o, b.count(ob))
    open(p, 'wb').write(b.replace(ob, n.encode('utf-8')))
print('mutated', mu)
PY
  [ $? = 0 ] || { echo "MUTFAIL $MU" | tee $OUT/mut_$MU.txt; continue; }
  dotnet build $T/PriceOfBread.Tests -c Release --artifacts-path $T/art -v q -nologo > $OUT/build_$MU.txt 2>&1 || { echo "BUILDFAIL $MU" | tee $OUT/mut_$MU.txt; continue; }
  dotnet test $T/PriceOfBread.Tests -c Release --artifacts-path $T/art --no-build --filter "$FILT" > $OUT/raw_$MU.txt 2>&1
  { echo "# $MU（$REV）"; grep -E "^\s+(失敗|Failed) |合計:|Total" $OUT/raw_$MU.txt; } > $OUT/mut_$MU.txt
  rm -rf $T
done
echo MUTDONE > $OUT/mut.mark
