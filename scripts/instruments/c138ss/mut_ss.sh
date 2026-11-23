#!/usr/bin/env bash
# (1325) c138ss 突然変異（事前登録の〈突然変異ごとの門の赤〉の表・㉚・c137ak/mut_ak.sh の写し）：bash mut_ss.sh <コミット> <出力フォルダ（D:/…）> [M…]
#   コミットを git archive で使い捨ての木へ出し、M ごとに EconomyStep.cs か InstrumentsSubst.cs を書き換え、SubsistenceSellSurplusTests と s3 の写しの試験（S1〜S4）を回す。
#   試験は旗を盤面で置く（旗の既定はコミットの値のまま）。
REV=${1:?}; OUT=${2:?}; shift 2; R=D:/PoB_work/wt_c138ss; W=D:/PoB_work/c138ss_work/mut
export TEMP=D:/PoB_work/c138ss_work/tmp TMP=D:/PoB_work/c138ss_work/tmp DOTNET_CLI_TELEMETRY_OPTOUT=1
mkdir -p $OUT $TEMP
LIST="${*:-M1 M2 M3 M4 M5 M6 M7 M8 M11 M12 M14 M15 M16 M17 Ma1 Ma2 Ma3}"
for MU in $LIST; do
  T=$W/$MU; rm -rf $T; mkdir -p $T
  git -C $R archive $REV PriceOfBread.SimCore PriceOfBread.Tests | tar -x -C $T || { echo ARCHFAIL; exit 1; }
  python -I - "$T/PriceOfBread.SimCore" $MU <<'PY' || { echo "MUTFAIL $MU" | tee $OUT/mut_$MU.txt; continue; }
import sys
d, mu = sys.argv[1], sys.argv[2]
E = d + '/EconomyStep.cs'; I = d + '/Instruments/InstrumentsSubst.cs'
PAY = b'                region.Pops[sp].EarnedPerMonth += new MoneyPerMonth(pay);'
R = {
 'M1': (E, [(PAY, b'                // (M1) R \xe3\x82\x92\xe6\x89\x95\xe3\x82\x8f\xe3\x81\xaa\xe3\x81\x84')]),
 'M2': (E, [(b'            if (nPrime[gi] != 0 || revenue[gi] <= 0) continue;', b'            if (producerCount[gi] != 0 || revenue[gi] <= 0) continue;')]),
 'M3': (E, [(b'            if (sell) m.LastProduced += sEHat[gi];', b'            if (sell) m.LastProduced += 0.0;')]),   # (1325) 形の変え直しの後＝余りを LastProduced に入れない
 'M4': (E, [(b'if (fdLedger) { double raw = sub + qty;', b'if (fdLedger) { double raw = (sell && staple ? p.GoodNeed * sraw![fdI] * sF : sub) + qty;')]),
 'M5': (E, [(b'                    if (x > 1.0 && sOk)\n                    {\n                        double eq = p.GoodNeed * (x - 1.0) * sQ;',
             b'                    if (sraw![pIdx] > 1.0 && sOk)\n                    {\n                        double eq = p.GoodNeed * (sraw![pIdx] - 1.0) * sF * sQ;')]),
 'M6': (E, [(b'        return raw > 0 ? Math.Min(1.0, raw * (1.0 - sE)) : 0.0;', b'        return raw > 0 ? Math.Min(1.0, raw) : 0.0;')]),
 'M7': (E, [(b'                (sR, sQ) = SubsistenceSellGood(gi);', b'                (sR, sQ) = (gi, 1.0);')]),
 'M8': (E, [(PAY, b'                foreach (var pq in region.Pops) pq.EarnedPerMonth += new MoneyPerMonth(pay * pq.Size / totalPop);')]),
 'M11': (E, [(b'        if (coef.SubsistenceSellSurplus > 0) { int npp', b'        if (false) { int npp')]),
 'M12': (E, [(b'        double lneed = StapleNeedPerCapita * lpop.Size / (lpop.SubsistenceBasis * t * fbar);', b'        double lneed = StapleNeedPerCapita * lpop.Size / (lpop.SubsistenceBasis * t);')]),
 'M14': (E, [(b'            if (sell) available += sEHat[gi];', b'            if (sell) available += 0.0;'),
             (b'            m.Stock = Math.Max(0, available - sold);', b'            m.Stock = Math.Max(0, available - sold) + (sell ? sEHat[gi] : 0.0);')]),
 'M15': (E, [(b'(prodP[sr] > Eps ? seq / prodP[sr] : 1.0 / nPrime[sr])', b'(prodP[sr] > Eps ? seq / prodP[sr] : 1.0 / producerCount[sr])')]),
 'M16': (E, [(b'            int t = ((int)a.Tier).CompareTo((int)b.Tier);', b'            int t = region.Markets[a.Id].Price.CompareTo(region.Markets[b.Id].Price);')]),
 'M17': (E, [(b'region.LastSubsistenceOutput += sub;', b'region.LastSubsistenceOutput += (sell && staple ? p.GoodNeed * sraw![fdI] * sF : sub);')]),
 'Ma1': (I, [(b'        return r.HarvestFactor * r.WeatherBandFactor * crop * landF;', b'        return r.HarvestFactor * r.WeatherBandFactor * crop;')]),
 'Ma2': (I, [(b'            if (inp.Length == 0) break;\n            q *= inp[0].QtyPerOutput; cur = inp[0].GoodId;\n        }\n        return (cur, q);\n    }\n\n    /// <summary>\xe5\x9b\x9b',
              b'            break;\n        }\n        return (cur, q);\n    }\n\n    /// <summary>\xe5\x9b\x9b')]),
 'Ma3': (I, [(b'        => (c.LandTaxFixedBasis > 0 ? p.LandRentPerMonth + p.LandTaxBasisAdjPerMonth : p.LandRentPerMonth).Value;', b'        => p.LandRentPerMonth.Value;')]),
}
p, subs = R[mu]
b = open(p, 'rb').read()
for o, n in subs:
    assert b.count(o) == 1, (mu, o[:60])
    b = b.replace(o, n)
open(p, 'wb').write(b); print('mutated', mu)
PY
  dotnet build $T/PriceOfBread.Tests -c Release --artifacts-path $T/art -v q -nologo > $OUT/build_$MU.txt 2>&1 || { echo "BUILDFAIL $MU" | tee $OUT/mut_$MU.txt; rm -rf $T; continue; }
  dotnet test $T/PriceOfBread.Tests -c Release --artifacts-path $T/art --no-build --filter "FullyQualifiedName~SubsistenceSellSurplusTests|FullyQualifiedName~の写しは本体と同じ" --logger "console;verbosity=normal" > $OUT/raw_$MU.txt 2>&1
  { echo "# $MU（$REV）"; grep -E "^\s+(失敗|成功|合格|Passed|Failed) |合計:|Total" $OUT/raw_$MU.txt; } > $OUT/mut_$MU.txt
  rm -rf $T
done
echo MUTDONE
