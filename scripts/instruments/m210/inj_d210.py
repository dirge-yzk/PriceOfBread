"""[計測の写しだけ] (210) 線F 前半＝(205) の exact −4.6 の帰属と不履行の頻度の単位を読む計器（読むだけ）。
環境変数 POB_D210_DIR の下に d<seed>.txt。未設定なら1命令も走らない。r202（62cc878）と r207（4f73d13）の両方の木に当たる（どちらにもある行だけを錨にする）。
  M（月・Fiscal.Step の不履行の計数の直前）＝r（支払い率）・借入の門（underLimit）・国債>0・助走 → 国×四半期の和
  Q 行＝国×四半期（r＜1 の月がある四半期だけ）＝月の数・r＜1 の月・Σ(1−r)・計数に入る月（門が閉じ国債>0 で助走でない）・その Σ(1−r)・r の最小・門が閉じた月・国債÷年GDP（最後の月）
  D 行＝不履行の瞬間（国債を消す前と、正統性の打撃の直後）＝正統性・導出政体・議会の実効統制・文民制度・国債÷年GDP・交戦・金本位
  N 行＝国×四半期（全ての国・毎四半期）＝ラベル政体・導出政体・宗主・1914 表の exact/near（表に無い国は −1）・正統性・議会の実効統制・文民制度・内戦・交戦・州の数・植民地・国力（序列）・国債÷年GDP・不履行の回数・金本位・人口
力学は読まない（加算と記録だけ・読む関数は純粋＝BuildStandings と同じ関数）。使い方：python inj_d210.py <木の根>"""
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

