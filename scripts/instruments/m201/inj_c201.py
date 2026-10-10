"""[計測の写しだけ] (201) 線E 後半＝財政の正のフィードバックの鎖の計器（読むだけ）。環境変数 POB_C201_DIR の下に c<seed>.txt。未設定なら1命令も走らない。
  C 行＝国×四半期（四半期の終わり＝MultiNationSim.StepQuarter の後）：国庫・国庫の台帳の区分の四半期の増分
    （切り下げ Devalue・借入 Borrow・利払い Interest・その他の支出 OtherOut〔港の国家の上乗せほか＝月初の予算の外〕・税 Tax・歳入の合計）・
    累積の切り下げ÷年の名目GDP・切り下げの記憶 broken（Fiscal と同じ式＝累積÷月の名目GDP＞CapitalFlightDistrustThreshold）・
    金本位・兌換・国債・年の名目GDP・張り付きの計数 InsolvencyQuarters（四半期末の値）・不履行の回数（累計）・兌換停止の回数（累計）・
    国債の金利・平均クーポン・利払い÷歳入（LastInterest÷LastRevenue）・支払い率 r（四半期末の月）・交戦
力学は読まない（台帳と状態を読むだけ）。使い方：python inj_c201.py <木の根>（inj_g186 の後に当てる）"""
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

/// <summary>[計測の写しだけ] (201) 財政の鎖（読むだけ）。</summary>
public static class C201
{
    public static readonly string? Dir = System.Environment.GetEnvironmentVariable("POB_C201_DIR");
    public static readonly bool On = Dir is { Length: > 0 };
    static string F(double v) => double.IsNaN(v) ? "nan" : v.ToString("G6", System.Globalization.CultureInfo.InvariantCulture);
    sealed class P { public double[] L = new double[(int)TreasuryCat.Count]; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, P> T = new();
    public static void Quarter(MultiNationSim sim, ulong seed, int q, int quarters)
    {
        var sb = Buf(sim);
        for (int i = 0; i < sim.Nations.Count; i++)
        {
            var w = sim.Nations[i].World;
            var p = T.GetValue(w, _ => new P());
            double[] d = new double[(int)TreasuryCat.Count];
            for (int k = 0; k < d.Length; k++) { d[k] = w.TreasuryLedger[k] - p.L[k]; p.L[k] = w.TreasuryLedger[k]; }
            double ng = w.NominalGdp, ag = ng * 12;
            bool broken = ng > 1e-9 && w.CumulativeDevaluation / ng > w.Coef.CapitalFlightDistrustThreshold;
            double rev = 0; for (int k = 0; k <= (int)TreasuryCat.OtherIn; k++) if (k != (int)TreasuryCat.Borrow && k != (int)TreasuryCat.Devalue) rev += d[k];
            string nm = w.SelfPower is { } sp ? (sp.BaseName.Length > 0 ? sp.BaseName : sp.Name) : "?";
            sb.Append("C|").Append(q).Append('|').Append(w.Year).Append('|').Append(i).Append('|').Append(nm);
            foreach (var v in new[] { w.Treasury, d[(int)TreasuryCat.Devalue], d[(int)TreasuryCat.Borrow], d[(int)TreasuryCat.Interest], d[(int)TreasuryCat.OtherOut], d[(int)TreasuryCat.Tax], rev,
                ag > 1e-9 ? w.CumulativeDevaluation / ag : double.NaN, broken ? 1 : 0, w.OnGoldStandard ? 1 : 0, w.Convertibility, w.GovDebt, ag,
                w.InsolvencyQuarters, w.SovereignDefaultCount, w.GoldSuspensions, w.GovBondRate, w.GovAvgCoupon,
                w.LastRevenue > 1e-9 ? w.LastInterest / w.LastRevenue : double.NaN, w.FiscalFundingRatio, w.AtWarNow ? 1 : 0, d[(int)TreasuryCat.WarCost], d[(int)TreasuryCat.DebtRepay] })
                sb.Append('|').Append(F(v));
            sb.Append('\n');
        }
        if (q != quarters - 1) return;
        using var W = new System.IO.StreamWriter(System.IO.Path.Combine(Dir!, $"c{seed}.txt"), false, new System.Text.UTF8Encoding(false), 1 << 16);
        W.Write("#C|q|yr|idx|name|treasury|dDevalue|dBorrow|dInterest|dOtherOut|dTax|dRevenue|cumDevalAnn|broken|onGold|conv|debt|annNomGdp|insolv|defaults|suspensions|bondRate|coupon|intRev|r|atWar|dWarCost|dDebtRepay\n");
        W.Write(sb.ToString());
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, System.Text.StringBuilder> B = new();
    static System.Text.StringBuilder Buf(MultiNationSim s) => B.GetValue(s, _ => new System.Text.StringBuilder());
}
'''
edit('PriceOfBread.SimCore/Fiscal.cs', [], TAIL)
edit('PriceOfBread.SimCore/SelfPlay.cs', [
    ("                if (G186.On) G186.Quarter(sim, seed, q, quarters);\n",
     "                if (G186.On) G186.Quarter(sim, seed, q, quarters);\n                if (C201.On) C201.Quarter(sim, seed, q, quarters);\n"),
])
print('ok c201', root)
