# (207) m181/inj_fin181 の写し＝(204) の不履行の計数（払えなかった大きさの累計）の行に合わせた（計器の中身は同じ）
"""[計測の写しだけ] (181) 線M 後半＝月の決算の中身・戦時段の請求の払われ方・不履行の盤面・潤沢線の計器（読むだけ）。
環境変数 POB_F181_DIR の下に f<seed>.txt。未設定なら1命令も走らない。inj_war181 の後に当てる（その Quarter の行の後ろに足す）。
  F 行＝国×四半期（その四半期の月の和）：
    月の数・請求の和 claims・払える額 payable0（max(0,国庫)＋歳入）・国庫・歳入・借りた額・切り下げ・支払い率 r の和・
    請求の内訳（利払い・植民地の維持費・福祉と年金・軍〔州の装備の予算＋兵士の給与の予算〕・行政）・
    施行中の器 env・戦時段の上乗せ warAdd（戦時段の割合×1件の額＝借入の門が開いた月）・
    戦時段の請求 W＝max(0, 軍の請求−(器−上乗せ))＝限界の見方（他の行を先に置いた時の残り）とその払われ方
      （歳入の余り・国庫の蓄え・借入・切り下げ・払えない〔r<1〕）・重なりの月（W>0 かつ他の行だけで払える額を超えた月）・W>0 の月・
    潤沢線（OpulenceLineOf）・使える歳入（SpendableRevenue）・張り付きの月（Fiscal.Step の InsolvencyQuarters が数えた月）・不履行の回数・
    利払いの限界の月（利払い≧限界×歳入）
  D 行＝不履行（Fiscal.Step のソブリン・デフォルト）の瞬間：国債・年の名目GDP・歳入・利払い・国債の金利・平均クーポン・国庫・施行・交戦・総動員・兌換・
    張り付きの窓（数えた月）の平均＝請求・払える額・借りた額・切り下げ・r・利払い・植民地・福祉・軍・行政・国庫・歳入
力学は読まない（加算と記録だけ・読む関数は純粋）。使い方：python inj_fin181.py <木の根>"""
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

