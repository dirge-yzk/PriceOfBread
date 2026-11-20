import sys
def rep(path, old, new, cnt=1):
    b = open(path,'rb').read()
    o = old.encode(); n = new.encode()
    assert b.count(o)==cnt, (path, old[:50], b.count(o))
    open(path,'wb').write(b.replace(o,n))
I='Instruments/Instruments.cs'
rep(I, "        RdhPhase(sim, seed, time);   // RdhPhase〔履歴 Instruments.cs#251〕\n", "        RdhPhase(sim, seed, time);   // RdhPhase〔履歴 Instruments.cs#251〕\n        PopClosePhase(sim, time);    // (1261) popclose_s\n")
rep(I, "        BasisxTable(sim, seed, q); WarQuarter(sim, seed, q);   // BasisxTable〔履歴 Instruments.cs#328〕\n", "        BasisxTable(sim, seed, q); WarQuarter(sim, seed, q);   // BasisxTable〔履歴 Instruments.cs#328〕\n        PopCloseTable(sim, seed, q);   // (1261) popclose_s\n")
rep(I, "        RdhClose(sim);   // RdhClose〔履歴 Instruments.cs#364〕\n", "        RdhClose(sim);   // RdhClose〔履歴 Instruments.cs#364〕\n        PopCloseClose(sim);   // (1261) popclose_s\n")
rep(I, "        to[dst] = to.GetValueOrDefault(dst) + sizeK;\n    }\n", "        to[dst] = to.GetValueOrDefault(dst) + sizeK;\n        PopMove(PopMoveKind.Migration, src, dst, sizeK);   // (1261) popclose_s\n    }\n")
rep(I, "        to[dst] = to.GetValueOrDefault(dst) + flowK;\n    }\n", "        to[dst] = to.GetValueOrDefault(dst) + flowK;\n        PopMove(PopMoveKind.CrossBorder, src, dst, flowK);   // (1261) popclose_s\n    }\n")
T='Instruments/InstrumentsTg.cs'
rep(T, "    internal static bool Pay = true;\n", "    internal static bool Pay = true;\n    internal static bool PopClose = true;\n")
rep(T, "        Pay = Instruments.Wants(\"pay_s\");\n", "        Pay = Instruments.Wants(\"pay_s\");\n        PopClose = Instruments.Wants(\"popclose_s\");\n")
R='Instruments/InstrumentsRdh.cs'
rep(R, "internal static void RdhRegion(Region? r) { if (RdhOn) RdhCurRegion = r; }", "internal static void RdhRegion(Region? r) { if (RdhOn || Tg.PopClose) RdhCurRegion = r; }")
rep(R, "        if (!RdhOn || RdhCurRegion is not { } r) return;\n        var a = RdhOf(r); a.Births", "        if (Tg.PopClose && RdhCurRegion is { } pcr) PcDemo(pcr, births, deathsNormal, deathsFamine);   // (1261) popclose_s\n        if (!RdhOn || RdhCurRegion is not { } r) return;\n        var a = RdhOf(r); a.Births")
M='MultiNationSim.cs'
rep(M, "                Military.MoveCohort(garrison, loyalPop, garrison.Size * stay, _coef);\n", "                double insG0 = Instruments.On ? garrison.Size : 0;   // (1261) popclose_s\n                Military.MoveCohort(garrison, loyalPop, garrison.Size * stay, _coef);\n                if (Instruments.On) Instruments.PopMove(Instruments.PopMoveKind.Garrison, rr, loyalRegion, insG0 - garrison.Size);   // (1261) 兵の移しの口（gs の前提）\n")
rep(M, "            moved += MoveAbroad(src, dst, sp, dp, flow);\n", "            double mvSp = MoveAbroad(src, dst, sp, dp, flow);\n            moved += mvSp;\n            if (Instruments.On) Instruments.PopMove(Instruments.PopMoveKind.Specialist, srcR, dstR, mvSp);   // (1261) popclose_s\n")
S='Simulation.cs'
rep(S, "            same.AnnexBaggage += inP;", "            if (Instruments.On) Instruments.PopMove(Instruments.PopMoveKind.ColonyMerge, null, same, inP);   // (1261) popclose_s\n            same.AnnexBaggage += inP;")
