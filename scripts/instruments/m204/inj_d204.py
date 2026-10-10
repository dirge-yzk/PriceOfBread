"""[計測の写しだけ] (204) 線F 後半＝不履行の判定の鎖を月ごとに読む計器（読むだけ）。
環境変数 POB_D204_DIR の下に d<seed>.txt。未設定なら1命令も走らない。inj_fin181 の後に当てる（fin181 が書き換えた行に合わせる）。
  月の記録（月初に Simulation が r を決めた直後）＝r・請求・払える額（借入と切り下げの前）・借りた額・切り下げ・借りた後に残った払えない額（deficit）・
    切り下げの余地（room＝deficit×(1−兌換)〔銀は改鋳の窓〕）・月の上限（総貯蓄×DevaluationCapFrac）・頼んだ切り下げ（min(room,上限)）・兌換・通貨の法・
    利払い÷歳入（先月の量＝門の見方）・借入の門（月初）・国債÷年GDP・国債の金利・国庫・税率・交戦。
  X 行＝不履行の瞬間：張り付きの窓（Fiscal.Step の InsolvencyQuarters が数えた月）の月ごとの記録（r・deficit÷請求・room÷deficit・上限÷deficit・切り下げ÷頼んだ額・兌換）
    と窓の要約（月の数・r の平均と最小・縛った段の月の数＝兌換〔room＜deficit かつ room≦上限〕・上限〔room＞上限〕・移しの不足〔切り下げ＜頼んだ額〕・ほか）。
  E 行＝不履行にならずに閉じた窓（計数が0 へ戻った月）＝月の数・r の平均と最小・閉じた理由（r＝1／門が開いた／国債0／助走）・縛った段の月の数。
  Q 行＝国×四半期（その四半期の月の和・何かあった四半期だけ）＝r＜1 の月・張り付きの月・門が閉じた月・(1−r) の和・張り付きの月の (1−r) の和。
力学は読まない（加算と記録だけ）。使い方：python inj_d204.py <木の根>"""
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

