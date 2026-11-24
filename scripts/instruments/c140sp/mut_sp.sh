#!/usr/bin/env bash
# (1336) c140sp 突然変異（事前登録の〈突然変異ごとの門の赤〉の表・㉚・c139nc/mut_nc.sh の写し）：bash mut_sp.sh <コミット> <出力フォルダ（D:/…）>
#   コミットを git archive で使い捨ての木へ出し、μ ごとに InternationalMarket.cs（μ0〜μ3）か InstrumentsStProc.cs（μ4・μ5）か MilitaryTests.cs（μ6＝旗 1 の木だけ）を 1 か所だけ書き換え、
#   StateProcureLedgerTests と MilitaryTests の侵攻の物資の試験を回す。試験は旗を盤面で置く（MilitaryTests は旗の既定＝コミットの値）。
REV=${1:?}; OUT=${2:?}; R=D:/PoB_work/wt_c140sp; W=D:/PoB_work/c140sp_work/mut
export TEMP=D:/PoB_work/c140sp_work/tmp TMP=D:/PoB_work/c140sp_work/tmp DOTNET_CLI_TELEMETRY_OPTOUT=1
mkdir -p $OUT $TEMP
for MU in M0 M1 M2 M3 M4 M5 M6; do
  T=$W/$MU; rm -rf $T; mkdir -p $T
  git -C $R archive $REV PriceOfBread.SimCore PriceOfBread.Tests | tar -x -C $T || { echo ARCHFAIL; exit 1; }
  python -I - "$T" $MU <<'PY' || { echo "SKIP $MU（書き換えの元が無い）" | tee $OUT/mut_$MU.txt; rm -rf $T; continue; }
import sys
d, mu = sys.argv[1], sys.argv[2]
IM = d + '/PriceOfBread.SimCore/InternationalMarket.cs'; SP = d + '/PriceOfBread.SimCore/Instruments/InstrumentsStProc.cs'; MT = d + '/PriceOfBread.Tests/MilitaryTests.cs'
FLAG = b'if (buyer is null && !(w.Coef.StateProcureAsSold > 0)) m.LastExport += buy;'
ELSE = b'        else m.LastSold += buy;\n'
R = {'M0': (IM, [(FLAG, b'if (buyer is null && true) m.LastExport += buy;')]),                                                  # 旗を読まない（何もしない実装）
     'M1': (IM, [(ELSE, b'        else if (buyer is not null) m.LastSold += buy; else m.LastExport += buy;\n')]),              # 旗 1 の枝も LastExport へ
     'M2': (IM, [(ELSE, b'        else { m.LastSold += buy; if (buyer is null) m.LastExport += buy; }\n')]),                   # 両方に積む
     'M3': (IM, [(ELSE, b'        else if (buyer is not null && w.Coef.StateProcureAsSold > 0) m.LastExport += buy; else m.LastSold += buy;\n')]),  # buyer ありも旗 1 で LastExport
     'M4': (SP, [(b'if (s.Tick != w.Tick) { s.Tick = w.Tick; s.Settle = 0; }', b'if (s.Tick != w.Tick) { s.Tick = w.Tick; }')]),  # 決済の月を Tick で戻さない
     'M5': (SP, [(b'newRule = asSold ? now : now - settle', b'newRule = asSold ? now : now + settle')]),                          # 新の規則の符号
     'M6': (MT, [(b'sd0[k] = m[goods[k]].LastSold;', b'sd0[k] = m[goods[k]].LastExport;'), (b'm[goods[k]].LastSold - sd0[k]', b'm[goods[k]].LastExport - sd0[k]')])}  # 書き換えを外す
p, subs = R[mu]
b = open(p, 'rb').read()
for o, n in subs:
    if b.count(o) != 1: print('NOSRC', mu, o); sys.exit(3)
    b = b.replace(o, n)
open(p, 'wb').write(b); print('mutated', mu)
PY
  dotnet build $T/PriceOfBread.Tests -c Release --artifacts-path $T/art -v q -nologo > $OUT/build_$MU.txt 2>&1 || { echo "BUILDFAIL $MU" | tee $OUT/mut_$MU.txt; rm -rf $T; continue; }
  dotnet test $T/PriceOfBread.Tests -c Release --artifacts-path $T/art --no-build --filter "FullyQualifiedName~StateProcureLedgerTests|FullyQualifiedName~侵攻の物資は国家の調達" --logger "console;verbosity=normal" > $OUT/raw_$MU.txt 2>&1
  { echo "# $MU（$REV）"; grep -E "^\s+(失敗|成功|合格|Passed|Failed) |合計:|Total" $OUT/raw_$MU.txt; } > $OUT/mut_$MU.txt
  rm -rf $T
done
echo MUTDONE
