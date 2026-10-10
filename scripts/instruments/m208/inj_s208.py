"""[計測の写しだけ] (208) 線S＝金本位の波の入力の計器（読むだけ）。
環境変数 POB_S208_DIR の下に s<seed>.txt。未設定なら1命令も走らない。
  W 行＝世界×四半期：同じ州の銀÷金の比（全て・金の国の州・銀の国の州・不換の国の州）・銀と金の値段÷参照（幾何平均・群ごと）・
        銀と金の産出／需要／在庫の和（州の市場の直近の月）・この四半期の造幣の買い（量）・準備の売り（量）・準備の和・表示用の統計÷参照・法の国の数
  N 行＝国×四半期：法・目標（四半期の終わり）・AI の願い（工業化の閾／金の法）・要求で置いた目標・強制の施行・
        純支持（金へ一歩）と3ブロックの賛否・銀の下落の入力（統計）・交易網の引力・期待インフレ・工業化・自国の州の銀÷金の比・
        取引の相手の本位の割合（越境の成約の額＝金の相手・銀の相手・不換の相手）・造幣の買い（銀・金）・準備
力学は読まない（加算と記録だけ）。fx205 の後に当てる（同じ成約の行に足す）。使い方：python inj_s208.py <木の根>"""
import sys
root = sys.argv[1]


def edit(rel, pairs, tail=''):
    p = root + '/' + rel
    raw = open(p, 'rb').read(); bom = raw.startswith(bytes([0xef, 0xbb, 0xbf])); s = raw.decode('utf-8-sig')
    CR, LF = chr(13), chr(10)
    crlf = (CR + LF) in s
    cr0 = raw.count(b'\r')
    for old, new in pairs:
        if crlf: old = old.replace(LF, CR + LF); new = new.replace(LF, CR + LF)
        assert s.count(old) == 1, (rel, old[:90], s.count(old))
        s = s.replace(old, new)
    s += tail.replace(LF, CR + LF) if crlf else tail
    out = (b'\xef\xbb\xbf' if bom else b'') + s.encode('utf-8')
    if not crlf: assert out.count(b'\r') == cr0, (rel, 'CR changed')
    open(p, 'wb').write(out)


