import sys
p = sys.argv[1] + "/PriceOfBread.SimCore/BilateralMarket.cs"
s = open(p, 'rb').read().decode('utf-8')
def rep(old, new, cnt=1):
    global s
    assert s.count(old) == cnt, (old, s.count(old))
    s = s.replace(old, new)
# static census class
rep("public sealed partial class InternationalMarket\n{\n", """public static class CS
{
    // [clr 0=full,1=thin][ps 0=import,1=shortage-only,2=idle]
    public static long[,] Rows = new long[2,3], Scanned = new long[2,3], Fills = new long[2,3], Dealt = new long[2,3], DistinctSum = new long[2,3], Deals = new long[2,3];
    public static long[,,] Over = new long[2,3,6];   // sum max(0, fills-K) for K=1,2,3,4,8,16
    public static long[,,] FillsDealt = new long[2,3,2];  // fills in rows that dealt / not
    public static long[,] FillHist = new long[3,12];
    static CS() { System.AppDomain.CurrentDomain.ProcessExit += (_, _) => Dump(); }
    static void Dump()
    {
        string[] cn = {"full","thin"}; string[] pn = {"import","shortOnly","idle"};
        for (int a = 0; a < 2; a++) for (int b = 0; b < 3; b++)
            System.Console.Error.WriteLine($"[CS] {cn[a]} {pn[b]} rows={Rows[a,b]} scanned={Scanned[a,b]} fills={Fills[a,b]} dealtRows={Dealt[a,b]} deals={Deals[a,b]} distinct={DistinctSum[a,b]} fillsInDealt={FillsDealt[a,b,0]} fillsInUndealt={FillsDealt[a,b,1]} over[K=1,2,3,4,8,16]={Over[a,b,0]},{Over[a,b,1]},{Over[a,b,2]},{Over[a,b,3]},{Over[a,b,4]},{Over[a,b,5]}");
    }
}
public sealed partial class InternationalMarket
{
""")
# row entry
rep("                m.ClearFailLp = 0;   // 印は走査して成約しなかったときだけ", """                int csC = thin ? 1 : 0; int csP = m.PrevImport > 0 ? 0 : (m.LastShortage > 0 ? 1 : 2); CS.Rows[csC, csP]++; long csFill0 = nFill; long csDeals = 0; long csDistinct = 0; int csLast = -2;
                m.ClearFailLp = 0;   // 印は走査して成約しなかったときだけ""")
# deal
rep("                    dealtRow = true;\n                    double vA = qA * sA;", "                    dealtRow = true; csDeals++; if (bestA != csLast) { csDistinct++; csLast = bestA; }\n                    double vA = qA * sA;")
# row end
rep("                if (!dealtRow) m.ClearFailLp = lp;", """                { long rf = nFill - csFill0; CS.Fills[csC, csP] += rf; if (rf > 0) CS.Scanned[csC, csP]++; if (dealtRow) { CS.Dealt[csC, csP]++; CS.FillsDealt[csC, csP, 0] += rf; } else CS.FillsDealt[csC, csP, 1] += rf; CS.Deals[csC, csP] += csDeals; CS.DistinctSum[csC, csP] += csDistinct;
                  int[] ks = {1,2,3,4,8,16}; for (int q = 0; q < 6; q++) if (rf > ks[q]) CS.Over[csC, csP, q] += rf - ks[q]; }
                if (!dealtRow) m.ClearFailLp = lp;""")
open(p, 'wb').write(s.encode('utf-8'))
print("ok")
