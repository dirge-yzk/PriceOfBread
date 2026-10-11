#!/usr/bin/env bash
# (266) 番人の突然変異：ソースを1か所変えてビルド→番人を走らせ→赤を確かめ→元に戻す（コピーから戻す＝作業ツリーの未コミットの編集を守る）。
#   bash mutate_a266.sh <リポジトリの根> <出力ファイル>
export PATH="/c/Program Files/dotnet:$PATH"
R=${1:?根}; OUT=${2:?出力}; : > $OUT
cd $R
run() {  # run <名前> <ファイル> <python の置換式 old|new> <フィルタ>
  NAME=$1; FILE=$2; OLD=$3; NEW=$4; FILT=$5
  cp "$FILE" /tmp/mut_backup.cs
  python - "$FILE" "$OLD" "$NEW" <<'PYEOF'
import sys
p,old,new=sys.argv[1:4]
b=open(p,'rb').read().decode('utf-8')
assert old in b, 'ANCHOR-MISSING'
b=b.replace(old,new,1)
open(p,'wb').write(b.encode('utf-8'))
PYEOF
  RC=$?
  if [ $RC -ne 0 ]; then echo "$NAME ANCHOR-MISSING" >> $OUT; cp /tmp/mut_backup.cs "$FILE"; return; fi
  dotnet build PriceOfBread.Tests -c Release 2>&1 | grep -E " error " | head -2 >> $OUT
  RES=$(dotnet test PriceOfBread.Tests -c Release --no-build --filter "$FILT" 2>&1 | tail -3 | tr '\n' ' ')
  echo "$NAME => $RES" >> $OUT
  cp /tmp/mut_backup.cs "$FILE"
}
G="FullyQualifiedName~FiscalTests.行政の予算は必要額で頭打ちされ"
run "M1 頭打ちを外す（claim をそのまま返す）" PriceOfBread.SimCore/Fiscal.cs "return need < claim ? need : claim;" "return claim;" "$G"
run "M2 頭打ちを max にする" PriceOfBread.SimCore/Fiscal.cs "return need < claim ? need : claim;" "return need > claim ? need : claim;" "$G"
run "M3 必要額の距離の割増を落とす" PriceOfBread.SimCore/EconomyStep.cs "adminMul = world is not null ? Fiscal.AdminDistanceMul(world, region) : 1.0;" "adminMul = 1.0;" "$G"
run "M4 Fiscal.Step が別の式で書き直す" PriceOfBread.SimCore/Fiscal.cs "double adminClaim = w.LastAdminClaimPerMonth.Value;" "double adminClaim = c.AdminBudgetFrac.Value * w.AdminSpending * budgetBase * AdminDistanceMean(w); w.LastAdminClaimPerMonth = new MoneyPerMonth(adminClaim);" "$G"
run "M5 余剰の控除が請求を読まない" PriceOfBread.SimCore/Fiscal.cs "double surplus = Math.Max(0, budgetBase - interest - adminClaim - militaryClaim);" "double surplus = Math.Max(0, budgetBase - interest - militaryClaim);" "$G"
run "M6 州の予算の和でなく先頭の州だけを請求に書く" PriceOfBread.SimCore/Simulation.cs "adminClaimNat += r.AdminBudget;" "adminClaimNat = r.AdminBudget;" "$G"
run "M7 FiscalEnabled の門を外す（書かない世界にも書く）" PriceOfBread.SimCore/Simulation.cs "if (World.FiscalEnabled) World.LastAdminClaimPerMonth" "World.LastAdminClaimPerMonth" "FullyQualifiedName~SteelMarketAiTests|FullyQualifiedName~FiscalTests"
echo END >> $OUT
