"""[計測の写しだけ] S3a 補給の鎖の計器（読むだけ・国の欄つき）。環境変数 POB_S3A_DIR の下に s<seed>.txt。未設定なら1命令も走らない。
  N 行＝全ての生きた国×四半期：補給の充足（全体・食料・日用品）・兵の充足と即応度・装備・基礎貨幣・国庫・物価・為替・兌換・外資・戦争・内戦・
        貯蓄（全POP・商人）・支払い率・輸出入・予算の取り置き（在庫切れの食料）の割合・張り付いた主食の人口比
  W 行＝世界×四半期：国の数・充足<0.3 の国・張り付いた市場の数（主食・日用・嗜好・その他）・世界の取り置きの割合
  R/G 行＝充足<0.3 の入口（その国の最初の四半期と以後8四半期ごと）と、0.45 を割った入口の四半期：州ごと・食料と日用品の財ごと
EconomyStep の3点（予算の始め・支払い・在庫切れの取り置き）は同じ式を読むだけ（元の行は1字も変えない）。"""
import sys
root = sys.argv[1]

def edit(rel, pairs, tail=''):
    p = root + '/' + rel
    raw = open(p, 'rb').read(); bom = raw.startswith(b'\xef\xbb\xbf'); s = raw.decode('utf-8-sig')
    for old, new in pairs:
        assert s.count(old) == 1, (rel, old[:90], s.count(old))
        s = s.replace(old, new)
    s += tail
    open(p, 'wb').write((b'\xef\xbb\xbf' if bom else b'') + s.encode('utf-8'))

edit('PriceOfBread.SimCore/EconomyStep.cs', [
    ('        for (int pi0 = 0; pi0 < region.Pops.Count; pi0++) lastSupply[pi0] = region.Pops[pi0].SupplyFill;\n',
     '        for (int pi0 = 0; pi0 < region.Pops.Count; pi0++) lastSupply[pi0] = region.Pops[pi0].SupplyFill;\n'
     '        var s3acc = S3aDiag.On ? S3aDiag.For(region) : null; if (s3acc is not null) System.Array.Clear(s3acc);\n'),
    ('        if (BudgetBindDiag.On) foreach (var p in region.Pops) budgetStartDiag += Math.Max(0, p.RemainingBudget);\n',
     '        if (BudgetBindDiag.On) foreach (var p in region.Pops) budgetStartDiag += Math.Max(0, p.RemainingBudget);\n'
     '        if (s3acc is not null) { foreach (var p in region.Pops) s3acc[0] += Math.Max(0, p.RemainingBudget); }\n'),
    ('                    double pay = qty * m.Price * (1 + stateMonop + distM + monRent + guildRent) - subsidy;  // 専売＋流通マージン＋独占レント＋同業組合の上乗せ込み − 国家負担分＝popの実支払い\n',
     '                    double pay = qty * m.Price * (1 + stateMonop + distM + monRent + guildRent) - subsidy;  // 専売＋流通マージン＋独占レント＋同業組合の上乗せ込み − 国家負担分＝popの実支払い\n'
     '                    if (s3acc is not null) s3acc[foodTier ? 1 : 2] += pay;\n'),
    ('                        if (unfilled > 0) p.RemainingBudget -= unfilled * m.Price * (1 + stateMonop + distM + monRent + guildRent) - unfilled * insureBase * insure;\n',
     '                        if (unfilled > 0) p.RemainingBudget -= unfilled * m.Price * (1 + stateMonop + distM + monRent + guildRent) - unfilled * insureBase * insure;\n'
     '                        if (s3acc is not null && unfilled > 0) s3acc[3] += unfilled * m.Price * (1 + stateMonop + distM + monRent + guildRent) - unfilled * insureBase * insure;\n'),
])

