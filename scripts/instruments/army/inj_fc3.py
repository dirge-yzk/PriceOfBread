"""[計測の写しだけ] (155) の写し＝sol/inj_fc2.py の Fiscal の錨を (155) の木（LastMilitaryClaim の書き手は月初の予算）にも合わせた版。旧の説明＝(147) の写し＝fc/inj_fc.py の兵力の決済の錨を (147) の木（兵士の所得へ直接）にも合わせた版。財政の費用の側（(136) 線2 前半）の計器（読むだけ）。
Fiscal の請求（人頭割りの行政費・距離の割増なしの行政費・治安維持費・利払い・行政の請求・軍需の請求・切り下げ）と、
EconomyStep の国家の購買の支払い（行政・上下水道・電化・兵力）を World の診断の欄へ積み、SelfPlayMatch.Run の各四半期の後に
全ての国の1行（C|…）を書く。出力＝環境変数 POB_FC_DIR の下に c<seed>.txt。未設定なら行を作らない（欄は積むが読まない）。
C 列：C|四半期|年|添字|BaseName|生存|年名目GDP|人口|兵士|治安維持力|行政費|軍事費|支払い率|
      行政費の人頭割り(請求・距離込み)|同(距離なし)|治安維持費(請求)|利払い(請求)|行政の請求|軍需の請求|切り下げ|
      行政の購買|上下水道の購買|電化の購買|兵力の購買|台帳の四半期差分(全区分・カンマ区切り)|
      国債|国債の金利|平均クーポン|兌換|議会統制力|期待インフレ|政策金利|本位|金本位|行政の充足(人口加重)|行政の価格(人口加重)|
      大衆の1人あたり月収|官僚の人数|官僚の月収|兵士の月収(1人あたり)|行政の生産者の産出|植民地の請求|所得税率|政体|緊縮の強度|専売の段
力学は読まない（加算と記録だけ）。使い方：python inj_fc.py <写しの木>"""
import sys
root = sys.argv[1]
def edit(rel, pairs):
    p = root + '/' + rel
    raw = open(p, 'rb').read(); bom = raw.startswith(b'\xef\xbb\xbf'); s = raw.decode('utf-8-sig'); crlf = '\r\n' in s
    def nl(x): return x.replace('\n', '\r\n') if crlf else x
    for a, b, mode in pairs:
        a = nl(a); b = nl(b)
        assert s.count(a) == 1, (rel, a[:60], s.count(a))
        s = s.replace(a, (a + b) if mode == 'after' else (b + a))
    open(p, 'wb').write((b'\xef\xbb\xbf' if bom else b'') + s.encode('utf-8'))
