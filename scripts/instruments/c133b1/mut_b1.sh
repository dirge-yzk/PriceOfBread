#!/usr/bin/env bash
# (1291) c133b1 突然変異の腕（使い捨ての写しの木・scratch.sh）を順に回し、表を measurements/m1291_20261122/mut/ へ書く。
#   bash scripts/instruments/c133b1/mut_b1.sh   （bg.sh で起こす）
cd "$(dirname "$0")/../../.."; R=$(pwd); S=$R/scripts/instruments/c133b1; OUT=$R/measurements/m1291_20261122/mut; mkdir -p $OUT
P=PriceOfBread.SimCore/Coefficients.p04_trade.cs
F1="$P::public double EndowmentCostFollowsAbundance = 0.0;::public double EndowmentCostFollowsAbundance = 1.0;"
SIM=PriceOfBread.SimCore/Simulation.cs
FT="FullyQualifiedName~EndowmentCostTests|FullyQualifiedName~ColonialPopTests"
FALL="FullyQualifiedName~ColonialPopTests|FullyQualifiedName~DiscoveryScoreTests|FullyQualifiedName~CorncfInstrumentTests|FullyQualifiedName~RecipeoutInstrumentTests|FullyQualifiedName~EconomyTests|FullyQualifiedName~TradePhase0Tests|FullyQualifiedName~EndowmentCostTests|FullyQualifiedName~DormantIdentityTests"
run() { local nm=$1; shift; bash $S/scratch.sh "$nm" "$@"; echo "$nm rc=$?" >> $OUT/rc.txt; cp D:/PoB_work/c133b1_work/$nm/sum.txt $OUT/$nm.txt; }
rm -f $OUT/rc.txt
run f1all "$FALL" "$F1"
run f0all "$FALL"
run mu1_gate "$FT" "$F1" "$SIM::costDiv > 1.0 ? c.EndowmentCostFollowsAbundance::costDiv > 1.0 ? 1.0"
run mu2_pow2 "$FT" "$F1" "$SIM::pr.MarginalCost /= System.Math.Pow(costDiv, gate);::pr.MarginalCost /= System.Math.Pow(costDiv, 2.0 * gate);"
run mu3_scarce "$FT" "$F1" "$SIM::costDiv < 1.0 ? c.EndowmentCostFollowsScarcity::costDiv < 1.0 ? c.EndowmentCostFollowsAbundance"
run mu4_noseed "$FT" "$F1" "$SIM::        pr.Productivity *= capMul;
        DivideCostByEndowment(pr, costDiv, c);::        pr.Productivity *= capMul;"
run mu5_noremit "$FT" "$F1" "PriceOfBread.SimCore/EconomyStep.cs::Colonial.RemitAbsenteeRent(world!, region, absentee);::_ = absentee;"
echo "MUTDONE $(date '+%F %T')" >> $OUT/rc.txt
