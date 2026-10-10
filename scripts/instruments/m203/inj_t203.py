"""[計測の写しだけ] (203) 潤沢の減税と税の段（読むだけ）。環境変数 POB_T203_DIR の下に t<seed>.txt。未設定なら1命令も走らない。
inj_g186 の後に当てる（G186.Quarter の行の後ろに足す）。
  T 行＝国×四半期（AI の決定＝UtilityAgent.DecideQuarter の Apply の後）：
    国庫・潤沢線（OpulenceLineOf）・減税の倍率（RelieveOpulence が掛けた mul・掛けなかった四半期は 1）・
    減税の前の目標（所得・人頭・土地・累進・関税）・減税の後の目標（門の前）・門の後の目標・今の率（施行）・直近の歳入・直近の請求（利払い＋行政＋軍）・兵士の給与
力学は読まない（記録だけ）。使い方：python inj_t203.py <木の根>"""
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

/// <summary>[計測の写しだけ] (203) 潤沢の減税と税の段（読むだけ）。</summary>
public static class T203
{
    public static readonly string? Dir = System.Environment.GetEnvironmentVariable("POB_T203_DIR");
    public static readonly bool On = Dir is { Length: > 0 };
    static readonly string[] Nat = { "イギリス", "フランス", "プロイセン", "ロシア", "オーストリア", "アメリカ", "日本", "清", "スペイン", "オスマン" };
    static string F(double v) => double.IsNaN(v) ? "nan" : v.ToString("G6", System.Globalization.CultureInfo.InvariantCulture);
    sealed class S { public double Line = double.NaN, Mul = 1; public double[] Pre = new double[5], Aft = new double[5]; public bool Rel; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, S> G = new();
    static S V(World w) => G.GetValue(w, _ => new S());
    static void Tg(World w, double[] a) { var p = w.Policy; a[0] = p.IncomeTaxTarget; a[1] = p.PollTaxTarget; a[2] = p.LandTaxTarget; a[3] = p.ProgressiveTaxTarget; a[4] = p.TariffTarget; }
    public static void Pre(World w, double line) { var s = V(w); s.Line = line; s.Mul = 1; s.Rel = false; Tg(w, s.Pre); Tg(w, s.Aft); }
    public static void Mul(World w, double mul) { var s = V(w); s.Mul = mul; s.Rel = true; }
    public static void Aft(World w) { var s = V(w); Tg(w, s.Aft); }
    public static void Post(World w)
    {
        if (!G.TryGetValue(w, out var s)) return;
        string nm = w.SelfPower is { } sp ? (sp.BaseName.Length > 0 ? sp.BaseName : sp.Name) : "?";
        if (System.Array.IndexOf(Nat, nm) < 0) return;
        var b = Buf(w);
        b.Append(nm).Append('|').Append(w.Year).Append('|').Append(F(w.Treasury)).Append('|').Append(F(s.Line)).Append('|').Append(s.Rel ? 1 : 0).Append('|').Append(F(s.Mul));
        for (int k = 0; k < 5; k++) b.Append('|').Append(F(s.Pre[k]));
        for (int k = 0; k < 5; k++) b.Append('|').Append(F(s.Aft[k]));
        var p = w.Policy;
        foreach (var v in new[] { p.IncomeTaxTarget, p.PollTaxTarget, p.LandTaxTarget, p.ProgressiveTaxTarget, p.TariffTarget, w.TaxRate, w.PollTax, w.LandTax, w.ProgressiveTax, w.Tariff,
                                  w.LastRevenue, w.LastInterest + w.LastAdminClaim + w.LastMilitaryClaim, w.LastSoldierPay, w.GovDebt, w.AtWarNow ? 1 : 0 })
            b.Append('|').Append(F(v));
        b.Append('\n');
        s.Line = double.NaN;
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, System.Text.StringBuilder> WB = new();
    static System.Text.StringBuilder Buf(World w) => WB.GetValue(w, _ => new System.Text.StringBuilder());
    public static void Quarter(MultiNationSim sim, ulong seed, int q, int quarters)
    {
        var sb = SB(sim);
        for (int i = 0; i < sim.Nations.Count; i++)
        {
            var w = sim.Nations[i].World;
            if (!WB.TryGetValue(w, out var b) || b.Length == 0) continue;
            foreach (var line in b.ToString().Split('\n'))
                if (line.Length > 0) sb.Append("T|").Append(q).Append('|').Append(i).Append('|').Append(line).Append('\n');
            b.Clear();
        }
        if (q != quarters - 1) return;
        using var W = new System.IO.StreamWriter(System.IO.Path.Combine(Dir!, $"t{seed}.txt"), false, new System.Text.UTF8Encoding(false), 1 << 16);
        W.Write("#T|q|idx|name|yr|treasury|opLine|relieved|mul|pre5(inc,poll,land,prog,tariff)|aft5|gate5|cur5(taxRate,poll,land,prog,tariff)|lastRevenue|lastClaims(int+adm+mil)|soldierPay|govDebt|atWar\n");
        W.Write(sb.ToString());
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, System.Text.StringBuilder> B = new();
    static System.Text.StringBuilder SB(MultiNationSim s) => B.GetValue(s, _ => new System.Text.StringBuilder());
}
'''
edit('PriceOfBread.SimCore/Fiscal.cs', [], TAIL)
edit('PriceOfBread.SimCore/SelfPlay.cs', [
    ("                if (G186.On) G186.Quarter(sim, seed, q, quarters);\n",
     "                if (G186.On) G186.Quarter(sim, seed, q, quarters);\n                if (T203.On) T203.Quarter(sim, seed, q, quarters);\n"),
    ("        Apply(w, ChooseStance(w));\n",
     "        Apply(w, ChooseStance(w));\n        if (T203.On) T203.Post(w);\n"),
    ("        mul = 1.0 - w.Coef.OpulenceTaxRelief * (1.0 - mul);             // 適用率でブレンド（1=満額）\n",
     "        mul = 1.0 - w.Coef.OpulenceTaxRelief * (1.0 - mul);             // 適用率でブレンド（1=満額）\n        if (T203.On) T203.Mul(w, mul);\n"),
    ("        RelieveOpulence(w);\n",
     "        if (T203.On) T203.Pre(w, OpulenceLineOf(w));\n        RelieveOpulence(w);\n        if (T203.On) T203.Aft(w);\n"),
])
print('ok t203', root)