TAIL = r'''

/// <summary>[計測の写しだけ] S3a 補給の鎖の計器（読むだけ）。0=予算の始め 1=食料の支払い 2=食料以外の支払い 3=在庫切れで取り置いた食料の予算（州・前の月）。</summary>
public static class S3aDiag
{
    public static readonly string? Dir = System.Environment.GetEnvironmentVariable("POB_S3A_DIR");
    public static readonly bool On = Dir is { Length: > 0 };
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Region, double[]> Acc = new();
    public static double[] For(Region r) => Acc.GetValue(r, _ => new double[4]);
    public static void ResetRegion(Region r) => System.Array.Clear(For(r));
    static string F(double v) => double.IsNaN(v) ? "nan" : v.ToString("G5", System.Globalization.CultureInfo.InvariantCulture);

    public sealed class Match
    {
        readonly System.IO.StreamWriter W;
        readonly System.Collections.Generic.Dictionary<World, int> Ep = new();
        readonly System.Collections.Generic.Dictionary<World, double> Prev = new();
        public Match(ulong seed)
        {
            W = new System.IO.StreamWriter(System.IO.Path.Combine(Dir!, $"s{seed}.txt"), false, new System.Text.UTF8Encoding(false), 1 << 16);
            W.Write("#N\tq\tyr\tidx\tbase\tname\tnreg\tpop\tfill\tfood\tdaily\tsolW\tready\tequip\tbm\ttreas\tpl\ter\tgold\tfci\twar\tcw\tpopSav\tmerSav\tffr\timp\texp\tresv\tpinStaplePop\tsDem\tsProd\tsStock\tsImp\tsShort\tsSold\tsPrel\tdDem\tdShort\tdSold\tdPrel\tstaplePF\tgrainPF\thh\n");
            W.Write("#W\tq\tyr\tnations\tlow03\tpinStaple\tpinDaily\tpinLux\tpinOther\tresv\n");
            W.Write("#R\tq\tidx\tname\tregion\tkind\tport\tpop\tfill\tfood\tdaily\tpl\tmerSav\tpopSav\tbudget\tpaidFood\tpaidOther\tresv\tadmin\tharvest\twarInt\tcap\tthrottle\tsubsist\tsoldiers\n");
            W.Write("#G\tq\tidx\tregion\tgood\ttier\tprel\tstock\ttarget\tprod\timp\tprevImp\texp\tdemand\tshort\tration\n");
        }
        public void Quarter(MultiNationSim sim, int q, int quarters)
        {
            var ns = sim.Nations; int yr = ns[0].World.Year;
            int live = 0, low = 0, pinS = 0, pinD = 0, pinL = 0, pinO = 0; double wBud = 0, wRes = 0;
            for (int i = 0; i < ns.Count; i++)
            {
                var w = ns[i].World;
                if (!(w.TotalPopulation > 0)) continue;
                live++;
                double fm = 0, fn = 0, nm = 0, nt = 0, sz = 0, sf = 0, popSav = 0, merSav = 0, bud = 0, res = 0, pinPop = 0; int nreg = 0;
                double sDem = 0, sProd = 0, sStock = 0, sImp = 0, sShort = 0, sSold = 0, sLn = 0, dDem = 0, dShort = 0, dSold = 0, dLn = 0, sCap = 0, sDes = 0, gCap = 0, gDes = 0;
                foreach (var r in w.Regions)
                {
                    if (r.State == RegionState.Active) nreg++;
                    var a = For(r); bud += a[0]; res += a[3];
                    double rpop = 0; foreach (var p in r.Pops) { rpop += p.Size; fm += p.FoodMet; fn += p.FoodNeed; nm += p.NeedMet; nt += p.NeedTotal; sz += p.Size; sf += p.SupplyFill * p.Size; popSav += p.Savings; }
                    if (r.MerchantPopIndex >= 0 && r.MerchantPopIndex < r.Pops.Count) merSav += r.Pops[r.MerchantPopIndex].Savings;
                    bool pinnedStaple = false;
                    foreach (var g in r.Goods)
                    {
                        if (g.Tier == GoodTier.Service || g.NonTradeable) continue;
                        var m = r.Markets[g.Id];
                        if (!(m.LastDemand > 0)) continue;
                        double prel = m.Price / System.Math.Max(1e-12, g.RefPrice * r.PriceLevel);
                        if (g.Tier == GoodTier.Staple) { sDem += m.LastDemand; sProd += m.LastProduced; sStock += m.Stock; sImp += m.LastImport + m.PrevImport; sShort += m.LastShortage; sSold += m.LastSold; sLn += m.LastDemand * System.Math.Log(System.Math.Max(1e-12, prel)); }
                        else if (g.Tier == GoodTier.Daily) { dDem += m.LastDemand; dShort += m.LastShortage; dSold += m.LastSold; dLn += m.LastDemand * System.Math.Log(System.Math.Max(1e-12, prel)); }
                        if (prel > 20 && !(m.Stock > 1e-9) && !(m.LastProduced > 1e-9) && !(m.LastImport > 1e-9))
                        {
                            if (g.Tier == GoodTier.Staple) { pinS++; pinnedStaple = true; } else if (g.Tier == GoodTier.Daily) pinD++; else if (g.Tier == GoodTier.Luxury) pinL++; else pinO++;
                        }
                    }
                    if (pinnedStaple) pinPop += rpop;
                    foreach (var pr in r.Producers)
                    {
                        if (!(pr.Capacity > 0)) continue;
                        if (pr.GoodId >= 0 && pr.GoodId < Catalog.Goods.Length && Catalog.Goods[pr.GoodId].Tier == GoodTier.Staple) { sCap += pr.Capacity; sDes += pr.DesiredOutput; }
                        else if (pr.GoodId == Catalog.WheatId || pr.GoodId == Catalog.FlourId) { gCap += pr.Capacity; gDes += pr.DesiredOutput; }
                    }
                }
                wBud += bud; wRes += res;
                double fill = sz > 0 ? sf / sz : 0;
                if (!(fill >= 0.3)) low++;
                double imp = 0, exp = 0; if (w.ImportsByGood is { } ib) foreach (var v in ib) imp += v; if (w.ExportsByGood is { } eb) foreach (var v in eb) exp += v;
                W.Write($"N\t{q + 1}\t{yr}\t{i}\t{w.SelfPower?.BaseName ?? w.Region.Name}\t{w.SelfPower?.Name ?? w.Region.Name}\t{nreg}\t{F(sz)}\t{F(fill)}\t{F(fn > 0 ? fm / fn : 1)}\t{F(nt > 0 ? nm / nt : 1)}\t{F(PowerShares.SoldierWelfare(w))}\t{F(w.MilitaryReadiness)}\t{F(w.Region.MilitaryEquipFill)}\t{F(w.BaseMoney)}\t{F(w.Treasury)}\t{F(w.PriceLevel)}\t{F(w.ExchangeRate)}\t{(w.OnGoldStandard ? 1 : 0)}\t{F(w.ForeignCapitalIn)}\t{(w.AtWarNow ? 1 : 0)}\t{(w.InCivilWar ? 1 : 0)}\t{F(popSav)}\t{F(merSav)}\t{F(w.FiscalFundingRatio)}\t{F(imp)}\t{F(exp)}\t{F(bud > 0 ? res / bud : 0)}\t{F(sz > 0 ? pinPop / sz : 0)}\t{F(sDem)}\t{F(sProd)}\t{F(sStock)}\t{F(sImp)}\t{F(sShort)}\t{F(sSold)}\t{F(sDem > 0 ? System.Math.Exp(sLn / sDem) : 0)}\t{F(dDem)}\t{F(dShort)}\t{F(dSold)}\t{F(dDem > 0 ? System.Math.Exp(dLn / dDem) : 0)}\t{F(sCap > 0 ? sDes / sCap : 1)}\t{F(gCap > 0 ? gDes / gCap : 1)}\t{F(w.Bank?.HouseholdLoans ?? 0)}\n");
                // 入口の詳細（充足<0.3 の最初と8四半期ごと・0.45 を割った四半期）
                Ep.TryGetValue(w, out int ep); bool hasPrev = Prev.TryGetValue(w, out double pf);
                bool detail = false;
                if (!(fill >= 0.3)) { ep++; if (ep == 1 || ep % 8 == 0) detail = true; }
                else { ep = 0; if (fill < 0.45 && (!hasPrev || pf >= 0.45)) detail = true; }
                Ep[w] = ep; Prev[w] = fill;
                if (detail) Detail(w, i, q);
            }
            W.Write($"W\t{q + 1}\t{yr}\t{live}\t{low}\t{pinS}\t{pinD}\t{pinL}\t{pinO}\t{F(wBud > 0 ? wRes / wBud : 0)}\n");
            if (q == quarters - 1) W.Dispose();
        }
        void Detail(World w, int i, int q)
        {
            foreach (var r in w.Regions)
            {
                double sz = 0, sf = 0, fm = 0, fn = 0, nm = 0, nt = 0, popSav = 0, sub = 0, sol = 0;
                foreach (var p in r.Pops) { sz += p.Size; sf += p.SupplyFill * p.Size; fm += p.FoodMet; fn += p.FoodNeed; nm += p.NeedMet; nt += p.NeedTotal; popSav += p.Savings; sub += p.Subsistence * p.Size; if (p.IsSoldier) sol += p.Size; }
                double merSav = r.MerchantPopIndex >= 0 && r.MerchantPopIndex < r.Pops.Count ? r.Pops[r.MerchantPopIndex].Savings : double.NaN;
                var a = For(r);
                W.Write($"R\t{q + 1}\t{i}\t{w.SelfPower?.Name ?? w.Region.Name}\t{r.Name}\t{(r.State == RegionState.Active ? (r.Tier == RegionTier.Colonial ? "col" : "act") : "lock")}\t{(r.IsPort ? 1 : 0)}\t{F(sz)}\t{F(sz > 0 ? sf / sz : 0)}\t{F(fn > 0 ? fm / fn : 1)}\t{F(nt > 0 ? nm / nt : 1)}\t{F(r.PriceLevel)}\t{F(merSav)}\t{F(popSav)}\t{F(a[0])}\t{F(a[1])}\t{F(a[2])}\t{F(a[3])}\t{F(r.AdminFill)}\t{F(r.HarvestFactor)}\t{F(r.LastWarIntensity)}\t{F(r.LastTransportCap)}\t{F(r.LastTradeThrottle)}\t{F(sz > 0 ? sub / sz : 0)}\t{F(sol)}\n");
                foreach (var g in r.Goods)
                {
                    if (!(g.Tier == GoodTier.Staple || g.Tier == GoodTier.Daily || g.Nutrition > 0)) continue;
                    var m = r.Markets[g.Id];
                    if (!(m.LastDemand > 0) && !(m.LastProduced > 0)) continue;
                    double prel = m.Price / System.Math.Max(1e-12, g.RefPrice * r.PriceLevel);
                    W.Write($"G\t{q + 1}\t{i}\t{r.Name}\t{g.Name}\t{(g.Tier == GoodTier.Staple ? "S" : g.Tier == GoodTier.Daily ? "D" : "F")}\t{F(prel)}\t{F(m.Stock)}\t{F(m.TargetStock)}\t{F(m.LastProduced)}\t{F(m.LastImport)}\t{F(m.PrevImport)}\t{F(m.LastExport)}\t{F(m.LastDemand)}\t{F(m.LastShortage)}\t{F(m.LastRation)}\n");
                }
            }
        }
    }
}
'''

edit('PriceOfBread.SimCore/SelfPlay.cs', [
    ('        sim.StartWarmup();\n        if (Instruments.On) Instruments.Phase(sim, seed, "warm");   // 助走の直後の断面\n        // ★異常リストの有界化（2026-08-01',
     '        sim.StartWarmup();\n        if (Instruments.On) Instruments.Phase(sim, seed, "warm");   // 助走の直後の断面\n        var s3aM = S3aDiag.On ? new S3aDiag.Match(seed) : null;\n        // ★異常リストの有界化（2026-08-01'),
    ('                sim.StepQuarter();\n                if (Instruments.On) Instruments.Quarter(sim, seed, q);   // 計器（読むだけ）\n                if (sim.Nations.Count > gdpPeak.Length)',
     '                sim.StepQuarter();\n                s3aM?.Quarter(sim, q, quarters);\n                if (Instruments.On) Instruments.Quarter(sim, seed, q);   // 計器（読むだけ）\n                if (sim.Nations.Count > gdpPeak.Length)'),
], TAIL)
print('ok', root)