/// <summary>[計測の写しだけ] (181) 月の決算・戦時段の請求の払われ方・不履行の盤面・潤沢線（読むだけ）。</summary>
public static class F181
{
    public static readonly string? Dir = System.Environment.GetEnvironmentVariable("POB_F181_DIR");
    public static readonly bool On = Dir is { Length: > 0 };
    static string F(double v) => double.IsNaN(v) ? "nan" : v.ToString("G6", System.Globalization.CultureInfo.InvariantCulture);
    const int NF = 36;   // 四半期の和（28〜35＝(181) v2：兌換・平均クーポン・国債の金利・金本位の月・国債〔最後の月〕・期待インフレ・議会の統制・交戦の月）
    const int NM = 16;   // 今月の値（月初に書き Fiscal.Step が読む）
    const int NI = 14;   // 張り付きの窓の和
    sealed class S { public double[] Q = new double[NF]; public double[] M = new double[NM]; public double[] I = new double[NI]; public System.Text.StringBuilder D = new(); }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, S> G = new();
    static S V(World w) => G.GetValue(w, _ => new S());
    // 月初（Simulation の請求の和の直後・借入の前）
    public static void Pre(World w, double claims, double payable0)
    {
        var s = V(w); var q = s.Q; var m = s.M;
        double T = w.Treasury, R = w.LastRevenue;
        double mil = 0, adm = 0;
        foreach (var r in w.Regions) if (r.State == RegionState.Active) { mil += r.MilitaryBudget + r.SoldierPayBudget; adm += r.AdminBudget; }
        double env = Military.EnvelopeOf(w, w.Coef);
        double share = Military.WarStageShare(w, w.MilitarySpending);
        double warAdd = share > 0 && Fiscal.BorrowGateOpen(w, w.Coef) ? share * Fiscal.BorrowTicket(w.NominalGdp, w.Coef) : 0;
        double W = warAdd > 0 ? System.Math.Max(0, mil - (env - warAdd)) : 0;
        q[0] += 1; q[1] += claims; q[2] += payable0; q[3] += T; q[4] += R;
        q[8] += w.LastInterest; q[9] += w.LastColonialUpkeep; q[10] += w.LastWelfareCost + w.LastPensionCost; q[11] += mil; q[12] += adm;
        q[13] += env; q[14] += warAdd;
        q[23] += UtilityAgent.OpulenceLineOf(w); q[24] += w.SpendableRevenue();
        if (!(w.LastInterest < w.Coef.MaxInterestShareOfRevenue * w.LastRevenue)) q[27] += 1;
        q[28] += w.Convertibility; q[29] += w.GovAvgCoupon; q[30] += w.GovBondRate; q[31] += w.OnGoldStandard ? 1 : 0; q[32] = w.GovDebt; q[33] += w.ExpectedInflation; q[34] += w.ParliamentControl; q[35] += w.AtWarNow ? 1 : 0;
        m[0] = claims; m[1] = payable0; m[2] = T; m[3] = R; m[4] = w.LastInterest; m[5] = w.LastColonialUpkeep; m[6] = w.LastWelfareCost + w.LastPensionCost;
        m[7] = mil; m[8] = adm; m[9] = W; m[10] = 0; m[11] = 0; m[12] = 0;
    }
    // 月初（借入と切り下げの後・r を決めた直後）
    public static void Post(World w, double borrowed, double devalued, double payRate)
    {
        var s = V(w); var q = s.Q; var m = s.M;
        q[5] += borrowed; q[6] += devalued; q[7] += payRate;
        m[10] = borrowed; m[11] = devalued; m[12] = payRate;
        double W = m[9];
        if (W > 0)
        {
            double claims = m[0], T = m[2], R = m[3], rest = claims - W;
            double own = System.Math.Max(0, T) + R + System.Math.Min(0, T);   // 借入の前に払える額（借越を差し引いた）
            double avail0 = System.Math.Max(0, own - rest);
            double a0 = System.Math.Min(W, avail0);
            double fromRev = System.Math.Min(a0, System.Math.Max(0, R - System.Math.Max(0, rest)));
            double restDef = System.Math.Max(0, rest - own);
            double w1 = W - a0;
            double bW = System.Math.Min(w1, System.Math.Max(0, borrowed - restDef));
            double w2 = w1 - bW;
            double restRem = System.Math.Max(0, restDef - borrowed);
            double dW = System.Math.Min(w2, System.Math.Max(0, devalued - restRem));
            q[15] += W; q[16] += fromRev; q[17] += a0 - fromRev; q[18] += bW; q[19] += dW; q[20] += w2 - dW;
            if (restDef > 0) q[21] += 1;
            q[22] += 1;
        }
    }
    // 月末（Fiscal.Step の張り付きの計数の直後）
    public static void Insolv(World w)
    {
        var s = V(w); var m = s.M; var I = s.I;
        if (w.InsolvencyQuarters > 0)
        {
            s.Q[25] += 1;
            I[0] += 1; I[1] += m[0]; I[2] += m[1]; I[3] += m[10]; I[4] += m[11]; I[5] += m[12]; I[6] += m[4]; I[7] += m[5]; I[8] += m[6]; I[9] += m[7]; I[10] += m[8]; I[11] += m[2]; I[12] += m[3];
        }
        else System.Array.Clear(I);
    }
    // 月末（不履行の直前＝国債を消す前）
    public static void Default(World w, double nominalGdpMonthly, int q, int yr)
    {
        var s = V(w); var I = s.I;
        s.Q[26] += 1;
        double n = I[0] > 0 ? I[0] : 1;
        var sb = s.D;
        sb.Append("D|").Append(w.Year).Append('|').Append(w.SelfPower is { } sp ? (sp.BaseName.Length > 0 ? sp.BaseName : sp.Name) : "?");
        foreach (var v in new[] { w.GovDebt, nominalGdpMonthly * 12, w.LastRevenue, w.LastInterest, w.GovBondRate, w.GovAvgCoupon, w.Treasury, w.MilitarySpending, w.AtWarNow ? 1 : 0, w.Mobilized ? 1 : 0, w.Convertibility, I[0] })
            sb.Append('|').Append(F(v));
        for (int k = 1; k < NI; k++) sb.Append('|').Append(F(I[k] / n));
        var c = w.Coef; double ag = nominalGdpMonthly * 12, dg = ag > 0 ? w.GovDebt / ag : double.NaN;
        foreach (var v in new[] { w.ExpectedInflation, w.ParliamentControl, c.BondBaseRate, c.BondRateDebtScale * dg, System.Math.Max(c.FisherDeflationFloor, w.ExpectedInflation) * c.FisherInflationWeight, c.PolicyRateBondWeight * w.InterestRate, 1.0 - w.Convertibility * (1.0 - c.GoldStandardRateDiscount), 1.0 - c.BondRateRepresentationDiscount * PoliticalQuantities.Clamp01(w.ParliamentControl), w.OnGoldStandard ? 1 : 0, w.SovereignDefaultCount })
            sb.Append('|').Append(F(v));
        sb.Append('\n');
    }
    public static void Quarter(MultiNationSim sim, ulong seed, int q, int quarters)
    {
        var sb = Buf(sim);
        for (int i = 0; i < sim.Nations.Count; i++)
        {
            var w = sim.Nations[i].World;
            if (!G.TryGetValue(w, out var s)) continue;
            string nm = w.SelfPower is { } sp ? (sp.BaseName.Length > 0 ? sp.BaseName : sp.Name) : "?";
            sb.Append("F|").Append(q).Append('|').Append(w.Year).Append('|').Append(i).Append('|').Append(nm);
            for (int k = 0; k < NF; k++) sb.Append('|').Append(F(s.Q[k]));
            sb.Append('\n');
            System.Array.Clear(s.Q);
            if (s.D.Length > 0)
            {
                foreach (var line in s.D.ToString().Split('\n'))
                    if (line.Length > 0) sb.Append(line.Substring(0, 2)).Append(q).Append('|').Append(i).Append('|').Append(line.Substring(2)).Append('\n');
                s.D.Clear();
            }
        }
        if (q != quarters - 1) return;
        using var W = new System.IO.StreamWriter(System.IO.Path.Combine(Dir!, $"f{seed}.txt"), false, new System.Text.UTF8Encoding(false), 1 << 16);
        W.Write("#F|q|yr|idx|name|months|claims|payable0|treasury|revenue|borrowed|devalued|rSum|interest|colonial|welfarePension|mil|admin|env|warAdd|W|W_rev|W_treas|W_borrow|W_deval|W_unpaid|overlapM|warM|opLine|spendable|insolvM|defaults|capM|conv|coupon|bondRate|goldM|debtLast|expInfl|parlCtl|warM2\n");
        W.Write("#D|q|idx|yr|name|debt|annNomGdp|revenue|interest|bondRate|coupon|treasury|milSpend|atWar|mob|conv|insolvMonths|avgClaims|avgPayable0|avgBorrowed|avgDevalued|avgR|avgInterest|avgColonial|avgWelfare|avgMil|avgAdmin|avgTreasury|avgRevenue|pad|expInfl|parlCtl|base|debtTerm|fisher|policy|convDisc|reprDisc|onGold|defCount\n");
        W.Write(sb.ToString());
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, System.Text.StringBuilder> B = new();
    static System.Text.StringBuilder Buf(MultiNationSim s) => B.GetValue(s, _ => new System.Text.StringBuilder());
}
'''
edit('PriceOfBread.SimCore/Simulation.cs', [
    ("        double spare = payable - claims + Math.Min(0, World.Treasury);\n",
     "        if (F181.On) F181.Pre(World, claims, payable);\n        double spare = payable - claims + Math.Min(0, World.Treasury);\n"),
    ("        World.FiscalFundingRatio = payRate;\n",
     "        World.FiscalFundingRatio = payRate;\n        if (F181.On) F181.Post(World, f181b, f181d, payRate);\n"),
    ("            double borrowed = Fiscal.Borrow(World, World.Coef, spare);\n",
     "            double borrowed = Fiscal.Borrow(World, World.Coef, spare); f181b = borrowed;\n"),
    ("        double devalued = 0;   // この月の切り下げの額",
     "        double f181b = 0, f181d = 0;\n        double devalued = 0;   // この月の切り下げの額"),
    ("            World.NoDevaluationMonths = devalued > 0 ? 0 : World.NoDevaluationMonths + 1;\n",
     "            World.NoDevaluationMonths = devalued > 0 ? 0 : World.NoDevaluationMonths + 1; f181d = devalued;\n"),
], TAIL)
edit('PriceOfBread.SimCore/Fiscal.cs', [
    ("        if (payRate < 1 && w.GovDebt > 1e-9 && !underLimit && !w.WarmupFrozen) w.InsolvencyQuarters += 1.0 - payRate; else w.InsolvencyQuarters = 0;\n",
     "        if (payRate < 1 && w.GovDebt > 1e-9 && !underLimit && !w.WarmupFrozen) w.InsolvencyQuarters += 1.0 - payRate; else w.InsolvencyQuarters = 0;\n        if (F181.On) F181.Insolv(w);\n"),
    ("            RepudiateDebt(w, c);   // 債務・保有・兌換・借入の停止",
     "            if (F181.On) F181.Default(w, nominalGdp, 0, 0);\n            RepudiateDebt(w, c);   // 債務・保有・兌換・借入の停止"),
])
edit('PriceOfBread.SimCore/SelfPlay.cs', [
    ("                if (W176.On) W176.Quarter(sim, seed, q, quarters);\n",
     "                if (W176.On) W176.Quarter(sim, seed, q, quarters);\n                if (F181.On) F181.Quarter(sim, seed, q, quarters);\n"),
])
print('ok fin181', root)