edit('PriceOfBread.SimCore/World.cs', [
    ('    public double[] TreasuryLedger = new double[(int)TreasuryCat.Count];',
     '\n    public double YBAdm, YBAdm0, YBCoer, YBInt, YBAdmClaim, YBMilClaim, YDeval, YPAdm, YPInfra, YPElec, YPMp, YBCol;   // [計器の写しだけ]', 'after'),
])
HAS_UPKEEP = 'double upkeep = coercionUpkeep + adminCost;' in open(root + '/PriceOfBread.SimCore/Fiscal.cs', encoding='utf-8-sig').read()   # (136) の後の木は維持費の人頭割りが無い＝0 のまま
edit('PriceOfBread.SimCore/Fiscal.cs', ([
    ('        double upkeep = coercionUpkeep + adminCost;\n',
     '        w.YBAdm += adminCost; w.YBAdm0 += adminCost > 0 ? c.AdminCostPerPop * popSizeAll : 0; w.YBCoer += coercionUpkeep;\n', 'after')] if HAS_UPKEEP else []) + [
    ('        w.LastAdminClaim = adminClaim;' if 'w.LastMilitaryClaim = militaryClaim; w.LastAdminClaim' not in open(root + '/PriceOfBread.SimCore/Fiscal.cs', encoding='utf-8-sig').read() else '        w.LastMilitaryClaim = militaryClaim; w.LastAdminClaim = adminClaim;',
     '\n        w.YBInt += interest; w.YBAdmClaim += adminClaim; w.YBMilClaim += militaryClaim; w.YDeval += devalued; w.YBCol += w.LastColonialUpkeep;', 'after'),
])
_e = open(root + '/PriceOfBread.SimCore/EconomyStep.cs', encoding='utf-8-sig').read()
PAYANCHOR = '            revenue[Catalog.MilitaryPowerId] += pay;\n' if 'revenue[Catalog.MilitaryPowerId] += pay;' in _e else '            region.Pops[region.SoldierPopIndex].Earned += pay;\n'
edit('PriceOfBread.SimCore/EconomyStep.cs', [
    ('            revenue[Catalog.AdminId] += pay;\n', '            if (world is not null) world.YPAdm += pay;\n', 'before'),
    ('            revenue[Catalog.InfrastructureId] += pay;\n', '            if (world is not null) world.YPInfra += pay;\n', 'before'),
    ('            revenue[Catalog.ElecInfraId] += pay;\n', '            if (world is not null) world.YPElec += pay;\n', 'before'),
    (PAYANCHOR, '            if (world is not null) world.YPMp += pay;\n', 'before'),
])
edit('PriceOfBread.SimCore/SelfPlay.cs', [
    ('        sim.StartWarmup();\n', '        var ycDir = System.Environment.GetEnvironmentVariable("POB_FC_DIR"); var ycSb = ycDir is { Length: > 0 } ? new System.Text.StringBuilder() : null; var ycPrev = new System.Collections.Generic.List<double[]>();\n'
        '        foreach (var yn0 in sim.Nations) { var yw0 = yn0.World; yw0.YBAdm = yw0.YBAdm0 = yw0.YBCoer = yw0.YBInt = yw0.YBAdmClaim = yw0.YBMilClaim = yw0.YDeval = yw0.YPAdm = yw0.YPInfra = yw0.YPElec = yw0.YPMp = yw0.YBCol = 0; }\n', 'after'),
    ('                if (sim.Nations.Count > gdpPeak.Length)',
'''                if (ycSb is not null)
                {
                    for (int yi = 0; yi < sim.Nations.Count; yi++)
                    {
                        var yw = sim.Nations[yi].World;
                        while (ycPrev.Count <= yi) ycPrev.Add(new double[(int)TreasuryCat.Count]);
                        var yl = yw.TreasuryLedger; var yp = ycPrev[yi];
                        var yd = new System.Text.StringBuilder();
                        for (int k = 0; k < yl.Length; k++) { if (k > 0) yd.Append(','); yd.Append((yl[k] - yp[k]).ToString("F1")); yp[k] = yl[k]; }
                        double yaf = 0, yap = 0, yprice = 0, ymE = 0, ymS = 0, ybS = 0, ybE = 0, ysS = 0, ysE = 0, yout = 0;
                        foreach (var yr in yw.Regions)
                        {
                            if (yr.State != RegionState.Active) continue;
                            double yps = 0; foreach (var ypp in yr.Pops) yps += ypp.Size;
                            yaf += yr.AdminFill * yps; yap += yps;
                            if (Catalog.AdminId < yr.Markets.Length) yprice += yr.Markets[Catalog.AdminId].Price * yps;
                            foreach (var ypp in yr.Pops)
                            {
                                if (ypp.IsSoldier) { ysS += ypp.Size; ysE += ypp.Earned; continue; }
                                if (Politics.IsMassPop(ypp)) { ymS += ypp.Size; ymE += ypp.Earned; }
                                if (ypp.ClassId == PopClass.Bureaucrat) { ybS += ypp.Size; ybE += ypp.Earned; }
                            }
                            foreach (var ypr in yr.Producers) if (ypr.GoodId == Catalog.AdminId) yout += System.Math.Max(0, ypr.Output);
                        }
                        ycSb.Append($"C|{q + 1}|{yw.Year}|{yi}|{yw.SelfPower?.BaseName}|{(yw.TotalPopulation > 0 ? 1 : 0)}|{yw.NominalGdp * 12:F1}|{yap:F2}|{ysS:F2}|{yw.BaseCoercion:F2}|{yw.AdminSpending:F3}|{yw.MilitarySpending:F3}|{yw.FiscalFundingRatio:F3}|"
                            + $"{yw.YBAdm:F2}|{yw.YBAdm0:F2}|{yw.YBCoer:F2}|{yw.YBInt:F2}|{yw.YBAdmClaim:F2}|{yw.YBMilClaim:F2}|{yw.YDeval:F2}|{yw.YPAdm:F2}|{yw.YPInfra:F2}|{yw.YPElec:F2}|{yw.YPMp:F2}|{yd}|"
                            + $"{yw.GovDebt:F1}|{yw.GovBondRate:F4}|{yw.GovAvgCoupon:F4}|{yw.Convertibility:F3}|{yw.ParliamentControl:F3}|{yw.ExpectedInflation:F4}|{yw.InterestRate:F4}|{(int)yw.MonetaryLaw}|{(yw.OnGoldStandard ? 1 : 0)}|{(yap > 0 ? yaf / yap : 0):F3}|{(yap > 0 ? yprice / yap : 0):F4}|"
                            + $"{(ymS > 0 ? ymE / ymS : 0):F5}|{ybS:F3}|{ybE:F3}|{(ysS > 0 ? ysE / ysS : 0):F5}|{yout:F3}|{yw.YBCol:F2}|{yw.TaxRate:F4}|{(int)yw.Polity}|{yw.AusterityIntensity:F3}|{yw.StateMonopolyTier}").AppendLine();
                        yw.YBAdm = yw.YBAdm0 = yw.YBCoer = yw.YBInt = yw.YBAdmClaim = yw.YBMilClaim = yw.YDeval = yw.YPAdm = yw.YPInfra = yw.YPElec = yw.YPMp = yw.YBCol = 0;
                    }
                    if (q == quarters - 1) System.IO.File.WriteAllText(System.IO.Path.Combine(ycDir!, $"c{seed}.txt"), ycSb.ToString());
                }
''', 'before'),
])
print('ok fc', root)
