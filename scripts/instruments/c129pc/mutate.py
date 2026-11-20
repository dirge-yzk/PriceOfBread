# 使い捨ての木（wt_c129pc_mut）に突然変異を 1 本入れる/戻す。使い方：python -X utf8 -I mutate.py <木> <mu1|mu2|mu3|mu4|mu5|mu6|mu0|reset>
import sys, subprocess
tree, mu = sys.argv[1], sys.argv[2]
S = tree + '/PriceOfBread.SimCore/'
def rep(path, old, new):
    b = open(path, 'rb').read().decode('utf-8'); assert b.count(old) == 1, (path, old[:40], b.count(old))
    open(path, 'wb').write(b.replace(old, new).encode('utf-8'))
M = S + 'MultiNationSim.cs'; SI = S + 'Simulation.cs'; P = S + 'Instruments/InstrumentsPopClose.cs'; IN = S + 'Instruments/Instruments.cs'
H1 = "            if (Instruments.On) Instruments.PopMove(Instruments.PopMoveKind.Specialist, srcR, dstR, mvSp);   // (1261) popclose_s\n"
H2 = "                if (Instruments.On) Instruments.PopMove(Instruments.PopMoveKind.Garrison, rr, loyalRegion, insG0 - garrison.Size);   // (1261) 兵の移しの口（gs の前提）\n"
H4 = "            if (Instruments.On) Instruments.PopMove(Instruments.PopMoveKind.ColonyMerge, null, same, inP);   // (1261) popclose_s\n"
if mu == 'reset': subprocess.run(['git', '-C', tree, 'checkout', '--', '.'], check=True)
elif mu == 'mu1': rep(M, H1, '')
elif mu == 'mu2': rep(M, H2, '')
elif mu == 'mu3': rep(P, "a.Births += births; ", "")
elif mu == 'mu4': rep(SI, H4, '')
elif mu == 'mu5': rep(IN, "        PopMove(PopMoveKind.Migration, src, dst, sizeK);   // (1261) popclose_s\n", '')
elif mu == 'mu6': rep(IN, "        PopMove(PopMoveKind.CrossBorder, src, dst, flowK);   // (1261) popclose_s\n", '')
elif mu == 'mu0': rep(M, H1, ''); rep(M, H2, ''); rep(SI, H4, '')
else: raise SystemExit('?')
