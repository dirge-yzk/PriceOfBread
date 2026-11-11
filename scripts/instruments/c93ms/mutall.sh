#!/usr/bin/env bash
# (1007) 突然変異 5 本を順に回す（出力は 1 本のファイルへ）
D=$(dirname "$0")/mutate.sh   # (1011) 同じ木の mutate.sh
echo "== Mu1 lockBOpen の不等式を逆にする"; bash $D mu1 'wSavPc >= avgPerCap' 'wSavPc < avgPerCap'
echo "== Mu2 max(0,貯蓄) を外す"; bash $D mu2 'System.Math.Max(0, p.Savings.Value)' 'p.Savings.Value'
echo "== Mu3 merchantK に職人を足す"; bash $D mu3 'double mk = merchant?.Size ?? 0,' 'double mk = (merchant?.Size ?? 0) + (artisan?.Size ?? 0),'
echo "== Mu4 植民地格だけを書く"; bash $D mu4 'var m = MerchLocks(r, pettyRate);' 'if (r.Tier != RegionTier.Colonial) continue; var m = MerchLocks(r, pettyRate);'
echo "== Mu5 meritActive を lockBOpen から外す"; bash $D mu5 'bool lockB = meritActive && pettyEntryRate' 'bool lockB = pettyEntryRate'
