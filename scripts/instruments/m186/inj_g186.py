"""[計測の写しだけ] (186) 線B 後半＝兌換停止の診断の計器（読むだけ）。
環境変数 POB_G186_DIR の下に g<seed>.txt。未設定なら1命令も走らない。
  S 行＝兌換停止（Fiscal.Step の強制離脱＝GoldSuspensions を数える瞬間）：引き金（財政不足 r<0.7・カバー率割れ）と、そのときの財政と貨幣の盤面
    （r・カバー率・交戦・総動員・軍事費の段・軍の請求〔州の装備＋兵士の給与の予算〕・行政の請求・利払い・歳入・国庫・年の名目GDP・国債・
      この月の切り下げ・切り下げの記憶〔broken・累積÷月の名目GDP〕・基礎貨幣・広義の貨幣・与信・兌換・議会の統制）
  G 行＝国×四半期（その四半期の月の和）：月の数・金本位の月・金属本位の法の月・法があるのに金本位でない月・
    再加入の門が閉じた理由ごとの月（r＜GoldStandardMinFunding・カバー率＜GoldCoverMin×GoldRejoinCoverMargin・切り下げの記憶 broken）・
    r の和・カバー率の和・兌換の和・交戦の月・兌換停止の回数
力学は読まない（加算と記録だけ）。使い方：python inj_g186.py <木の根>（fullM の後でも前でも当たる）"""
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

