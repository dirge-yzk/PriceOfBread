#!/usr/bin/env bash
# (1139) c111tf 突然変異：使い捨ての木（git worktree・分離ビルド）で 1 本ずつ当てて tb の試験を回す。使い方：mutate.sh <木> <出力>
T="$1"; OUT="$2"; cd "$T" || exit 2
BS=PriceOfBread.SimCore/Instruments/InstrumentsBuyShadow.cs; BM=PriceOfBread.SimCore/BilateralMarket.cs
run() { # 名 ファイル 旧 新
  python - "$2" "$3" "$4" <<'PY' || { echo "== $1 当たらない" >> "$OUT"; return; }
import sys
p,o,n=sys.argv[1:4]; b=open(p,'rb').read(); oo=o.encode(); assert b.count(oo)==1,(p,o,b.count(oo)); open(p,'wb').write(b.replace(oo,n.encode()))
PY
  echo "== $1" >> "$OUT"
  dotnet test PriceOfBread.Tests -c Release --artifacts-path D:/PoB_work/c111tf_mtart --filter "FullyQualifiedName~TradeBilateralInstrumentTests" 2>&1 | grep -E "^\s+失敗 |^(成功|失敗)!" | cut -c1-200 >> "$OUT"
  git checkout -q -- .
}
: > "$OUT"
run M1_B0を以上に $BS 'return (true, ubBb > 0,' 'return (true, ubBb >= 0,'
run M2_B1からUbA1を落とす $BS 'ubBb <= 0 && ubA1 > 0' 'ubBb <= 0'
run M3_B2を未満に $BS 'ubA1 <= 0, ev' 'ubA1 < 0, ev'
run M4_呼びを消す $BM 'if (tbOn) { double tbBm' 'if (false && tbOn) { double tbBm'
run M5_呼びをOnの外へ $BM 'if (tbOn) { double tbBm' 'if (true) { double tbBm'
run M6_倒産と眠りの枝を消す $BS 'if (p.Bankrupt || EconomyStep.SleepState(r, p, c) == 1) {' 'if (false) {'
run M6b_眠りの半分だけ落とす $BS 'if (p.Bankrupt || EconomyStep.SleepState(r, p, c) == 1) {' 'if (p.Bankrupt) {'
run M6c_倒産の半分だけ落とす $BS 'if (p.Bankrupt || EconomyStep.SleepState(r, p, c) == 1) {' 'if (EconomyStep.SleepState(r, p, c) == 1) {'
run M7_NaNを通す $BS 'if (!(rpT > 1e-9) || !(um > 1e-9)' 'if (rpT <= 1e-9 || um <= 1e-9'
echo done >> "$OUT"
