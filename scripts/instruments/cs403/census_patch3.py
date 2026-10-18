import sys
root = sys.argv[1]
p = root + "/PriceOfBread.SimCore/BilateralMarket.cs"
s = open(p, 'rb').read().decode('utf-8')
def rep(old, new, cnt=1):
    global s
    assert s.count(old) == cnt, (old, s.count(old))
    s = s.replace(old, new)
rep("    public static long[,] FillEx = new long[2,2]", "    public static long[,,] Cov = new long[2,3,8]; public static double[,,] CovW = new double[2,3,8]; public static long[,] StapleShortFills = new long[2,2]; public static long[,] StapleShortRows = new long[2,2];\n    public static long[,] FillEx = new long[2,2]")
rep("    static void Dump()\n    {\n", """    static void Dump()
    {
        for (int a = 0; a < 2; a++) for (int b = 0; b < 3; b++)
            System.Console.Error.WriteLine($"[CS3] clr={a} cls={b} dealsByPrevMemRank <1,<2,<3,<4,<8,anyInPrev,notInPrev: {Cov[a,b,0]},{Cov[a,b,1]},{Cov[a,b,2]},{Cov[a,b,3]},{Cov[a,b,4]},{Cov[a,b,5]},{Cov[a,b,6]} weights: {CovW[a,b,0]:F0},{CovW[a,b,1]:F0},{CovW[a,b,2]:F0},{CovW[a,b,3]:F0},{CovW[a,b,4]:F0},{CovW[a,b,5]:F0},{CovW[a,b,6]:F0}");
        for (int a = 0; a < 2; a++) System.Console.Error.WriteLine($"[CS3] clr={a} shortOnly staple/grain fills={StapleShortFills[a,1]} rows={StapleShortRows[a,1]} nonStaple fills={StapleShortFills[a,0]} rows={StapleShortRows[a,0]}");
""")
rep("long csFill0 = nFill; long csDeals = 0;", "long csFill0 = nFill; var csPrevMem = m.CsMem; csNewMem.Clear(); bool csStaple = gd.Tier == GoodTier.Staple || gd.GrainChain; long csDeals = 0;")
# deal coverage
rep("dealtRow = true; csDeals++;", """dealtRow = true; csDeals++;
                    { var sr = tr[snA].r; int ix = csPrevMem is null ? -1 : Array.IndexOf(csPrevMem, sr); int bk = ix < 0 ? 6 : (ix < 1 ? 0 : ix < 2 ? 1 : ix < 3 ? 2 : ix < 4 ? 3 : ix < 8 ? 4 : 5); CS.Cov[csC, csP, bk]++; CS.CovW[csC, csP, bk] += qA * Wg; if (!csNewMem.Contains(sr) && csNewMem.Count < 32) csNewMem.Add(sr); }""")
rep("                { long rf = nFill - csFill0;", "                { long rf = nFill - csFill0; m.CsMem = csNewMem.Count > 0 ? csNewMem.ToArray() : null; if (csP == 1) { CS.StapleShortFills[csC, csStaple ? 1 : 0] += rf; CS.StapleShortRows[csC, csStaple ? 1 : 0]++; }")
rep("            long nFill = 0;   // 着値の充填の局所の数", "            long nFill = 0; var csNewMem = new List<Region>(8);   // 着値の充填の局所の数")
open(p, 'wb').write(s.encode('utf-8'))
p = root + "/PriceOfBread.SimCore/Model.cs"
s = open(p, 'rb').read().decode('utf-8')
rep("    [System.NonSerialized] internal double ClearFailLp;", "    [System.NonSerialized] internal double ClearFailLp;\n    [System.NonSerialized] public Region[]? CsMem;")
open(p, 'wb').write(s.encode('utf-8'))
print("ok3")