/// <summary>[計測の写しだけ] (186) 兌換停止の引き金と、金本位へ戻れない理由（読むだけ）。</summary>
public static class G186
{
    public static readonly string? Dir = System.Environment.GetEnvironmentVariable("POB_G186_DIR");
    public static readonly bool On = Dir is { Length: > 0 };
    static string F(double v) => double.IsNaN(v) ? "nan" : v.ToString("G6", System.Globalization.CultureInfo.InvariantCulture);
    const int NG = 14;
    sealed class S { public double[] Q = new double[NG]; public System.Text.StringBuilder E = new(); }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, S> T = new();
    static S V(World w) => T.GetValue(w, _ => new S());
    // Fiscal.Step の金本位の門の直後（同じ月・同じ値を読む）
    public static void Month(World w, Coefficients c, bool was, double coverage, bool broken, double payRate, double devalued, double nominalGdp)
    {
        var s = V(w); var q = s.Q;
        bool law = w.MetallicStandardLaw;
        q[0] += 1; q[1] += w.OnGoldStandard ? 1 : 0; q[2] += law ? 1 : 0;
        if (law && !w.OnGoldStandard)
        {
            q[3] += 1;
            if (!(w.FiscalFundingRatio >= c.GoldStandardMinFunding)) q[4] += 1;
            if (!(coverage >= c.GoldCoverMin * c.GoldRejoinCoverMargin)) q[5] += 1;
            if (broken) q[6] += 1;
            if (w.FiscalFundingRatio >= c.GoldStandardMinFunding && coverage >= c.GoldCoverMin * c.GoldRejoinCoverMargin && !broken) q[12] += 1;   // 門が全て開いていた（次の月に戻る）
        }
        q[7] += w.FiscalFundingRatio; q[8] += coverage; q[9] += w.Convertibility; q[10] += w.AtWarNow ? 1 : 0;
        if (was && !w.OnGoldStandard && w.GoldStandardSuspendedInCrisis)
        {
            q[11] += 1;
            double mil = 0, adm = 0;
            foreach (var r in w.Regions) if (r.State == RegionState.Active) { mil += r.MilitaryBudget + r.SoldierPayBudget; adm += r.AdminBudget; }
            double ag = nominalGdp * 12;
            var sb = s.E;
            sb.Append("S|").Append(w.Year).Append('|').Append(w.SelfPower is { } sp ? (sp.BaseName.Length > 0 ? sp.BaseName : sp.Name) : "?");
            foreach (var v in new[] { w.FiscalFundingRatio, coverage, c.GoldCoverMin, w.FiscalFundingRatio < 0.7 ? 1 : 0, coverage < c.GoldCoverMin ? 1 : 0,
                w.AtWarNow ? 1 : 0, w.Mobilized ? 1 : 0, w.MilitarySpending, mil, adm, w.LastInterest, w.LastRevenue, w.Treasury, ag, w.GovDebt, devalued,
                broken ? 1 : 0, nominalGdp > 1e-9 ? w.CumulativeDevaluation / nominalGdp : double.NaN, w.BaseMoney, w.BroadMoney, w.OutstandingLoans, w.Convertibility, w.ParliamentControl, payRate })
                sb.Append('|').Append(F(v));
            sb.Append('\n');
        }
    }
    public static void Quarter(MultiNationSim sim, ulong seed, int q, int quarters)
    {
        var sb = Buf(sim);
        for (int i = 0; i < sim.Nations.Count; i++)
        {
            var w = sim.Nations[i].World;
            if (!T.TryGetValue(w, out var s)) continue;
            string nm = w.SelfPower is { } sp ? (sp.BaseName.Length > 0 ? sp.BaseName : sp.Name) : "?";
            sb.Append("G|").Append(q).Append('|').Append(w.Year).Append('|').Append(i).Append('|').Append(nm);
            for (int k = 0; k < NG; k++) sb.Append('|').Append(F(s.Q[k]));
            sb.Append('\n');
            System.Array.Clear(s.Q);
            if (s.E.Length > 0)
            {
                foreach (var line in s.E.ToString().Split('\n'))
                    if (line.Length > 0) sb.Append("S|").Append(q).Append('|').Append(i).Append('|').Append(line.Substring(2)).Append('\n');
                s.E.Clear();
            }
        }
        if (q != quarters - 1) return;
        using var W = new System.IO.StreamWriter(System.IO.Path.Combine(Dir!, $"g{seed}.txt"), false, new System.Text.UTF8Encoding(false), 1 << 16);
        W.Write("#G|q|yr|idx|name|months|goldM|lawM|offLawM|failR|failCov|failBroken|rSum|covSum|convSum|warM|susp|allOpenM|pad\n");
        W.Write("#S|q|idx|yr|name|r|coverage|coverMin|fundTrip|coverTrip|atWar|mob|milSpending|milClaims|adminClaims|interest|revenue|treasury|annNomGdp|debt|devalued|broken|cumDevalRatio|base|broad|loans|conv|parlCtl|payRate\n");
        W.Write(sb.ToString());
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, System.Text.StringBuilder> B = new();
    static System.Text.StringBuilder Buf(MultiNationSim s) => B.GetValue(s, _ => new System.Text.StringBuilder());
}
'''
edit('PriceOfBread.SimCore/Fiscal.cs', [
    ("            double coverage = w.BroadMoney > 1e-9 ? w.BaseMoney / w.BroadMoney : 1.0;\n            if (w.OnGoldStandard)\n",
     "            double coverage = w.BroadMoney > 1e-9 ? w.BaseMoney / w.BroadMoney : 1.0;\n            bool g186was = w.OnGoldStandard;\n            if (w.OnGoldStandard)\n"),
    ("            { w.OnGoldStandard = true; w.GoldStandardSuspendedInCrisis = false; }\n        }\n",
     "            { w.OnGoldStandard = true; w.GoldStandardSuspendedInCrisis = false; }\n            if (G186.On) G186.Month(w, c, g186was, coverage, broken, payRate, devalued, nominalGdp);\n        }\n"),
], TAIL)
sp = open(root + '/PriceOfBread.SimCore/SelfPlay.cs', encoding='utf-8-sig').read()
if 'if (F181.On) F181.Quarter(sim, seed, q, quarters);' in sp:
    edit('PriceOfBread.SimCore/SelfPlay.cs', [
        ("                if (F181.On) F181.Quarter(sim, seed, q, quarters);\n",
         "                if (F181.On) F181.Quarter(sim, seed, q, quarters);\n                if (G186.On) G186.Quarter(sim, seed, q, quarters);\n"),
    ])
else:
    edit('PriceOfBread.SimCore/SelfPlay.cs', [
        ("                sim.StepQuarter();\n                if (sim.Nations.Count > gdpPeak.Length)",
         "                sim.StepQuarter();\n                if (G186.On) G186.Quarter(sim, seed, q, quarters);\n                if (sim.Nations.Count > gdpPeak.Length)"),
    ])
print('ok g186', root)
