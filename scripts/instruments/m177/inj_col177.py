"""⚠★(402)：注入の鍵（Fiscal.cs の注〈仲介者の取り分（支配度 段3・休眠 RuleSharesTaxBase=0）〉）は (399) で消えた＝今の木では注入先が見つからない。古い一回きりの道具＝走らせるならその回のコミットの木で。
[計測の写しだけ] (177) 植民地の収支の計器（読むだけ）＝(173)〔旧仮番号 (175)〕の inj_col175 の直し（反証役 vc1#1）：
首都の投資家の地代の列は「受け取りの側」＝不在地主が受け取った地代（国内の送金の正味〔RemitAbsenteeRent の移した額・符号つき〕＋輸出代金の地代〔PaySales で不在地主へ払った重み〕）。
旧＝首都の州の精算の行で首都の投資家の LandRent を読んだ＝植民地の州の送金はその後に着き翌月の頭で0へ戻る＝構造的に0（計器の誤り）。
環境変数 POB_COL175_DIR の下に c<seed>.txt。未設定なら1命令も走らない。
C 行＝国×四半期：
  q|年|添字|BaseName|Name|植民地格の州の数|植民地格の州の人口|国の人口|
  植民地格の州の支出（四半期和・StateSpent）：陸（兵の給与と軍需）|行政|その他の正の区分の和|
  国の植民地維持費（台帳 ColonialUpkeep の四半期の動き＝補給財）|国の税の台帳（Tax の四半期の動き）|
  税：植民地の POP の払った税（1本の率の後）|うち地税|全ての POP の税|うち地税|国庫へ動いた税（漏れの後）|
  地代（RGO の残余＝国営持分の後）：植民地格の州の輸出作物（賦存かつ主食でない）の国内の売上の地代|同・それ以外|輸出代金の地代（輸出作物）|同・それ以外|
  首都の投資家の地代（LandRent・月末の値の四半期和）|
  汽船の割合|海運の容量|海運占有|汽船の解禁度（首都）|帆船の値段|汽船の値段|石炭の値段|機関の時代係数|国家の支払い率 r|国庫|歳入|
  補給財の代金のうち国営持分で国庫へ戻った額（台帳 StateFactory へ）
読み手＝an_col175.py。使い方：python inj_col175.py <写しの木>"""
import sys
root = sys.argv[1]


def edit(rel, pairs, tail=''):
    p = root + '/' + rel
    raw = open(p, 'rb').read(); bom = raw.startswith(bytes([0xef, 0xbb, 0xbf])); s = raw.decode('utf-8-sig')
    CR, LF = chr(13), chr(10)
    crlf = (CR + LF) in s
    for old, new in pairs:
        if crlf: old = old.replace(LF, CR + LF); new = new.replace(LF, CR + LF)
        assert s.count(old) == 1, (rel, old[:90], s.count(old))
        s = s.replace(old, new)
    s += tail.replace(LF, CR + LF) if crlf else tail
    open(p, 'wb').write((b'\xef\xbb\xbf' if bom else b'') + s.encode('utf-8'))


