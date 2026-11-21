#!/usr/bin/env bash
# (1284) c132df 突然変異（事前登録 4 の表）：bash mut_df.sh <コミット> <出力フォルダ（D:/…）> <μ...>
#   コミットを git archive で使い捨ての木へ出し、μ ごとに 1 か所だけ書き換えて、その μ の試験を回す（出力 mut_<μ>.txt）。
REV=${1:?}; OUT=${2:?}; shift 2; R=/c/Users/dirge/Documents/PriceOfBread; W=D:/PoB_work/c132df_work/mut
export TEMP=D:/PoB_work/c132df_work/tmp TMP=D:/PoB_work/c132df_work/tmp DOTNET_CLI_TELEMETRY_OPTOUT=1
mkdir -p $OUT $TEMP
for MU in "$@"; do
  T=$W/$MU; rm -rf $T; mkdir -p $T
  git -C $R archive $REV PriceOfBread.SimCore PriceOfBread.Tests | tar -x -C $T || { echo ARCHFAIL; exit 1; }
  python -I - "$T" $MU <<'PY'
import sys
t, mu = sys.argv[1], sys.argv[2]
F = t + '/PriceOfBread.SimCore/Fiscal.cs'; I = t + '/PriceOfBread.SimCore/Instruments/InstrumentsArrears.cs'
FT = t + '/PriceOfBread.Tests/FiscalTests.cs'; AT = t + '/PriceOfBread.Tests/ArrearsOpenedRoomTests.cs'
gate = b'        if (goldGateFirst && w.MonetaryEnabled && w.OnGoldStandard) GoldGateOfArrears(arrearsMonth);'
judge = b'        if (arrearsMonth) w.InsolvencyMonths += ArrearsOfMonth(w, c, payRate, spare, unopenedRoom); else w.InsolvencyMonths = 0;'
R = {'M0': (F, b'bool goldGateFirst = c.ArrearsOnePredicate > 0,', b'bool goldGateFirst = false,'),
     'M1': (F, b'GoldGateOfArrears(arrearsMonth);', b'GoldGateOfArrears(payRate < c.GoldSuspendMinFundingRatio);'),
     'M4': (F, gate, None),
     'M6': (F, b'GoldGateOfArrears(arrearsMonth);', b'GoldGateOfArrears(payRate < 1 && w.GovDebt.Value > 1e-9 && !w.WarmupFrozen);'),
     'M7': (I, b'bool oldTrip = money && onGold && (!law || r < c.GoldSuspendMinFundingRatio || coverTrip);', b'bool oldTrip = money && onGold && (!law || r < 1 || coverTrip);'),
     'M8': (I, b'double newArr = onGold && !newTrip ? unpaid : offStd;', b'double newArr = unpaid;'),
     'Mf': (FT, b'MF_NEW', b'MF_OLD'),
     'Ma': (AT, b'        w.Coef.ArrearsOnePredicate = 0;   ', b'        // '),}
p, o, n = R[mu]
b = open(p, 'rb').read()
if mu == 'M4':
    blk = gate + b'\n' + judge
    assert b.count(blk) == 1, mu
    b = b.replace(blk, judge + b'\n' + gate)
elif mu == 'Mf':
    import json
    pairs = json.load(open(t + '/../mf_pairs.json', encoding='utf-8'))
    for new, old in pairs:
        nb, ob = new.encode('utf-8'), old.encode('utf-8')
        assert b.count(nb) == 1, ('Mf', new[:40])
        b = b.replace(nb, ob)
else:
    assert b.count(o) == 1, mu
    b = b.replace(o, n)
open(p, 'wb').write(b); print('mutated', mu)
PY
  [ $? = 0 ] || { echo "MUTFAIL $MU" | tee $OUT/mut_$MU.txt; continue; }
  case $MU in M7|M8) FLT="FullyQualifiedName~ArrearsInstrumentTests";; Mf) FLT="FullyQualifiedName~FiscalTests.不履行は払えなかった";; Ma) FLT="FullyQualifiedName~ArrearsOpenedRoomTests";; *) FLT="FullyQualifiedName~ArrearsOnePredicateTests|FullyQualifiedName~ArrearsInstrumentTests";; esac
  dotnet build $T/PriceOfBread.Tests -c Release --artifacts-path $T/art -v q -nologo > $OUT/build_$MU.txt 2>&1 || { echo "BUILDFAIL $MU" | tee $OUT/mut_$MU.txt; continue; }
  dotnet test $T/PriceOfBread.Tests -c Release --artifacts-path $T/art --no-build --filter "$FLT" --logger "console;verbosity=normal" > $OUT/raw_$MU.txt 2>&1
  { echo "# $MU（$REV）filter=$FLT"; grep -E "^\s+(失敗|成功|合格) |合計:|Passed |Failed " $OUT/raw_$MU.txt; } > $OUT/mut_$MU.txt
  rm -rf $T
done
echo MUTDONE
