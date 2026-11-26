#!/usr/bin/env bash
# (1384) c147vs 突然変異：使い捨ての木 D:/PoB_work/wt_c147vs_mut で M1〜M3 を 1 本ずつ当てて SoldierPayBillTests を回す。
#   bash mut.sh <出力フォルダ>   （木は ce1b045cd 以後の c147vs の旗 0 の器）
set -u
OUT="${1:?出力フォルダ}"; T=D:/PoB_work/wt_c147vs_mut; A=D:/PoB_work/c147vs_work/mut_art
mkdir -p "$OUT"
S=PriceOfBread.SimCore/Simulation.cs
ap() {  # ap <旧> <新>：Simulation.cs の 1 か所をバイトで置き換える
  python -I - "$T/$S" "$1" "$2" <<'PY'
import sys
p, a, b = sys.argv[1], sys.argv[2].encode('utf-8'), sys.argv[3].encode('utf-8')
s = open(p, 'rb').read(); assert s.count(a) == 1, ('見つからない', a); open(p, 'wb').write(s.replace(a, b))
PY
}
run() {
  ( cd "$T" && dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path "$A" \
      --filter "FullyQualifiedName~SoldierPayBillTests" --logger "trx;LogFileName=$OUT/$1.trx" > "$OUT/$1.log" 2>&1 )
  { echo "## $1"; grep -E "^\s+(失敗|合格) |合計:" "$OUT/$1.log"; } >> "$OUT/summary.txt"
  git -C "$T" checkout -q -- "$S"
}
: > "$OUT/summary.txt"
run M0_none
ap 'if (!billPrevMonth) World.LastSoldierPayPerMonth = new MoneyPerMonth(0);' 'World.LastSoldierPayPerMonth = new MoneyPerMonth(0);'
run M1_zero_at_head
ap 'if (billPrevMonth) World.LastSoldierPayPerMonth = billSum;' '{ }'
run M2_no_write
ap 'bool billPrevMonth = World.Coef.SoldierPayBillPrevMonth > 0;' 'bool billPrevMonth = true;'
run M3_ignore_flag
