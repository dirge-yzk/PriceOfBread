# (1238) プローブの Program の写しへ T の表を足す（バイトで読み書き）
import sys
p = sys.argv[1]; b = open(p, 'rb').read()
def rep(a, c):
    global b
    assert b.count(a) == 1, a[:50]; b = b.replace(a, c)
rep(b'foreach (var o in new[] { oJ, oX, oD, oG, oC, oR }) o.WriteLine($"# c124pf probe override=[{ov}] months={months} seeds={s0}..{s1}");\n',
    b'using var oT = new StreamWriter(pre + "_T.tsv", false, new System.Text.UTF8Encoding(false)) { NewLine = "\n" };\n'
    b'foreach (var o in new[] { oJ, oX, oD, oG, oC, oR, oT }) o.WriteLine($"# c125rd probe override=[{ov}] months={months} seeds={s0}..{s1}");\n'
    b'oT.WriteLine("seed\tphase\tt\tclears\tnoNode\tnotLinked\tlpZero\tstockZero\tavailLe0\tinSellers\tsellAccZero\tcapLeftZeroAtSellers\tbuyerRows\trowsJamInList\trowsJamLbBelowLp\trowsJamLeft0\trowsJamCapLeft0\tcandJam\tcandJamDelGeLp\tcandJamNewBest\titerJamBest\titerX0\titerY0\titerBudget0\titerCap0\titerOk\trowsSelf\tX0priceSide\tX0needSide\tcap0seller\tdeals\tpotRows\tsumCapLeftAtSellers\tsumCapAtSellers\tsumSellers\tsumRank\tsumX\tsumY\tsumYPrice\tsumBudgetQ\tsumCapQ\tsumQ\tjamPotential\tjamCap\tjamThrottle");\n'
    b'void DumpT(ulong sd, string ph, int t)\n{\n'
    b'    var C = InternationalMarket.PrC; var S = InternationalMarket.PrS;\n'
    b'    int[] ci = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28];\n'
    b'    int[] si = [0, 1, 2, 6, 7, 8, 9, 10, 11, 12, 3, 4, 5];\n'
    b'    oT.WriteLine(string.Join(\'\t\', new[] { sd.ToString(), ph, t.ToString() }.Concat(ci.Select(i => C[i].ToString())).Concat(si.Select(i => G(S[i])))));\n'
    b'    oT.Flush();\n'
    b'    Array.Clear(C); for (int i = 0; i < S.Length; i++) if (i != 3 && i != 4 && i != 5) S[i] = 0;\n'
    b'}\n')
rep(b'    foreach (var n in sim.Nations) foreach (var r in n.World.Regions) if (r.Name == "\xe3\x82\xb8\xe3\x83\xa3\xe3\x83\x9e\xe3\x82\xa4\xe3\x82\xab") jam = r;\n',
    b'    foreach (var n in sim.Nations) foreach (var r in n.World.Regions) if (r.Name == "\xe3\x82\xb8\xe3\x83\xa3\xe3\x83\x9e\xe3\x82\xa4\xe3\x82\xab") jam = r;\n'
    b'    InternationalMarket.PrJam = jam; InternationalMarket.PrGood = coffee; Array.Clear(InternationalMarket.PrC); Array.Clear(InternationalMarket.PrS);\n')
rep(b'sim.StartWarmup(() => { q++; tt = q; DumpJ("warm", q); });', b'sim.StartWarmup(() => { q++; tt = q; DumpJ("warm", q); DumpT(seed, "warm", q); });')
rep(b'phase = "m"; tt = 0; DumpJ("m", 0); DumpD(0);', b'phase = "m"; tt = 0; DumpJ("m", 0); DumpD(0); DumpT(seed, "m", 0);')
rep(b'        DumpJ("m", mo); DumpD(mo);\n', b'        DumpJ("m", mo); DumpD(mo); DumpT(seed, "m", mo);\n')
open(p, 'wb').write(b); print('ok')
