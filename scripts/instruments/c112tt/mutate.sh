#!/usr/bin/env bash
# (1146) c112tt 突然変異：使い捨ての木（git worktree・分離ビルド）で 1 本ずつ当てて試験を回す。使い方：mutate.sh <木> <出力> [名...]
T="$1"; OUT="$2"; shift 2; WANT="$*"; cd "$T" || exit 2
BS=PriceOfBread.SimCore/Instruments/InstrumentsBuyShadow.cs; TR=PriceOfBread.SimCore/Transport.cs
FT="FullyQualifiedName~TradeBilateralInstrumentTests|FullyQualifiedName~BuyShadowInstrumentTests|FullyQualifiedName~TransportInstrumentTests"
run() { # 名 ファイル 旧 新
  if [ -n "$WANT" ] && [[ " $WANT " != *" $1 "* ]]; then return; fi
  python - "$2" "$3" "$4" <<'PY' || { echo "== $1 当たらない" >> "$OUT"; return; }
import sys
p,o,n=sys.argv[1:4]; b=open(p,'rb').read(); oo=o.encode(); assert b.count(oo)==1,(p,o,b.count(oo)); open(p,'wb').write(b.replace(oo,n.encode()))
PY
  echo "== $1" >> "$OUT"
  dotnet test PriceOfBread.Tests -c Release --artifacts-path D:/PoB_work/c112tt_mtart --filter "$FT" 2>&1 | grep -E "^\s+失敗 |^(成功|失敗)!" | cut -c1-200 >> "$OUT"
  git checkout -q -- .
}
run M1_未発明の枝を消す $BS 'if (!baumol && !(gate > 0)) o = 0;' 'if (false) o = 0;'
run M2_床の枝を消す $BS 'else if (!(target > 0) && o < c.TargetStockFloor) o = 0;' 'else if (false) o = 0;'
run M3_床の枝を無条件に $BS 'else if (!(target > 0) && o < c.TargetStockFloor) o = 0;' 'else if (!(target > 0)) o = 0;'
run M4_UbBbから0.5を引く $BS 'double ubBb = ts + d * root - p - s, ubA1 = ubBb;' 'double ubBb = ts + d * root - p - s - 0.5, ubA1 = ts + d * root - p - s;'
run M5_rpTの無限をNGに $BS 'double.IsInfinity(um)) return' 'double.IsInfinity(um) || double.IsInfinity(rpT)) return'
run M6b_眠りの半分だけ落とす $BS 'if (p.Bankrupt || EconomyStep.SleepState(r, p, c) == 1) {' 'if (p.Bankrupt) {'
run M7_RailMoveBeginの呼びを消す $TR 'var mv = Instruments.On ? Instruments.RailMoveBegin(from, r) : null;' 'var mv = (Instruments.RailMove?)null;'
run M8_RailMonthBeginの呼びを消す $TR 'var rm = Instruments.On ? Instruments.RailMonthBegin(w, c) : null;' 'var rm = (Instruments.RailMonth?)null;'
echo done >> "$OUT"
