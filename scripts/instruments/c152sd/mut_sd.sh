#!/usr/bin/env bash
# (1422) c152sd 突然変異（c150sd/mut_sd.sh の写し）：bash mut_sd.sh <コミット> <出力フォルダ（D:/…）>  事前登録 m1422 [9] の μ1〜μ5 と M0（対照）。
#   μ1 FloorOf が旗を読まない（φN）・μ2 P_n＝人口（π を掛けない）・μ3 P_n を組の州の中だけで足す（Factions.Step の 2 か所と Compute）・μ4 :49 だけ旧の足切り・μ5 FactionStrength の σ を 1 に固定。
REV=${1:?}; OUT=${2:?}; R=D:/PoB_work/wt_c152sd; W=D:/PoB_work/c152sd_work/mutw
export TEMP=D:/PoB_work/c152sd_work/tmp TMP=D:/PoB_work/c152sd_work/tmp DOTNET_CLI_TELEMETRY_OPTOUT=1
mkdir -p $OUT $TEMP
MUS=${MUS:-M0 M1 M2 M3 M4 M5}; FILT=${FILT:-FullyQualifiedName~FactionFloorByParticipationTests|FullyQualifiedName~SatReadersToDiscontentTests}
for MU in $MUS; do
  T=$W/$MU; rm -rf $T; mkdir -p $T
  git -C $R archive $REV PriceOfBread.SimCore PriceOfBread.Tests | tar -x -C $T || { echo ARCHFAIL; exit 1; }
  python -I - "$T/PriceOfBread.SimCore" $MU <<'PY'
import sys
d, mu = sys.argv[1], sys.argv[2]
FS = d + '/FactionSnapshot.cs'; FA = d + '/Factions.cs'; PO = d + '/Politics.cs'
R = {'M0': [],
     'M1': [(FS, b'=> w.Coef.FactionFloorByParticipation > 0 ? MinSizeFraction * participationTotal : MinSizeFraction * totalPop;', b'=> MinSizeFraction * totalPop;', 1)],
     'M2': [(FS, b'=> FloorOf(w, totalPop, w.Coef.FactionFloorByParticipation > 0 ? ParticipationTotal(clusters) : 0.0);', b'=> FloorOf(w, totalPop, w.Coef.FactionFloorByParticipation > 0 ? totalPop : 0.0);', 1)],
     'M3': [(FA, b'            if (size < floor) continue;', b'            if (size < (w.Coef.FactionFloorByParticipation > 0 ? FactionSnapshot.MinSizeFraction * observed.Where(o => ReferenceEquals(o.Region, reg)).Sum(o => o.Size) : floor)) continue;', 1),
            (FA, b'f.Ideo == ideo && size >= floor) { seen = true; break; }', b'f.Ideo == ideo && size >= (w.Coef.FactionFloorByParticipation > 0 ? FactionSnapshot.MinSizeFraction * observed.Where(o => ReferenceEquals(o.Region, reg)).Sum(o => o.Size) : floor)) { seen = true; break; }', 1),
            (FS, b'            if (size < floor) continue;', b'            if (size < (w.Coef.FactionFloorByParticipation > 0 ? MinSizeFraction * clusters.Where(o => ReferenceEquals(o.Region, reg)).Sum(o => o.Size) : floor)) continue;', 1)],
     'M4': [(FA, b'f.Ideo == ideo && size >= floor) { seen = true; break; }', b'f.Ideo == ideo && size >= FactionSnapshot.MinSizeFraction * totalPop) { seen = true; break; }', 1)],
     'M5': [(PO, b'        double silenced = 1.0 - voice;', b'        double silenced = 1.0;', 1)]}
for p, o, n, k in R[mu]:
    b = open(p, 'rb').read()
    assert b.count(o) == k, (mu, o, b.count(o))
    open(p, 'wb').write(b.replace(o, n))
print('mutated', mu)
PY
  [ $? = 0 ] || { echo "MUTFAIL $MU" | tee $OUT/mut_$MU.txt; continue; }
  dotnet build $T/PriceOfBread.Tests -c Release --artifacts-path $T/art -v q -nologo > $OUT/build_$MU.txt 2>&1 || { echo "BUILDFAIL $MU" | tee $OUT/mut_$MU.txt; continue; }
  dotnet test $T/PriceOfBread.Tests -c Release --artifacts-path $T/art --no-build --filter "$FILT" > $OUT/raw_$MU.txt 2>&1
  { echo "# $MU（$REV）"; grep -E "^\s+(失敗|Failed) |合計:|Total" $OUT/raw_$MU.txt; } > $OUT/mut_$MU.txt
  rm -rf $T
done
echo MUTDONE > $OUT/mut.mark
