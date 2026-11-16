#!/usr/bin/env bash
# 突然変異の駆動：使い捨ての変更を worktree に当て、試験を回し、git checkout で戻す
export TEMP=D:/PoB_work/c117b0_work/tmp TMP=D:/PoB_work/c117b0_work/tmp
W=/d/PoB_work/wt_c117b0; M=/d/PoB_work/c117b0_work/mut
cd $W
IM=PriceOfBread.SimCore/Instruments/InstrumentsMintcap.cs; SI=PriceOfBread.SimCore/Simulation.cs
run() { # name file old new
  python -X utf8 -I $M/rep.py "$2" "$3" "$4" || { echo "$1 APPLY-FAIL"; return; }
  dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path D:/PoB_work/c117b0_art --filter "FullyQualifiedName~MintcapInstrumentTests" > $M/$1.log 2>&1
  echo "$1 rc=$? $(grep -E '失敗:|Failed:|成功!|失敗!' $M/$1.log | tail -1)" >> $M/summary.txt
  git checkout -- "$2"
}
: > $M/summary.txt
run M1p $IM "foreach (var r in w.Regions) oldQ +=" "foreach (var r in new[] { cap }) oldQ +="
run M1 $IM "double newQ = MintcapModelQty(cap, metal, parity, rate, ref gapNew, out bool aboveNotAt);" "double newQ = 0; bool aboveNotAt; MintcapModelQty(cap, metal, parity, rate, ref gapNew, out aboveNotAt); foreach (var rr in w.Regions) if (!ReferenceEquals(rr, cap)) newQ += MintcapModelQty(rr, metal, parity, rate, ref gapNew, out _); newQ += 0;"
run M2 $SI "bool capOnly = World.Coef.MintBuysAtCapital > 0;" "bool capOnly = !(World.Coef.MintBuysAtCapital > 0);"
run M3 $IM "else { a.OldAu += oldQ; a.NewAu += newQ; }" "else { }"
run M4 $SI "if (insOn) { Instruments.MintregMonth(World); Instruments.MintcapMonth(World); }" "if (insOn) { Instruments.MintregMonth(World); } Instruments.MintcapMonth(World);"
run M5 $SI "if (capOnly && !ReferenceEquals(r, World.Region)) continue;" "if (false && capOnly && !ReferenceEquals(r, World.Region)) continue;"
echo ALLDONE >> $M/summary.txt