TAIL = r'''

/// <summary>[計測の写しだけ] (208) 金本位の波の入力の計器（読むだけ）。</summary>
public static class S208
{
    public static readonly string? Dir = System.Environment.GetEnvironmentVariable("POB_S208_DIR");
    public static readonly bool On = Dir is { Length: > 0 };
    // 国ごとの四半期の足し算：0 金の相手 1 銀の相手 2 不換の相手 3 造幣の銀 4 造幣の金 5 売りの銀 6 売りの金 7 願い(0/1/2=閾/3=金の法) 8 要求の目標(+1) 9 強制(+1)
    const int K = 10;
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, double[]> A = new();
    static double[] Acc(World w) => A.GetValue(w, _ => new double[K]);
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, int[]> Prev = new();
    static string F(double v) => double.IsNaN(v) ? "nan" : v.ToString("G6", System.Globalization.CultureInfo.InvariantCulture);
    static double qMintAg, qMintAu, qSellAg, qSellAu;
    public static void Deal(World buyer, World seller, double v)
    {
        if (!(v > 0)) return;
        var b = Acc(buyer); var s = Acc(seller);
        b[(int)LawIdx(seller)] += v; s[(int)LawIdx(buyer)] += v;
    }
    static int LawIdx(World w) => w.MonetaryLaw == MonetaryStandardLaw.Gold ? 0 : w.MonetaryLaw == MonetaryStandardLaw.Silver ? 1 : 2;
    public static void Mint(World w, int metal, double qty)
    {
        var a = Acc(w);
        if (metal == Catalog.SilverId) { a[3] += qty; qMintAg += qty; } else { a[4] += qty; qMintAu += qty; }
    }
    public static void Sell(World w, int metal, double qty)
    {
        var a = Acc(w);
        if (metal == Catalog.SilverId) { a[5] += qty; qSellAg += qty; } else { a[6] += qty; qSellAu += qty; }
    }
    public static void Wish(World w, bool wish, bool wasGold, bool ind) { Acc(w)[7] = wasGold ? 3 : ind ? 2 : wish ? 1 : 0; }
    public static void Demand(World w, int target) { Acc(w)[8] = target + 1; }
    public static void Forced(World w, int v) { Acc(w)[9] = v + 1; }
    static double OwnRatio(World w, out int n)
    {
        double ln = 0; n = 0;
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active) continue;
            if (Catalog.SilverId >= r.Markets.Length || Catalog.GoldOreId >= r.Markets.Length) continue;
            var mAg = r.Markets[Catalog.SilverId]; var mAu = r.Markets[Catalog.GoldOreId];
            if (!(mAg.LastDemand > 1e-9 || mAg.LastProduced > 1e-9) || !(mAu.LastDemand > 1e-9 || mAu.LastProduced > 1e-9)) continue;
            if (!(mAg.Price > 1e-9) || !(mAu.Price > 1e-9)) continue;
            ln += System.Math.Log(mAg.Price / mAu.Price); n++;
        }
        double refRatio = Catalog.RefPrices[Catalog.SilverId] / Catalog.RefPrices[Catalog.GoldOreId];
        return n > 0 ? System.Math.Exp(ln / n) / refRatio : double.NaN;
    }
    public static void Quarter(MultiNationSim sim, ulong seed, int q, int quarters)
    {
        var sb = Buf(sim);
        int yr = 0;
        double refAg = Catalog.RefPrices[Catalog.SilverId], refAu = Catalog.RefPrices[Catalog.GoldOreId], refRatio = refAg / refAu;
        // 群ごと（0 金・1 銀・2 不換・3 全て）
        var lnR = new double[4]; var nR = new int[4]; var lnAg = new double[4]; var lnAu = new double[4]; var nP = new int[4];
        double prodAg = 0, prodAu = 0, demAg = 0, demAu = 0, stAg = 0, stAu = 0, resAg = 0, resAu = 0;
        int nGold = 0, nSilver = 0, nFiat = 0;
        for (int i = 0; i < sim.Nations.Count; i++)
        {
            var w = sim.Nations[i].World; var p = w.SelfPower; if (p is null) continue;
            yr = w.Year;
            int g = LawIdx(w);
            if (w.TotalPopulation > 0) { if (g == 0) nGold++; else if (g == 1) nSilver++; else nFiat++; }
            resAg += w.SilverReserveQty; resAu += w.GoldReserveQty;
            foreach (var r in w.Regions)
            {
                if (r.State != RegionState.Active) continue;
                if (Catalog.SilverId >= r.Markets.Length || Catalog.GoldOreId >= r.Markets.Length) continue;
                var mAg = r.Markets[Catalog.SilverId]; var mAu = r.Markets[Catalog.GoldOreId];
                prodAg += mAg.LastProduced; prodAu += mAu.LastProduced; demAg += mAg.LastDemand; demAu += mAu.LastDemand; stAg += mAg.Stock; stAu += mAu.Stock;
                if (!(mAg.LastDemand > 1e-9 || mAg.LastProduced > 1e-9) || !(mAu.LastDemand > 1e-9 || mAu.LastProduced > 1e-9)) continue;
                if (!(mAg.Price > 1e-9) || !(mAu.Price > 1e-9)) continue;
                double l = System.Math.Log(mAg.Price / mAu.Price);
                lnR[g] += l; nR[g]++; lnR[3] += l; nR[3]++;
                double la = System.Math.Log(mAg.Price / refAg), lu = System.Math.Log(mAu.Price / refAu);
                lnAg[g] += la; lnAu[g] += lu; nP[g]++; lnAg[3] += la; lnAu[3] += lu; nP[3]++;
            }
            var a = Acc(w);
            var (tr, mo, ma, net) = PolicyPreferences.Stance(w, PolicyAxis.MonetaryStandard, true);
            double silverDrop = 0;
            if (w.MonetaryLaw == MonetaryStandardLaw.Silver && w.WorldSilverPrice > 1e-9) silverDrop = System.Math.Clamp(1.0 - w.WorldSilverPrice / refAg, 0, 1);
            double own = OwnRatio(w, out int nOwn);
            double tt = a[0] + a[1] + a[2];
            sb.Append("N|").Append(q).Append('|').Append(w.Year).Append('|').Append(i).Append('|').Append(p.BaseName.Length > 0 ? p.BaseName : p.Name)
              .Append('|').Append(g == 0 ? 2 : g == 1 ? 1 : 0).Append('|').Append(w.Policy.MonetaryTarget).Append('|').Append((int)a[7]).Append('|').Append((int)a[8]).Append('|').Append((int)a[9])
              .Append('|').Append(F(net)).Append('|').Append(F(tr)).Append('|').Append(F(mo)).Append('|').Append(F(ma))
              .Append('|').Append(F(silverDrop)).Append('|').Append(F(w.TradeGoldPull)).Append('|').Append(F(w.ExpectedInflation)).Append('|').Append(F(w.MeanIndustrialization)).Append('|').Append(F(w.IndustrialFrontier))
              .Append('|').Append(F(own)).Append('|').Append(nOwn)
              .Append('|').Append(F(tt)).Append('|').Append(F(tt > 0 ? a[0] / tt : double.NaN)).Append('|').Append(F(tt > 0 ? a[1] / tt : double.NaN))
              .Append('|').Append(F(a[3])).Append('|').Append(F(a[4])).Append('|').Append(F(a[5])).Append('|').Append(F(a[6]))
              .Append('|').Append(F(w.SilverReserveQty)).Append('|').Append(F(w.GoldReserveQty)).Append('|').Append(F(w.ExchangeRate)).Append('|').Append(F(w.Convertibility))
              .Append('|').Append(F(w.TotalPopulation)).Append('|').Append(F(w.NominalGdp)).Append('|').Append(F(w.PriceLevel)).Append('\n');
            System.Array.Clear(a);
        }
        double stat = Catalog.SilverId < sim.Market.Price.Count ? sim.Market.Price[Catalog.SilverId] : 0;
        sb.Append("W|").Append(q).Append('|').Append(yr);
        for (int g = 0; g < 4; g++)
            sb.Append('|').Append(F(nR[g] > 0 ? System.Math.Exp(lnR[g] / nR[g]) / refRatio : double.NaN)).Append('|').Append(nR[g])
              .Append('|').Append(F(nP[g] > 0 ? System.Math.Exp(lnAg[g] / nP[g]) : double.NaN)).Append('|').Append(F(nP[g] > 0 ? System.Math.Exp(lnAu[g] / nP[g]) : double.NaN));
        sb.Append('|').Append(F(prodAg)).Append('|').Append(F(prodAu)).Append('|').Append(F(demAg)).Append('|').Append(F(demAu)).Append('|').Append(F(stAg)).Append('|').Append(F(stAu))
          .Append('|').Append(F(qMintAg)).Append('|').Append(F(qMintAu)).Append('|').Append(F(qSellAg)).Append('|').Append(F(qSellAu)).Append('|').Append(F(resAg)).Append('|').Append(F(resAu))
          .Append('|').Append(F(refAg > 1e-9 ? stat / refAg : 0)).Append('|').Append(nGold).Append('|').Append(nSilver).Append('|').Append(nFiat).Append('\n');
        qMintAg = qMintAu = qSellAg = qSellAu = 0;
        if (q != quarters - 1) return;
        using var W = new System.IO.StreamWriter(System.IO.Path.Combine(Dir!, $"s{seed}.txt"), false, new System.Text.UTF8Encoding(false), 1 << 16);
        W.Write("#N|q|yr|idx|base|law(0不換1銀2金)|target|wish(0無1他2閾3金)|demand(+1)|forced(+1)|net|trad|modern|mass|silverDrop|tradeGoldPull|expInfl|meanInd|indFrontier|ownR|nOwn|trade|shGold|shSilver|mintAg|mintAu|sellAg|sellAu|resAg|resAu|E|conv|pop|nomGdp|PL\n");
        W.Write("#W|q|yr|R金|n|Ag金|Au金|R銀|n|Ag銀|Au銀|R不換|n|Ag不換|Au不換|R全|n|Ag全|Au全|prodAg|prodAu|demAg|demAu|stAg|stAu|mintAg|mintAu|sellAg|sellAu|resAg|resAu|statRel|nGold|nSilver|nFiat\n");
        W.Write(sb.ToString());
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, System.Text.StringBuilder> B = new();
    static System.Text.StringBuilder Buf(MultiNationSim s) => B.GetValue(s, _ => new System.Text.StringBuilder());
}
'''
_A = "                                : w.Coef.SilverStandardGate > 0 && w.MeanIndustrialization >= w.Coef.SilverStandardIndThreshold ? 1 : 0;\n"
_B = "        w.Policy.MonetaryTarget = goldWish ? 2 : w.Coef.SilverStandardGate > 0 ? 1 : 0;\n"
if _A in open(root + '/PriceOfBread.SimCore/SelfPlay.cs', encoding='utf-8-sig').read():   # r207 の木（工業化の閾）
    WISH = (_A, _A + "        if (S208.On) S208.Wish(w, goldWish, w.MonetaryLaw == MonetaryStandardLaw.Gold, w.MeanIndustrialization > w.Coef.GoldStandardIndThreshold);\n")
