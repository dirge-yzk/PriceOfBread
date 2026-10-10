"""[計測の写しだけ] (205) 線T＝英の交易の計器（読むだけ）。
環境変数 POB_FX205_DIR の下に fx<seed>.txt。未設定なら1命令も走らない。
  N 行＝国×四半期（四半期の終わりの断面）：通貨の法・為替・兌換・物価・銀の世界価格÷参照（平価の元）・輸出入（清算1回分）・月の名目GDP・
        国境の台帳（CumulativeTradeFlow）・対外の資産と負債・国庫・関税・船腹・四半期に足した越境の成約（輸出 FOB・輸入 CIF・輸入 FOB・
        輸入の数量×参照価格・輸入の数量×買い手の金建ての州の価格・輸出の数量×参照価格・払った運賃・受け取った運賃・清算の回数）
  W 行＝世界×四半期：銀の表示用の統計（Market.Price[銀]）÷参照・州の銀の価格×為替の単純平均÷参照（統計のクランプ前の近似）・
        同じ州の中の銀÷金鉱の比の対数平均（器A が使う量）÷参照の比・数えた州の数・州の銀の価格（自国通貨）の平均÷参照
力学は読まない（加算と記録だけ）。使い方：python inj_fx205.py <木の根>"""
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

/// <summary>[計測の写しだけ] (205) 英の交易の計器（読むだけ）。</summary>
public static class FX205
{
    public static readonly string? Dir = System.Environment.GetEnvironmentVariable("POB_FX205_DIR");
    public static readonly bool On = Dir is { Length: > 0 };
    const int K = 10;   // 0 輸出FOB 1 輸入CIF 2 輸入FOB 3 輸入q×Ref 4 輸入q×lp 5 輸出q×Ref 6 払った運賃 7 受け取った運賃 8 清算の回数 9 予備
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, double[]> A = new();
    static double[] Acc(World w) => A.GetValue(w, _ => new double[K]);
    static string F(double v) => double.IsNaN(v) ? "nan" : v.ToString("G6", System.Globalization.CultureInfo.InvariantCulture);
    public static void Deal(World buyer, World seller, double vFob, double freight, double refQ, double lpQ)
    {
        var b = Acc(buyer); var s = Acc(seller);
        b[1] += vFob + freight; b[2] += vFob; b[3] += refQ; b[4] += lpQ; b[6] += freight;
        s[0] += vFob; s[5] += refQ;
    }
    public static void Freight(World w, double goldAmt) { Acc(w)[7] += goldAmt; }
    public static void Clearing(World w) { Acc(w)[8] += 1; }
    public static void Quarter(MultiNationSim sim, ulong seed, int q, int quarters)
    {
        var sb = Buf(sim);
        int yr = 0;
        double refAg = Catalog.RefPrices[Catalog.SilverId], refAu = Catalog.RefPrices[Catalog.GoldOreId];
        for (int i = 0; i < sim.Nations.Count; i++)
        {
            var w = sim.Nations[i].World; var p = w.SelfPower; if (p is null) continue;
            yr = w.Year;
            double ex = 0, im = 0;
            if (w.ExportsByGood is { } eg) foreach (var v in eg) ex += v;
            if (w.ImportsByGood is { } ig) foreach (var v in ig) im += v;
            int law = w.MonetaryLaw == MonetaryStandardLaw.Fiat ? 0 : w.MonetaryLaw == MonetaryStandardLaw.Silver ? 1 : 2;
            var a = Acc(w);
            sb.Append("N|").Append(q).Append('|').Append(w.Year).Append('|').Append(i).Append('|').Append(p.BaseName.Length > 0 ? p.BaseName : p.Name)
              .Append('|').Append(law).Append('|').Append(F(w.ExchangeRate)).Append('|').Append(F(w.Convertibility)).Append('|').Append(w.OnGoldStandard ? 1 : 0)
              .Append('|').Append(F(w.PriceLevel)).Append('|').Append(F(refAg > 1e-9 ? w.WorldSilverPrice / refAg : 0))
              .Append('|').Append(F(ex)).Append('|').Append(F(im)).Append('|').Append(F(w.NominalGdp)).Append('|').Append(F(w.Coef.ClearingsPerYear))
              .Append('|').Append(F(w.CumulativeTradeFlow)).Append('|').Append(F(w.CapitalAbroad)).Append('|').Append(F(w.ForeignCapitalIn))
              .Append('|').Append(F(w.ForeignBondClaims)).Append('|').Append(F(w.ForeignDebt)).Append('|').Append(F(w.GovDebt)).Append('|').Append(F(w.Treasury))
              .Append('|').Append(F(w.LastTariffRevenue)).Append('|').Append(F(w.ShipCapacity)).Append('|').Append(F(w.TotalPopulation));
            for (int k = 0; k < 9; k++) sb.Append('|').Append(F(a[k]));
            sb.Append('\n');
            System.Array.Clear(a);
        }
        // 世界の行（銀の平価の元）
        double stat = Catalog.SilverId < sim.Market.Price.Count ? sim.Market.Price[Catalog.SilverId] : 0;
        double sumLpE = 0; int nLpE = 0; double sumLoc = 0; int nLoc = 0; double lnR = 0; int nR = 0;
        foreach (var n in sim.Nations)
            foreach (var r in n.World.Regions)
            {
                if (r.State != RegionState.Active) continue;
                if (Catalog.SilverId >= r.Markets.Length || Catalog.GoldOreId >= r.Markets.Length) continue;
                var mAg = r.Markets[Catalog.SilverId]; var mAu = r.Markets[Catalog.GoldOreId];
                if (mAg.Price > 1e-9) { sumLpE += mAg.Price * n.World.ExchangeRate; nLpE++; sumLoc += mAg.Price; nLoc++; }
                if (!(mAg.LastDemand > 1e-9 || mAg.LastProduced > 1e-9) || !(mAu.LastDemand > 1e-9 || mAu.LastProduced > 1e-9)) continue;
                if (!(mAg.Price > 1e-9) || !(mAu.Price > 1e-9)) continue;
                lnR += System.Math.Log(mAg.Price / mAu.Price); nR++;
            }
        double refRatio = refAu > 1e-9 ? refAg / refAu : 0;
        sb.Append("W|").Append(q).Append('|').Append(yr).Append('|').Append(F(refAg > 1e-9 ? stat / refAg : 0))
          .Append('|').Append(F(nLpE > 0 && refAg > 1e-9 ? sumLpE / nLpE / refAg : 0))
          .Append('|').Append(F(nR > 0 && refRatio > 1e-9 ? System.Math.Exp(lnR / nR) / refRatio : 0)).Append('|').Append(nR)
          .Append('|').Append(F(nLoc > 0 && refAg > 1e-9 ? sumLoc / nLoc / refAg : 0)).Append('\n');
        if (q != quarters - 1) return;
        using var W = new System.IO.StreamWriter(System.IO.Path.Combine(Dir!, $"fx{seed}.txt"), false, new System.Text.UTF8Encoding(false), 1 << 16);
        W.Write("#N|q|yr|idx|base|law(0不換1銀2金)|E|conv|onGold|PL|silverRelRaw|ex|im|nomGdpMonth|cpy|CTF|capAbroad|capIn|bondClaims|forDebt|govDebt|treasury|tariffRev|shipCap|pop|exFOB|imCIF|imFOB|imRefQ|imLpQ|exRefQ|frPaid|frIn|nClear\n");
        W.Write("#W|q|yr|statRel|lpERel|ratioRel|nRatio|locRel\n");
        W.Write(sb.ToString());
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, System.Text.StringBuilder> B = new();
    static System.Text.StringBuilder Buf(MultiNationSim s) => B.GetValue(s, _ => new System.Text.StringBuilder());
}
'''
edit('PriceOfBread.SimCore/BilateralMarket.cs', [
    ("                    if (crossA) { importVal[ni] += vA + frG; impByGood[ni][g] += vA + frG; exportVal[siA] += vA; expByGood[siA][g] += vA; }\n",
     "                    if (crossA) { importVal[ni] += vA + frG; impByGood[ni][g] += vA + frG; exportVal[siA] += vA; expByGood[siA][g] += vA; }\n"
     "                    if (FX205.On && crossA) FX205.Deal(nations[ni].World, nations[siA].World, vA, frG, qA * gd.RefPrice, qA * lp);\n"),
    ("            double amt = freightPool * shipShare[i] / exRate[i];\n",
     "            double amt = freightPool * shipShare[i] / exRate[i];\n            if (FX205.On) FX205.Freight(nations[i].World, freightPool * shipShare[i]);\n"),
    ("        for (int i = 0; i < nn; i++) nations[i].World.LastTariffRevenue = 0;\n",
     "        if (FX205.On) for (int i = 0; i < nn; i++) FX205.Clearing(nations[i].World);\n        for (int i = 0; i < nn; i++) nations[i].World.LastTariffRevenue = 0;\n"),
], TAIL)
edit('PriceOfBread.SimCore/SelfPlay.cs', [
    ("                sim.StepQuarter();\n",
     "                sim.StepQuarter();\n                if (FX205.On) FX205.Quarter(sim, seed, q, quarters);\n"),
])
print('ok fx205', root)
