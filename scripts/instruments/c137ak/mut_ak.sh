#!/usr/bin/env bash
# (1318) c137ak 突然変異（事前登録の〈突然変異ごとの門の赤〉の表・㉚・c135nc/mut_nc.sh の写し）：bash mut_ak.sh <コミット> <出力フォルダ（D:/…）>
#   コミットを git archive で使い捨ての木へ出し、M ごとに MultiNationSim.cs（M1〜M6・M8）か InstrumentsSecTreas.cs（M7）を 1 か所だけ書き換え、SecessionTreasuryShareTests を回す。
#   試験は旗を盤面で置く（旗の既定はコミットの値のまま）。
REV=${1:?}; OUT=${2:?}; R=D:/PoB_work/wt_c137ak; W=D:/PoB_work/c137ak_work/mut
export TEMP=D:/PoB_work/c137ak_work/tmp TMP=D:/PoB_work/c137ak_work/tmp DOTNET_CLI_TELEMETRY_OPTOUT=1
mkdir -p $OUT $TEMP
for MU in M1 M2 M3 M4 M5 M6 M7 M8; do
  T=$W/$MU; rm -rf $T; mkdir -p $T
  git -C $R archive $REV PriceOfBread.SimCore PriceOfBread.Tests | tar -x -C $T || { echo ARCHFAIL; exit 1; }
  python -I - "$T/PriceOfBread.SimCore" $MU <<'PY'
import sys
d, mu = sys.argv[1], sys.argv[2]
MN = d + '/MultiNationSim.cs'; ST = d + '/Instruments/InstrumentsSecTreas.cs'
NUM = b'            foreach (var rr in regs) moved += rr.SumSavings();'
DEN = b'            double sMoved = moved.Value, sParent = pw.TotalMoney.Value;'
TT = b'            T = Math.Max(0, pw.Treasury.Value) * (sMoved > 0 && sParent > 0 ? sMoved / sParent : 0);'
TR = b'        World.Transfer((pw, null), (null, null, null), T, TreasuryCat.OtherOut);'
R = {'M1': (MN, [(TT, b'            T = Math.Max(0, pw.Treasury.Value) * (parentPop > 1e-9 ? P / parentPop : 0);')]),          # 人口の比へ戻す
     'M2': (MN, [(DEN, b'            double sMoved = moved.Value, sParent = pw.TotalMoney.Value - moved.Value;')]),           # 分母を移した後の親の貨幣
     'M3': (MN, [(NUM, b'            moved += regs[0].SumSavings();')]),                                                     # 分子を首都の州（移る州の最初）だけ
     'M4': (MN, [(TT, b'            T = pw.Treasury.Value * (sMoved > 0 && sParent > 0 ? sMoved / sParent : 0);')]),         # max(0, 国庫) を外す
     'M5': (MN, [(NUM, b'            foreach (var rr in regs) foreach (var pp in rr.Pops) moved += new MoneyStock(Math.Max(0, pp.Savings.Value));')]),   # 分子だけ負の貯蓄を 0
     'M6': (MN, [(TR, b'        // (M6) World.Transfer((pw, null), (null, null, null), T, TreasuryCat.OtherOut);')]),                   # 親から引かない
     'M7': (ST, [(b'TlF(tre), TlF(oldT), TlF(newT)', b'TlF(tre), TlF(newT), TlF(newT)')]),                                 # 計器の旧の規則の列に新の規則を書く
     'M8': (MN, [(TT, b'            T = Math.Max(0, pw.Treasury.Value) * (sMoved > 0 ? sMoved / sParent : 0);')])}           # 分母の守りを外す
p, subs = R[mu]
b = open(p, 'rb').read()
for o, n in subs:
    assert b.count(o) == 1, (mu, o)
    b = b.replace(o, n)
open(p, 'wb').write(b); print('mutated', mu)
PY
  dotnet build $T/PriceOfBread.Tests -c Release --artifacts-path $T/art -v q -nologo > $OUT/build_$MU.txt 2>&1 || { echo "BUILDFAIL $MU" | tee $OUT/mut_$MU.txt; continue; }
  dotnet test $T/PriceOfBread.Tests -c Release --artifacts-path $T/art --no-build --filter "FullyQualifiedName~SecessionTreasuryShareTests" --logger "console;verbosity=normal" > $OUT/raw_$MU.txt 2>&1
  { echo "# $MU（$REV）"; grep -E "^\s+(失敗|成功|合格|Passed|Failed) |合計:|Total" $OUT/raw_$MU.txt; } > $OUT/mut_$MU.txt
  rm -rf $T
done
echo MUTDONE