/// <summary>[計測の写しだけ] (204) 不履行の判定の鎖を月ごとに読む（読むだけ）。</summary>
public static class D204
{
    public static readonly string? Dir = System.Environment.GetEnvironmentVariable("POB_D204_DIR");
    public static readonly bool On = Dir is { Length: > 0 };
    static string F(double v) => double.IsNaN(v) ? "nan" : v.ToString("G6", System.Globalization.CultureInfo.InvariantCulture);
    const int NM = 20;
    const int NQ = 6;
    sealed class S { public double[] M = new double[NM]; public System.Collections.Generic.List<double[]> L = new(); public double[] Q = new double[NQ]; public System.Text.StringBuilder D = new(); }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, S> G = new();
    static S V(World w) => G.GetValue(w, _ => new S());
    // 月初（r を決めた直後）
    public static void Month(World w, double claims, double payable, double borrowed, double devalued, double deficit, double room, double cap, double req, double payRate)
    {
        var s = V(w); var m = s.M;
        double ag = w.NominalGdpPerMonth * 12;
        m[0] = payRate; m[1] = claims; m[2] = payable - borrowed - devalued; m[3] = borrowed; m[4] = devalued; m[5] = deficit; m[6] = room; m[7] = cap; m[8] = req;
        m[9] = w.Convertibility; m[10] = (int)w.MonetaryLaw; m[11] = w.LastRevenuePerMonth > 0 ? w.LastInterestPerMonth / w.LastRevenuePerMonth : double.NaN;
        m[12] = Fiscal.BorrowGateOpen(w, w.Coef) ? 1 : 0; m[13] = ag > 0 ? w.GovDebt / ag : double.NaN; m[14] = w.GovBondRatePerYear; m[15] = w.Treasury;
        m[16] = w.TaxRate; m[17] = w.AtWarNow ? 1 : 0; m[18] = w.Year; m[19] = w.BorrowCooldownMonths;
        var q = s.Q;
        if (payRate < 1) { q[0] += 1; q[3] += 1 - payRate; }
        if (m[12] < 0.5) q[2] += 1;
    }
    static int Bind(double[] m)
    {
        double deficit = m[5], room = m[6], cap = m[7], req = m[8], dev = m[4];
        if (!(deficit > 1e-9)) return 4;            // 払えない額が無い（r＜1 の別の理由）
        if (dev < req - 1e-9 * System.Math.Max(1, req)) return 2;   // 移しの不足
        if (room > cap) return 1;                     // 月の上限
        if (room < deficit - 1e-9 * System.Math.Max(1, deficit)) return 0;   // 兌換（金・銀の窓）
        return 3;                                     // ほか
    }
    // 月末（Fiscal.Step の張り付きの計数の直後）
    public static void Insolv(World w, bool underLimit, double payRate)
    {
        var s = V(w);
        if (w.InsolvencyQuarters > 0)
        {
            s.L.Add((double[])s.M.Clone());
            s.Q[1] += 1; s.Q[4] += 1 - payRate; s.Q[5] = System.Math.Max(s.Q[5], w.InsolvencyQuarters);
        }
        else if (s.L.Count > 0)
        {
            string why = payRate >= 1 ? "r1" : underLimit ? "gate" : !(w.GovDebt > 1e-9) ? "debt0" : w.WarmupFrozen ? "warm" : "other";
            s.D.Append("E|").Append(w.Year).Append('|').Append(Nm(w)).Append('|').Append(why);
            Summ(s.D, s.L);
            s.D.Append('\n');
            s.L.Clear();
        }
    }
    static string Nm(World w) => w.SelfPower is { } sp ? (sp.BaseName.Length > 0 ? sp.BaseName : sp.Name) : "?";
    static void Summ(System.Text.StringBuilder sb, System.Collections.Generic.List<double[]> L)
    {
        int n = L.Count; double rs = 0, rmin = 1; var b = new int[5];
        double conv = 0, law = 0, intr = 0, debt = 0, rate = 0, war = 0, tax0 = L[0][16], tax1 = L[n - 1][16];
        foreach (var m in L) { rs += m[0]; rmin = System.Math.Min(rmin, m[0]); b[Bind(m)]++; conv += m[9]; law += m[10]; intr += m[11]; debt += m[13]; rate += m[14]; war += m[17]; }
        foreach (var v in new double[] { n, rs / n, rmin, b[0], b[1], b[2], b[3], b[4], conv / n, law / n, intr / n, debt / n, rate / n, war / n, tax0, tax1 })
            sb.Append('|').Append(F(v));
    }
    // 月末（不履行の直前＝国債を消す前）
    public static void Default(World w)
    {
        var s = V(w);
        if (s.L.Count == 0) s.L.Add((double[])s.M.Clone());
        s.D.Append("X|").Append(w.Year).Append('|').Append(Nm(w)).Append("|def");
        Summ(s.D, s.L);
        s.D.Append("|M");
        foreach (var m in s.L)
        {
            double cl = m[1] > 0 ? m[1] : 1, df = m[5] > 0 ? m[5] : double.NaN, rq = m[8] > 0 ? m[8] : double.NaN;
            s.D.Append(';').Append(F(m[0])).Append(',').Append(F(m[5] / cl)).Append(',').Append(F(m[6] / df)).Append(',').Append(F(m[7] / df)).Append(',').Append(F(m[4] / rq)).Append(',').Append(F(m[9])).Append(',').Append(Bind(m));
        }
        s.D.Append('\n');
        s.L.Clear();
    }
    public static void Quarter(MultiNationSim sim, ulong seed, int q, int quarters)
    {
        var sb = Buf(sim);
        for (int i = 0; i < sim.Nations.Count; i++)
        {
            var w = sim.Nations[i].World;
            if (!G.TryGetValue(w, out var s)) continue;
            if (s.Q[0] > 0 || s.Q[1] > 0)
            {
                sb.Append("Q|").Append(q).Append('|').Append(w.Year).Append('|').Append(i).Append('|').Append(Nm(w));
                for (int k = 0; k < NQ; k++) sb.Append('|').Append(F(s.Q[k]));
                sb.Append('\n');
            }
            System.Array.Clear(s.Q);
            if (s.D.Length > 0)
            {
                foreach (var line in s.D.ToString().Split('\n'))
                    if (line.Length > 0) sb.Append(line.Substring(0, 2)).Append(q).Append('|').Append(i).Append('|').Append(line.Substring(2)).Append('\n');
                s.D.Clear();
            }
        }
        if (q != quarters - 1) return;
        using var W = new System.IO.StreamWriter(System.IO.Path.Combine(Dir!, $"d{seed}.txt"), false, new System.Text.UTF8Encoding(false), 1 << 16);
        W.Write("#Q|q|yr|idx|name|rlt1M|insolvM|gateClosedM|sum1mr|insolvSum1mr|maxCount\n");
        W.Write("#X|q|idx|yr|name|kind|n|avgR|minR|bConv|bCap|bXfer|bOther|bNoDef|conv|law|intRev|debtGdp|bondRate|war|tax0|tax1|M;r,def/claims,room/def,cap/def,dev/req,conv,bind...\n");
        W.Write("#E|q|idx|yr|name|why|n|avgR|minR|bConv|bCap|bXfer|bOther|bNoDef|conv|law|intRev|debtGdp|bondRate|war|tax0|tax1\n");
        W.Write(sb.ToString());
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, System.Text.StringBuilder> B = new();
    static System.Text.StringBuilder Buf(MultiNationSim s) => B.GetValue(s, _ => new System.Text.StringBuilder());
}
'''
edit('PriceOfBread.SimCore/Simulation.cs', [
    ("        double f181b = 0, f181d = 0;\n",
     "        double f181b = 0, f181d = 0;\n        double d204def = 0, d204room = 0, d204cap = 0, d204req = 0;\n"),
    ("            double deficit = spare < 0 ? -spare : 0;",
     "            double deficit = spare < 0 ? -spare : 0; d204def = deficit;"),
    ("            if (devalueRoom > 1e-9)\n",
     "            d204room = devalueRoom;\n            if (devalueRoom > 1e-9)\n"),
    ("                double devalue = Math.Min(devalueRoom, totalSav * World.Coef.DevaluationCapFrac);\n",
     "                double devalue = Math.Min(devalueRoom, totalSav * World.Coef.DevaluationCapFrac); d204cap = totalSav * World.Coef.DevaluationCapFrac; d204req = devalue;\n"),
    ("        if (F181.On) F181.Post(World, f181b, f181d, payRate);\n",
     "        if (F181.On) F181.Post(World, f181b, f181d, payRate);\n        if (D204.On) D204.Month(World, claims, payable, f181b, f181d, d204def, d204room, d204cap, d204req, payRate);\n"),
], TAIL)
edit('PriceOfBread.SimCore/Fiscal.cs', [
    ("        if (F181.On) F181.Insolv(w);\n",
     "        if (F181.On) F181.Insolv(w);\n        if (D204.On) D204.Insolv(w, underLimit, payRate);\n"),
    ("            RepudiateDebt(w, c);   // 債務・保有・兌換・借入の停止",
     "            if (D204.On) D204.Default(w);\n            RepudiateDebt(w, c);   // 債務・保有・兌換・借入の停止"),
])
edit('PriceOfBread.SimCore/SelfPlay.cs', [
    ("                if (F181.On) F181.Quarter(sim, seed, q, quarters);\n",
     "                if (F181.On) F181.Quarter(sim, seed, q, quarters);\n                if (D204.On) D204.Quarter(sim, seed, q, quarters);\n"),
])
print('ok d204', root)
