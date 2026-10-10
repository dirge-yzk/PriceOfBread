"""[計測の写しだけ] (181) 線M 後半＝(176) の戦時の財政の計器の写し（読むだけ）＝施行の後の値 x1 は失効（(181)）と Move の後（SetMilitarySpending の直前）で読む・x0 は失効の前。環境変数 POB_W176_DIR の下に g<seed>.txt。未設定なら1命令も走らない。
  G 行＝国×四半期：
    ①軍事費の段の施行（Government.Step の軍事費の Move＝四半期の頭）＝交戦・総動員・施行の前の値 x0・AI の目標の生の和（陸＋海の目標）・
      門を通った目標（MilitaryTotalTarget）・施行の後の値 x1・Move の中身（純支持 net・抵抗 resist・速さ speed・政治の押しの予算・在庫で止まった・目標＝現状で動かない）
    ②月初の借入（Simulation の Fiscal.Borrow の前後・その四半期の月の和）＝払えない額があった月・払えない額の和・借りた額の和・
      門で止まった月（冷却中・助走・利払い÷歳入が限界以上）・利払い÷歳入（月の和）・切り下げの和（借入の後に残った払えない額）
  力学は読まない（加算と記録だけ）。使い方：python inj_war176.py <木の根>（inj_cas を当てた木に当てる・単独でも可）"""
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

/// <summary>[計測の写しだけ] (176) 戦時の財政の計器（読むだけ）。</summary>
public static class W176
{
    public static readonly string? Dir = System.Environment.GetEnvironmentVariable("POB_W176_DIR");
    public static readonly bool On = Dir is { Length: > 0 };
    static string F(double v) => double.IsNaN(v) ? "nan" : v.ToString("G6", System.Globalization.CultureInfo.InvariantCulture);
    // Move の中身（軍事費の Move の呼び出しの間だけ記録＝単一スレッド＝四半期の頭の Government.Step）
    [System.ThreadStatic] public static bool Arm;
    [System.ThreadStatic] public static double MNet, MResist, MSpeed, MBlocked, MCalled;
    const int NG = 24;
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, double[]> G = new();
    static double[] V(World w) => G.GetValue(w, _ => new double[NG]);
    public static void MoveRec(double net, double resist, double speed, bool blocked) { if (!Arm) return; MCalled = 1; MNet = net; MResist = resist; MSpeed = speed; MBlocked = blocked ? 1 : 0; }
    public static void StageRec(World w, double x0, double raw, double tgt, double x1)
    {
        var v = V(w);
        v[0] = 1; v[1] = w.AtWarNow ? 1 : 0; v[2] = w.Mobilized ? 1 : 0; v[3] = x0; v[4] = raw; v[5] = tgt; v[6] = x1;
        v[7] = MCalled; v[8] = MNet; v[9] = MResist; v[10] = MSpeed; v[11] = MBlocked; v[12] = w.PoliticalPushBudget;
        MCalled = 0; MNet = MResist = MSpeed = MBlocked = 0;
    }
    public static void BorrowPre(World w, double spare)
    {
        var v = V(w);
        v[13] += 1;
        if (spare < 0) { v[14] += 1; v[15] += -spare; }
        if (w.BorrowCooldownMonths > 0) v[17] += 1;
        if (w.WarmupFrozen) v[18] += 1;
        if (!(w.LastInterestPerMonth < w.Coef.MaxInterestShareOfRevenue * w.LastRevenuePerMonth)) v[19] += 1;
        v[20] += w.LastRevenuePerMonth > 0 ? w.LastInterestPerMonth / w.LastRevenuePerMonth : double.NaN;
    }
    public static void BorrowPost(World w, double borrowed, double devalued)
    {
        var v = V(w);
        v[16] += borrowed; v[21] += devalued;
    }
    public static void Quarter(MultiNationSim sim, ulong seed, int q, int quarters)
    {
        var sb = Buf(sim);
        for (int i = 0; i < sim.Nations.Count; i++)
        {
            var w = sim.Nations[i].World;
            if (!G.TryGetValue(w, out var v)) continue;
            sb.Append("G|").Append(q).Append('|').Append(w.Year).Append('|').Append(i).Append('|').Append(w.SelfPower is { } sp ? (sp.BaseName.Length > 0 ? sp.BaseName : sp.Name) : "?");
            for (int k = 0; k < 22; k++) sb.Append('|').Append(F(v[k]));
            sb.Append('\n');
            System.Array.Clear(v);
        }
        if (q != quarters - 1) return;
        using var W = new System.IO.StreamWriter(System.IO.Path.Combine(Dir!, $"g{seed}.txt"), false, new System.Text.UTF8Encoding(false), 1 << 16);
        W.Write("#G|q|yr|idx|name|stageRec|atWar|mob|x0|raw|tgt|x1|moveCalled|net|resist|speed|blocked|pushBudget|months|shortM|shortSum|borrowed|coolM|warmM|capM|intRevSum|devalued\n");
        W.Write(sb.ToString());
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, System.Text.StringBuilder> B = new();
    static System.Text.StringBuilder Buf(MultiNationSim s) => B.GetValue(s, _ => new System.Text.StringBuilder());
}
'''
_g = open(root + '/PriceOfBread.SimCore/Government.cs', encoding='utf-8-sig').read()
MOVE_LINE = [l.rstrip(chr(13)) for l in _g.split(chr(10)) if l.startswith('        double milTotal = Move(')]
assert len(MOVE_LINE) == 1, MOVE_LINE
MOVE_LINE = MOVE_LINE[0]
edit('PriceOfBread.SimCore/Government.cs', [
    (MOVE_LINE,
     "        double w176x0 = w.MilitarySpending; if (W176.On) W176.Arm = true;\n" + MOVE_LINE + "\n        if (W176.On) W176.Arm = false;"),
    ("        w.SetMilitarySpending(milTotal, navShareTarget);\n",
     "        if (W176.On) W176.StageRec(w, w176x0, milRawTotal, milTotalTarget, milTotal);\n        w.SetMilitarySpending(milTotal, navShareTarget);\n"),
    ("        double next = cur + speed * (target - cur);\n",
     "        double next = cur + speed * (target - cur);\n        if (W176.On) W176.MoveRec(net, resist, speed, false);\n"),
    ("            if (!PoliticalCapitalAffords(w, cost)) return cur;",
     "            if (!PoliticalCapitalAffords(w, cost)) { if (W176.On) W176.MoveRec(net, resist, speed, true); return cur; }"),
], TAIL)
edit('PriceOfBread.SimCore/Simulation.cs', [
    ("            double borrowed = Fiscal.Borrow(World, World.Coef, spare);\n",
     "            if (W176.On) W176.BorrowPre(World, spare);\n            double borrowed = Fiscal.Borrow(World, World.Coef, spare);\n"),
    ("            World.NoDevaluationMonths = devalued > 0 ? 0 : World.NoDevaluationMonths + 1;\n",
     "            World.NoDevaluationMonths = devalued > 0 ? 0 : World.NoDevaluationMonths + 1;\n            if (W176.On) W176.BorrowPost(World, borrowed, devalued);\n"),
])
edit('PriceOfBread.SimCore/SelfPlay.cs', [
    ("                if (CasDiag.On) CasDiag.Quarter(sim, seed, q, quarters);\n",
     "                if (CasDiag.On) CasDiag.Quarter(sim, seed, q, quarters);\n                if (W176.On) W176.Quarter(sim, seed, q, quarters);\n"),
])
print('ok war181', root)