else:   # (208) の木＝閾が残っていれば閾の印を立てる
    _ind = "w.MeanIndustrialization > w.Coef.GoldStandardIndThreshold" if 'w.Coef.GoldStandardIndThreshold' in open(root + '/PriceOfBread.SimCore/SelfPlay.cs', encoding='utf-8-sig').read() else "false"
    WISH = (_B, _B + "        if (S208.On) S208.Wish(w, goldWish, w.MonetaryLaw == MonetaryStandardLaw.Gold, " + _ind + ");\n")
edit('PriceOfBread.SimCore/BilateralMarket.cs', [
    ("                    if (crossA) { importVal[ni] += vA + frG; impByGood[ni][g] += vA + frG; exportVal[siA] += vA; expByGood[siA][g] += vA; }\n",
     "                    if (crossA) { importVal[ni] += vA + frG; impByGood[ni][g] += vA + frG; exportVal[siA] += vA; expByGood[siA][g] += vA; }\n"
     "                    if (S208.On && crossA) S208.Deal(nations[ni].World, nations[siA].World, vA);\n"),
], TAIL)
edit('PriceOfBread.SimCore/SelfPlay.cs', [
    ("                sim.StepQuarter();\n",
     "                sim.StepQuarter();\n                if (S208.On) S208.Quarter(sim, seed, q, quarters);\n"),
    WISH,
])
edit('PriceOfBread.SimCore/Parliament.cs', [
    ("            case PolicyAxis.MonetaryStandard: p.MonetaryTarget = w.Coef.SilverStandardGate > 0 ? StepTo((int)w.MonetaryLaw, 2) : (FlipTo(w.MonetaryLaw == MonetaryStandardLaw.Gold) ? 2 : 0); break;\n",
     "            case PolicyAxis.MonetaryStandard: p.MonetaryTarget = w.Coef.SilverStandardGate > 0 ? StepTo((int)w.MonetaryLaw, 2) : (FlipTo(w.MonetaryLaw == MonetaryStandardLaw.Gold) ? 2 : 0); if (S208.On) S208.Demand(w, p.MonetaryTarget); break;\n"),
])
edit('PriceOfBread.SimCore/Politics.cs', [
    ("StepEnumAxis((int)w.MonetaryLaw, 2, v => { w.MonetaryLaw = (MonetaryStandardLaw)v; p.MonetaryTarget = v; })",
     "StepEnumAxis((int)w.MonetaryLaw, 2, v => { if (S208.On) S208.Forced(w, v); w.MonetaryLaw = (MonetaryStandardLaw)v; p.MonetaryTarget = v; })"),
])
edit('PriceOfBread.SimCore/Simulation.cs', [
    ("            m.Stock -= qty; m.LastSold += qty;        // 造幣局への引き取り",
     "            if (S208.On) S208.Mint(World, metal, qty);\n            m.Stock -= qty; m.LastSold += qty;        // 造幣局への引き取り"),
    ("        m.Stock += sold; m.LastImport += sold;\n",
     "        if (S208.On) S208.Sell(World, gid, sold);\n        m.Stock += sold; m.LastImport += sold;\n"),
])
print('ok s208', root)