TAIL = r'''

/// <summary>[計測の写しだけ] (175) 植民地の収支の計器（読むだけ）。</summary>
public static class XCol
{
    public static readonly string? Dir = System.Environment.GetEnvironmentVariable("POB_COL175_DIR");
    public static readonly bool On = Dir is { Length: > 0 };
    static string F(double v) => double.IsNaN(v) ? "nan" : v.ToString("G6", System.Globalization.CultureInfo.InvariantCulture);
    public sealed class Acc
    {
        public double Army, Admin, OtherSpent, TaxCol, LandCol, TaxAll, LandAll, Moved, RDomExp, RDomLoc, RXExp, RXLoc, InvLand, SupplyBack;
        public double LedUp0, LedTax0; public bool Has;
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, Acc> S = new();
    public static Acc Of(World w) => S.GetValue(w, _ => new Acc());
    public static bool ExportRgo(Region r, int g)
        => g >= 0 && g < Catalog.Goods.Length && System.Array.IndexOf(r.Resources, g) >= 0 && !(Catalog.Goods[g].Tier == GoodTier.Staple || Catalog.Goods[g].GrainChain);
    public static System.Collections.Generic.HashSet<Pop> ColonialPopSet(World w)
    {
        var h = new System.Collections.Generic.HashSet<Pop>(System.Collections.Generic.ReferenceEqualityComparer.Instance);
        foreach (var r in w.Regions) if (r.Tier == RegionTier.Colonial) foreach (var p in r.Pops) h.Add(p);
        return h;
    }
    public static void TaxPaid(World w, bool col, double tax, double land)
    {
        var a = Of(w); lock (a) { a.TaxAll += tax; a.LandAll += land; if (col) { a.TaxCol += tax; a.LandCol += land; } }
    }
    public static void TaxMoved(World w, double moved) { var a = Of(w); lock (a) a.Moved += moved; }
    public static void SupplyBack(World w, double back) { var a = Of(w); lock (a) a.SupplyBack += back; }
    public static void Spent(World w, Region r)
    {
        var a = Of(w);
        lock (a)
        {
            a.Army += r.StateSpent[(int)TreasuryCat.Army]; a.Admin += r.StateSpent[(int)TreasuryCat.Admin];
            for (int k = (int)TreasuryCat.Navy; k < r.StateSpent.Length; k++) if (k != (int)TreasuryCat.Admin && r.StateSpent[k] > 0) a.OtherSpent += r.StateSpent[k];
        }
        // 首都の投資家の LandRent（月末＝この月の地代）は首都の精算の行で数える（Simulation の州ループの中＝首都は1回）
    }
    public static void Recv(World w, double v) { var a = Of(w); lock (a) a.InvLand += v; }   // (177) 不在地主が受け取った地代（受け取りの側）
    public static void CapitalRent(World w, Region r)
    {
        if (!ReferenceEquals(r, w.Region) || r.InvestorPopIndex < 0 || r.InvestorPopIndex >= r.Pops.Count) return;
        var a = Of(w); lock (a) a.InvLand += r.Pops[r.InvestorPopIndex].LandRentPerMonth;
    }
    public static void Rent(World w, Region r, int g, double rent, bool export)
    {
        var a = Of(w);
        bool ex = ExportRgo(r, g);
        lock (a)
        {
            if (export) { if (ex) a.RXExp += rent; else a.RXLoc += rent; }
            else { if (ex) a.RDomExp += rent; else a.RDomLoc += rent; }
        }
    }
    public static void Reset(World w)
    {
        var a = Of(w);
        a.Army = a.Admin = a.OtherSpent = a.TaxCol = a.LandCol = a.TaxAll = a.LandAll = a.Moved = a.RDomExp = a.RDomLoc = a.RXExp = a.RXLoc = a.InvLand = a.SupplyBack = 0;
        a.LedUp0 = w.TreasuryLedger[(int)TreasuryCat.ColonialUpkeep]; a.LedTax0 = w.TreasuryLedger[(int)TreasuryCat.Tax]; a.Has = true;
    }
    public static void Line(System.Text.StringBuilder sb, int q, int xi, World w)
    {
        var a = Of(w);
        if (!a.Has) Reset(w);
        int nc = 0; double cp = 0;
        foreach (var r in w.Regions) if (r.State == RegionState.Active && r.Tier == RegionTier.Colonial) { nc++; cp += r.SumPopulation(); }
        var cap = w.Region; var c = w.Coef;
        double gate = cap.Markets.Length > Catalog.SteamshipId ? Discovery.GoodGate(cap, Catalog.SteamshipId) : 0;
        double pSail = cap.Markets.Length > Catalog.SailingShipId ? cap.Markets[Catalog.SailingShipId].Price : 0;
        double pSteam = cap.Markets.Length > Catalog.SteamshipId ? cap.Markets[Catalog.SteamshipId].Price : 0;
        double pCoal = cap.Markets.Length > Catalog.CoalId ? cap.Markets[Catalog.CoalId].Price : 0;
        double era = c.EraFactor(w.WorldTechLevel, c.EraColonyCoal);
        sb.Append("C|").Append(q + 1).Append('|').Append(w.Year).Append('|').Append(xi).Append('|').Append(w.SelfPower?.BaseName).Append('|').Append(w.SelfPower?.Name).Append('|')
          .Append(nc).Append('|').Append(F(cp)).Append('|').Append(F(w.TotalPopulation)).Append('|')
          .Append(F(a.Army)).Append('|').Append(F(a.Admin)).Append('|').Append(F(a.OtherSpent)).Append('|')
          .Append(F(w.TreasuryLedger[(int)TreasuryCat.ColonialUpkeep] - a.LedUp0)).Append('|').Append(F(w.TreasuryLedger[(int)TreasuryCat.Tax] - a.LedTax0)).Append('|')
          .Append(F(a.TaxCol)).Append('|').Append(F(a.LandCol)).Append('|').Append(F(a.TaxAll)).Append('|').Append(F(a.LandAll)).Append('|').Append(F(a.Moved)).Append('|')
          .Append(F(a.RDomExp)).Append('|').Append(F(a.RDomLoc)).Append('|').Append(F(a.RXExp)).Append('|').Append(F(a.RXLoc)).Append('|').Append(F(a.InvLand)).Append('|')
          .Append(F(w.ShipSteamShareEma)).Append('|').Append(F(w.ShipCapacity)).Append('|').Append(F(w.ColonialShipBurden)).Append('|').Append(F(gate)).Append('|')
          .Append(F(pSail)).Append('|').Append(F(pSteam)).Append('|').Append(F(pCoal)).Append('|').Append(F(era)).Append('|').Append(F(w.FiscalFundingRatio)).Append('|')
          .Append(F(w.Treasury)).Append('|').Append(F(w.LastRevenuePerMonth)).Append('|').Append(F(a.SupplyBack)).AppendLine();
        Reset(w);
    }
}
'''

