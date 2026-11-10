import sys
def rep(path, old, new):
    b = open(path,'rb').read()
    o = old.encode('utf-8'); n = new.encode('utf-8')
    assert b.count(o) >= 1, (path, old[:70], b.count(o))
    open(path,'wb').write(b.replace(o, n))
root, m = sys.argv[1], sys.argv[2]
S = root + '/PriceOfBread.SimCore/'
W = S + 'InstrumentsWar.cs'; D = S + 'Diplomacy.cs'
if m == 'S': rep(W, 'sC = land * (1 - b) - need;', 'sC = land - need;')
elif m == 'T': rep(D, 'if (Instruments.On) Instruments.WarTag(this, ally, enemy, 4); ', '')
elif m == 'T2': rep(D, 'if (Instruments.On) Instruments.WarTagDefault(this, attacker, target, 3); ', '')
elif m == 'Q': rep(W, 'st.NextQ = q + 1;', 'st.NextQ = q + 2;')
elif m == 'R': rep(D, '{ bool okInv = gw.CanInvade(q, out var invReason); if (Instruments.On) Instruments.WarEval(this, c, great, q, invReason); if (!okInv) continue; }', 'if (!gw.CanInvade(q, out _)) continue;')
elif m == 'O':
    rep(W, '        if (d.IsAtWar(self, t)) return 4;\n        if (d.DependentCount(self) >= c.MaxDependents) return 5;\n        if (gw.ExpansionWill < gw.InvadeWillCost(t)) return 6;',
           '        if (gw.ExpansionWill < gw.InvadeWillCost(t)) return 6;\n        if (d.IsAtWar(self, t)) return 4;\n        if (d.DependentCount(self) >= c.MaxDependents) return 5;')
elif m == 'D': rep(W, '        if (d.Suzerain(t) is not null) return 1;', '        d.SubmitRng!.NextDouble();\n        if (d.Suzerain(t) is not null) return 1;')
elif m == 'N1': rep(W, 'return need * (1 - tau * tau);', 'return need * (1 - tau);')
elif m == 'N2': rep(W, '!(tension > 0) ? 0 : tension >= declare ? 1 : tension / declare', '!(tension > 0) ? 0 : tension / declare')
elif m == 'N3': rep(W, 'if (nOk) acc.NewOk++;', 'if (oOk) acc.NewOk++;')
elif m == 'N4': rep(D, 'if (ins) Instruments.WarTag(this, a, b, 1);', 'if (Instruments.On) Instruments.WarTag(this, a, b, 1);')
elif m == 'N5': rep(root + '/PriceOfBread.Tests/DiplomacyPendingTests.cs', 'Assert.Equal(8, header.Length);', 'Assert.Equal(7, header.Length);')
else: raise SystemExit('unknown ' + m)
print('applied', m)
