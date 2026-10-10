"""[計測の写しだけ] (211) 潤沢線の部品（読むだけ）。環境変数 POB_F211_DIR の下に f<seed>.txt。未設定なら1命令も走らない。
inj_t203 の後に当てる（T203.Post・T203.Quarter の行の後ろに足す）。
  P 行＝主要10か国×四半期（AI の決定＝UtilityAgent.DecideQuarter の Apply の後＝T203 と同じ時点）：
    国庫・潤沢線（その木の OpulenceLineOf）・財政バッファ・戦費予備（旧の潤沢線の式）・月の名目GDP・1件の額・
    国内の貸し手のプール（投資家の貯蓄×BondAppetite）・外資のプール（Fiscal.Borrow と同じ式）・国債の金利・平均クーポン・
    直近の歳入・利払い・借入の冷却（月）・助走・借入の門・交戦・総動員・軍事費の施行・目標・戦時段の割合・軍事費の器・兵士の給与・国債・支払い率 r
力学は読まない（記録だけ）。使い方：python inj_f211.py <木の根>"""
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

/// <summary>[計測の写しだけ] (211) 潤沢線の部品（読むだけ）。</summary>
public static class F211
{
    public static readonly string? Dir = System.Environment.GetEnvironmentVariable("POB_F211_DIR");
    public static readonly bool On = Dir is { Length: > 0 };
    static readonly string[] Nat = { "イギリス", "フランス", "プロイセン", "ロシア", "オーストリア", "アメリカ", "日本", "清", "スペイン", "オスマン" };
    static string F(double v) => double.IsNaN(v) ? "nan" : v.ToString("G6", System.Globalization.CultureInfo.InvariantCulture);
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, System.Text.StringBuilder> WB = new();
    static System.Text.StringBuilder Buf(World w) => WB.GetValue(w, _ => new System.Text.StringBuilder());
    public static void Post(World w)
    {
        string nm = w.SelfPower is { } sp ? (sp.BaseName.Length > 0 ? sp.BaseName : sp.Name) : "?";
        if (System.Array.IndexOf(Nat, nm) < 0) return;
        var c = w.Coef;
        double fb = UtilityAgent.FiscalBufferOf(w);
        double chest = (fb + c.FiscalBufferSoldierPayWeight * c.FiscalStressBufferMonths * w.LastSoldierPay) * UtilityAgent.OpulenceBufferQuarters;
        double line = UtilityAgent.OpulenceLineOf(w);
        double gdp = w.NominalGdp;
        double ticket = Fiscal.BorrowTicket(gdp, c);
        double capSav = CapitalMarket.InvestorSavings(w);
        double dom = capSav * c.BondAppetite;
        double openness = c.CapitalMobilityLawGate > 0
            ? (w.CapitalControlLaw ? PoliticalQuantities.Clamp01(c.CapitalControlOpenness) : 1.0)
            : PoliticalQuantities.Clamp01(1.0 - c.CapitalOpennessTariffWeight * w.Tariff);
        double forPool = openness * c.ForeignBondCapacity * System.Math.Max(0, gdp) * System.Math.Clamp(c.BondBaseRate / System.Math.Max(1e-9, w.GovBondRate), 0, 3.0);
        forPool = System.Math.Min(forPool, System.Math.Max(0.0, w.ForeignBondInvestorPool));
        double target = Government.MilitaryTotalTarget(w, w.Policy);
        double war = Military.WarStageShare(w, w.MilitarySpending);
        double env = Military.EnvelopeOf(w, c);
        var b = Buf(w);
        b.Append(nm).Append('|').Append(w.Year);
        foreach (var v in new[] { w.Treasury, line, fb, chest, gdp, ticket, dom, forPool, w.GovBondRate, w.GovAvgCoupon, w.LastRevenue, w.LastInterest,
                                  (double)w.BorrowCooldown, w.WarmupFrozen ? 1 : 0, Fiscal.BorrowGateOpen(w, c) ? 1 : 0, w.AtWarNow ? 1 : 0, w.Mobilized ? 1 : 0,
                                  w.MilitarySpending, target, war, env, w.LastSoldierPay, w.GovDebt, w.FiscalFundingRatio, w.LastAdminClaim, w.LastMilitaryClaim, w.SpendableRevenue() })
            b.Append('|').Append(F(v));
        b.Append('\n');
    }
    public static void Quarter(MultiNationSim sim, ulong seed, int q, int quarters)
    {
        var sb = SB(sim);
        for (int i = 0; i < sim.Nations.Count; i++)
        {
            var w = sim.Nations[i].World;
            if (!WB.TryGetValue(w, out var b) || b.Length == 0) continue;
            foreach (var line in b.ToString().Split('\n'))
                if (line.Length > 0) sb.Append("P|").Append(q).Append('|').Append(i).Append('|').Append(line).Append('\n');
            b.Clear();
        }
        if (q != quarters - 1) return;
        using var W = new System.IO.StreamWriter(System.IO.Path.Combine(Dir!, $"f{seed}.txt"), false, new System.Text.UTF8Encoding(false), 1 << 16);
        W.Write("#P|q|idx|name|yr|treasury|opLine|fiscalBuffer|chest|gdpMonth|ticket|domPool|forPool|bondRate|avgCoupon|lastRevenue|lastInterest|cooldown|warmup|gate|atWar|mobilized|milSpend|milTarget|warShare|milEnvelope|soldierPay|govDebt|payRate|adminClaim|milClaim|spendableRevenue\n");
        W.Write(sb.ToString());
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, System.Text.StringBuilder> B = new();
    static System.Text.StringBuilder SB(MultiNationSim s) => B.GetValue(s, _ => new System.Text.StringBuilder());
}
'''
edit('PriceOfBread.SimCore/Fiscal.cs', [], TAIL)
edit('PriceOfBread.SimCore/SelfPlay.cs', [
    ("                if (T203.On) T203.Quarter(sim, seed, q, quarters);\n",
     "                if (T203.On) T203.Quarter(sim, seed, q, quarters);\n                if (F211.On) F211.Quarter(sim, seed, q, quarters);\n"),
    ("        if (T203.On) T203.Post(w);\n",
     "        if (T203.On) T203.Post(w);\n        if (F211.On) F211.Post(w);\n"),
])
print('ok f211', root)