edit('PriceOfBread.SimCore/Simulation.cs', [
    ('                World.SettleStateSpent(r);',
     '                if (XCol.On) { if (r.Tier == RegionTier.Colonial) XCol.Spent(World, r); }\n                World.SettleStateSpent(r);'),
], TAIL)

edit('PriceOfBread.SimCore/Fiscal.cs', [
    ('        var taxPayers = new List<Pop>(); var taxClaim = new List<double>();\n',
     '        var taxPayers = new List<Pop>(); var taxClaim = new List<double>();\n        var xcolSet = XCol.On ? XCol.ColonialPopSet(w) : null;\n'),
    ('            // 土地税：RGOの地代（封建貢租の受け手＝地主）に課税。',
     '            double xTaxPre = tax;\n            // 土地税：RGOの地代（封建貢租の受け手＝地主）に課税。'),
    ('            // 仲介者の取り分（支配度 段3・休眠 RuleSharesTaxBase=0）',
     '            double xLand = tax - xTaxPre;\n            // 仲介者の取り分（支配度 段3・休眠 RuleSharesTaxBase=0）'),
    ('            tax *= rp; cut *= rp; tithe *= rp;\n',
     '            tax *= rp; cut *= rp; tithe *= rp;\n            if (xcolSet is not null) XCol.TaxPaid(w, xcolSet.Contains(p), tax, xLand * (ruleMul is not null ? ruleMul[pi] : 1.0) * rp);\n'),
    ('        revenue = World.Transfer((w, taxPayers), (w, null, null), revenue - delivered, TreasuryCat.Tax, fromWeights: taxClaim);\n',
     '        revenue = World.Transfer((w, taxPayers), (w, null, null), revenue - delivered, TreasuryCat.Tax, fromWeights: taxClaim);\n        if (XCol.On) XCol.TaxMoved(w, revenue);\n'),
])

edit('PriceOfBread.SimCore/EconomyStep.cs', [
    ('            s.LastRevenue = producerRevenue; s.LastCost = inputCost[pi] + wages;',
     '            if (XCol.On && world is not null && region.Tier == RegionTier.Colonial && s.IsRgo) XCol.Rent(world, region, s.GoodId, rent, false);\n            s.LastRevenue = producerRevenue; s.LastCost = inputCost[pi] + wages;'),
])

edit('PriceOfBread.SimCore/InternationalMarket.cs', [
    ('                    double sc = rent * World.StateProfitShare(pr, rr, coef);\n',
     '                    double sc = rent * World.StateProfitShare(pr, rr, coef);\n                    if (XCol.On && rr.Tier == RegionTier.Colonial && pr.IsRgo) XCol.Rent(w, rr, pr.GoodId, rent - sc, true);\n'),
    ('        double moved = 0;\n',
     '        double moved = 0;\n        if (XCol.On && cat == TreasuryCat.ColonialUpkeep && from.Pops is null) XCol.SupplyBack(w, stateCut);\n'),
])

edit('PriceOfBread.SimCore/Colonial.cs', [
    ('            double moved = World.Transfer((w, [payer]), (w, [payee], [1.0]), Math.Abs(a), income: true);',
     '            double moved = World.Transfer((w, [payer]), (w, [payee], [1.0]), Math.Abs(a), income: true);\n            if (XCol.On) XCol.Recv(w, a > 0 ? moved : -moved);'),
])

edit('PriceOfBread.SimCore/InternationalMarket.cs', [
    ('                        to.Add(Colonial.AbsenteeRgo(rr, pr) && Colonial.AbsenteeOwner(w, rr) is { } ab ? ab : rr.Pops[pr.OwnerPopIndex]); wt.Add(rent);',
     '                        var xab = Colonial.AbsenteeRgo(rr, pr) ? Colonial.AbsenteeOwner(w, rr) : null;\n                        if (XCol.On && xab is not null) XCol.Recv(w, rent);\n                        to.Add(xab ?? rr.Pops[pr.OwnerPopIndex]); wt.Add(rent);'),
])

edit('PriceOfBread.SimCore/SelfPlay.cs', [
    ('        sim.StartWarmup();\n',
     '        sim.StartWarmup();\n        var xcSb = XCol.On ? new System.Text.StringBuilder() : null;\n        if (XCol.On) foreach (var xn in sim.Nations) XCol.Reset(xn.World);\n'),
    ('                if (sim.Nations.Count > gdpPeak.Length)',
     '                if (xcSb is not null)\n                {\n                    for (int xi = 0; xi < sim.Nations.Count; xi++) XCol.Line(xcSb, q, xi, sim.Nations[xi].World);\n'
     '                    if (q == quarters - 1) System.IO.File.WriteAllText(System.IO.Path.Combine(XCol.Dir!, $"c{seed}.txt"), xcSb.ToString());\n                }\n'
     '                if (sim.Nations.Count > gdpPeak.Length)'),
])
print('ok col177', root)