/// <summary>[計測の写しだけ] (210) 不履行の帰属と頻度の単位（読むだけ）。</summary>
public static class D210
{
    public static readonly string? Dir = System.Environment.GetEnvironmentVariable("POB_D210_DIR");
    public static readonly bool On = Dir is { Length: > 0 };
    static string F(double v) => double.IsNaN(v) ? "nan" : v.ToString("G6", System.Globalization.CultureInfo.InvariantCulture);
    const int NQ = 8;
    sealed class S
    {
        public double[] Q = NewQ();
        public double DebtGdp = double.NaN;
        public double PreLeg, PreParl, PreCiv, PreDebt; public int PrePol;
        public System.Text.StringBuilder D = new();
    }
    static double[] NewQ() { var q = new double[NQ]; q[5] = 1; return q; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, S> G = new();
    static S V(World w) => G.GetValue(w, _ => new S());
    static string Nm(World w) => w.SelfPower is { } sp ? (sp.BaseName.Length > 0 ? sp.BaseName : sp.Name) : "?";
    // 月（Fiscal.Step の不履行の計数の直前）
    public static void Month(World w, double payRate, bool underLimit, double nominalGdp)
    {
        var s = V(w); var q = s.Q;
        q[0] += 1;
        if (payRate < 1) { q[1] += 1; q[2] += 1 - payRate; if (payRate < q[5]) q[5] = payRate; }
        if (payRate < 1 && w.GovDebt > 1e-9 && !underLimit && !w.WarmupFrozen) { q[3] += 1; q[4] += 1 - payRate; }
        if (!underLimit) q[6] += 1;
        double ag = nominalGdp * 12;
        s.DebtGdp = ag > 0 ? w.GovDebt / ag : double.NaN;
    }
    // 不履行の直前（国債を消す前）
    public static void Pre(World w, double nominalGdp)
    {
        var s = V(w);
        s.PreLeg = w.Legitimacy; s.PreParl = PowerShares.ParliamentEffective(w); s.PreCiv = PowerShares.CivilianInstitutions(w);
        s.PrePol = (int)PowerAffinity.PolityFromShares(w);
        double ag = nominalGdp * 12; s.PreDebt = ag > 0 ? w.GovDebt / ag : double.NaN;
    }
    // 正統性の打撃の直後
    public static void Post(World w)
    {
        var s = V(w);
        s.D.Append("D|").Append(w.Year).Append('|').Append(Nm(w))
            .Append('|').Append(F(s.PreLeg)).Append('|').Append(F(w.Legitimacy))
            .Append('|').Append(s.PrePol).Append('|').Append((int)PowerAffinity.PolityFromShares(w))
            .Append('|').Append(F(s.PreParl)).Append('|').Append(F(PowerShares.ParliamentEffective(w)))
            .Append('|').Append(F(s.PreCiv)).Append('|').Append(F(PowerShares.CivilianInstitutions(w)))
            .Append('|').Append(F(s.PreDebt)).Append('|').Append(w.AtWarNow ? 1 : 0).Append('|').Append((int)w.Polity).Append('|').Append(w.SovereignDefaultCount)
            .Append('\n');
    }
    public static void Quarter(MultiNationSim sim, ulong seed, int q, int quarters)
    {
        var sb = Buf(sim);
        for (int i = 0; i < sim.Nations.Count; i++)
        {
            var w = sim.Nations[i].World;
            var s = V(w);
            string bn = Nm(w);
            if (s.Q[1] > 0)
            {
                sb.Append("Q|").Append(q).Append('|').Append(i).Append('|').Append(bn);
                for (int k = 0; k < NQ - 1; k++) sb.Append('|').Append(F(s.Q[k]));
                sb.Append('|').Append(F(s.DebtGdp)).Append('\n');
            }
            s.Q = NewQ();
            if (s.D.Length > 0)
            {
                foreach (var line in s.D.ToString().Split('\n'))
                    if (line.Length > 0) sb.Append("D|").Append(q).Append('|').Append(i).Append('|').Append(line.Substring(2)).Append('\n');
                s.D.Clear();
            }
            // N 行
            int ex = -1, nr = -1;
            var derived = PowerAffinity.PolityFromShares(w);
            string suz = (w.SelfPower is { } sp2 ? sim.Diplomacy.Suzerain(sp2)?.BaseName : null) ?? "";
            foreach (var e in HistoricalReference.Table)
            {
                if (e.Nation != bn) continue;
                var (x, n) = HistoricalReference.PolityMatch(derived, suz, e.Exact, e.Near, e.SubjugatedOk, e.SuzerainsOk);
                ex = x ? 1 : 0; nr = n ? 1 : 0; break;
            }
            sb.Append("N|").Append(q).Append('|').Append(i).Append('|').Append(bn)
              .Append('|').Append((int)w.Polity).Append('|').Append((int)derived).Append('|').Append(suz)
              .Append('|').Append(ex).Append('|').Append(nr)
              .Append('|').Append(F(w.Legitimacy)).Append('|').Append(F(PowerShares.ParliamentEffective(w))).Append('|').Append(F(PowerShares.CivilianInstitutions(w)))
              .Append('|').Append(w.InCivilWar ? 1 : 0).Append('|').Append(w.AtWarNow ? 1 : 0)
              .Append('|').Append(w.Regions.Count).Append('|').Append(w.SelfPower?.Colonies ?? 0)
              .Append('|').Append(F(NationalScore.Standing(w))).Append('|').Append(F(s.DebtGdp)).Append('|').Append(w.SovereignDefaultCount)
              .Append('|').Append(w.OnGoldStandard ? 1 : 0).Append('|').Append(F(w.TotalPopulation))
              .Append('\n');
        }
        if (q != quarters - 1) return;
        using var W = new System.IO.StreamWriter(System.IO.Path.Combine(Dir!, $"d{seed}.txt"), false, new System.Text.UTF8Encoding(false), 1 << 16);
        W.Write("#Q|q|idx|base|months|rlt1M|sum1mr|cntM|cntSum1mr|minR|gateClosedM|debtGdp\n");
        W.Write("#D|q|idx|yr|base|preLeg|postLeg|prePol|postPol|preParl|postParl|preCiv|postCiv|debtGdp|war|label|defCount\n");
        W.Write("#N|q|idx|base|label|derived|suz|ex1914|nr1914|leg|parlEff|civInst|civilWar|war|regions|colonies|score|debtGdp|defCount|gold|pop\n");
        W.Write(sb.ToString());
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, System.Text.StringBuilder> B = new();
    static System.Text.StringBuilder Buf(MultiNationSim s) => B.GetValue(s, _ => new System.Text.StringBuilder());
}
'''
edit('PriceOfBread.SimCore/Fiscal.cs', [
    ("        if (w.BorrowCooldown > 0) w.BorrowCooldown--;\n",
     "        if (D210.On) D210.Month(w, payRate, underLimit, nominalGdp);\n        if (w.BorrowCooldown > 0) w.BorrowCooldown--;\n"),
    ("            RepudiateDebt(w, c);   // 債務・保有・兌換・借入の停止",
     "            if (D210.On) D210.Pre(w, nominalGdp);\n            RepudiateDebt(w, c);   // 債務・保有・兌換・借入の停止"),
    ("            w.Explain?.Add(\"Legitimacy\", \"国家破産（デフォルト）の信認崩壊\", w.Legitimacy - legBeforeDefault);   // 説明API（#63）\n",
     "            w.Explain?.Add(\"Legitimacy\", \"国家破産（デフォルト）の信認崩壊\", w.Legitimacy - legBeforeDefault);   // 説明API（#63）\n            if (D210.On) D210.Post(w);\n"),
], TAIL)
edit('PriceOfBread.SimCore/SelfPlay.cs', [
    ("                int lead = 0;\n",
     "                if (D210.On) D210.Quarter(sim, seed, q, quarters);\n                int lead = 0;\n"),
])
print('ok d210', root)
