import sys
p = sys.argv[1] + "/PriceOfBread.SimCore/BilateralMarket.cs"
s = open(p, 'rb').read().decode('utf-8')
def rep(old, new, cnt=1):
    global s
    assert s.count(old) == cnt, (old, s.count(old))
    s = s.replace(old, new)
rep("    public static long[,] FillHist = new long[3,12];", "    public static long[,] FillHist = new long[3,12];\n    public static long[,] FillEx = new long[2,2], DealEx = new long[2,2]; public static double[,] DealValEx = new double[2,2];")
rep("                            nFill++;\n", "                            nFill++; CS.FillEx[csC, mktOf[sellers[s2].Node].PrevExport > 0 ? 1 : 0]++;\n")
rep("dealtRow = true; csDeals++;", "dealtRow = true; csDeals++; CS.DealEx[csC, msA.PrevExport > 0 ? 1 : 0]++; CS.DealValEx[csC, msA.PrevExport > 0 ? 1 : 0] += qA * Wg;")
rep("    static void Dump()\n    {\n", "    static void Dump()\n    {\n        for (int a = 0; a < 2; a++) System.Console.Error.WriteLine($\"[CS] clr={a} fillsSellerNoPrevExport={FillEx[a,0]} fillsSellerPrevExport={FillEx[a,1]} dealsNoPE={DealEx[a,0]} dealsPE={DealEx[a,1]} dealWgNoPE={DealValEx[a,0]:F1} dealWgPE={DealValEx[a,1]:F1}\");\n")
open(p, 'wb').write(s.encode('utf-8'))
print("ok2")
